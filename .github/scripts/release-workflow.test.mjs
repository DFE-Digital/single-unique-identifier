import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { describe, it } from 'node:test';

// Publication is main-only, so PR tests inspect the workflow wiring and exercise its job guard locally.
const workflow = readFileSync(new URL('../workflows/ci-cd-orchestrator.yml', import.meta.url), 'utf8');
assert.doesNotMatch(workflow, /notification_service|NotificationService|publish-container|azure\/login/);

const releaseJob = workflow.split('  cut-release:\n')[1];
assert.ok(releaseJob, 'release job exists');
assert.match(releaseJob, /uses: \.\/\.github\/workflows\/publish-immutable-release\.yml/);
assert.match(releaseJob, /needs:[\s\S]*prepare-release-assets/);
assert.match(releaseJob, /matrix: \$\{\{ fromJSON\(needs\.prepare-release-assets\.outputs\.matrix\) \}\}/);
assert.match(releaseJob, /release_tag: sui-\$\{\{ matrix\.component\.slug \}\}-\$\{\{ github\.sha \}\}/);
assert.match(releaseJob, /asset_subdirectory: \$\{\{ matrix\.component\.name \}\}/);
const publisher = readFileSync(new URL('../workflows/publish-immutable-release.yml', import.meta.url), 'utf8');
assert.match(publisher, /git\/ref\/tags/);
assert.match(publisher, /verify_inventory/);
assert.match(publisher, /gh release delete/);
assert.match(publisher, /gh release create/);
assert.match(publisher, /gh release edit/);
assert.match(publisher, /attestation verify/);
assert.match(publisher, /\$\{#release_files\[@\]\} == 2/);
assert.match(publisher, /\$\{#attested_assets\[@\]\} == 1/);
const prepareAssetsJob = workflow.split('  prepare-release-assets:\n')[1]?.split('\n  cut-release:\n')[0];
assert.ok(prepareAssetsJob, 'release assets are prepared in a separate job');
assert.match(prepareAssetsJob, /node \.github\/scripts\/release-manifest\.mjs/);
assert.match(prepareAssetsJob, /name: release-assets/);
assert.match(prepareAssetsJob, /matrix: \$\{\{ steps\.prepare\.outputs\.matrix \}\}/);
const condition = releaseJob.match(/    if: >-\n([\s\S]*?)\n    strategy:/)?.[1]
  ?.trim().replace(/^\$\{\{\s*/, '').replace(/\s*\}\}$/, '');
assert.ok(condition, 'release job condition exists');

// GitHub's boolean operators and comparisons in this condition have the same
// semantics as JavaScript for these string-valued outcomes.
const expression = condition.replace(/needs\.([a-z][a-z-]*)/g, 'needs["$1"]');
const releaseAllowed = new Function('github', 'needs', 'always', `return (${expression});`);

function scenario({ event = 'push', ref = 'refs/heads/main', selected = [],
  failed = null } = {}) {
  const outputs = {
    ci_helpers: 'true',
    ...Object.fromEntries([
      ['auth_emulator', 'AuthEmulator'], ['get_an_id', 'GetAnIdentifier'],
      ['ui_harness', 'UIHarness'],
    ].map(([key, name]) => [key, String(selected.includes(name))])),
  };
  const needs = {
    'path-filter': { result: 'success', outputs },
    'prepare-release-assets': { result: selected.length ? 'success' : 'skipped' },
    'build-auth-emulator': { result: selected.includes('AuthEmulator') ? 'success' : 'skipped' },
    'build-get-an-id': { result: selected.includes('GetAnIdentifier') ? 'success' : 'skipped' },
    'build-ui-harness': { result: selected.includes('UIHarness') ? 'success' : 'skipped' },
    'attest-release-zips': { result: selected.length ? 'success' : 'skipped' },
    'trufflehog-scan': { result: 'success' },
    'trivy-scan': { result: 'success' },
    'trivy-iac-scan': { result: 'success' },
    'release-script-tests': { result: 'success' },
  };
  if (failed) needs[failed].result = 'failure';
  return Boolean(releaseAllowed({ event_name: event, ref }, needs, () => true));
}

describe('release publication job guard', () => {
  it('skips a main push with no selected components', () => {
    assert.equal(scenario(), false);
    assert.equal(scenario({ selected: ['NotificationService'] }), false);
  });

  it('allows a successful partial and first release', () => {
    assert.equal(scenario({ selected: ['AuthEmulator'] }), true);
    assert.equal(scenario({ selected: ['GetAnIdentifier', 'UIHarness'] }), true);
  });

  it('blocks PRs, manual runs and failures of mandatory jobs', () => {
    for (const event of ['pull_request', 'workflow_dispatch']) {
      assert.equal(scenario({ event, selected: ['AuthEmulator'] }), false);
    }
    for (const [failed, selected] of [
      ['path-filter', ['AuthEmulator']],
      ['build-auth-emulator', ['AuthEmulator']],
      ['build-get-an-id', ['GetAnIdentifier']],
      ['build-ui-harness', ['UIHarness']],
      ['attest-release-zips', ['AuthEmulator']],
      ['trufflehog-scan', ['AuthEmulator']],
      ['trivy-scan', ['AuthEmulator']],
      ['trivy-iac-scan', ['AuthEmulator']],
      ['release-script-tests', ['AuthEmulator']],
    ]) {
      assert.equal(scenario({ selected, failed }), false, failed);
    }
  });
});
