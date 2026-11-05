import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { mkdtemp, mkdir, readFile, writeFile, rm } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { afterEach, it } from 'node:test';
import { promisify } from 'node:util';
import { createManifests } from './release-manifest.mjs';

const execFileAsync = promisify(execFile);
const roots = [];
afterEach(async () => {
  await Promise.all(roots.splice(0).map((root) => rm(root, { recursive: true, force: true })));
});

async function createManifestFixture(components) {
  const root = await mkdtemp(path.join(os.tmpdir(), 'sui-manifest-'));
  roots.push(root);
  const assetsDir = path.join(root, 'drop');
  await mkdir(assetsDir);
  const projects = {
    AuthEmulator: 'SUI.AuthEmulator',
    GetAnIdentifier: 'SUI.GetAnIdentifier.API',
    UIHarness: 'SUI.UIHarness.Web',
  };
  // Manifest selection needs filenames and non-empty files; ZIP contents are verified elsewhere.
  for (const component of components) {
    await writeFile(path.join(assetsDir, `${component}-${projects[component]}-build.zip`), 'ZIP');
  }
  return {
    components, assetsDir,
    sourceSha: 'a'.repeat(40), runId: '123', runAttempt: '2',
    generatedAt: '2026-10-22T12:00:00Z', releaseDate: '2026.10.22', buildNumber: '123',
  };
}

it('creates separate component releases from one source commit', async () => {
  const inputs = await createManifestFixture(['AuthEmulator', 'UIHarness']);
  const releases = await createManifests(inputs);
  assert.deepEqual(releases.map(({ manifest }) => manifest.release.tag), [
    'auth-emulator-2026.10.22-build.123', 'ui-harness-2026.10.22-build.123',
  ]);
  for (const { component, zipPath, manifest } of releases) {
    assert.deepEqual(manifest.assets, [{ component, filename: path.basename(zipPath) }]);
    assert.equal(manifest.release.release_date, inputs.releaseDate);
    assert.equal(manifest.release.build_number, inputs.buildNumber);
    assert.equal(manifest.container, null);
  }
});

it('keeps tags stable across reruns and changes them for a new workflow run number', async () => {
  const inputs = await createManifestFixture(['AuthEmulator']);
  const original = await createManifests(inputs);
  const retry = await createManifests({ ...inputs, runAttempt: '3' });
  const nextBuild = await createManifests({ ...inputs, buildNumber: '124' });
  assert.equal(retry[0].manifest.release.tag, original[0].manifest.release.tag);
  assert.notEqual(nextBuild[0].manifest.release.tag, original[0].manifest.release.tag);
  assert.equal(nextBuild[0].manifest.release.tag, 'auth-emulator-2026.10.22-build.124');
});

it('writes component directories and a publication matrix from workflow inputs', async () => {
  const inputs = await createManifestFixture(['GetAnIdentifier', 'UIHarness']);
  const root = path.dirname(inputs.assetsDir);
  const output = path.join(root, 'github-output');
  await execFileAsync('node', [new URL('./release-manifest.mjs', import.meta.url).pathname], {
    cwd: root,
    env: { ...process.env, AUTH: 'false', GET_AN_ID: 'true', UI: 'true',
      GITHUB_SHA: inputs.sourceSha, GITHUB_RUN_ID: inputs.runId,
      GITHUB_RUN_ATTEMPT: inputs.runAttempt, RELEASE_DATE: inputs.releaseDate, GITHUB_RUN_NUMBER: inputs.buildNumber, GITHUB_OUTPUT: output },
  });
  const manifest = JSON.parse(await readFile(path.join(root, 'release-assets', 'GetAnIdentifier', 'release-manifest.json'), 'utf8'));
  assert.deepEqual(manifest.assets, [
    { component: 'GetAnIdentifier', filename: 'GetAnIdentifier-SUI.GetAnIdentifier.API-build.zip' },
  ]);
  assert.equal(await readFile(path.join(root, 'release-assets', 'GetAnIdentifier', manifest.assets[0].filename), 'utf8'), 'ZIP');
  assert.deepEqual(JSON.parse((await readFile(output, 'utf8')).trim().slice('matrix='.length)), {
    component: [
      { name: 'GetAnIdentifier', slug: 'get-an-identifier', tag: 'get-an-identifier-2026.10.22-build.123' },
      { name: 'UIHarness', slug: 'ui-harness', tag: 'ui-harness-2026.10.22-build.123' },
    ],
  });
});

it('rejects no component, deferred components, extra ZIPs, invalid release identity and empty ZIPs', async () => {
  const empty = await createManifestFixture([]);
  await assert.rejects(createManifests(empty), /Select at least one/);

  const extra = await createManifestFixture(['AuthEmulator']);
  await writeFile(path.join(extra.assetsDir, 'unexpected.zip'), 'ZIP');
  await assert.rejects(createManifests(extra), /inventory/);

  const zero = await createManifestFixture(['AuthEmulator']);
  await writeFile(path.join(zero.assetsDir, 'AuthEmulator-SUI.AuthEmulator-build.zip'), '');
  await assert.rejects(createManifests(zero), /Empty or invalid/);

  const invalid = await createManifestFixture(['AuthEmulator']);
  await assert.rejects(createManifests({ ...invalid, releaseDate: 'bad' }), /release date and build number/);
  await assert.rejects(createManifests({ ...invalid, buildNumber: '0' }), /release date and build number/);

  const notification = await createManifestFixture(['AuthEmulator']);
  await assert.rejects(createManifests({ ...notification, components: ['NotificationService'] }), /known component/);
});
