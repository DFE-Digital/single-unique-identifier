# Self-hosted runner (historical)

> **DEPRECATION NOTICE**: The self-hosted runner configuration described below is retired. Current workflows use GitHub-hosted `ubuntu-latest` runners. This document is retained for historical context.

The self-hosted runner was originally put in place as a workaround for GitHub Actions limits. The repository is public and current workflows use GitHub-hosted runners.

## Self-hosted runner (Retired)

- Hosted in Azure Container Apps using the [myoung34 GitHub runner image](https://github.com/myoung34/docker-github-actions-runner).
- Registered at the repo level.
- Labels: `self-hosted`, `ubuntu-latest`, `linux`, `ghrunner`.
- **No Docker daemon** in Container Apps, so Docker-based actions and `services:` containers will fail.
- Azurite is started as a local process when tests require it.
- The runner authenticates via the org‑managed GitHub App **sui-self-hosted-runner**.
    - Key env vars: `APP_ID`, `APP_PRIVATE_KEY`, `APP_LOGIN`, `RUNNER_SCOPE=repo`, and `REPO_URL`.
    - Required permissions: **Administration: read & write** on the repo (to create runner registration tokens); **Actions: read** and **Metadata: read** are also recommended.
- *Historical Note: The deploy workflows previously installed the Azure CLI at runtime because the self-hosted image lacked it by default. This dynamic installation has been removed.*

## Runner selection via RUNNER_LABELS (Legacy)

Workflows read runner labels from the `RUNNER_LABELS` GitHub variable (JSON array). If it is not set, the default is `["ubuntu-latest"]`.

Examples:

- Switch back to hosted runners (Current default): `RUNNER_LABELS=["ubuntu-latest"]`
- Legacy repo variable: `RUNNER_LABELS=["self-hosted","ubuntu-latest"]`

## Switching back to GitHub-hosted runners

- Set `RUNNER_LABELS=["ubuntu-latest"]` at the repo or environment level. See [`.github/workflows`](../../.github/workflows).
- Remove or scale down the Azure Container App runner if it is no longer needed.

Application deployment artifacts use GitHub Actions artifacts. Terraform
continues to use the existing Azure Blob remote state backend and locking.
