# CI workflows

This repo uses reusable workflows. The goal is to keep the top-level workflows thin and move shared logic into reusable workflows and local actions.

## Workflow layout

Top-level app workflows live in [`.github/workflows/*-build-and-deploy.yml`](../../.github/workflows) and call these reusable workflows:

- [`build-test-dotnet.yml`](../../.github/workflows/build-test-dotnet.yml) - builds, tests, scans, and publishes per-project artifacts.
- [`deploy-dotnet-webapp.yml`](../../.github/workflows/deploy-dotnet-webapp.yml) - deploys a web app from a build artifact.
- [`deploy-dotnet-functionapp.yml`](../../.github/workflows/deploy-dotnet-functionapp.yml) - deploys a function app from a build artifact.

## Runner selection

Workflows that use configurable runner labels read the `RUNNER_LABELS` GitHub Actions variable as a JSON array. If it is unset, they use `["ubuntu-latest"]`. Set the variable at repository or environment scope to select a different runner or runner group, and make sure the configured labels match an available runner.

The shared per-app build workflow selects the exact patched SDK in `global.json` and logs its version. The separate release build and its locked restore/test/package behaviour are described in the [immutable release guide](./immutable-releases.md).

## Application build artifacts

Application deployment artifacts are uploaded to GitHub Actions by the shared build workflow and downloaded by the deploy workflows. The build and deploy jobs in the app workflows use the same workflow run. When starting a deploy workflow separately, provide both the exact artifact name and the ID of the workflow run that uploaded it (`artifact_run_id`). The run must belong to this repository and the artifact must still be retained; the current default retention period is 60 days. Terraform state continues to use the existing Azure Blob backend and locking.

## Manual run inputs

Top-level workflows accept the following inputs on `workflow_dispatch`:

- `deploy` - whether to deploy after build.
- `upload_artifacts` - allows artifact upload without deploying.
- `skip_tests` - skips tests and Azurite/coverage setup.
- `skip_scans` - skips SonarCloud scanning steps.

Deployments are blocked when `skip_tests` or `skip_scans` is true.

## Manual deploy workflows

You can run the deploy workflows directly (without a build) and provide an existing GitHub Actions artifact:

- [`deploy-dotnet-webapp.yml`](../../.github/workflows/deploy-dotnet-webapp.yml)
- [`deploy-dotnet-functionapp.yml`](../../.github/workflows/deploy-dotnet-functionapp.yml)

Required inputs when running manually:

- `artifact_name` - the exact build artifact name, e.g. `Find-SUI.Find.FindApi-20260220-4b3e5b2-build`.
- `artifact_run_id` - the workflow run ID that uploaded the artifact.
- Either `component_descriptor` or the full app name override (`web_app_name` / `function_app_name`), e.g. `find01` or `s270d01func-ukw-01-find01`.

## Adding a new app workflow

1. Add a new `*-build-and-deploy.yml` in [`.github/workflows`](../../.github/workflows).
2. Call `build-test-dotnet.yml` with `project_names` and `directory_name`.
3. Call the appropriate deploy workflow and pass the artifact name from `build-test-dotnet.yml` outputs.
4. Keep `runs-on` consistent with the `RUNNER_LABELS` variable (JSON array). Default is `["ubuntu-latest"]`.

## Troubleshooting

- If deployments fail because an artifact cannot be found, verify the artifact name, source run ID, repository and retention period.

## Security scanning

The repo now uses separate workflows for infrastructure checks and secret scanning:

- [`trivy-iac.yml`](../../.github/workflows/trivy-iac.yml) blocks PRs and pushes to `main` when Trivy finds `HIGH` or `CRITICAL` IaC misconfigurations.
- [`trufflehog.yml`](../../.github/workflows/trufflehog.yml) blocks PRs and pushes to `main` on new verified or unknown secrets.
- [`trivy.yml`](../../.github/workflows/trivy.yml) runs a broader non-blocking repository scan on a schedule or via manual dispatch.
- [`trufflehog-deep-scan.yml`](../../.github/workflows/trufflehog-deep-scan.yml) runs a deeper scheduled/manual scan across the current branch history.

If you configure GitHub branch protection for `main`, set these required checks:

- `Trivy IaC Scan / Trivy IaC scan`
- `TruffleHog Secret Scan / TruffleHog secret scan`

## Artifact cleanup

The repo currently uses a 60-day default retention policy for artifacts and logs (check repo settings for changes). If you need to clear artifacts manually, run [`cleanup-artifacts.sh`](../../.github/scripts/cleanup-artifacts.sh) with `--help`.

# Pipeline & Security Escalation Routes

To ensure operational resilience and rapid response to supply-chain or infrastructure issues, all automated pipelines have documented owners. If a scheduled scan, deployment, or update fails, use the escalation routes below.

| Failure Type                     | Coverage Examples                                                                                                 | Primary Owner             | Checking & Escalation Route                                                                                                                                         |
|:---------------------------------|:------------------------------------------------------------------------------------------------------------------|:--------------------------|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Security Scans**               | Weekly CodeQL, scheduled TruffleHog (Deep Scan) and Trivy (Repo), plus PR-triggered scans (Trivy IaC, SonarCloud) | SUI Engineering Team      | Monitored manually by the team. Failures should be reported to the internal engineering Slack channel.                                                                   |
| **Automatic Deployments**        | Pushes to `main` triggering automatic deployments (e.g., Get An Identifier, Auth Emulator, UI Harness, etc.)      | SUI Engineering Team      | Monitored manually by the team. Failures should be reported to the internal engineering Slack channel. The SUI Technical Lead assigns an engineer if further investigation is needed. |
| **Manual Deployments**           | Manual workflow dispatches to `d01`, `d02`, `d03`, and targeted Terraform Applies                                 | Person running deployment | The person running the deployment is responsible for checking the run and reporting failures to the internal engineering Slack channel. |
| **Dependency Updates**           | Dependabot PRs (NuGet & Actions) failing checks or blocked by policy                                              | SUI Engineering Team      | Reviewed in bi-weekly sprint planning or reported to the internal engineering Slack channel.                                                                            |
| **Credential / Key Expirations** | Azure OIDC failures, NHS MESH / Digital certificate expiry                                                        | SUI Technical Lead        | *Automated rotation is not currently implemented.* Manual renewals and unexpected authentication failures are managed by the SUI Technical Lead via the internal engineering Slack channel. |

## Escalation Process

1. **Report & Acknowledge:** Automatic deployment failures on pushes to `main` and scheduled workflow failures must be checked manually by the SUI Engineering Team and reported to the internal engineering Slack channel. For manual deployments, the person triggering the deployment is responsible for checking the logs and reporting the failure.
2. **Investigate:** Review the GitHub Actions run logs. The SUI Technical Lead will assign an engineer if the issue requires further investigation.
3. **Resolve / Escalate:** Escalate to the SUI Technical Lead if nobody takes ownership or the assigned engineer needs support.

## Immutable application releases

The separate [immutable release workflow](./immutable-releases.md) publishes verified application ZIPs. The per-app workflows described above remain the operational build-and-deploy route until SBOM generation and deployment/promotion through the replacement flow are complete. They build their own deployment artifacts; deployments do not yet consume GitHub Releases.

## Environment Labels

- **d01**: experimental (Auth Emulator, FaUAPI front end)
- **d02**: stable testing (FaUAPI auth)
- **d03**: staging/pre-production (production-like FaUAPI auth)
