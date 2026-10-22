import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { chmod, copyFile, mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import { readFileSync } from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { afterEach, it } from 'node:test';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const workflow = readFileSync(new URL('../workflows/publish-immutable-release.yml', import.meta.url), 'utf8');
// Exercise the shell from the workflow itself, so these tests catch changes to the real publisher.
const publicationRunBlock = workflow.split('      - name: Create and publish immutable release\n')[1]?.split('        run: |\n')[1];
assert.ok(publicationRunBlock, 'publication shell step exists');
const publicationScript = publicationRunBlock.split('\n').map((line) => line.startsWith('          ') ? line.slice(10) : line).join('\n');
const roots = [];
afterEach(async () => {
  await Promise.all(roots.splice(0).map((root) => rm(root, { recursive: true, force: true })));
});

async function createPublisherFixture() {
  const root = await mkdtemp(path.join(os.tmpdir(), 'sui-release-publish-'));
  roots.push(root);
  const bin = path.join(root, 'bin');
  await mkdir(bin);
  // Put the stateful gh fixture ahead of the real CLI on PATH.
  const fakeGh = path.join(bin, 'gh');
  await copyFile(new URL('./fixtures/fake-gh.cjs', import.meta.url), fakeGh);
  await chmod(fakeGh, 0o755);
  const dir = path.join(root, 'release-assets', 'Service');
  await mkdir(dir, { recursive: true });
  const files = ['release-manifest.json', 'Service.zip'];
  const sha = 'a'.repeat(40);
  const tag = `sui-service-${sha}`;
  const manifestPath = path.join(dir, files[0]);
  const manifest = {
    schema_version: 1,
    release: { tag, source_sha: sha, workflow_run_id: '123', workflow_run_attempt: '1',
      generated_at: '2026-10-22T12:00:00Z' },
    assets: [{ component: 'Service', filename: files[1] }], container: null,
  };
  await writeFile(manifestPath, `${JSON.stringify(manifest)}\n`);
  await writeFile(path.join(dir, files[1]), 'tested ZIP bytes');
  const stateFile = path.join(root, 'state.json');
  const state = { release: null, assetContents: {}, tagSha: null, immutableEnabled: true };
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
    MANIFEST_FILENAME: files[0],
    SOURCE_SHA: sha,
    RELEASE_TITLE: 'Service release',
    RELEASE_NOTES: 'Tested release',
  };
  const run = () => execFileAsync('bash', ['-e', '-c', publicationScript], { cwd: root, env });
  const readState = async () => JSON.parse(await readFile(stateFile, 'utf8'));
  const writeState = async (value) => writeFile(stateFile, JSON.stringify(value));
  return { env, run, readState, writeState, manifestPath, manifest, zipPath: path.join(dir, files[1]) };
}

it('publishes a first release only after checking the complete draft inventory', async () => {
  const { env, run, readState } = await createPublisherFixture();
  await run();
  const state = await readState();
  assert.equal(state.release.immutable, true);
  assert.equal(state.tagSha, env.SOURCE_SHA);
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /create\nlist\nimmutability\npublish\n/);
  assert.match(await readFile(env.GITHUB_OUTPUT, 'utf8'), /release_id=42/);
});

it('replaces an incomplete draft, then reuses a published release on a later attempt', async () => {
  const { env, run, readState, writeState, manifestPath, manifest } = await createPublisherFixture();
  const state = await readState();
  state.release = { id: 7, tag_name: env.RELEASE_TAG, target_commitish: env.SOURCE_SHA,
    draft: true, immutable: false, assets: [{ name: 'unexpected.zip', digest: 'sha256:bad' }] };
  await writeState(state);
  await run();
  assert.deepEqual((await readState()).release.assets.map(({ name }) => name).sort(),
    ['Service.zip', 'release-manifest.json']);
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /delete\ncreate\n/);

  manifest.release.workflow_run_attempt = '2';
  manifest.release.generated_at = '2026-10-22T13:00:00Z';
  await writeFile(manifestPath, `${JSON.stringify(manifest)}\n`);
  await writeFile(env.MOCK_LOG, '');
  await run();
  assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /download/);

  manifest.release.workflow_run_id = 'another-run';
  await writeFile(manifestPath, `${JSON.stringify(manifest)}\n`);
  await writeFile(env.MOCK_LOG, '');
  await assert.rejects(run());
  assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
});

it('refuses mismatched draft targets, published assets and tag refs without mutation', async () => {
  for (const variant of ['draft-target', 'published-assets', 'tag-ref']) {
    const { env, run, readState, writeState } = await createPublisherFixture();
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

it('refuses altered published manifest bytes or a different ZIP on retry', async () => {
  for (const variant of ['manifest', 'zip']) {
    const { env, run, readState, writeState, zipPath } = await createPublisherFixture();
    await run();
    if (variant === 'manifest') {
      const state = await readState();
      state.assetContents['release-manifest.json'] = Buffer.from('{}').toString('base64');
      await writeState(state);
    } else {
      await writeFile(zipPath, 'different ZIP bytes');
    }
    await writeFile(env.MOCK_LOG, '');
    await assert.rejects(run());
    assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
  }
});

it('leaves an incomplete draft unpublished when release immutability is disabled', async () => {
  const { run, readState, writeState } = await createPublisherFixture();
  const state = await readState();
  state.immutableEnabled = false;
  await writeState(state);
  await assert.rejects(run());
  assert.equal((await readState()).release.draft, true);
});
