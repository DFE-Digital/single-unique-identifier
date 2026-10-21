import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { promisify } from 'node:util';
import { mkdtemp, mkdir, readFile, rm, writeFile } from 'node:fs/promises';
import os from 'node:os';
import path from 'node:path';
import { afterEach, beforeEach, describe, it } from 'node:test';
import { createManifest, PROJECT_BY_COMPONENT } from './release-manifest.mjs';

const execFileAsync = promisify(execFile);
const SHA = 'a'.repeat(40);
const IMAGE_DIGEST = `sha256:${'b'.repeat(64)}`;

describe('release manifest', () => {
  let root;
  let assets;
  let selectedJson;
  let gatesJson;
  let containerJson;
  let output;

  beforeEach(async () => {
    root = await mkdtemp(path.join(os.tmpdir(), 'sui-release-manifest-'));
    assets = path.join(root, 'assets');
    await mkdir(assets);
    selectedJson = path.join(root, 'selected.json');
    gatesJson = path.join(root, 'gates.json');
    containerJson = path.join(root, 'container.json');
    output = path.join(root, 'manifest.json');
  });

  afterEach(async () => rm(root, { recursive: true, force: true }));

  async function generate(components, { includeContainer = true } = {}) {
    await writeFile(selectedJson, JSON.stringify(components));
    await writeFile(gatesJson, JSON.stringify({
      secret_scan: 'success', iac_scan: 'success', repository_scan: 'success',
      components: Object.fromEntries(components.map((component) => [component, {
        tests: component === 'UIHarness' ? 'skipped' : 'success',
        sonar_quality_gate: component === 'UIHarness' ? 'skipped' : 'success',
      }])),
    }));
    for (const component of components) {
      await createDeploymentZip(component);
    }
    if (components.includes('NotificationService') && includeContainer) {
      await writeFile(containerJson, JSON.stringify({
        repository: 'example.azurecr.io/notification-service', digest: IMAGE_DIGEST,
        base_image: 'mcr.microsoft.com/dotnet/runtime:10.0', base_digest: IMAGE_DIGEST,
        scan: 'success', registry_publication: 'success',
      }));
    }
    return {
      assetsDir: assets, selectedJson, gatesJson, sourceSha: SHA, tag: `sui-${SHA}`,
      runId: '123', runAttempt: '2',
      containerJson: components.includes('NotificationService') && includeContainer ? containerJson : undefined,
      output,
    };
  }

  async function createDeploymentZip(component) {
    const filename = `${component}-${PROJECT_BY_COMPONENT[component]}-build.zip`;
    const payload = `${component} deployment payload`;
    await writeFile(path.join(assets, 'deployment-payload.txt'), payload);
    await execFileAsync('zip', ['-q', filename, 'deployment-payload.txt'], { cwd: assets });
    return path.join(assets, filename);
  }

  it('supports single and partial multi-component manifests', async () => {
    await createManifest(await generate(['AuthEmulator']));
    let manifest = JSON.parse(await readFile(output, 'utf8'));
    assert.deepEqual(manifest.assets.map((asset) => asset.component), ['AuthEmulator']);
    assert.equal(manifest.container, null);
    const zipBytes = await readFile(path.join(assets, manifest.assets[0].filename));
    assert.equal(manifest.assets[0].size_bytes, zipBytes.length);
    assert.equal(
      manifest.assets[0].sha256,
      createHash('sha256').update(zipBytes).digest('hex'),
    );

    await rm(assets, { recursive: true, force: true });
    await mkdir(assets);
    await createManifest(await generate(['UIHarness', 'NotificationService']));
    manifest = JSON.parse(await readFile(output, 'utf8'));
    assert.deepEqual(manifest.assets.map((asset) => asset.component), ['NotificationService', 'UIHarness']);
    assert.equal(manifest.quality_gates.components.UIHarness.tests, 'skipped');
    assert.equal(manifest.container.digest, IMAGE_DIGEST);
  });

  it('rejects missing or extra ZIPs', async () => {
    const args = await generate(['AuthEmulator']);
    await rm(path.join(assets, 'AuthEmulator-SUI.AuthEmulator-build.zip'));
    await assert.rejects(createManifest(args), /ZIP inventory/);
    await createDeploymentZip('AuthEmulator');
    await writeFile(path.join(assets, 'unexpected.zip'), 'extra');
    await assert.rejects(createManifest(args), /ZIP inventory/);
  });

  it('rejects failed required gates and unexpected skips', async () => {
    const args = await generate(['AuthEmulator', 'UIHarness']);
    const gates = JSON.parse(await readFile(gatesJson, 'utf8'));
    gates.repository_scan = 'skipped';
    await writeFile(gatesJson, JSON.stringify(gates));
    await assert.rejects(createManifest(args), /repository_scan/);
    gates.repository_scan = 'success';
    gates.components.AuthEmulator.tests = 'skipped';
    await writeFile(gatesJson, JSON.stringify(gates));
    await assert.rejects(createManifest(args), /AuthEmulator/);
  });

  it('requires a scanned and published immutable Notification Service digest', async () => {
    const missing = await generate(['NotificationService'], { includeContainer: false });
    await assert.rejects(createManifest(missing), /Notification Service image/);
    const args = await generate(['NotificationService']);
    const metadata = JSON.parse(await readFile(containerJson, 'utf8'));
    metadata.scan = 'skipped';
    await writeFile(containerJson, JSON.stringify(metadata));
    await assert.rejects(createManifest(args), /Notification Service image/);
  });

  it('requires the tag to identify the source commit', async () => {
    const args = await generate(['AuthEmulator']);
    args.tag = 'sui-other';
    await assert.rejects(createManifest(args), /Release tag/);
  });
});
