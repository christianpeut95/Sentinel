# OWASP ASVS 5.0 Level 1 self-assessment

## Current position

This is Sentinel's internal self-assessment for the `1.0.0-beta.1` source tree,
reviewed on 20 September 2026. It is a source, configuration and regression-
test assessment. It is **not** an independent certification, penetration test,
or assurance that a particular organisation's deployment is secure.

| Status | Count |
|---|---:|
| Pass | 52 |
| Partial | 6 |
| Fail | 0 |
| Not applicable | 12 |

All six Partial requirements depend on an organisation's live deployment;
there are no remaining product-code requirements marked Partial or Fail.

## Scope and evidence

The assessment covers Razor Pages, MVC/API controllers, Minimal APIs, Identity,
authorisation and disease hierarchy access, EF Core filters, reporting,
uploads/downloads, Docker configuration, dependencies and first-party browser
code.

Evidence includes source review, configuration review, focused security
regression tests and the complete automated test suite. The 20 September 2026
run passed **512 tests with 0 failures**.

Supporting records:

- [Authentication and abuse controls](authentication-abuse-controls.md)
- [Authorisation and data access](authorization-and-data-access.md)
- [File handling](file-handling.md)
- [Logging and telemetry](logging-and-telemetry.md)
- [Input validation rules](input-validation-rules.md)
- [Dependency management](dependency-management.md)
- [V14.2.1 and V15.3.1 implementation evidence](ASVS-V14.2.1-and-V15.3.1-evidence.md)
- [Production deployment security baseline](production-deployment.md)

The working spreadsheet is a review artefact rather than the canonical
repository record. A dated copy may be retained with release records or
published as a GitHub release asset when it contains no local paths, test data,
credentials, tokens, hostnames or raw logs.

## Deployment evidence required

Each public deployment should capture its own evidence for these requirements:

| ASVS | Required operational check |
|---|---|
| V3.4.1 | Confirm `Strict-Transport-Security` has a maximum age of at least one year. |
| V4.4.1 | Confirm SignalR/WebSocket connections use `wss://`. |
| V11.3.2 | Confirm Data Protection keys have appropriate host or volume permissions and protection. |
| V12.1.1 | Retain a TLS scan showing only TLS 1.2 and TLS 1.3. |
| V12.2.1 | Confirm public access is limited to ports 80/443 and HTTP redirects to HTTPS. |
| V12.2.2 | Confirm a publicly trusted certificate chain and renewal on the configured hostname. |

The [production deployment guide](production-deployment.md) contains the
corresponding Docker and operational baseline.

## Release use

Before a public release, review this document alongside the security documents,
run the regression suite with synthetic data, scan dependencies and review
security-relevant changes. Before each public deployment, complete the
deployment-evidence checks above.
