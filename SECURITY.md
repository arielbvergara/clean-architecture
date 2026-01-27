# Security and Software Supply Chain Policy

This project follows OWASP Top 10:2025 guidance, with a focus on A03: Software Supply Chain Failures. Our goal is to detect and remediate vulnerable dependencies early, and to provide transparency into the components used by the application.

## Dependency and vulnerability scanning in CI

All pull requests run a dedicated CI job named `Dependency and Security Checks` defined in `.github/workflows/pr-ci.yml`.

That job:

- Restores dependencies for the solution using `dotnet restore`.
- Runs `dotnet list package --vulnerable --include-transitive` to detect vulnerable NuGet dependencies across the solution.
- Fails when the underlying `dotnet list package` command fails, surfacing dependency issues directly in the PR checks.

Contributors should treat any reported high or critical vulnerabilities as blocking issues for merges unless there is an explicitly documented exception.

## SBOM generation

The same CI job also generates a Software Bill of Materials (SBOM) for the solution using a GitHub Action that scans the repository and produces an SPDX-compatible SBOM file. The SBOM is uploaded as a build artifact named `dependency-sbom`.

Consumers can:

- Download the `dependency-sbom` artifact from CI runs.
- Use it to understand which components and transitive dependencies are present.
- Incorporate it into their own compliance and risk processes.

## Handling vulnerable components

When vulnerable dependencies are discovered:

1. Prefer upgrading the affected package via a version bump (often via Dependabot PRs) and verifying that tests still pass.
2. For high or critical vulnerabilities that cannot be immediately fixed, open an issue describing:
   - The affected package and version.
   - Known CVEs or advisories.
   - Proposed remediation plan and timeline.
3. Avoid introducing new dependencies unless they are necessary and actively maintained.

## Automated updates and review

Dependabot is configured to create pull requests for NuGet, GitHub Actions, and Docker dependencies. These automated PRs:

- Are reviewed like any other code change.
- Should not be merged if they introduce failing security checks or regressions.

## Reporting security issues

If you discover a potential security vulnerability in this project:

- Do not open a public issue with exploit details.
- Instead, contact the maintainers privately (for example, via the profile email on the repository owner) so the issue can be triaged and fixed before disclosure.

Once a fix is available, the maintainers may publish details following responsible disclosure practices.
