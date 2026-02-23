# CI workflows

This repo uses reusable workflows. The goal is to keep the top-level workflows thin and move shared logic into reusable workflows and local actions. Self-hosted runner details (and the Azure artifact storage workaround) live in [Docs/Developers/ci-self-hosted-runner.md](./ci-self-hosted-runner.md).

## Workflow layout

Top-level app workflows live in [`.github/workflows/*-build-and-deploy.yml`](../../.github/workflows) and call these reusable workflows:

- [`build-test-dotnet.yml`](../../.github/workflows/build-test-dotnet.yml) - builds, tests, scans, and publishes per-project artifacts.
- [`deploy-dotnet-webapp.yml`](../../.github/workflows/deploy-dotnet-webapp.yml) - deploys a web app from a build artifact.
- [`deploy-dotnet-functionapp.yml`](../../.github/workflows/deploy-dotnet-functionapp.yml) - deploys a function app from a build artifact.

## Artifact storage backends

Artifacts can be stored in either GitHub or Azure Blob, controlled by the `artifact_store` input:

- `github` - uses GitHub Actions artifacts.
- `azure` - uploads to Azure Blob using AzCopy.

Manual runs default to `azure`. For push runs, set the repo variable `ARTIFACT_STORE` to `azure` or `github`. These flags make it easy to switch back when GitHub rate/budget limits are no longer an issue.

### Azure Blob settings

Azure uploads/downloads are handled by local composite actions:

- [`.github/actions/upload-blob-artifacts`](../../.github/actions/upload-blob-artifacts)
- [`.github/actions/download-blob-artifact`](../../.github/actions/download-blob-artifact)

These create a zip per project (matching the GitHub artifact structure) and unzip on download.

Required secrets/vars:

- `AZURE_ARTIFACTS_SAS` (secret) - container SAS token.
- `AZURE_ARTIFACTS_ACCOUNT` (secret or repo variable) - storage account name.
- `AZURE_ARTIFACTS_CONTAINER` (secret or repo variable) - container name.

## Manual run inputs

Top-level workflows accept the following inputs on `workflow_dispatch`:

- `deploy` - whether to deploy after build.
- `upload_artifacts` - allows artifact upload without deploying.
- `skip_tests` - skips tests and Azurite/coverage setup.
- `skip_scans` - skips SonarCloud scanning steps.
- `artifact_store` - `github` or `azure`.

Deployments are blocked when `skip_tests` or `skip_scans` is true.

## Manual deploy workflows

You can run the deploy workflows directly (without a build) and provide an existing artifact name:

- [`deploy-dotnet-webapp.yml`](../../.github/workflows/deploy-dotnet-webapp.yml)
- [`deploy-dotnet-functionapp.yml`](../../.github/workflows/deploy-dotnet-functionapp.yml)

Required inputs when running manually:

- `artifact_name` - the exact build artifact name (without a `.zip` suffix), e.g. `Find-SUI.Find.FindApi-20260220-4b3e5b2-build`.
- Either `component_descriptor` or the full app name override (`web_app_name` / `function_app_name`), e.g. `find01` or `s270d01func-ukw-01-find01`.
- `artifact_store` - `github` or `azure` (defaults to `azure` for manual deploys), e.g. `azure`.

## Adding a new app workflow

1. Add a new `*-build-and-deploy.yml` in [`.github/workflows`](../../.github/workflows).
2. Call `build-test-dotnet.yml` with `project_names`, `directory_name`, and `artifact_store`.
3. Call the appropriate deploy workflow and pass the artifact name from `build-test-dotnet.yml` outputs.
4. Keep `runs-on` consistent with the self-hosted runner labels.

## Troubleshooting

- If Azure uploads fail with missing inputs, ensure secrets/vars are set at the repo level (not just environment scope).
- If deployments fail due to artifacts not found, verify the artifact names and storage backend match across build and deploy jobs.

## Artifact cleanup

The repo currently uses a 60-day default retention policy for artifacts and logs (check repo settings for changes).
If you need to clear artifacts manually, run [`cleanup-artifacts.sh`](../../.github/scripts/cleanup-artifacts.sh) with `--help`.
