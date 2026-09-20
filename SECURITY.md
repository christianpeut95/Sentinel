# Sentinel security policy

## Supported versions

| Version | Security updates |
|---|---|
| 0.9.x beta | Current public-beta release line |
| Earlier versions | Not supported |

## Reporting a vulnerability

Please do not disclose a suspected vulnerability in a public issue, discussion,
pull request or social-media post.

Use [GitHub private vulnerability reporting](https://github.com/christianpeut95/Sentinel/security/advisories/new)
for this repository. Maintainers should enable GitHub's **Private vulnerability
reporting** setting before public release. If the reporting form is unavailable,
contact the repository owner through GitHub and ask for a private reporting
channel; do not include exploit details in a public issue.

Include, where possible:

- a concise description and the affected Sentinel version;
- reproducible steps or a minimal proof of concept;
- the likely impact and any prerequisites; and
- a safe way to contact you for follow-up.

Use synthetic data only. Do not include patient information, credentials,
setup tokens, production configuration, complete logs, or API keys.

## Handling reports

Reports are assessed privately. Maintainers will validate the issue, determine
the affected versions, prepare a fix and coordinate disclosure as appropriate.
Public-beta support does not make a fixed response-time commitment.

## Security documentation

The repository's [security documentation](docs/security/README.md) records
the supported controls, release checks and the internal OWASP ASVS 5.0 Level 1
self-assessment. It is not an independent certification or a guarantee that a
particular installation is secure; each operator remains responsible for its
deployment, access control, backups and monitoring.
