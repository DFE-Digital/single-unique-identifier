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

async function fixture(components) {
  const root = await mkdtemp(path.join(os.tmpdir(), 'sui-manifest-'));
  roots.push(root);
  const assetsDir = path.join(root, 'drop');
  await mkdir(assetsDir);
  const projects = {
    AuthEmulator: 'SUI.AuthEmulator',
    GetAnIdentifier: 'SUI.GetAnIdentifier.API',
    NotificationService: 'SUI.NotificationService',
    UIHarness: 'SUI.UIHarness.Web',
  };
  for (const component of components) {
    await writeFile(path.join(assetsDir, `${component}-${projects[component]}-build.zip`), 'ZIP');
  }
  const containerFile = path.join(root, 'container.json');
  if (components.includes('NotificationService')) {
    await writeFile(containerFile, JSON.stringify({
      repository: 'example.azurecr.io/notification-service',
      digest: `sha256:${'b'.repeat(64)}`,
      ignored: 'not part of the manifest',
    }));
  }
  return {
    components, assetsDir,
    containerFile: components.includes('NotificationService') ? containerFile : undefined,
    sourceSha: 'a'.repeat(40), runId: '123', runAttempt: '2',
    generatedAt: '2026-10-22T12:00:00Z',
  };
}

it('creates separate component releases from one source commit', async () => {
  const inputs = await fixture(['AuthEmulator', 'UIHarness']);
  const releases = await createManifests(inputs);
  assert.deepEqual(releases.map(({ manifest }) => manifest.release.tag), [
    `sui-auth-emulator-${inputs.sourceSha}`, `sui-ui-harness-${inputs.sourceSha}`,
  ]);
  for (const { component, zipPath, manifest } of releases) {
    assert.deepEqual(manifest.assets, [{ component, filename: path.basename(zipPath) }]);
    assert.equal(manifest.container, null);
  }
});

it('records only the published image identity when Notification Service is selected', async () => {
  const [{ manifest }] = await createManifests(await fixture(['NotificationService']));
  assert.deepEqual(manifest.container, {
    repository: 'example.azurecr.io/notification-service', digest: `sha256:${'b'.repeat(64)}`,
  });
});

it('writes component directories and a publication matrix from workflow inputs', async () => {
  const inputs = await fixture(['GetAnIdentifier', 'UIHarness']);
  const root = path.dirname(inputs.assetsDir);
  const output = path.join(root, 'github-output');
  await execFileAsync('node', [new URL('./release-manifest.mjs', import.meta.url).pathname], {
    cwd: root,
    env: { ...process.env, AUTH: 'false', GET_AN_ID: 'true', NOTIFICATION: 'false', UI: 'true',
      GITHUB_SHA: inputs.sourceSha, GITHUB_RUN_ID: inputs.runId,
      GITHUB_RUN_ATTEMPT: inputs.runAttempt, GITHUB_OUTPUT: output },
  });
  const manifest = JSON.parse(await readFile(path.join(root, 'release-assets', 'GetAnIdentifier', 'release-manifest.json'), 'utf8'));
  assert.deepEqual(manifest.assets, [
    { component: 'GetAnIdentifier', filename: 'GetAnIdentifier-SUI.GetAnIdentifier.API-build.zip' },
  ]);
  assert.equal(await readFile(path.join(root, 'release-assets', 'GetAnIdentifier', manifest.assets[0].filename), 'utf8'), 'ZIP');
  assert.deepEqual(JSON.parse((await readFile(output, 'utf8')).trim().slice('matrix='.length)), {
    component: [
      { name: 'GetAnIdentifier', slug: 'get-an-identifier' },
      { name: 'UIHarness', slug: 'ui-harness' },
    ],
  });
});

it('rejects no component, extra ZIPs, empty ZIPs and missing image metadata', async () => {
  const empty = await fixture([]);
  await assert.rejects(createManifests(empty), /Select at least one/);

  const extra = await fixture(['AuthEmulator']);
  await writeFile(path.join(extra.assetsDir, 'unexpected.zip'), 'ZIP');
  await assert.rejects(createManifests(extra), /inventory/);

  const zero = await fixture(['AuthEmulator']);
  await writeFile(path.join(zero.assetsDir, 'AuthEmulator-SUI.AuthEmulator-build.zip'), '');
  await assert.rejects(createManifests(zero), /Empty or invalid/);

  const image = await fixture(['NotificationService']);
  image.containerFile = undefined;
  await assert.rejects(createManifests(image), /metadata selection/);
});
