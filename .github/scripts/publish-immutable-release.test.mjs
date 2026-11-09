import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { chmod, copyFile, mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { it } from 'node:test';
import { temporaryDirectories, readWorkflow, workflowStep } from './test-helpers.mjs';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const workflow = readWorkflow(new URL('../workflows/publish-immutable-release.yml', import.meta.url));
const publicationScript = workflowStep(workflow, 'publish', 'Create and publish immutable release');
const createTemporaryDirectory = temporaryDirectories();

async function createPublisherFixture() {
  const root = await createTemporaryDirectory('sui-release-publish-');
  const bin = path.join(root, 'bin');
  await mkdir(bin);
  // Put the stateful gh fixture ahead of the real CLI on PATH.
  const fakeGh = path.join(bin, 'gh');
  await copyFile(new URL('./fixtures/fake-gh.cjs', import.meta.url), fakeGh);
  await chmod(fakeGh, 0o755);
  const dir = path.join(root, 'release-assets', 'Service');
  await mkdir(dir, { recursive: true });
  const files = ['release-manifest.json', 'Service.zip', 'Service.zip.cdx.json'];
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
  await writeFile(path.join(dir, files[2]), '{"bomFormat":"CycloneDX","components":[]}');
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
    GITHUB_RUN_ID: '123',
    GITHUB_RUN_ATTEMPT: '1',
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
  assert.match(await readFile(env.MOCK_LOG, 'utf8'), /create\nlist\npublish\n/);
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
    ['Service.zip', 'Service.zip.cdx.json', 'release-manifest.json']);
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
  for (const variant of ['manifest', 'zip', 'sbom']) {
    const { env, run, readState, writeState, zipPath } = await createPublisherFixture();
    await run();
    if (variant === 'manifest') {
      const state = await readState();
      state.assetContents['release-manifest.json'] = Buffer.from('{}').toString('base64');
      await writeState(state);
    } else if (variant === 'zip') {
      await writeFile(zipPath, 'different ZIP bytes');
    } else {
      await writeFile(`${zipPath}.cdx.json`, 'different SBOM bytes');
    }
    await writeFile(env.MOCK_LOG, '');
    await assert.rejects(run());
    assert.doesNotMatch(await readFile(env.MOCK_LOG, 'utf8'), /delete|create|publish/);
  }
});

it('fails after publication if repository release immutability is disabled', async () => {
  const { env, run, readState, writeState } = await createPublisherFixture();
  const state = await readState();
  state.immutableEnabled = false;
  await writeState(state);
  await assert.rejects(run(), /Published release is not immutable/);
  const published = (await readState()).release;
  assert.equal(published.draft, false);
  assert.equal(published.immutable, false);
  const output = await readFile(env.GITHUB_OUTPUT, 'utf8');
  assert.doesNotMatch(output, /release_id=/);
  assert.match(output, /failed_stage=verify published release/);
});

it('reports the failed publication stage when an existing tag targets another commit', async () => {
  const { env, run, readState, writeState } = await createPublisherFixture();
  const state = await readState();
  state.tagSha = 'b'.repeat(40);
  await writeState(state);
  await assert.rejects(run(), /check existing tag/);
  assert.match(await readFile(env.GITHUB_OUTPUT, 'utf8'), /failed_stage=check existing tag/);
});

it('rejects a verified SBOM attestation whose predicate differs from the release asset', async () => {
  const root = await createTemporaryDirectory('sui-sbom-verify-');
  const bin = path.join(root, 'bin');
  const dir = path.join(root, 'release-assets', 'Service');
  await mkdir(bin, { recursive: true });
  await mkdir(dir, { recursive: true });
  const sbom = { bomFormat: 'CycloneDX', components: [{ name: 'example' }] };
  await writeFile(path.join(dir, 'Service.zip'), 'ZIP');
  await writeFile(path.join(dir, 'Service.zip.cdx.json'), JSON.stringify(sbom));
  const sha = 'a'.repeat(40);
  await writeFile(path.join(dir, 'release-manifest.json'), JSON.stringify({ release: { tag: 'tag', source_sha: sha }, assets: [{ component: 'Service', filename: 'Service.zip' }] }));
  const gh = path.join(bin, 'gh');
  await writeFile(gh, '#!/bin/sh\ncase "$*" in *--predicate-type*) printf "%s\\n" "$MOCK_ATTESTATION";; esac\n');
  await chmod(gh, 0o755);
  const script = workflowStep(workflow, 'publish', 'Verify release asset inventory and build attestations');
  const env = { ...process.env, PATH: `${bin}:${process.env.PATH}`, RUNNER_TEMP: root, GITHUB_OUTPUT: path.join(root, 'output'),
    GITHUB_REPOSITORY: 'acme/repo', GITHUB_SHA: sha, SOURCE_SHA: sha, SOURCE_REF: 'refs/heads/main',
    ASSET_GLOB: '*.zip', ASSET_SUBDIRECTORY: 'Service', MANIFEST_FILENAME: 'release-manifest.json',
    SIGNER_WORKFLOW: 'acme/repo/.github/workflows/attest.yml', RELEASE_TAG: 'tag' };
  const run = (predicate) => execFileAsync('bash', ['-e', '-c', script], { cwd: root,
    env: { ...env, MOCK_ATTESTATION: JSON.stringify([{ verificationResult: { statement: { predicate } } }]) } });
  await run(sbom);
  await assert.rejects(run({ ...sbom, components: [] }));
});
