# CI workflows

This repo uses reusable workflows. The goal is to keep the top-level workflows thin and move shared logic into reusable workflows and local actions.

## Workflow layout

Top-level app workflows live in [`.github/workflows/*-build-and-deploy.yml`](../../.github/workflows) and call these reusable workflows:

- [`build-test-dotnet.yml`](../../.github/workflows/build-test-dotnet.yml) - builds, tests, scans, and publishes per-project artifacts.
- [`deploy-dotnet-webapp.yml`](../../.github/workflows/deploy-dotnet-webapp.yml) - deploys a web app from a build artifact.
- [`deploy-dotnet-functionapp.yml`](../../.github/workflows/deploy-dotnet-functionapp.yml) - deploys a function app from a build artifact.

## Runner selection

Workflows that use configurable runner labels read the `RUNNER_LABELS` GitHub Actions variable as a JSON array. If it is unset, they use `["ubuntu-latest"]`. Set the variable at repository or environment scope to select a different runner or runner group, and make sure the configured labels match an available runner.

Both .NET build workflows select the exact patched SDK in `global.json` and log its version. The release build restores committed NuGet locks in locked mode, then builds, tests and publishes deployment output with `--no-build`. When the SDK changes, regenerate affected locks with that SDK and validate locked restore before merging.

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

The release orchestrator validates pull requests and supports manual validation without publishing images or releases. A protected-main push selects changed components and publishes a separate immutable GitHub Release for each selected component only after the build gates and the secret, repository and IaC scans succeed. A commit can produce several independent component releases or none. Shared SDK/NuGet and release-workflow changes select all apps; Notification Service also responds to shared-library changes. Manual validation can select all components with `force_deploy_all`. Main pushes use separate runs so a later push cannot cancel a pending release for another component.

- `ci-cd-orchestrator.yml` builds and scans selected components. On PR/manual runs it packages and scans Notification Service locally. The `package-container-from-artifact` composite action downloads a tested ZIP, optionally verifies its build attestation, extracts the output, builds the image without publishing, and scans it. On main, the orchestrator authenticates to Azure, then calls `publish-scanned-container-to-acr` with the scanned local image, registry, repository, tag and Dockerfile. That action pushes the same image, obtains its ACR digest, and records the digest and pinned final base image. Both actions take service-specific values as inputs; Azure credentials and release gates remain with the caller.
- `build-test-dotnet-release.yml` contains the isolated release build. It uploads the tested deployment ZIP. The main-only `attest-release-zips.yml` workflow signs its exact downloaded bytes; PR/manual validation cannot call that workflow or receive its permissions. Existing app workflows continue to call `build-test-dotnet.yml` and use the existing deployment paths.
- `release-manifest.mjs` validates the selected ZIP inventory and writes a separate manifest and asset directory for each selected component. Each manifest records its component's immutable tag, full source SHA, workflow run ID and attempt, and ZIP name. For any container component identified by the orchestrator, it also records the published image repository and digest from that component's metadata artifact. Required build and scan gates remain enforced by the workflow and their actual results are available on the linked Actions run. The source commit identifies the SDK, project settings and NuGet locks; each ZIP's signed build attestation binds it to that commit and the trusted signing workflow. Consumers resolve each GitHub release ID from its `sui-<component>-<full source SHA>` tag.
- `publish-immutable-release.yml` is a reusable publication workflow called once for each selected component. Its caller supplies a release asset artifact and component directory, the attested-asset filename pattern, manifest filename, tag, source SHA/ref, signer workflow, title and notes. It verifies that the component directory contains exactly its manifest and signed ZIP, refuses an existing tag, creates a draft at the source SHA, and publishes it as immutable. It returns the GitHub release ID and URL. SUI-specific component selection, manifest creation and container publishing stay in the orchestrator.
- Before any container image is pushed, the selected build, ZIP attestation, mandatory repository scans, changed CI-helper tests, and a main-only release immutability preflight must pass. The release job checks immutability again immediately before publication using `RELEASE_SETTINGS_READ_TOKEN` (Administration: read for this repository). The publishing job's `GITHUB_TOKEN` has only the permissions needed to publish. GitHub locks the tag and assets and creates a release attestation when the draft is published.
- If a tag or draft already exists, publication stops without changing it. An operator must inspect and reconcile an incomplete draft or orphaned tag before a new release run can proceed. Published immutable releases cannot be replaced or reused.
- Application deployment artefacts use GitHub Actions artefacts. Terraform remains a manual plan/apply flow with its existing Azure remote state and locking.
- Promotion and deployment from immutable releases are handled separately. The release orchestrator does not deploy applications or apply Terraform.

To add another container component here, call the two container actions with that component's ZIP, Dockerfile and ACR repository. Upload the resulting `<Component>.json` as `release-container-metadata-<Component>`, include the publication job in the release gates, and list the component in `CONTAINER_COMPONENTS`. The manifest helper handles the metadata without a service-specific branch.

The container actions can be called from another repository at a reviewed commit SHA. A caller supplies its own tested ZIP, Dockerfile, registry values, Azure authentication and publication gates. GitHub checks out the caller's repository inside an externally called reusable workflow, so this orchestrator and its local action paths are not a portable cross-repository workflow as written. A shared reusable workflow would need these actions referenced at fixed versions and a caller-defined registry configuration. [GitHub's reusable workflow documentation](https://docs.github.com/en/actions/concepts/workflows-and-actions/reusing-workflow-configurations) explains this checkout behaviour.

## Environment Labels

- **d01**: experimental (Auth Emulator, FaUAPI front end)
- **d02**: stable testing (FaUAPI auth)
- **d03**: staging/pre-production (production-like FaUAPI auth)
