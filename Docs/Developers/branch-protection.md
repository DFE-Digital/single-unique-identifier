## Branch Protection & CI Governance (`main`)

### Active Ruleset
Merging to `main` requires:
* A pull request (direct pushes are blocked)
* At least **1** approving review (new commits dismiss stale approvals)
* All review conversations resolved
* Branch up to date with `main`
* All required status checks passing

### Required Status Checks
* **Application build, test & SonarCloud Quality Gate:**
    * `GetAnIdentifier - Build Test Dotnet / build-test-dotnet`
    * `AuthEmulator - Build Test Dotnet / build-test-dotnet`
    * `Notification Service - build and test / build-test-dotnet`
    * `UIHarness - Build Test Dotnet / build-test-dotnet`
* **Container validation:** `Notification Service - container validation`
* **Security & IaC scans:**
    * `Trivy repository scan` (vulnerabilities and licences)
    * `Trivy IaC scan`
    * `Checkov ELZ scan (d01)`, `Checkov ELZ scan (d02)`, `Checkov ELZ scan (d03)`
    * `TruffleHog secret scan`
* **Formatting:** `Terraform format`

### Path-Aware No-Op Filtering
Required workflows do not use workflow-level `on.pull_request.paths` filters, which would leave PRs blocked waiting for unexecuted checks. Instead, each path-scoped workflow runs a `detect-changes` job (`dorny/paths-filter`) outputting `has_changes`:
* When relevant files change, the downstream job runs and must pass.
* When no relevant files change, the downstream job is skipped via `if:`, which GitHub treats as a passing check.

`TruffleHog secret scan` is unfiltered and runs on every PR.

### Trivy Thresholds & Exception Process
`Trivy repository scan` and `Trivy IaC scan` fail the build on `HIGH` and `CRITICAL` findings.

To record a time-bound exception (false positive, no upstream fix, or accepted risk):
1. Add the finding ID to `.trivyignore` at the repository root with an expiry date (`exp:YYYY-MM-DD`) and a comment citing the justification and ticket:
   ```text
   # Ticket: <ID> - <reason/mitigation>
   CVE-XXXX-XXXXX exp:YYYY-MM-DD