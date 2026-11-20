import assert from 'node:assert/strict';
import { readWorkflow, workflowStep } from './test-helpers.mjs';
import { describe, it } from 'node:test';

// Check structure independently of YAML formatting.
const workflow = readWorkflow(new URL('../workflows/ci-cd-orchestrator.yml', import.meta.url));
assert.doesNotMatch(JSON.stringify(workflow), /notification_service|NotificationService|publish-container|azure\/login/);
const releaseJob = workflow.jobs['cut-release'];
assert.equal(releaseJob.uses, './.github/workflows/release-component.yml');
assert.equal(releaseJob.needs, 'prepare-release-assets');
assert.match(releaseJob.strategy.matrix, /fromJSON\(needs\.prepare-release-assets\.outputs\.matrix\)/);
assert.match(releaseJob.with.release_tag, /matrix\.component\.tag/);
assert.match(releaseJob.with.component, /matrix\.component\.name/);
const publisher = readWorkflow(new URL('../workflows/publish-immutable-release.yml', import.meta.url));
const verify = workflowStep(publisher, 'publish', 'Verify release asset inventory and build attestations');
const publish = workflowStep(publisher, 'publish', 'Create and publish immutable release');
for (const pattern of [/git\/ref\/tags/, /verify_inventory/, /gh release delete/, /gh release create/, /gh release edit/]) assert.match(publish, pattern);
assert.match(verify, /attestation verify/);
assert.match(verify, /\$\{#release_files\[@\]\} == 3/);
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
const condition = prepare.if.trim().replace(/^\$\{\{\s*/, '').replace(/\s*\}\}$/, '');
assert.ok(condition, 'release preparation condition exists');

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

it('publishes through one matrix and passes the local publication ID to Get an Identifier promotion', () => {
  const componentRelease = readWorkflow(new URL('../workflows/release-component.yml', import.meta.url));
  const publisher = componentRelease.jobs.publish;
  const promotion = componentRelease.jobs['promote-get-an-identifier'];
  assert.equal(publisher.uses, './.github/workflows/publish-immutable-release.yml');
  assert.equal(publisher.with.asset_subdirectory, '${{ inputs.component }}');
  assert.equal(publisher.with.release_tag, '${{ inputs.release_tag }}');
  assert.equal(publisher.permissions['id-token'], undefined);
  assert.equal(promotion.needs, 'publish');
  assert.match(promotion.if, /inputs\.component == 'GetAnIdentifier'/);
  assert.match(promotion.if, /vars\.GET_AN_IDENTIFIER_IMMUTABLE_PROMOTION == 'true'/);
  assert.equal(promotion.uses, './.github/workflows/promote-get-an-identifier.yml');
  assert.equal(promotion.permissions.contents, 'read');
  assert.equal(promotion.with.release_id, '${{ needs.publish.outputs.release_id }}');
});

it('verifies the release once and uses its exact ZIP in three dependent stages', () => {
  const promotion = readWorkflow(new URL('../workflows/promote-get-an-identifier.yml', import.meta.url));
  const verify = promotion.jobs.verify;
  assert.deepEqual(Object.keys(promotion.on), ['workflow_call']);
  assert.match(verify.if, /vars\.GET_AN_IDENTIFIER_IMMUTABLE_PROMOTION == 'true'/);
  assert.match(verify.if, /github\.ref == 'refs\/heads\/main'/);
  assert.equal(verify.uses, './.github/workflows/verify-immutable-zip.yml');
  assert.equal(verify.with.expected_source_sha, '${{ github.sha }}');
  for (const [target, needs] of [['d01', 'verify'], ['d02', ['verify', 'd01']], ['d03', ['verify', 'd02']]]) {
    const stage = promotion.jobs[target];
    assert.deepEqual(stage.needs, needs);
    assert.equal(stage.if, undefined, 'default success condition must apply');
    assert.equal(stage.uses, './.github/workflows/deploy-verified-functionapp.yml');
    assert.equal(stage.with.target_environment, target);
    assert.equal(stage.with.approval_environment, 'get-an-identifier-' + target);
    assert.equal(stage.with.release_id, '${{ inputs.release_id }}');
    assert.equal(stage.with.artifact_id, '${{ needs.verify.outputs.artifact_id }}');
    assert.equal(stage.with.digest, '${{ needs.verify.outputs.digest }}');
  }
});

it('rolls back through the shared deploy workflow with a separate approval for d01', () => {
  const rollback = readWorkflow(new URL('../workflows/rollback-get-an-identifier.yml', import.meta.url));
  assert.deepEqual(rollback.on.workflow_dispatch.inputs.target_environment.options, ['d01', 'd02', 'd03']);
  assert.match(rollback.jobs.verify.if, /github\.ref == 'refs\/heads\/main'/);
  assert.match(rollback.jobs.verify.if, /vars\.GET_AN_IDENTIFIER_IMMUTABLE_PROMOTION == 'true'/);
  assert.equal(rollback.jobs.verify.uses, './.github/workflows/verify-immutable-zip.yml');
  assert.equal(rollback.jobs.verify.with.expected_source_sha, undefined);
  const deploy = rollback.jobs.rollback;
  assert.equal(deploy.needs, 'verify');
  assert.equal(deploy.uses, './.github/workflows/deploy-verified-functionapp.yml');
  assert.equal(deploy.with.app, 'get-an-identifier');
  assert.equal(deploy.with.target_environment, '${{ inputs.target_environment }}');
  assert.match(deploy.with.approval_environment, /inputs\.target_environment == 'd01' && 'get-an-identifier-d01-rollback'/);
  assert.equal(deploy.with.artifact_id, '${{ needs.verify.outputs.artifact_id }}');
});

it('deploys the verified ZIP in a native job with a lock for the physical target', () => {
  const deployment = readWorkflow(new URL('../workflows/deploy-verified-functionapp.yml', import.meta.url));
  const job = deployment.jobs.deploy;
  assert.match(job.if, /github\.ref == 'refs\/heads\/main'/);
  assert.equal(job.permissions.deployments, undefined);
  assert.deepEqual(job.environment, { name: '${{ inputs.approval_environment }}' });
  assert.deepEqual(job.concurrency, {
    group: '${{ inputs.app }}-release-${{ inputs.target_environment }}',
    'cancel-in-progress': false,
    queue: 'max',
  });
  const download = job.steps.find((step) => step.name === 'Download verified ZIP');
  assert.equal(download.with['artifact-ids'], '${{ inputs.artifact_id }}');
  assert.equal(download.with['digest-mismatch'], 'error');
  const names = job.steps.map((step) => step.name);
  assert.deepEqual(names.slice(-4), [
    'Deploy verified ZIP', 'Restore Function App SCM access restrictions',
    'Require successful SCM cleanup', 'Run smoke tests',
  ]);
  const cleanup = job.steps.at(-2);
  assert.equal(cleanup.env.CLEANUP_OUTCOME, '${{ steps.close_access.outcome }}');
  assert.equal(cleanup.run, 'test "$CLEANUP_OUTCOME" = success');
  assert.match(job.steps.at(-1).run, /TreatNoTestsAsError=true/);
});

it('guards the legacy build and deploy entry points while preserving PR validation', () => {
  const legacy = readWorkflow(new URL('../workflows/get-an-identifier-build-and-deploy.yml', import.meta.url));
  const standalone = readWorkflow(new URL('../workflows/deploy-dotnet-functionapp.yml', import.meta.url));
  const flag = /vars\.GET_AN_IDENTIFIER_IMMUTABLE_PROMOTION != 'true'/;
  assert.match(legacy.jobs['build-and-test-GetAnIdentifier'].if, flag);
  assert.match(legacy.jobs['build-and-test-GetAnIdentifier'].if, /github\.event_name == 'pull_request'/);
  assert.match(legacy.jobs['deploy-GetAnIdentifier'].if, flag);
  assert.match(legacy.jobs['smoke-test-GetAnIdentifier'].if, flag);
  assert.match(standalone.jobs['resolve-app'].if, flag);
  assert.equal(standalone.jobs.deploy.needs, 'resolve-app');
});
