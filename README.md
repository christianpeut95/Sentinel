# Sentinel

**Infectious Disease Surveillance and Outbreak Management Platform**

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![Status](https://img.shields.io/badge/status-alpha-orange)]()

---

## Live Demo

A publicly hosted demo is available at:

**http://sentinel-demo.ddns.net**

> Demo credentials are pre-loaded. See the [Demo Mode](#demo-mode) section for login details.

---

## Overview

**Sentinel** is a proof of concept platform for infectious disease surveillance, outbreak management and contact tracing. It aims to provide a hybrid platform somewhere between rigid data structures and a fully metadata-driven application. The goal is to accommodate most public health, surveillance and outbreak investigation requirements without the need for code changes or new feature builds. The platform is aimed at epidemiologists and public health units.

The project is currently in alpha release and is intended primarily for experimentation, development and discussion, with the goal of developing into a viable, production-ready platform.

**Technology:** ASP.NET Core (.NET 10), Entity Framework Core 9, SQL Server 2019+, Blazor

> **Note:** Sentinel uses **SurveyJS** for the survey builder component. Production use of SurveyJS requires purchase of a developer licence.

---

## Screenshots

> Screenshots coming soon. The live demo is available at http://sentinel-demo.ddns.net

---

## Key Features

- Patient and case management with duplicate detection and audit history
- Multi-disease surveillance
- Laboratory results, symptoms, exposures, hospitalisation and outcomes tracking
- Dynamic custom fields configurable without database changes
- Integrated survey system with conditional logic and versioning
- Task and workflow management for case follow-up, automated task creation for cases or contacts
- Outbreak investigation with case linking and contact tracing modes
- Interviewer and contact tracing supervisor interfaces
- No-code reporting, line listings and pivot table analytics
- Role-based security with field-level permissions and audit logging
- Disease-based access control
- Geospatial features including address geocoding and jurisdiction assignment
- Bulk contact creation via CSV import

---

## Installation

### Prerequisites
- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server 2019+** or SQL Server Express (free)
- **Visual Studio 2022** or VS Code with C# extension

### Quick Start

```bash
# 1. Clone repository
git clone https://github.com/christianpeut95/Sentinel
cd Sentinel/Sentinel

# 2. Restore packages
dotnet restore

# 3. Update connection string in appsettings.json

# 4. Apply migrations
dotnet ef database update

# 5. Run
dotnet run
```

**First Run:**
- Database auto-seeds with lookup data and default permissions
- Navigate to `/Identity/Account/Register` — the first registered user is automatically assigned the Admin role

---

## Docker Deployment

The recommended way to run Sentinel is via Docker Compose.

### Prerequisites
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (Windows/Mac) or Docker Engine (Linux)

### Using Docker Compose

A `docker-compose.yml` is included in the `Sentinel/` folder.

```bash
# 1. Clone the repository
git clone https://github.com/christianpeut95/Sentinel
cd Sentinel/Sentinel

# 2. Copy the example environment file and set your SQL password
cp .env.example .env

# 3. Start the stack
docker compose up -d

# 4. Open browser to http://localhost:8080
```

The stack starts two containers:
- `sentinel-app` — the ASP.NET Core application
- `sentinel-db` — SQL Server 2022

Migrations run automatically on first startup.

### Using the Pre-built Docker Image

```bash
docker pull christianpeut/sentinel:latest
```

### Environment Variables

| Variable | Description | Default |
|---|---|---|
| `SQL_PASSWORD` | SQL Server SA password | `YourStrong!Password123` |
| `ASPNETCORE_ENVIRONMENT` | Environment name | `Production` |
| `Demo__EnableDemoUsers` | Seed demo user accounts on startup | `false` |
| `Demo__EnableDemoMode` | Enable demo mode (version badge, test data generator) | `false` |
| `Demo__ShowDemoBanner` | Show the demo environment banner across the top of the UI | `false` |

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
| Nominatim | Free, no API key required, rate limited |
| Google | Requires a Google Maps API key |

---

## Current Status

**Alpha Release — Active Development**

### Stable Features
- Patient and case management
- Duplicate detection and merging
- Survey system with field mapping and versioning
- Task management and interview workflows
- Outbreak investigation and contact tracing
- Report builder with line listing and pivot tables
- Disease-based access control
- Bulk contact creation

### Known Limitations
- Permissions audit incomplete
- UI polish needed in some areas
- Performance optimisation needed for duplicate detection on large datasets

---

## Roadmap

### Near-Term
- HL7 support for laboratory result import
- Vaccination module with immunisation tracking
- Enhanced charting and visualisations
- LDAP/Active Directory integration
- Performance tuning (database queries, caching)
- Support for genomic data linkage

---

## Contributing

Contributions are welcome!

### How to Help
- **Report bugs** — Create an issue with reproduction steps
- **Suggest features** — Open a discussion with your use case
- **Submit code** — Fork, create a branch, submit a PR to `develop`

### Before Submitting Code
- Run `dotnet build` with no errors
- Follow the coding conventions in [.github/copilot-instructions.md](.github/copilot-instructions.md)
- Update documentation if needed

---

## Documentation

Full documentation available on [Sentinel Notion](https://www.notion.so/Sentinel-31b00376e60880bd9f11f04959729498)

---

## License

**MIT License** — See [LICENSE](LICENSE) file for full terms.

You are free to use, modify and distribute this software for any purpose, including commercial use, subject to the MIT licence conditions.

### Third-Party Licences

| Library | Licence | Notes |
|---|---|---|
| SurveyJS | Commercial | Requires a paid developer licence for production use (~$999/year per developer) |
| AG Grid Community | MIT | Free |
| WebDataRocks | Free (non-commercial) | Attribution required |
| ASP.NET Core / EF Core | MIT | Free |
| Bootstrap / Bootstrap Icons | MIT | Free |

---

## Acknowledgements

Built on top of excellent open-source work: ASP.NET Core, Entity Framework Core, SurveyJS, AG Grid, WebDataRocks, Bootstrap.

---

*Built with love for public health*
