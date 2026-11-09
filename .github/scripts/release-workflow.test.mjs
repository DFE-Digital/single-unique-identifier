import assert from 'node:assert/strict';
import { readWorkflow, workflowStep } from './test-helpers.mjs';
import { describe, it } from 'node:test';

// Check structure independently of YAML formatting.
const workflow = readWorkflow(new URL('../workflows/ci-cd-orchestrator.yml', import.meta.url));
assert.doesNotMatch(JSON.stringify(workflow), /notification_service|NotificationService|publish-container|azure\/login/);
const releaseJob = workflow.jobs['cut-release'];
assert.equal(releaseJob.uses, './.github/workflows/publish-immutable-release.yml');
assert.ok(releaseJob.needs.includes('prepare-release-assets'));
assert.match(releaseJob.strategy.matrix, /fromJSON\(needs\.prepare-release-assets\.outputs\.matrix\)/);
assert.match(releaseJob.with.release_tag, /matrix\.component\.tag/);
assert.match(releaseJob.with.release_title, /matrix\.component\.name/);
assert.match(releaseJob.with.asset_subdirectory, /matrix\.component\.name/);
const publisher = readWorkflow(new URL('../workflows/publish-immutable-release.yml', import.meta.url));
const verify = workflowStep(publisher, 'publish', 'Verify release asset inventory and build attestations');
const publish = workflowStep(publisher, 'publish', 'Create and publish immutable release');
for (const pattern of [/git\/ref\/tags/, /verify_inventory/, /gh release delete/, /gh release create/, /gh release edit/]) assert.match(publish, pattern);
assert.match(verify, /attestation verify/);
assert.match(verify, /\$\{#release_files\[@\]\} == 2/);
assert.match(verify, /\$\{#attested_assets\[@\]\} == 1/);
const prepare = workflow.jobs['prepare-release-assets'];
const prepareStep = prepare.steps.find((step) => step.run === 'node .github/scripts/release-manifest.mjs');
assert.ok(prepareStep);
assert.match(prepareStep.env.RELEASE_DATE, /steps\.release-date\.outputs\.date/);
assert.ok(prepare.steps.some((step) => step.with?.name === 'release-assets'));
const date = workflowStep(workflow, 'prepare-release-assets', 'Resolve stable release date');
assert.match(date, /gh api "repos\/\$GITHUB_REPOSITORY\/actions\/runs\/\$GITHUB_RUN_ID" --jq \.created_at/);
assert.match(date, /date --utc --date="\$created_at" \+%Y\.%m\.%d/);
assert.match(prepare.outputs.matrix, /steps\.prepare\.outputs\.matrix/);
const condition = releaseJob.if.trim().replace(/^\$\{\{\s*/, '').replace(/\s*\}\}$/, '');
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
