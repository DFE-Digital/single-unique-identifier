# ⚠️ DEPRECATED: Self-hosted runner and artifact storage

**Notice:** As part of the GitHub Actions Security Hardening initiative, the CI/CD pipelines have been heavily optimized and locked down to use GitHub-hosted `ubuntu-latest` runners.

To enforce strict supply-chain security and improve build speeds, dynamic runtime installations of `az`, `azcopy`, `zip`, and `unzip` were removed from the workflow files. Because the `myoung34` self-hosted runner image does not include these tools natively, **this self-hosted runner configuration is currently incompatible with the deployment pipelines and is officially retired.**

*The documentation below is preserved for historical context only.*

---

## Self-hosted runner (Retired)
- Hosted in Azure Container Apps using the [myoung34 GitHub runner image](https://github.com/myoung34/docker-github-actions-runner).
- Registered at the repo level.
- Labels: `self-hosted`, `ubuntu-latest`, `linux`, `ghrunner`.
- **No Docker daemon** in Container Apps, so Docker-based actions and `services:` containers will fail.
- Azurite is started as a local process when tests require it.
- The runner authenticates via the org‑managed GitHub App **sui-self-hosted-runner**.

## Runner selection via RUNNER_LABELS

Workflows read runner labels from the `RUNNER_LABELS` GitHub variable (JSON array). If it is not set, the default is `["ubuntu-latest"]`.

Examples:

- Repo variable: `RUNNER_LABELS=["self-hosted","ubuntu-latest"]`
- Environment variable (e.g. `d01`, `d02`): `RUNNER_LABELS=["self-hosted","ubuntu-latest"]`
- Switch back to hosted runners: `RUNNER_LABELS=["ubuntu-latest"]`

## Switching back to GitHub-hosted runners

- Set `RUNNER_LABELS=["ubuntu-latest"]` at the repo or environment level. See [`.github/workflows`](../../.github/workflows).
- Remove or scale down the Azure Container App runner if it is no longer needed.

## Artifact storage

Artifact storage was moved to Azure Blob as a workaround for GitHub artifact storage limits.

The storage backend is controlled by the `artifact_store` input and/or `ARTIFACT_STORE` repo variable:

- `github` - use GitHub Actions artifacts.
- `azure` - use Azure Blob via AzCopy.

Manual runs default to `azure`. For push runs, set the repo variable `ARTIFACT_STORE` to `azure` or `github`.

### Switching back to GitHub artifact storage

- Set `artifact_store=github` on manual runs.
- Set `ARTIFACT_STORE=github` for push runs.

### Azure Blob implementation

Azure uploads/downloads are handled by local composite actions:

- [`.github/actions/upload-blob-artifacts`](../../.github/actions/upload-blob-artifacts)
- [`.github/actions/download-blob-artifact`](../../.github/actions/download-blob-artifact)

These create a zip per project (matching the GitHub artifact structure) and unzip on download.
The composite actions install `zip`, `unzip`, and `azcopy` via `apt` if they are missing.

Required secrets/vars:

- `AZURE_ARTIFACTS_SAS` (secret) - container SAS token.
- `AZURE_ARTIFACTS_ACCOUNT` (secret or repo variable) - storage account name.
- `AZURE_ARTIFACTS_CONTAINER` (secret or repo variable) - container name.

The storage account connection currently uses a SAS token that is set to expire on `February 23, 2027`. We recommend switching to service‑principal access when permissions allow, but this was not done yet due to lack of role assignment permissions. The service principal needs the `Storage Blob Data Contributor` role on the storage account or container.

Lifecycle management is enabled on the artifact container to delete blobs more than 60 days old, matching the GitHub artifact retention window.
