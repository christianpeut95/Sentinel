<div align="center">
  <img src="wwwroot/design/sentinel-hz-w300-1024px (1).png" alt="Sentinel" width="480" />
  <br /><br />
  <strong>Watchful infrastructure for public health</strong>
  <br /><br />

  [![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
  [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
  [![Status](https://img.shields.io/badge/status-alpha-orange)]()
</div>

---

## Live Demo

A publicly hosted demo is available at **https://demo.sentinelsurveillance.app**

> Demo credentials are pre-loaded. See the [Demo Mode](#demo-mode) section for login details.

---

## Overview

**Sentinel** is an infectious disease surveillance platform for epidemiologists and public health units. It provides a configurable system for case management, outbreak investigation, and contact tracing without requiring code changes for most surveillance requirements.

**Technology:** ASP.NET Core (.NET 10), Entity Framework Core 9, SQL Server 2019+, Blazor

**Status:** Alpha — active development, suitable for evaluation and experimentation.

> **Note:** Sentinel uses **SurveyJS** for the survey builder component. Production use of SurveyJS requires a developer licence (~$999/year per developer).

---

## Core Capabilities

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

**Outbreaks & contact tracing** — Hierarchical outbreak structures (outbreak ? sub-outbreak), interview queues with assignment and status tracking, supervisor dashboards for workload monitoring, bulk contact import from CSV, contact-to-case conversion, and interactive mind-map visualization of case-to-case and case-to-contact relationships.

- Hierarchical outbreak linking (parent-child relationships)
- Interview queue management with assignment rules
- Contact relationship mapping (household, workplace, social)
- Bulk operations: CSV import, mass assign, batch convert
- Generation tracking (index ? generation 1 ? generation 2)
- Exposure windows and infectious period calculations
- Network graph visualization of transmission chains

---

## Design Principles

### 01 · Epidemiologist-first

**The surveillance team configures the system, not the developers.**

Geographies, demographics, diseases, case definitions, surveys, and reports are configurable through the interface. No code changes, no schema migrations. Adding a question to a survey ("Did the case eat bean sprouts?") requires zero developer involvement.

### 02 · Simplicity

**Scaling to 100 contact tracers overnight leaves no time to retrain.**

The design target is end-users in large operations. A changed field or relabelled question does not require training: the contact tracer clicks next, and the new question appears. The system accommodates the user, not the reverse.

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

## UI Design System

Sentinel follows a clean, data-forward design system optimized for high-density information display and rapid decision-making.

### Design Tokens

**Color Palette**
- **Forest** (#0C2A20) — Primary dark, body text
- **Signal** (#3DD598) — Brand accent, "healthy" indicator
- **Moss** (#1E5D44) — Secondary dark
- **Bone** (#F5F3EC) — Page background
- **Paper** (#ECEAE1) — App background
- **Chalk** (#FBFAF5) — Card surface

**Status Colors**
- **Outbreak** (#E04D2B) — Critical/confirmed outbreak
- **Watch** (#E0A43A) — Under surveillance
- **Clear** (#3DD598) — Resolved/healthy
- **Info** (#6B8CF5) — Informational

**Typography**
- **Geist Sans** — All UI text (weights 300–700)
- **Geist Mono** — Case IDs, timestamps, numeric data, labels

**Spacing** — 4px base grid (space-1 through space-20)  
**Shadows** — 4 elevation levels (xs, sm, md, lg)  
**Radius** — 5 values (2px, 4px, 6px, 8px, 12px, 999px)

Full design system documentation: [wwwroot/design/UI Guidelines.html](wwwroot/design/UI%20Guidelines.html)

---
## Key Features

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
- **Pivot Table Analytics** — Interactive data slicing with drill-down using WebDataRocks component
- **Custom Dashboards** — Role-specific views with KPIs, case counts, and filtered lists
- **Scheduled Reports** — (Planned) Automated report generation and email distribution

### Security & Governance

- **Role-Based Access Control** — Granular permissions for case creation, editing, viewing, deletion, and exporting; configure at role level
- **Disease-Based Restrictions** — Restrict users to specific diseases or disease groups (e.g., STI officers only see STI cases)
- **Field-Level Permissions** — Control visibility and editability of sensitive fields per role (e.g., hide patient name from contact tracers)
- **Audit Logging** — Track all changes to cases, patients, outbreaks, and system configuration with timestamp, user, old/new values

### Geographic Features

- **Address Geocoding** — Automatic latitude/longitude lookup using Nominatim (free, rate-limited) or Google Maps API (paid, accurate)
- **Jurisdiction Assignment** — Automatically assign cases to health units based on address geocoding and jurisdiction boundaries
- **Map Visualisations** — (Planned) Plot cases and outbreaks spatially with heat maps and cluster detection

### Workflow Automation

- **Task Management** — Create, assign, and track follow-up tasks for cases and contacts with due dates, priorities, and completion tracking
- **Automated Task Creation** — Trigger tasks on case status changes, survey completion, classification changes, or custom rules
- **Interview Workflows** — Guide contact tracers through structured interview processes with task checklists and progress tracking

### Data Import & Integration

- **HL7 Lab Feeds** — Ingest HL7 v2.x messages (ORM, ORU, ADT) from file drops, auto-match to patients using configurable strategies (exact, fuzzy, probabilistic), parse with LOINC and SNOMED
- **Bulk Contact Import** — CSV upload for mass contact creation during outbreak response with field mapping and validation
- **Manual Data Entry** — Full UI for case and patient creation when automation isn't available
- **API Integration** — (Planned) RESTful API for third-party system integration

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
- Navigate to `/Identity/Account/Register`
- First registered user is automatically assigned Admin role

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
# Edit .env and set SQL_PASSWORD

# Start stack
docker compose up -d

# Access at http://localhost:8080
```

**Stack Components:**
- `sentinel-app` — ASP.NET Core application
- `sentinel-db` — SQL Server 2022

Migrations run automatically on first startup.

### Pre-built Docker Image

```bash
docker pull christianpeut/sentinel:latest
```

### Environment Variables

| Variable | Description | Default |
|---|---|---|
| `SQL_PASSWORD` | SQL Server SA password | `YourStrong!Password123` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `Demo__EnableDemoUsers` | Seed demo accounts | `false` |
| `Demo__EnableDemoMode` | Enable demo mode (test data generator) | `false` |
| `Demo__ShowDemoBanner` | Show demo banner in UI | `false` |

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
    "Timezone": "UTC"
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
| Google | Requires Google Maps API key, paid service |

---

## Status

**Alpha — Active Development**

### Stable
- Patient and case management
- Duplicate detection and merging
- Survey system with field mapping and versioning
- Task management and interview workflows
- Outbreak investigation and contact tracing
- Report builder (line listing, pivot tables)
- Disease-based access control
- Bulk contact operations

### Known Limitations
- Permissions audit incomplete
- UI polish needed in some areas
- Performance optimization needed for duplicate detection on large datasets

---

## Roadmap

### Near-Term
- HL7 support for lab result import
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
- **Submit code** — Fork ? branch ? PR to `develop`

### Before Submitting Code
- Run `dotnet build` with no errors
- Follow coding conventions in [`.github/copilot-instructions.md`](.github/copilot-instructions.md)
- Update documentation if needed

---

## Documentation

Full documentation: [Sentinel Notion](https://www.notion.so/Sentinel-31b00376e60880bd9f11f04959729498)

---

## License

**MIT License** — See [`LICENSE`](LICENSE) file for full terms.

You are free to use, modify, and distribute this software for any purpose, including commercial use.

### Third-Party Licenses

| Library | License | Notes |
|---|---|---|
| SurveyJS | Commercial | Requires paid developer licence (~$999/year per developer) for production |
| AG Grid Community | MIT | Free |
| WebDataRocks | Free (non-commercial) | Attribution required |
| ASP.NET Core / EF Core | MIT | Free |
| Bootstrap / Bootstrap Icons | MIT | Free |

---

## Acknowledgements

Built with: ASP.NET Core, Entity Framework Core, SurveyJS, AG Grid, WebDataRocks, Bootstrap.

Design system typefaces: [Geist Sans](https://vercel.com/font) and Geist Mono by Vercel.

---

*Built with love for public health*
