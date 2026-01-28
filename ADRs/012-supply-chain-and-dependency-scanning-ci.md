# 12. Supply Chain and Dependency Scanning in CI

## Status
- **Status**: Accepted
- **Date**: 2026-01-27
- **Related issue**: [GitHub issue #10](https://github.com/arielbvergara/clean-architecture/issues/10)

## Context

The solution relies on several external dependencies (NuGet packages such as EF Core, Swashbuckle, test libraries, GitHub Actions, Docker images, etc.), but originally had no explicit software supply chain management or dependency vulnerability scanning in CI.

This created several risks related to OWASP Top 10:2025 A03 (Software Supply Chain Failures):
- **Unmonitored vulnerable components**: no automated process to detect or fail on vulnerable NuGet dependencies.
- **Lack of SBOM visibility**: consumers could not easily see which components and transitive dependencies are present.
- **Undocumented handling policy**: no shared understanding of when vulnerabilities should block merges versus be tracked for remediation.

At the same time:
- Dependabot was already configured to open PRs for NuGet, GitHub Actions, and Docker dependencies.
- The PR CI workflow (`.github/workflows/pr-ci.yml`) contained a commented-out `security-and-deps` job stub suggesting a direction for security checks, but it was not active or complete.

## Decision

We will add a first-class supply chain and dependency scanning capability to PR CI, with a dedicated job, SBOM generation, and lightweight guardrails.

### 1. Activate `security-and-deps` job in PR CI

- Define a `security-and-deps` job in `.github/workflows/pr-ci.yml` that runs for pull requests alongside the existing `build-and-test` and `lint-and-format` jobs.
- The job:
  - Checks out the repository using `actions/checkout@v6`.
  - Sets up the .NET SDK using `actions/setup-dotnet@v5` with the shared `DOTNET_VERSION` environment variable and cached `*.csproj` lock paths.
  - Restores dependencies via `dotnet restore $SOLUTION_FILE`.
  - Runs `dotnet list package --vulnerable --include-transitive` against the solution to surface vulnerable NuGet dependencies.
- The CI behavior uses the exit code from `dotnet list package`:
  - If vulnerabilities cause the command to fail, the `security-and-deps` job fails, surfacing the issue as a blocking signal on the PR.

### 2. Generate and publish an SBOM as part of CI

- Introduce environment variables in the workflow `env` block to avoid magic strings:
  - `SBOM_OUTPUT_FILE` (e.g., `dependency-sbom.spdx.json`).
  - `SBOM_FORMAT` (e.g., `spdx-json`).
- Extend `security-and-deps` with SBOM-related steps:
  - `Generate SBOM` using `anchore/sbom-action@v0` with:
    - `path: .` (scan the repository).
    - `format: ${{ env.SBOM_FORMAT }}`.
    - `output-file: ${{ env.SBOM_OUTPUT_FILE }}`.
    - `upload-artifact: false` (we handle artifact upload explicitly in the next step).
  - `Upload SBOM artifact` using `actions/upload-artifact@v6` with:
    - `name: dependency-sbom` (logical artifact name).
    - `path: ${{ env.SBOM_OUTPUT_FILE }}`.
- This ensures every PR run produces an SBOM artifact that downstream consumers can download and inspect.

### 3. Add CI guardrails to protect the security job

- Add a small bash script `scripts/verify-ci-security.sh` that checks `.github/workflows/pr-ci.yml` for the presence of:
  - The `security-and-deps` job.
  - The `dotnet list package --vulnerable --include-transitive` step.
  - The `Generate SBOM` step.
  - The `Upload SBOM artifact` step.
- Wire this script into the `lint-and-format` job as an additional step:
  - `Verify CI security configuration` running `bash scripts/verify-ci-security.sh`.
- If any of the expected steps or job names are missing, the script exits with a non-zero code, causing the CI job to fail and preventing accidental removal of the security checks.

### 4. Document handling expectations

- Model the handling policy in documentation (originally captured in `SECURITY.md`) and in this ADR:
  - Treat **high or critical vulnerabilities** reported by `dotnet list package` as blocking for merges unless there is a documented exception.
  - Prefer fixing vulnerabilities by upgrading dependencies (often via Dependabot PRs) rather than suppressing warnings.
  - For issues that cannot be fixed immediately, open a tracking issue describing:
    - Affected package and version.
    - Relevant CVEs or advisories.
    - Proposed remediation path and timeline.

## Consequences

### Positive

- **Improved supply chain visibility and hygiene**
  - Vulnerable NuGet dependencies are surfaced automatically in PR CI via `dotnet list package --vulnerable --include-transitive`.
  - SBOM artifacts provide a machine-readable inventory of direct and transitive dependencies for each run.
  - The approach aligns with OWASP Top 10:2025 A03 by adding concrete, automated checks to the software supply chain.

- **Clear, centralized configuration**
  - Environment variables (`SBOM_OUTPUT_FILE`, `SBOM_FORMAT`) and a named `security-and-deps` job avoid scattered magic strings in the workflow.
  - The guardrail script encodes expectations about the CI configuration in one place.

- **Better review and auditing**
  - Reviewers can rely on the `security-and-deps` job and the `dependency-sbom` artifact to understand the risk profile of a change.
  - The ADR documents why the job exists and how it behaves, making future refinements easier.

### Negative / Trade-offs

- **Longer CI times**
  - Running `dotnet list package --vulnerable --include-transitive` and generating an SBOM add extra time to each PR run.

- **Dependency on external tooling**
  - The approach relies on a third-party GitHub Action (`anchore/sbom-action`) and on the current behavior of `dotnet list package` for vulnerability detection.
  - Breaking changes in these tools may require updates to the workflow or guardrail script.

- **Potential noise from vulnerabilities**
  - In ecosystems with frequent CVE disclosures, the job may surface many issues that need triage, increasing maintenance overhead.

## Implementation Notes

- Workflow-level environment variables:
  - `DOTNET_VERSION`, `SOLUTION_FILE`, `BUILD_CONFIGURATION`, `TEST_RESULTS_DIR` (existing).
  - `SBOM_OUTPUT_FILE`, `SBOM_FORMAT` (added for SBOM support).
- `security-and-deps` job is defined in `.github/workflows/pr-ci.yml` and runs on `ubuntu-latest` with `actions/checkout@v6` and `actions/setup-dotnet@v5`.
- SBOM generation uses `anchore/sbom-action@v0` with SPDX JSON output stored as `dependency-sbom.spdx.json` and uploaded as `dependency-sbom` artifact.
- Guardrail script lives at `scripts/verify-ci-security.sh` and is invoked from the `lint-and-format` job.

## Related ADRs

- [ADR-014: Security Design and Threat Model for User Endpoints](./014-security-design-and-threat-model-for-user-endpoints.md) – context for the application assets that this supply chain security protects.


## References

- [GitHub issue #10](https://github.com/arielbvergara/clean-architecture/issues/10)
- OWASP Top 10:2025 A03 – Software Supply Chain Failures
- `.github/workflows/pr-ci.yml` for CI configuration
- `scripts/verify-ci-security.sh` for CI guardrails
- ADR 009 – Automated Dependency Management with Dependabot
