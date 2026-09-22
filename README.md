<div align="center">
  <a href="https://sentinelsurveillance.app">
    <img src="https://raw.githubusercontent.com/christianpeut95/Sentinel/master/Sentinel/wwwroot/design/sentinel-hz-w300-1024px%20(1).png" alt="Sentinel" width="480" />
  </a>
  <br /><br />

  [![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
  [![License](https://img.shields.io/badge/license-GPLv3-blue)](LICENSE.md)
  [![Status](https://img.shields.io/badge/status-beta-blue)]()

  <br />

  **[Visit Homepage](https://sentinelsurveillance.app)**
</div>

---

## Live Demo

A publicly hosted demo is available at **https://demo.sentinelsurveillance.app**

> Demo credentials are pre-loaded. See the [Demo Mode](#demo-mode) section for login details.

---

## Overview

**Sentinel** is an infectious disease surveillance platform for epidemiologists and public health units. It provides a configurable system for case management, outbreak investigation, and contact tracing without requiring code changes for most surveillance requirements.

**Technology:** ASP.NET Core (.NET 10), Entity Framework Core 9, SQL Server 2019+, Blazor

**Status:** v1.0.0 beta 1 — suitable for evaluation and
organisation-led deployment validation.

> **Note:** Sentinel uses SurveyJS Form Library (MIT) to render surveys and
> SurveyJS Creator for visual survey design. The Creator has separate vendor
> terms; see [third-party release conditions](docs/licensing.md) before
> distributing a Sentinel build.

---

## At a Glance

### 01 · Lab Integration

**HL7 lab feeds** — Automated processing of HL7 v2.x messages from file drop. Patient matching uses configurable strategies (exact match, fuzzy match, probabilistic). Results parsed with LOINC and SNOMED terminologies. Epidemiologist controls which data enters automatically vs. requires manual review.

- HL7 v2.x message parsing (ORM, ORU, ADT)
- LOINC code mapping for test types
- SNOMED CT mapping for organisms and results
- Configurable auto-match rules with confidence thresholds
- Manual override and full audit trail
- Duplicate result detection

### 02 · Classification

**Automated case definitions** — Machine-readable case definitions evaluate in the background against lab results, symptoms, and exposure data. Epidemiologist configures whether they apply automatically, flag for review, or remain manual. Full evaluation history with override capability and reason tracking.

- Background evaluation engine with configurable schedules
- Manual override with audit and reason codes
- Historical evaluation tracking (who, when, what changed)
- Confidence scoring and threshold configuration
- Laboratory-confirmed, probable, and suspect classifications
- Differential diagnosis support

### 03 · Surveys

**Surveys & data mapping** — Dynamic surveys with conditional logic (show/hide, enable/disable, validation) and full version control. Survey responses map bidirectionally to case and patient fields: trusted fields save automatically, ambiguous ones queue for human review. Add questions without code changes or database migrations.

- Conditional logic engine (show/hide, validation, skip patterns)
- Version control with change tracking and rollback
- Field mapping to structured data (auto vs. review queue)
- Free-text narrative entry alongside structured data
- Survey branching based on previous responses
- Multi-language support (planned)
- PDF export of completed surveys

### 04 · Outbreaks

**Outbreaks & contact tracing** — Hierarchical outbreak structures (outbreak to sub-outbreak), interview queues with assignment and status tracking, supervisor dashboards for workload monitoring, bulk contact import from CSV, contact-to-case conversion, and interactive mind-map visualization of case-to-case and case-to-contact relationships.

- Hierarchical outbreak linking (parent-child relationships)
- Interview queue management with assignment rules
- Contact relationship mapping (household, workplace, social)
- Bulk operations: CSV import, mass assign, batch convert
- Generation tracking (index to generation 1 to generation 2)
- Exposure windows and infectious period calculations
- Network graph visualization of transmission chains

---

## Design Principles

### Epidemiologist-first configuration

Surveillance teams configure diseases, case definitions, surveys, geography,
demographics and reports through the application. Routine surveillance changes
should not require source-code or database-schema changes.

### Operational simplicity

Sentinel is designed for time-critical public-health operations. Interfaces and
workflows aim to keep data entry, investigation and reassignment understandable
when teams scale quickly or processes change.

---

## Screenshots

<table>
  <tr>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/bf61e9a7-4e81-477d-96bc-26bbdc399661" alt="Dashboard" width="100%" />
      <br/><sub><b>Dashboard</b></sub>
    </td>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/791483f9-8225-4d12-8bd1-bd0e84c691b7" alt="Case" width="100%" />
      <br/><sub><b>Case Management</b></sub>
    </td>
  </tr>
  <tr>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/79358351-12d5-4b7a-a740-a6157f4797a2" alt="Custom Fields" width="100%" />
      <br/><sub><b>Custom Fields</b></sub>
    </td>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/bfdc1a12-77a7-4cd2-b59b-22785e63f4d7" alt="Survey Field Mappings" width="100%" />
      <br/><sub><b>Survey Field Mappings</b></sub>
    </td>
  </tr>
  <tr>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/b1c40eb6-8ed0-4c04-bb8c-beaab8bd2941" alt="Review Queue" width="100%" />
      <br/><sub><b>Review Queue</b></sub>
    </td>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/8e5f5031-5772-458f-b247-ba52438adefd" alt="Report Builder" width="100%" />
      <br/><sub><b>Report Builder</b></sub>
    </td>
  </tr>
  <tr>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/24557495-ff35-47b5-b7b6-ff58853765e8" alt="Outbreak" width="100%" />
      <br/><sub><b>Outbreak Investigation</b></sub>
    </td>
    <td align="center" width="50%">
      <img src="https://github.com/user-attachments/assets/2f8db7c9-a3f6-4956-9f5f-0633e0c031f3" alt="Demo Login" width="100%" />
      <br/><sub><b>Demo Login</b></sub>
    </td>
  </tr>
</table>

---

## User Interface Design

Sentinel uses a data-forward interface designed for high-density information and
rapid decision-making. The design system uses Geist Sans and Geist Mono, a 4 px
spacing scale, and defined status colours for outbreak, watch, clear and
informational states. The detailed reference is available at
[UI Guidelines](Sentinel/wwwroot/design/UI%20Guidelines.html).

---

## Current Beta Features

### Core Surveillance

- **Patient and Case Management** — Comprehensive patient records with duplicate detection algorithms (Soundex, Levenshtein distance), merge workflows with field-by-field comparison, and full audit history of all changes
- **Multi-Disease Surveillance** — Unlimited diseases with custom fields, case definitions, notification requirements, and workflows configurable per disease
- **Laboratory Results** — Track test orders, results, specimen types, and lab identifiers with LOINC/SNOMED mapping
- **Symptoms, Exposures & Outcomes** — Record clinical presentation, epidemiological risk factors (travel, food, animal contact), hospitalisation dates, ICU admission, ventilation, and case outcomes (recovered, died, lost to follow-up)
- **Dynamic Custom Fields** — Add disease-specific or organisation-specific fields through the UI without code changes or database schema migrations (supports text, number, date, dropdown, checkbox, multi-select)

### Surveys & Data Collection

- **Integrated Survey System** — Create structured questionnaires with conditional logic (show/hide, enable/disable), skip patterns, validation rules, and full version control
- **Survey-to-Field Mapping** — Bidirectional mapping between survey questions and case/patient fields with confidence levels: auto-save trusted fields, queue ambiguous responses for review
- **Narrative Entry** — Capture unstructured interview notes and clinical narratives alongside structured data
- **Version Control** — Track survey changes over time, maintain historical survey data without breaking existing responses, rollback to previous versions

### Classification & Automation

- **Automated Case Definitions** — Machine-readable definitions evaluated in background against labs, symptoms, exposures, and demographics
- **Review Workflow** — Epidemiologist decides whether classifications apply automatically, flag for review, or stay manual; configure per disease and per definition
- **Audit Trail** — Full history of each classification decision with timestamp, user, confidence score, and override reason
- **Confidence Scoring** — Configurable thresholds for automatic vs. manual classification

### Outbreak Investigation

- **Hierarchical Outbreaks** — Nest sub-outbreaks, link cases to multiple outbreaks, track outbreak status and resolution
- **Contact Tracing** — Interview queues with assignment rules, supervisor dashboards for workload monitoring, contact-to-case conversion with data carryover
- **Relationship Mapping** — Visualise case-to-case and case-to-contact relationships in interactive network graph with generation tracking
- **Bulk Operations** — CSV import for mass contact creation, batch assignment to interviewers, bulk status updates

### Reporting & Analytics

- **No-Code Report Builder** — Create line listings with custom columns, filters, and sorting without writing SQL; save and share report definitions
- **Optional Pivot Table Analytics** — Interactive data slicing with drill-down using WebDataRocks after organisation and user acceptance of its separate vendor licence
- **Custom Dashboards** — Role-specific views with KPIs, case counts, and filtered lists
- **Scheduled Reports** — Planned: automated report generation and email distribution

### Security & Governance

- **Role-Based Access Control** — Granular permissions for case creation, editing, viewing, deletion, and exporting; configure at role level
- **Disease-Based Restrictions** — Restrict users to specific diseases or disease groups (e.g., STI officers only see STI cases)
- **Field-Level Permissions** — Control visibility and editability of sensitive fields per role (e.g., hide patient name from contact tracers)
- **Optional Two-Factor Authentication** — Users can enrol a TOTP-compatible authenticator app with a locally generated QR code and one-time recovery codes; see [authentication-abuse controls](docs/security/authentication-abuse-controls.md) for setup and recovery guidance
- **Audit Logging** — Track all changes to cases, patients, outbreaks, and system configuration with timestamp, user, old/new values

### Geographic Features

- **Address Geocoding** — Automatic latitude/longitude lookup using Nominatim (free, rate-limited) or Google Maps API (paid, accurate)
- **Jurisdiction Assignment** — Automatically assign cases to health units based on address geocoding and jurisdiction boundaries
- **Map Visualisations** — Planned: plot cases and outbreaks spatially with heat maps and cluster detection

### Workflow Automation

- **Task Management** — Create, assign, and track follow-up tasks for cases and contacts with due dates, priorities, and completion tracking
- **Automated Task Creation** — Trigger tasks on case status changes, survey completion, classification changes, or custom rules
- **Interview Workflows** — Guide contact tracers through structured interview processes with task checklists and progress tracking

### Data Import & Integration

- **HL7 Lab Feeds** — Ingest HL7 v2.x messages (ORM, ORU, ADT) from file drops, auto-match to patients using configurable strategies (exact, fuzzy, probabilistic), parse with LOINC and SNOMED
- **Bulk Contact Import** — CSV upload for mass contact creation during outbreak response with field mapping and validation
- **Manual Data Entry** — Full UI for case and patient creation when automation isn't available
- **Public API Integration** — Planned: documented REST API for third-party system integration

---

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
# Then generate separate SQL Server administrator and application passwords.
# Keep the generated values to letters, digits and ! for safe use in both
# connection strings and SQL bootstrap scripts.
SQL_SA_PASSWORD="$(openssl rand -hex 32)Aa1!"
SQL_APP_PASSWORD="$(openssl rand -hex 32)Bb2!"
sed -i "s|^SQL_SA_PASSWORD=.*|SQL_SA_PASSWORD=$SQL_SA_PASSWORD|" .env
sed -i "s|^SQL_APP_PASSWORD=.*|SQL_APP_PASSWORD=$SQL_APP_PASSWORD|" .env
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

The application and bundled database are available only on the Docker network. The proxy is the sole public service and publishes ports 80 and 443. A short-lived bootstrap container creates the `SentinelDb` database and its database-scoped `sentinel_app` login; Sentinel does not connect as SQL Server `sa`. Named volumes retain database data, protected files, Caddy certificates and ASP.NET Core Data Protection keys across restarts.

### Pre-built Docker Image

```bash
docker pull christianpeut/sentinel:latest
```

### Environment Variables

| Variable | Description | Default |
|---|---|---|
| `SQL_SA_PASSWORD` | Required SQL Server administrator password, used only for SQL Server and the bootstrap job | None |
| `SQL_APP_PASSWORD` | Required password for Sentinel's database-scoped `sentinel_app` login | None |
| `SENTINEL_HOSTNAME` | Public DNS hostname for the Sentinel site | None |
| `ACME_EMAIL` | Email address used by Caddy for certificate notices | None |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `Demo__EnableDemoUsers` | Seed demo accounts | `false` |
| `Demo__EnableDemoMode` | Enable demo mode (test data generator) | `false` |
| `Demo__ShowDemoBanner` | Show demo banner in UI | `false` |

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

## Demo Mode

Sentinel includes a built-in demo mode that seeds pre-configured users with different roles for evaluation purposes and enables additional UI and tooling for demonstrations.

### Demo Configuration Variables

| Key | Type | Default | Description |
|---|---|---|---|
| `Demo:EnableDemoUsers` | `bool` | `false` | Seeds the five demo user accounts on startup. Shows one-click login buttons on the sign-in page. |
| `Demo:EnableDemoMode` | `bool` | `false` | Enables demo mode globally. Appends `(Demo)` to the version string throughout the UI and enables the test data generator tool. |
| `Demo:ShowDemoBanner` | `bool` | `false` | Displays a red "DEMO ENVIRONMENT" banner fixed to the top of every page to make it obvious the instance is for demonstration purposes. |

### Enabling Demo Mode

**Option 1 — `appsettings.json` (local/development)**

Add the `Demo` block to your `appsettings.json`:

```json
"Demo": {
  "EnableDemoUsers": true,
  "EnableDemoMode": true,
  "ShowDemoBanner": true
}
```

**Option 2 — `appsettings.Demo.json` (recommended for demo environments)**

The repository ships with a pre-configured `appsettings.Demo.json`. Set `ASPNETCORE_ENVIRONMENT=Demo` and all three flags are already enabled:

```json
"Demo": {
  "EnableDemoUsers": true,
  "EnableDemoMode": true,
  "ShowDemoBanner": true
}
```

Start the application with:
```bash
ASPNETCORE_ENVIRONMENT=Demo dotnet run
```

**Option 3 — Docker environment variables**

```env
ASPNETCORE_ENVIRONMENT=Demo
```

Or set individual variables:

```env
Demo__EnableDemoUsers=true
Demo__EnableDemoMode=true
Demo__ShowDemoBanner=true
```

### What Each Variable Does

**`EnableDemoUsers`**
- On startup, seeds five demo accounts with pre-set passwords if they do not already exist
- Replaces the standard login form with one-click login buttons for each demo account
- Has no effect on standard installs (accounts are never created unless this is `true`)

**`EnableDemoMode`**
- Appends `(Demo)` to the version string shown in the sidebar, login page and About page
- Unlocks the **Test Data Generator** tool (`/Tools/TestDataGenerator`) for generating synthetic patients, cases and contacts

**`ShowDemoBanner`**
- Renders a prominent orange/red banner fixed to the top of every page with the text **"DEMO ENVIRONMENT"**
- Useful when sharing a demo link publicly so evaluators are aware they are on a non-production instance

### Demo Accounts

| Name | Email | Password | Role |
|---|---|---|---|
| Emma Thompson | manager@sentinel-demo.com | `Demo123!@#Manager` | Surveillance Manager |
| Isabella Chen | officer@sentinel-demo.com | `Demo123!@#Officer` | Surveillance Officer |
| Emma Rodriguez | tracer@sentinel-demo.com | `Demo123!@#Tracer` | Contact Tracer |
| James Wilson | supervisor@sentinel-demo.com | `Demo123!@#Supervisor` | Surveillance Manager |
| Megge Taylor | stiofficer@sentinel-demo.com | `Demo123!@#STI` | Surveillance Officer |

> Demo users are only created when `Demo:EnableDemoUsers` is `true`. They are not created on standard installs.

---

## Configuration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SentinelDB;Trusted_Connection=True;"
  },
  "Organization": {
    "Name": "Your Organization",
    "CountryCode": "AU",
    "TimeZoneId": "Australia/Adelaide",
    "Locale": "en-AU"
  },
  "Geocoding": {
    "Provider": "Nominatim",
    "ApiKey": ""
  }
}
```

### Geocoding Providers

| Provider | Notes |
|---|---|
| Nominatim | Free, no API key, rate limited (~1 req/sec) |
| Google | Requires a server-only key for Geocoding API v4 and Places API (New), plus a separately restricted browser key for Maps JavaScript autocomplete. |

---

## Status

**v1.0.0 beta 1 — First Public Beta**

### Stable
- Patient and case management
- Duplicate detection and merging
- HL7 laboratory ingestion, matching and review workflows
- Configurable case definitions and automatic case evaluation
- Survey system with field mapping and versioning
- Task management and interview workflows
- Outbreak investigation and contact tracing
- Report builder (line listing, pivot tables)
- Role-based and disease-hierarchy access control
- Bulk contact operations

### Known Limitations
- Each deployment must complete its environment-specific HTTPS, DNS, backup,
  access-control and operational verification before handling live data.
- UI polish and accessibility improvements continue through the beta period.
- Duplicate detection needs performance testing against each organisation's
  expected data volume.

---

## Roadmap

### Near-Term
- Vaccination module with immunisation tracking
- Enhanced charting and visualizations
- LDAP/Active Directory integration
- Performance tuning (queries, caching)
- Genomic data linkage support

---

## Contributing

Contributions are welcome.

### How to Help
- **Report bugs** — Create an issue with reproduction steps
- **Suggest features** — Open a discussion with use case
- **Submit code** — Open a focused pull request against the repository's
  default branch

### Before Submitting Code
- Follow the [contribution guide](CONTRIBUTING.md), including local build and
  test checks.
- Update documentation when behaviour, configuration or security controls
  change.

---

## Documentation

The maintained in-repository documentation is indexed in
[docs/README.md](docs/README.md). Product guidance is also available in
[Sentinel Notion](https://www.notion.so/Sentinel-31b00376e60880bd9f11f04959729498).

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

### Third-Party Licenses

| Library | License | Notes |
|---|---|---|
| SurveyJS Form Library | MIT | Runtime form-rendering component |
| SurveyJS Creator | Commercial / operator-managed | Not included in the standard source or image. A demo override can mount separately obtained assets; see [deployment guidance](docs/deployment/survey-designer.md). |
| ClosedXML 0.105.1 | MIT | Used for occupation-reference-data Excel import |
| QRCoder 1.8.0 | MIT | Generates authenticator-enrolment QR codes locally; no QR data is sent to an external service. |
| WebDataRocks | Separate vendor EULA | Optional component: disabled by default; organisation and user acceptance are recorded before its CDN files load. See [integration guidance](docs/licensing-webdatarocks.md). |
| ASP.NET Core / EF Core | MIT | Free |
| Bootstrap / Bootstrap Icons | MIT | Free |

---

## Acknowledgements

Built with: ASP.NET Core, Entity Framework Core, SurveyJS, AG Grid, Bootstrap, and optionally WebDataRocks for accepted interactive pivot reporting.

Design system typefaces: [Geist Sans](https://vercel.com/font) and Geist Mono by Vercel.


