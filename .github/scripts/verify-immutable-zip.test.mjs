import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { it } from 'node:test';
import { promisify } from 'node:util';
import { readWorkflow, temporaryDirectories, workflowStep } from './test-helpers.mjs';

const run = promisify(execFile);
const temporaryDirectory = temporaryDirectories();
const workflow = readWorkflow(new URL('../workflows/verify-immutable-zip.yml', import.meta.url));
const verify = workflowStep(workflow, 'verify', 'Verify release manifest and ZIP');
const sourceSha = 'a'.repeat(40);
const tag = 'example-2026.11.19-build.1';
const zipName = 'Example-build.zip';
const zip = 'verified ZIP bytes';
const digest = (bytes) => 'sha256:' + createHash('sha256').update(bytes).digest('hex');

async function fixture({ draft = false, component = 'Example', filename = zipName, zipBytes = zip,
  expectedSource = sourceSha, verifyError = false, provenanceError = false,
  expectedComponent = 'Example',
  subjects = [{ name: zipName, digest: { sha256: digest(zip).slice(7) } }] } = {}) {
  const directory = await temporaryDirectory('verify-immutable-zip-');
  const output = path.join(directory, 'output');
  const manifest = JSON.stringify({
    schema_version: 1, container: null, release: { tag, source_sha: sourceSha },
    assets: [{ component, filename }],
  });
  await writeFile(path.join(directory, zipName), zipBytes);
  await writeFile(path.join(directory, 'release-manifest.json'), manifest);
  await writeFile(path.join(directory, 'release.json'), JSON.stringify({
    id: 123, draft, immutable: true, published_at: '2026-11-19T19:00:00Z',
    tag_name: tag, target_commitish: sourceSha,
    assets: [
      { name: zipName, state: 'uploaded', digest: digest(zip) },
      { name: 'release-manifest.json', state: 'uploaded', digest: digest(manifest) },
    ],
  }));
  await writeFile(path.join(directory, 'gh'), `#!/bin/bash
set -euo pipefail
case "$1 $2" in
  "api repos/acme/repo/releases/123")
    cat "$FIXTURE/release.json"
    ;;
  "release download")
    [[ "$3" == "$TAG" && "$4 $5 $6 $8" == "--repo acme/repo --pattern --dir" ]]
    cp -- "$FIXTURE/$7" "$9/$7"
    ;;
  "release verify-asset")
    [[ "$3" == "$TAG" && -s "$4" && "$5 $6" == "--repo acme/repo" ]]
    [[ "$(basename "$4")" == release-manifest.json || "$VERIFY_ERROR" == false ]]
    ;;
  "attestation verify")
    [[ -s "$3" && "$4 $5" == "--repo acme/repo" ]]
    [[ "$6 $7" == "--signer-workflow acme/repo/.github/workflows/attest-release-zips.yml" ]]
    shift 7
    [[ "$1 $2 $3 $4" == "--source-digest $SOURCE_SHA --source-ref refs/heads/main" ]]
    [[ "$PROVENANCE_ERROR" == false ]]
    [[ "$5 $6" == "--format json" ]]
    printf '%s\n' "$PROVENANCE"
    ;;
  *) exit 99 ;;
esac
`, { mode: 0o700 });
  return {
    output,
    execute: () => run('bash', ['-c', verify], { env: {
      ...process.env, PATH: directory + ':' + process.env.PATH, RUNNER_TEMP: directory,
      GITHUB_OUTPUT: output, GITHUB_REPOSITORY: 'acme/repo', RELEASE_ID: '123',
      COMPONENT: expectedComponent, EXPECTED_SOURCE_SHA: expectedSource, FIXTURE: directory,
      TAG: tag, SOURCE_SHA: sourceSha, VERIFY_ERROR: String(verifyError), PROVENANCE_ERROR: String(provenanceError),
      PROVENANCE: JSON.stringify([{ verificationResult: { statement: { subject: subjects } } }]),
    } }),
  };
}

it('passes the exact verified ZIP and release metadata to the upload step', async () => {
  for (const expectedSource of [sourceSha, '']) {
    const { execute, output } = await fixture({ expectedSource });
    await execute();
    const values = Object.fromEntries((await readFile(output, 'utf8')).trimEnd().split('\n')
      .map((line) => line.split('=')));
    assert.equal(path.basename(values.package), 'release.zip');
    assert.equal(await readFile(values.package, 'utf8'), zip);
    assert.equal(values.digest, digest(zip));
    assert.equal(values.source_sha, sourceSha);
  }
});

it('fails before upload for invalid releases or failed verification', async () => {
  for (const inputs of [
    { draft: true }, { expectedSource: 'b'.repeat(40) }, { component: 'Other' },
    { filename: '../Example-build.zip' }, { zipBytes: 'mismatched ZIP' },
    { verifyError: true }, { provenanceError: true },
    { component: 'Other', expectedComponent: 'Other' },
    { subjects: [] },
    { subjects: [
      { name: 'Other-build.zip', digest: { sha256: digest(zip).slice(7) } },
      { name: zipName, digest: { sha256: digest('another app').slice(7) } },
    ] },
  ]) {
    const { execute, output } = await fixture(inputs);
    await assert.rejects(execute(), ({ code }) => code === 1 || code === 4);
    await assert.rejects(readFile(output), { code: 'ENOENT' });
  }
});
