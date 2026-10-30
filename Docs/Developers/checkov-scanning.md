## 🛡️ DfE ELZ Checkov Policy Scanning

This repository uses [DfE Checkov Policies](https://github.com/DFE-Digital/dfe-checkov-policies) to enforce security and governance standards (ELZ and CMN policies) across our Azure IaC. The scan runs automatically on PRs and pushes to `main`.

### Handling Baseline Exceptions
If a policy violation is flagged but cannot be immediately remediated (e.g., waiting on ELZ platform owner approval to move regions from `ukwest` to `uksouth` for `DFE_ELZ_0002`, or transitioning to Key Vault RBAC for `DFE_CMN_0004`), you must add an inline exception to the Terraform code.

Exceptions must be time-bound and explicitly state the approver. Add a comment directly above the failing resource block:
```hcl
resource "azurerm_resource_group" "example" {
  #checkov:skip=DFE_ELZ_0002: "Exception approved by [Name/Team] on [Date] due to resourcing constraints. Expires [Date]."
  name     = "rg-example"
  location = "ukwest"
}
```

### Running Scans Locally

Because we store .tfvars inside the repository, you can run security scans locally that are identical to the CI/CD pipeline.

Note: The Checkov Docker image is built for x86 architecture. If you are on an Apple Silicon (M-series) Mac, the ```--platform linux/amd64``` flag is required to run it via emulation.

Run this command from the root of the repository to validate your changes before pushing:
```bash
docker run --rm \
  --platform linux/amd64 \
  -v "$(pwd)":/tf \
  ghcr.io/dfe-digital/dfe-checkov-policies/checkov-elz:ELZ-2026.09.0 \
  -d /tf/terraform \
  --var-file /tf/terraform/environments/d01.tfvars \
  --output cli
```

### Updating the Pinned Checkov Image

We intentionally pin the Checkov ELZ image tag (e.g., ELZ-2026.09.0) instead of using latest. This prevents upstream policy changes from unexpectedly breaking our CI/CD pipeline.

When a new version of the DfE Checkov policies is released:

1. Create a chore branch (e.g., chore/update-checkov-image).
2. Update the image tag in [`.github/workflows/checkov-elz-policy-scan.yml`](../../.github/workflows/checkov-elz-policy-scan.yml) workflow.
3. Push the branch to trigger a scan against the new policies.
4. Remediate or skip any new violations before merging.