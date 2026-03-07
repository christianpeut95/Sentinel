# Sentinel

**Infectious Disease Surveillance and Outbreak Management Platform**

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green)](LICENSE)

---

## Overview

**Sentinel** is a proof of concept platform for infectious disease surveillance, outbreak management and contact tracing. It aims to provide a hybrid platform somewhere between rigid data structures and complete metadata driven application. The goal for users is to have a complete data platform that can accomodate most public health, surveillance and outbreak investigation requirements without the need for code changes or new feature builds. The platform is aimed at epidemiologistis and public health units.

The project is currently in an alpha release and is intended primarily for experimentation, development and discussion, with hopes of developing one day into a viable, usable platform.

**Technology:** ASP.NET Core (.NET 10), Entity Framework Core 9, SQL Server 2019+

Noting use of SurveyJS Survey Builder which if being used for production requires purchase of a developer licence

---

## Key Features for Users

- Patient and case management with duplicate detection and audit history
- Multi-disease surveillance
- Laboratory results, symptoms, exposures, hospitalisation and outcomes tracking
- Dynamic custom fields configurable without database changes
- Integrated survey system with conditional logic and versioning
- Task and workflow management for case follow-up, automated task creation for cases or contacts
- Outbreak investigation with case linking and contact tracing modes
- Interviewer / contact tracing supervisor interfaces
- No-code reporting, line listings and pivot table analytics
- Role-based security with field-level permissions and audit logging
- Disease based access
- Geospatial features including address geocoding and jurisdiction assignment

---

## Installation

### Prerequisites
- **.NET 10 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **SQL Server 2019+** or SQL Server Express (free)
- **Visual Studio 2022** or VS Code with C# extension

### Quick Start

```bash
# 1. Clone repository
git clone https://github.com/christianpeut95/Sentinel
cd Sentinel

# 2. Restore packages
dotnet restore

# 3. Update connection string
# Edit: Sentinel/appsettings.json
# Set: "DefaultConnection" to your SQL Server

# 4. Apply migrations
cd Sentinel
dotnet ef database update

# 5. Run
dotnet run

# 6. Open browser to https://localhost:7XXX
```

**First Run:**
- Database auto-seeds with lookup data
- Create admin user via registration https://localhost:7XXX/Identity/Account/Regiser (first user will default to Admin)

---

### Required Settings (`appsettings.json`)

For geocoding reccomendation is to use a Google API set prefered provider to either `Google` or `Nominatim`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SentinelDB;Trusted_Connection=True;..."
  },
  "GeocodingSettings": {
    "Provider": "Google",
    "GoogleApiKey": "YOUR_KEY" // Optional
  }
}
```

---

## Current Status

**?? Alpha Release - Active Development**

**Stable Features:**
-  Patient and case management
-  Duplicate detection and merging
-  Survey system with field mapping
-  Task management
-  Outbreak investigation
-  Report builder
-  Disease access control

**Known Limitations:**
-  Permissions audit incomplete
-  UI polish needed (minor issues)
-  Performance optimization needed (duplicate detection, large reports)

---

## Future features

### **Near-Term (Next 3-6 Months)**
- HL7 Support for laboratory result import
- Vaccination module with immunization tracking
- Enhanced charting and visualizations
- LDAP/AD integration for corporate authentication
- Proper Content Security Policy (CSP) headers
- Performance tuning (database queries, caching)
- Support for genomic data

---

## Contributing
Contibutions are welcome!

### **How to Help**
- **Report bugs** - Create an issue with reproduction steps
- **Suggest features** - Open a discussion with use case
- **Submit code** - Fork, create branch, submit PR to `develop`

### **Before Submitting Code**
- [ ] Run `dotnet test` (all tests pass)
- [ ] Run `dotnet build` (no warnings)
- [ ] Update documentation if needed
- [ ] Follow [Copilot Instructions](.github/copilot-instructions.md)


---

## Documentation

Documentation available on [Sentinel Notion] (https://www.notion.so/Sentinel-31b00376e60880bd9f11f04959729498)

---

## License

**MIT License** - See [LICENSE](LICENSE) file

**Third-Party Licenses:**
- **SurveyJS** - Commercial license required for production (~$999/year per developer)
- **AG Grid Community** - MIT License
- **WebDataRocks** - Free for non-commercial use

---


## Acknowledgements

- **Open-Source Libraries:** ASP.NET Core, Entity Framework Core, SurveyJS, AG Grid, WebDataRocks

---


**Built with ?? for public health**
