# Secret Rotation

This repo includes an operational workflow for rotating supported application secrets that are managed from infrastructure code.

Current workflow:

- [`rotate-keyvault-secret.yml`](../../.github/workflows/rotate-keyvault-secret.yml)
- [`rotate-keyvault-secret.manifest.json`](../../.github/rotate-keyvault-secret.manifest.json)

## Current model

This documentation covers the current single-active secret architecture used by the supported rotation workflow.

- Rotations are cutover-based. The workflow creates a new secret value, refreshes dependent configuration, and restarts the supported app where required.
- The workflow is designed to support multiple secrets over time, but today it only supports `find-match-api-key`.
- Multi-secret overlap and true zero-downtime switchover are tracked separately.

## Supported secrets

| Workflow `secret_name` | Display name | Source system | Dependent config | Scheduled rotation | Verification |
| --- | --- | --- | --- | --- | --- |
| `find-match-api-key` | Find Match API key | Azure Key Vault via Terraform in [`terraform/find`](../../terraform/find) | Find Function App Key Vault reference (`MatchFunction__XApiKey`) | Yes | Health check, auth token request, `matchperson` smoke test |

The manifest file is the source of truth for:

- supported `secret_name` values
- shared app/domain rotation profiles
- scheduled environments
- Terraform rotation targets
- dependent app refresh/restart strategy
- post-rotation verification strategy

The manifest is structured as:

- `profiles`: shared defaults for a domain or app, such as resource naming, refresh/restart behaviour, and verification strategy
- `secrets`: individual secret entries that reference a profile and define their own rotation targets and schedule

## Manual rotation

Run the workflow from the Actions tab:

1. Open [`rotate-keyvault-secret.yml`](../../.github/workflows/rotate-keyvault-secret.yml).
2. Select the target `environment`.
3. Enter the supported `secret_name`.
4. Set the `reason`:
   - `planned_rotation`
   - `suspected_exposure`
   - `exposed_secret`
5. Set `apply`:
   - `false` for a plan-only run
   - `true` to rotate the secret
6. Leave `verify` enabled unless you have a specific reason to skip the smoke checks.

Recommended operator flow:

1. Run a plan-only execution first with `apply = false`.
2. Review the workflow summary and confirm the target secret and environment are correct.
3. Re-run with `apply = true`.
4. Review the post-rotation summary and verification steps.

## Scheduled rotation

The workflow runs on a weekly schedule at `03:00 UTC` every Monday.

For scheduled runs:

- only secrets explicitly marked as scheduled are included
- the scheduled targets are defined as an explicit workflow matrix so new secrets can be added as extra `{ environment, secret_name }` entries
- the workflow checks the current secret expiry metadata
- rotation only proceeds when the secret is within the `30` day rotation window

If a scheduled run determines that rotation is not yet required, the workflow summary records that it was skipped.

## What the workflow does

For supported secrets, the workflow:

1. loads the target environment configuration from tfvars
2. resolves the secret-specific Terraform state and resource context
3. reads the current Key Vault secret metadata
4. plans a targeted Terraform rotation
5. optionally applies the rotation
6. refreshes Key Vault app-setting references where needed
7. restarts the dependent app where needed so the new secret becomes active
8. runs post-rotation smoke tests where configured
9. writes a workflow summary showing the outcome without exposing the secret value

The workflow never writes the secret value to logs or artifacts. Any secret value fetched for verification is masked before use.

## Post-rotation checks

Always review the workflow summary after an apply run.

For the Find Match API key, the workflow currently runs the Find smoke-test subset from [`Apps/Find/tests/SUI.Find.E2ETests`](../../Apps/Find/tests/SUI.Find.E2ETests).

Those smoke tests verify:

- the Find API health endpoint returns `Healthy`
- a bearer token can still be obtained from `/api/v1/auth/token`
- the rotated `x-api-key` works against `/api/v1/matchperson`

If any of these checks fail:

- review the workflow logs for the failed step
- confirm the Function App restart completed
- confirm the Key Vault reference refresh step completed
- confirm the secret metadata in Key Vault has the expected new version and expiry

## Planned vs exposed rotation

The workflow accepts `planned_rotation`, `suspected_exposure`, and `exposed_secret` reasons so operators can record why the run was triggered.

In the current single-active model:

- `planned_rotation` is still a controlled cutover
- `suspected_exposure` is the same technical flow, with incident handling decided outside the workflow
- `exposed_secret` should be applied promptly so the old secret can be retired as quickly as possible

These modes do not yet keep old and new secrets active at the same time.

## Current limitation

This workflow does not provide overlap-based zero-downtime secret rotation.

Today the supported Find API only accepts one configured API key at a time, so rotating the secret invalidates callers still using the previous value as soon as the app switches over. The current workflow is still useful for controlled cutover and operational response, but multi-secret app support is required before planned rotations can keep both old and new keys valid during switchover.
