# Changelog

All notable user-facing changes are recorded here. Sentinel follows
[Semantic Versioning](https://semver.org/).

## [0.9.0-beta] - Unreleased

Initial public-beta release preparation.

### Highlights

- Configurable infectious-disease surveillance workflows for cases, patients,
  laboratory results, surveys, tasks, outbreaks and reporting.
- HL7 v2.x laboratory-message processing with configurable mappings and case
  definition evaluation.
- Disease-hierarchy access controls, protected file storage and audited
  object-level authorization.
- Docker deployment support with HTTPS reverse-proxy and persistent data
  volumes.

### Changed

- Sentinel-owned source code is now licensed under GPL-3.0-or-later. See
  [LICENSE.md](LICENSE.md) and
  [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
- Replaced EPPlus with MIT-licensed ClosedXML for the occupation Excel import.
- Added the third-party release policy and required WebDataRocks attribution.
- Excluded separately licensed SurveyJS Creator assets from the standard source
  archive and Docker image. A documented, operator-managed demo Compose override can
  mount operator-supplied assets when explicitly enabled.

### Security

- Security controls and the OWASP ASVS Level 1 self-assessment are documented
  in [docs/security](docs/security/README.md).
- Deployment-specific validation remains the responsibility of each
  installation operator; see
  [production deployment guidance](docs/security/production-deployment.md).

## Format

Future releases should add dated entries above this section under `Added`,
`Changed`, `Fixed`, `Security`, and `Removed` headings as appropriate.
