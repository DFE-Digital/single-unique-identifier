import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { readFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { afterEach, it } from 'node:test';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const workflow = readFileSync(new URL('../workflows/publish-immutable-release.yml', import.meta.url), 'utf8');
const block = workflow.split('      - name: Create and publish immutable release\n')[1]?.split('        run: |\n')[1];
assert.ok(block, 'publication shell step exists');
const script = block.split('\n').map((line) => line.startsWith('          ') ? line.slice(10) : line).join('\n');
const roots = [];
afterEach(async () => {
  await Promise.all(roots.splice(0).map((root) => rm(root, { recursive: true, force: true })));
});

const ghMock = `#!/usr/bin/env node
const fs = require('node:fs');
const crypto = require('node:crypto');
const args = process.argv.slice(2);
const state = JSON.parse(fs.readFileSync(process.env.MOCK_STATE, 'utf8'));
const log = (value) => fs.appendFileSync(process.env.MOCK_LOG, value + '\\n');
const save = () => fs.writeFileSync(process.env.MOCK_STATE, JSON.stringify(state));
if (args[0] === 'api' && args.includes('--paginate')) {
  log('list');
  console.log(JSON.stringify(state.release ? [state.release] : []));
} else if (args[0] === 'api' && args[1].includes('/git/ref/tags/')) {
  log('tag');
  if (!state.tagSha) { process.stderr.write('(HTTP 404)\\n'); process.exit(1); }
  console.log('commit\\t' + state.tagSha);
} else if (args[0] === 'api' && args[1].includes('/immutable-releases')) {
  log('immutability');
  console.log(state.immutableEnabled ? 'true' : 'false');
} else if (args[0] === 'release' && args[1] === 'delete') {
  log('delete');
  if (!state.release?.draft) process.exit(1);
  state.release = null;
  save();
} else if (args[0] === 'release' && args[1] === 'create') {
  log('create');
  if (state.release) process.exit(1);
  const files = args.slice(3, args.indexOf('--repo'));
  const sha = args[args.indexOf('--target') + 1];
  state.release = {
    id: 42, html_url: 'https://github.com/acme/repo/releases/tag/' + args[2],
    tag_name: args[2], target_commitish: sha, draft: true, immutable: false,
    assets: files.map((file) => ({ name: require('node:path').basename(file),
      digest: 'sha256:' + crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex') })),
  };
  save();
} else if (args[0] === 'release' && args[1] === 'edit') {
  log('publish');
  if (!state.release?.draft || !state.immutableEnabled) process.exit(1);
  state.release.draft = false;
  state.release.immutable = true;
  state.tagSha = state.release.target_commitish;
  save();
} else {
  throw new Error('Unexpected gh call: ' + args.join(' '));
}
`;

async function fixture() {
  const root = await mkdtemp(path.join(os.tmpdir(), 'sui-release-publish-'));
  roots.push(root);
  const bin = path.join(root, 'bin');
  await mkdir(bin);
  await writeFile(path.join(bin, 'gh'), ghMock, { mode: 0o755 });
  const dir = path.join(root, 'release-assets', 'Service');
  await mkdir(dir, { recursive: true });
  const files = ['release-manifest.json', 'Service.zip'];
  await writeFile(path.join(dir, files[0]), '{"release":"test"}\n');
  await writeFile(path.join(dir, files[1]), 'tested ZIP bytes');
  const sha = 'a'.repeat(40);
  const tag = `sui-service-${sha}`;
  const stateFile = path.join(root, 'state.json');
  const state = { release: null, tagSha: null, immutableEnabled: true };
  await writeFile(stateFile, JSON.stringify(state));
  const env = {
    ...process.env,
    PATH: `${bin}:${process.env.PATH}`,
    MOCK_STATE: stateFile,
    MOCK_LOG: path.join(root, 'calls'),
    GITHUB_OUTPUT: path.join(root, 'github-output'),
    GITHUB_REPOSITORY: 'acme/repo',
    RELEASE_SETTINGS_READ_TOKEN: 'mock-settings-token',
    RELEASE_TAG: tag,
    ASSET_SUBDIRECTORY: 'Service',
    SOURCE_SHA: sha,
    RELEASE_TITLE: 'Service release',
    RELEASE_NOTES: 'Tested release',
  };
  const run = () => execFileAsync('bash', ['-e', '-c', script], { cwd: root, env });
  const readState = async () => JSON.parse(await readFile(stateFile, 'utf8'));
  const writeState = async (value) => writeFile(stateFile, JSON.stringify(value));
  const expectedAssets = await Promise.all(files.map(async (name) => ({
    name, digest: `sha256:${createHash('sha256').update(await readFile(path.join(dir, name))).digest('hex')}`,
  })));
  return { env, run, readState, writeState, expectedAssets };
}

it('publishes a first release only after checking the complete draft inventory', async () => {
  const { env, run, readState } = await fixture();
  await run();
  const state = await readState();
  assert.equal(state.release.immutable, true);
  assert.equal(state.tagSha, env.SOURCE_SHA);
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /create\nlist\nimmutability\npublish\n/);
  assert.match(await readFile(env.GITHUB_OUTPUT, 'utf8'), /release_id=42/);
});

it('replaces an incomplete draft, then treats a matching published release as a safe retry', async () => {
  const { env, run, readState, writeState } = await fixture();
  const state = await readState();
  state.release = { id: 7, tag_name: env.RELEASE_TAG, target_commitish: env.SOURCE_SHA,
    draft: true, immutable: false, assets: [{ name: 'unexpected.zip', digest: 'sha256:bad' }] };
  await writeState(state);
  await run();
  assert.deepEqual((await readState()).release.assets.map(({ name }) => name).sort(),
    ['Service.zip', 'release-manifest.json']);
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /delete\ncreate\n/);

  await writeFile(env.MOCK_LOG, '');
  await run();
  assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
});

it('refuses mismatched draft targets, published assets and tag refs without mutation', async () => {
  for (const variant of ['draft-target', 'published-assets', 'tag-ref']) {
    const { env, run, readState, writeState } = await fixture();
    const state = await readState();
    state.release = { id: 7, tag_name: env.RELEASE_TAG, target_commitish: env.SOURCE_SHA,
      draft: variant !== 'published-assets', immutable: variant === 'published-assets', assets: [] };
    if (variant === 'draft-target') state.release.target_commitish = 'b'.repeat(40);
    if (variant === 'tag-ref') state.tagSha = 'b'.repeat(40);
    await writeState(state);
    await assert.rejects(run(), variant);
    assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
  }
});

it('leaves an incomplete draft unpublished when release immutability is disabled', async () => {
  const { run, readState, writeState } = await fixture();
  const state = await readState();
  state.immutableEnabled = false;
  await writeState(state);
  await assert.rejects(run());
  assert.equal((await readState()).release.draft, true);
});
