## Branch Protection & CI Governance (`main`)

### Active Ruleset
Merging to `main` requires:
- A pull request (direct pushes are blocked)
- At least **1** approving review (new commits dismiss stale approvals)
- All review conversations resolved
- Branch up to date with `main`
- All required status checks passing

### Required Status Checks
- **Application build, test, SonarCloud Quality Gate & container validation:**
  - `GetAnIdentifier - CI Gate`
  - `AuthEmulator - CI Gate`
  - `Notification Service - CI Gate` (covers build, test, SonarCloud Quality Gate, and container validation)
  - `UIHarness - CI Gate`
- **Security & IaC scans:**
  - `Trivy repository scan` (vulnerabilities and licences)
  - `Trivy IaC scan`
  - `Checkov ELZ scan` (aggregates `d01`, `d02`, and `d03`)
  - `TruffleHog secret scan`
- **Formatting:** `Terraform format`

### Path-Aware No-Op Filtering
Required workflows do not use workflow-level `on.pull_request.paths` filters, which would leave PRs blocked waiting for unexecuted checks. Instead, path-scoped workflows use `dorny/paths-filter`:
- **Gate-job workflows (apps, Checkov, Trivy, and Trivy IaC):** A `detect-changes` job outputs `has_changes` and an `if: always()` gate job reports the required status check. The gate passes only if `detect-changes` succeeds and downstream jobs either pass (when relevant files changed) or are skipped on a PR with no relevant changes (`has_changes != 'true'`).
- **Inline-filtered workflow (`Terraform format`):** Runs `dorny/paths-filter` as a step inside the single job and skips the Terraform CLI and formatting steps when no relevant files changed.
- **Unfiltered workflow (`TruffleHog secret scan`):** Runs on every PR.

### Trivy Thresholds & Exception Process
`Trivy repository scan` and `Trivy IaC scan` fail the build on `HIGH` and `CRITICAL` findings.

To record a time-bound exception (false positive, no upstream fix, or accepted risk):
1. Add the finding ID to `.trivyignore` at the repository root with an expiry date (`exp:YYYY-MM-DD`) and a comment citing the justification and ticket:
   ```text
   # Ticket: <ID> - <reason/mitigation>
   CVE-XXXX-XXXXX exp:YYYY-MM-DD
   ```
2. Submit the change via pull request for standard review and audit history.
3. Review and remove entries once resolved or before expiry.