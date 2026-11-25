import assert from 'node:assert/strict';
import { execFile } from 'node:child_process';
import { readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { it } from 'node:test';
import { promisify } from 'node:util';
import { readWorkflow, temporaryDirectories } from './test-helpers.mjs';

const run = promisify(execFile);
const temporaryDirectory = temporaryDirectories();
const action = (verb) => readWorkflow(new URL(`../actions/${verb}-function-app-access/action.yml`, import.meta.url)).runs.steps[0].run;
const stale = 'github-deploy-10-1';
const current = 'github-deploy-11-1';
const unrelated = 'github-deploy-monitoring';

async function fixture({ access = 'Disabled', rules = [], defaultAction = 'Deny', fault = '' } = {}) {
  const dir = await temporaryDirectory('function-app-access-');
  const file = (name) => path.join(dir, name);
  await writeFile(file('access'), access);
  await writeFile(file('restrictions'), JSON.stringify({
    ipSecurityRestrictions: [], ipSecurityRestrictionsDefaultAction: defaultAction,
    scmIpSecurityRestrictionsDefaultAction: 'Deny', scmIpSecurityRestrictionsUseMain: false,
    scmIpSecurityRestrictions: rules.map((name) => ({ name, action: 'Allow', ipAddress: '192.0.2.1/32' })),
  }));
  await writeFile(file('az'), `#!/bin/bash
set -euo pipefail
command="$1 $2"
if [[ "$command" == "functionapp config" ]]; then
  command="$command $3 $4"
  shift 4
else
  shift 2
fi
if [[ "$command" == "functionapp list" ]]; then
  echo example-rg
  exit
fi
[[ "$1 $2 $3 $4" == "--resource-group example-rg --name example-app" ]]
shift 4
case "$command" in
  "functionapp show")
    [[ "$FAULT" != show ]]
    cat "$STATE/access"
    ;;
  "functionapp update")
    if [[ "$2" == publicNetworkAccess=Disabled && "$FAULT" == disable ]]; then exit 21; fi
    printf '%s' "\${2#*=}" > "$STATE/access"
    if [[ "$2" == publicNetworkAccess=Enabled && "$FAULT" == enable ]]; then exit 22; fi
    ;;
  "functionapp config access-restriction show")
    [[ "$FAULT" != query ]]
    [[ "$FAULT" != verify-rules || ! -f "$STATE/rule-removed" ]]
    if [[ $# == 0 ]]; then
      cat "$STATE/restrictions"
    else
      [[ "$1 $3 $4" == "--query -o tsv" ]]
      rule=$(cut -d "'" -f 2 <<< "$2")
      jq --arg rule "$rule" '[.scmIpSecurityRestrictions[] | select(.name == $rule)] | length' "$STATE/restrictions"
    fi
    ;;
  "functionapp config access-restriction add")
    [[ "$1 $9 \${10}" == "--rule-name --scm-site true" ]]
    jq --arg rule "$2" '.scmIpSecurityRestrictions += [{name: $rule}]' "$STATE/restrictions" > "$STATE/new"
    mv "$STATE/new" "$STATE/restrictions"
    [[ "$FAULT" != add ]]
    ;;
  "functionapp config access-restriction remove")
    [[ "$1 $3 $4" == "--rule-name --scm-site true" && "$2" != github-deploy-monitoring ]]
    [[ "$FAULT" != remove ]]
    [[ "$FAULT" != remove-first || "$2" != github-deploy-10-1 ]]
    jq -e --arg rule "$2" '
      select(any(.scmIpSecurityRestrictions[]; .name == $rule))
      | .scmIpSecurityRestrictions |= map(select(.name != $rule))
    ' "$STATE/restrictions" > "$STATE/new"
    mv "$STATE/new" "$STATE/restrictions"
    touch "$STATE/rule-removed"
    ;;
  *) exit 99 ;;
esac
`, { mode: 0o700 });
  await writeFile(file('sleep'), '#!/bin/bash\nexit 0\n', { mode: 0o700 });
  await writeFile(file('curl'), '#!/bin/bash\nprintf 401\n', { mode: 0o700 });
  return {
    open: (resourceGroup = 'example-rg') => execute('open', resourceGroup),
    close: () => execute('close', 'example-rg'),
    output: () => readFile(file('output'), 'utf8'),
    state: async () => ({
      access: await readFile(file('access'), 'utf8'),
      rules: JSON.parse(await readFile(file('restrictions'), 'utf8')).scmIpSecurityRestrictions.map(({ name }) => name),
    }),
  };

  function execute(verb, resourceGroup) {
    return run('bash', ['-eo', 'pipefail', '-c', action(verb)], { env: {
      ...process.env, PATH: dir + ':' + process.env.PATH, STATE: dir, FAULT: fault,
      APP_NAME: 'example-app', RESOURCE_GROUP: resourceGroup, RG: resourceGroup,
      RUNNER_IP: '192.0.2.2', RULE_NAME: current, GITHUB_OUTPUT: file('output'),
    } });
  }
}

it('recovers interrupted access, preserves unrelated rules and supports legacy resource lookup', async () => {
  const app = await fixture({ access: 'Enabled', rules: [stale, unrelated] });
  await app.open('');
  assert.deepEqual(await app.state(), { access: 'Enabled', rules: [unrelated, current] });
  await app.close();
  await app.close();
  assert.deepEqual(await app.state(), { access: 'Disabled', rules: [unrelated] });
});

it('fails closed on unexpected public access, insecure restrictions or failed reads', async () => {
  for (const inputs of [
    { access: 'Enabled', rules: [unrelated] }, { defaultAction: 'Allow' }, { fault: 'query' },
  ]) {
    const app = await fixture(inputs);
    await assert.rejects(app.open());
    await assert.rejects(app.output(), { code: 'ENOENT' });
    assert.deepEqual(await app.state(), { access: inputs.access ?? 'Disabled', rules: inputs.rules ?? [] });
  }
});

it('provides cleanup context after partial rule or network changes', async () => {
  for (const fault of ['add', 'enable']) {
    const app = await fixture({ fault });
    await assert.rejects(app.open());
    assert.equal(await app.output(), 'rg=example-rg\n');
    assert.deepEqual((await app.state()).rules, [current]);
    await app.close();
    assert.deepEqual(await app.state(), { access: 'Disabled', rules: [] });
  }
});

it('removes stale SCM rules even when disabling or verifying public access fails', async () => {
  for (const fault of ['disable', 'show']) {
    const app = await fixture({ access: 'Enabled', rules: [stale, unrelated], fault });
    const diagnostic = fault === 'disable' ? /Failed to disable publicNetworkAccess/ : /Could not verify that publicNetworkAccess is Disabled/;
    await assert.rejects(app.open(), ({ stdout }) => diagnostic.test(stdout));
    assert.equal(await app.output(), 'rg=example-rg\n');
    const expected = { access: fault === 'disable' ? 'Enabled' : 'Disabled', rules: [unrelated] };
    assert.deepEqual(await app.state(), expected);
    if (fault === 'disable') {
      await assert.rejects(app.open(), ({ stdout }) => /publicNetworkAccess must be Disabled/.test(stdout));
      assert.deepEqual(await app.state(), expected);
    }
  }
});

it('attempts every stale rule removal and blocks deployment if a rule remains', async () => {
  const app = await fixture({ access: 'Enabled', rules: [stale, 'github-deploy-9-1', unrelated], fault: 'remove-first' });
  await assert.rejects(app.open(), ({ stdout }) => {
    assert.match(stdout, /Failed to remove stale rule github-deploy-10-1/);
    assert.match(stdout, /Stale rules still remain/);
    return true;
  });
  assert.deepEqual(await app.state(), { access: 'Disabled', rules: [stale, unrelated] });
});

it('blocks deployment when stale rule absence cannot be verified', async () => {
  const app = await fixture({ access: 'Enabled', rules: [stale, unrelated], fault: 'verify-rules' });
  await assert.rejects(app.open(), ({ stdout }) => /Could not verify that stale SCM rules are absent/.test(stdout));
  assert.deepEqual(await app.state(), { access: 'Disabled', rules: [unrelated] });
});

it('removes the temporary rule when disabling or verifying public access fails', async () => {
  for (const fault of ['disable', 'show']) {
    const app = await fixture({ access: 'Enabled', rules: [current, unrelated], fault });
    await assert.rejects(app.close(), ({ stdout }) => {
      assert.match(stdout, /Could not verify that publicNetworkAccess is Disabled/);
      assert.match(stdout, /Temporary access restriction successfully verified as removed/);
      return true;
    });
    const expected = { access: fault === 'disable' ? 'Enabled' : 'Disabled', rules: [unrelated] };
    assert.deepEqual(await app.state(), expected);
    if (fault === 'disable') {
      await assert.rejects(app.open(), ({ stdout }) => /publicNetworkAccess must be Disabled/.test(stdout));
      assert.deepEqual(await app.state(), expected);
    }
  }
});

it('disables public access and fails when SCM rule removal or verification fails', async () => {
  for (const fault of ['remove', 'query']) {
    const app = await fixture({ access: 'Enabled', rules: [current, unrelated], fault });
    const diagnostic = fault === 'remove' ? /Temporary SCM rule .* still exists/ : /Could not verify that temporary SCM rule .* is absent/;
    await assert.rejects(app.close(), ({ stdout }) => diagnostic.test(stdout));
    assert.deepEqual(await app.state(), {
      access: 'Disabled', rules: fault === 'remove' ? [current, unrelated] : [unrelated],
    });
  }
});
