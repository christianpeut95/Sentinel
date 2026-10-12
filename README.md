<div align="center">
  <a href="https://sentinelsurveillance.app">
    <img src="https://raw.githubusercontent.com/christianpeut95/Sentinel/master/docs/design/sentinel-wordmark.png" alt="Sentinel" width="480" />
  </a>
  <br /><br />

  [![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
  [![License](https://img.shields.io/badge/license-GPLv3-blue)](LICENSE.md)
  [![Status](https://img.shields.io/badge/status-beta-blue)](https://github.com/christianpeut95/Sentinel/releases)
  <br /><br />

  **[Website](https://sentinelsurveillance.app) · [Live demo](https://demo.sentinelsurveillance.app) · [Documentation](docs/README.md) · [Releases](https://github.com/christianpeut95/Sentinel/releases)**
</div>

---

Sentinel is a configurable infectious-disease surveillance platform for public
health teams. It supports case and contact management, laboratory integration,
case definitions, surveys, outbreak investigation, workflows and reporting.

The project is at **v1.0.0 beta 1**. It is suitable for evaluation and
organisation-led deployment validation. Deployments must complete their own
security, privacy, operational and clinical-governance checks before handling
live data.

The [Sentinel website](https://sentinelsurveillance.app) contains the current
feature list, roadmap and screenshots.

## Installation tutorial

[![Watch the Sentinel installation tutorial](https://img.youtube.com/vi/zMDoHE4Zt-4/maxresdefault.jpg)](https://www.youtube.com/watch?v=zMDoHE4Zt-4&t=1s)

Watch the installation tutorial for a guided local evaluation setup.

## What it does

- Manages patients, cases, laboratory results, contacts, tasks and outbreaks.
- Ingests HL7 v2.x laboratory messages through a controlled file-drop workflow.
- Applies configurable case definitions and review workflows.
- Collects structured survey data and maps it to Sentinel records.
- Supports disease-hierarchy restrictions, role-based permissions and audit logs.
- Produces line lists and optional interactive pivot reports.

For screenshots, detailed feature descriptions and planned work, visit the
[Sentinel website](https://sentinelsurveillance.app).

## Design

Sentinel is configured through the application by surveillance teams. Routine
workflow changes should not require source-code or database-schema changes.
The [website](https://sentinelsurveillance.app) provides current screenshots;
the in-repository [UI guidelines](docs/design/ui-guidelines.html) are the
design reference for contributors.

## Installation

### Prerequisites
- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server 2019+** or SQL Server Express (free)
- **Visual Studio 2022** (18.5+) or VS Code with C# extension

### Quick Start

```bash
# Clone repository
git clone https://github.com/christianpeut95/Sentinel
cd Sentinel/Sentinel

# Restore packages
dotnet restore

# Update connection string in appsettings.json
# DefaultConnection: "Server=.;Database=SentinelDB;Trusted_Connection=True;"

# Apply migrations
dotnet ef database update

# Run
dotnet run
```

**First Run:**
- Database auto-seeds with lookup data and default permissions
- Sentinel creates a one-time setup token in the protected path configured by `Setup:TokenFilePath` (by default `App_Data/SentinelSetup/setup-token.txt`). It is never printed in application logs.
- Open `/Setup` and use that token to create the initial administrator. The token expires after 48 hours and is deleted after setup completes.
- Create later accounts from **Settings → Users** with an authorised administrator account

---

## Docker Deployment

Recommended deployment method using Docker Compose.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac) or Docker Engine (Linux)

### Using Docker Compose

```bash
# Clone repository
git clone https://github.com/christianpeut95/Sentinel
cd Sentinel/Sentinel

# Configure environment
cp .env.example .env
# Set DOCKERHUB_USERNAME, SENTINEL_HOSTNAME and ACME_EMAIL in .env.
# The hostname must already resolve to this server and ports 80 and 443 must
# be reachable from the internet for the TLS certificate to be issued.
# Then generate separate SQL Server administrator, application and backup passwords.
# Keep the generated values to letters, digits and ! for safe use in both
# connection strings and SQL bootstrap scripts.
SQL_SA_PASSWORD="$(openssl rand -hex 32)Aa1!"
SQL_APP_PASSWORD="$(openssl rand -hex 32)Bb2!"
SQL_BACKUP_PASSWORD="$(openssl rand -hex 32)Cc2!"
sed -i "s|^SQL_SA_PASSWORD=.*|SQL_SA_PASSWORD=$SQL_SA_PASSWORD|" .env
sed -i "s|^SQL_APP_PASSWORD=.*|SQL_APP_PASSWORD=$SQL_APP_PASSWORD|" .env
sed -i "s|^SQL_BACKUP_PASSWORD=.*|SQL_BACKUP_PASSWORD=$SQL_BACKUP_PASSWORD|" .env
chmod 600 .env

# Start stack
docker compose up -d

# Retrieve the one-time setup token. It is not written to Docker logs.
docker compose exec sentinel-web cat /var/lib/sentinel/setup/setup-token.txt

# Access at https://<SENTINEL_HOSTNAME>
```

Open `https://<SENTINEL_HOSTNAME>/Setup` and enter the displayed token to create
the initial administrator. Sentinel deletes the token file once setup succeeds.
If setup is still incomplete and the token expires or its protected file is
lost, restart `sentinel-web`; Sentinel creates a replacement token and writes
only it to the protected setup volume.

**Stack Components:**
- `sentinel-proxy` — Caddy reverse proxy, automatic HTTPS certificate and HTTP-to-HTTPS redirect
- `sentinel-app` — ASP.NET Core application, private to the Docker network
- `sentinel-db` — SQL Server 2022
- `sentinel-db-init` — short-lived database/login bootstrap job

Migrations run automatically on first startup.

The application and bundled database are available only on the Docker network. The proxy is the sole public service and publishes ports 80 and 443. A short-lived bootstrap container creates the `SentinelDb` database, its database-scoped `sentinel_app` login, and the separate `sentinel_backup` login. Sentinel does not connect as SQL Server `sa`. The backup login is limited to creating backups of `SentinelDb`; it is not used for routine application requests. Named volumes retain database data, protected files, backup files, Caddy certificates and ASP.NET Core Data Protection keys across restarts.

### Pre-built Docker Image

```bash
docker pull christianpeut/sentinel:1.0.0-beta.1
```

`latest` tracks the current beta build. For a reproducible deployment, retain
the versioned `VERSION_TAG` supplied in `.env.example` and upgrade deliberately
after reviewing the release notes.

### Optional visual survey designer

The standard image includes survey rendering and completion, but leaves the
separately licensed SurveyJS Creator visual designer disabled. For a Windows
Docker Desktop trial, an opt-in helper can download the matching Creator assets
directly from their public package source, create a private local folder, and
set `SURVEYJS_CREATOR_ASSETS_DIR` in `.env`:

```powershell
.\Scripts\Enable-SurveyDesigner.ps1 -AcceptSurveyJsCreatorTerms
```

Review the [SurveyJS Creator licensing information](https://surveyjs.io/survey-creator/documentation/overview)
before using the helper. It does not add Creator assets to the Sentinel image or
repository. See [optional SurveyJS Creator deployment](docs/deployment/survey-designer.md)
for the Docker override command and non-Windows deployment instructions.

### Environment Variables

| Variable | Description | Default |
|---|---|---|
| `SQL_SA_PASSWORD` | Required SQL Server administrator password, used only for SQL Server and the bootstrap job | None |
| `SQL_APP_PASSWORD` | Required password for Sentinel's database-scoped `sentinel_app` login | None |
| `SQL_BACKUP_PASSWORD` | Required password for the dedicated `sentinel_backup` login, used only to create database backups | None |
| `SENTINEL_HOSTNAME` | Public DNS hostname for the Sentinel site | None |
| `ACME_EMAIL` | Email address used by Caddy for certificate notices | None |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `Demo__EnableDemoUsers` | Seed demo accounts | `false` |
| `Demo__EnableDemoMode` | Enable demo mode (test data generator) | `false` |
| `Demo__ShowDemoBanner` | Show demo banner in UI | `false` |

### Database backups

The Docker deployment automatically configures a separate `sentinel_backup`
database login and a private shared backup volume. Sentinel writes backup files
through `/var/lib/sentinel/backups`; SQL Server writes the same files through
its own `/var/opt/mssql/backups` mount. Do not grant backup privileges to
`sentinel_app` or use the SQL Server `sa` credential in Sentinel.

For an external or managed SQL Server, configure `ConnectionStrings:BackupConnection`
with a dedicated login that is scoped to the Sentinel database and has the SQL
Server `db_backupoperator` role. Configure `Backup:Path` for the Sentinel host
and `Backup:SqlServerPath` for the path visible to SQL Server. If that cannot be
provided, leave Sentinel's in-app backup feature unconfigured and use the
organisation's managed database backup service instead. Database restore is a
DBA operation and is disabled by default.

---

## Security documentation

Security controls, production deployment checks and release procedures are maintained in [docs/security](docs/security/README.md). These documents include an [OWASP ASVS 5.0 Level 1 self-assessment](docs/security/owasp-asvs-l1-assessment.md), input-validation rules, account-abuse controls, authorization/data access, safe file handling, logging/telemetry, dependency remediation and Docker HTTPS deployment.

### Voluntary two-factor authentication

Sentinel does not currently impose two-factor authentication organisation-wide.
Instead, a user can choose **User menu → Two-Factor Authentication**, scan the
locally generated authenticator-app QR code (or enter its setup key), verify a
six-digit TOTP code, and securely store the one-time recovery codes shown at
the end of enrolment. The setup key and QR code never leave Sentinel for an
external QR service. See the [authentication controls](docs/security/authentication-abuse-controls.md)
for recovery, reset and operational guidance.

---

## Optional feedback and remote diagnostics

Sentinel does **not** send feedback, usage information, or automatic error reports by default. During setup, an administrator may independently enable the feedback widget and the single **Anonymous Usage Statistics & Automatic Error Reports** option. Either choice can be withdrawn later in **Settings → Feedback & Bug Reports**.

When remote diagnostics are enabled, Sentinel sends only approved aggregate counts, semantic page identifiers, safe runtime characteristics, and coarse error fingerprints linked to the installation ID. It never sends patient data, laboratory results, survey answers, exception messages, stack traces, request content, credentials, or raw HL7 payloads. See the [logging and telemetry policy](docs/security/logging-and-telemetry.md) for the complete data boundary.

---

## Demo configuration

Use the public demo for exploration. To host a synthetic-data demonstration,
set `ASPNETCORE_ENVIRONMENT=Demo`; the supplied `appsettings.Demo.json` enables
demo users, the demo banner and the test-data generator. Demo accounts are never
created in a normal installation.

## Contributing

Contributions are welcome from anyone helping Sentinel become more useful,
understandable and trustworthy. That can mean trying an installation,
contributing public-health or HL7 expertise, improving documentation, reporting
a problem, or submitting code. See [ways of contributing to Sentinel](CONTRIBUTING.md).

---

## Documentation

The maintained in-repository documentation is indexed in
[docs/README.md](docs/README.md).

Maintainer release checks: [docs/releasing.md](docs/releasing.md)

---

## License

**GNU General Public License v3.0 or later** — Sentinel-owned source code is
licensed under GPL-3.0-or-later. See [LICENSE.md](LICENSE.md) for the complete
terms. You may use, modify and redistribute the covered source, including for
commercial purposes, subject to the GPL's reciprocal source-code obligations
when distributing a covered work.

Third-party components are not relicensed by Sentinel. Their terms, including
components requiring separate commercial licensing, are recorded in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md). A distributor must comply
with both Sentinel's GPL terms and the applicable third-party terms.


