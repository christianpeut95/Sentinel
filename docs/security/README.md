# Sentinel security documentation

This directory records Sentinel's security controls, operating assumptions and release checks. It is part of the source repository and should be reviewed with application changes. It must never contain credentials, setup tokens, patient information, raw production logs or complete telemetry payloads. Initial setup tokens are generated into protected operator storage, never application logs, and are deleted once setup completes.

## Documents

| Document | Purpose | Review point |
|---|---|---|
| [Input validation rules](input-validation-rules.md) | Expected structure and server-side validation for high-risk input | New create/edit/import/API workflow |
| [Authentication-abuse controls](authentication-abuse-controls.md) | Password, lockout, reset, voluntary authenticator-app 2FA, rate-limit and session controls | Identity or rate-limit change |
| [Authorization and data access](authorization-and-data-access.md) | Permission, disease hierarchy and object-level access model | New page, endpoint, export or worker |
| [File handling](file-handling.md) | Upload, storage, download and import safety rules | New attachment/import feature |
| [Logging and telemetry](logging-and-telemetry.md) | Safe diagnostic logging and optional remote telemetry | Logging or telemetry change |
| [Production deployment](production-deployment.md) | Docker, HTTPS, proxy and operational deployment baseline | Each public deployment |
| [Dependency management](dependency-management.md) | Vulnerability monitoring, remediation timeframes and exceptions | Every release and dependency update |
| [OWASP ASVS Level 1 self-assessment](owasp-asvs-l1-assessment.md) | Current self-assessment position, supporting evidence and deployment checks | Every release and material security change |
| [V14.2.1 and V15.3.1 evidence](ASVS-V14.2.1-and-V15.3.1-evidence.md) | API-key URL handling and response/export contract evidence | Google integration, API or export change |

## Release security review

Before a public release, the maintainer should:

1. Complete the checks listed in the relevant documents above.
2. Run the ASVS regression scenarios using synthetic data and restricted-disease test users.
3. Run `dotnet list package --vulnerable --include-transitive` and record any accepted exception.
4. Confirm the production deployment uses HTTPS and does not publish Sentinel or SQL Server directly.
5. Review `git diff` for configuration, permission, logging and upload changes.

This documentation supports an internal OWASP ASVS 5.0 Level 1 self-assessment. It is not a certification or a substitute for an independent security assessment.
