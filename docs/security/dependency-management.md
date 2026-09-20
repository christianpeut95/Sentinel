# Dependency management and vulnerability remediation

## Policy

Sentinel dependencies are reviewed at least for every release, after a material security advisory, and when a direct package is changed. Use supported framework/container base images and prefer maintained packages with a clear licence and update history.

## Remediation targets

| Severity | Target | Required action |
|---|---:|---|
| Critical | 7 calendar days | Patch, remove, mitigate or stop affected public deployment; record the decision immediately |
| High | 30 calendar days | Patch or deploy a documented compensating control and approved exception |
| Medium | 90 calendar days | Schedule remediation in the next practical release and track it |
| Low | Next planned release | Update when compatible; document why any item remains |

The clock starts when the project becomes aware of an applicable advisory. Exploitable public-facing issues take precedence over severity labels.

## Release procedure

1. Run `dotnet list package --vulnerable --include-transitive`.
2. Review container/base-image advisories through the selected registry or image scanner.
3. Update direct dependencies and lock/restore artifacts as appropriate.
4. Build and run targeted regression tests after the update.
5. Record each unresolved item in the release notes or security exception register.

## Exception record

An exception must state: package/image, installed version, advisory identifier, severity, affected Sentinel surface, exploitability assessment, compensating control, owner, approval date, expiry/review date and planned remediation version. Exceptions expire; they must not become permanent silently.

## Current verification — 17 September 2026

`dotnet list Sentinel/Sentinel.csproj package --vulnerable --include-transitive` completed with no vulnerable packages from the configured sources. The former low-severity `NuGet.Packaging` and `NuGet.Protocol` 6.12.1 advisories were carried by the scaffolding-only `Microsoft.VisualStudio.Web.CodeGeneration.Design` package. That unused project dependency was removed rather than shipped with Sentinel.

Repeat the scan before every public release and after any package update.
