# Third-party notices and distribution checks

`LICENSE.md` applies to Sentinel-owned source code. It does not change the
licence of third-party packages, browser assets, services, or other material
that carries its own copyright notice. Distributors must preserve the relevant
notices and comply with every applicable term.

This inventory records the direct dependencies and browser components inspected
for the 0.9.0-beta GPL release preparation. It is not legal advice.

## Release status

| Status | Component | Current use | Required action |
|---|---|---|---|
| Compatible | ClosedXML 0.105.1 | Occupation-reference-data Excel import | MIT-licensed replacement for EPPlus. Preserve its and its dependency notices. |
| Compatible | SurveyJS Form Library 1.9.131 | Rendering completed surveys | MIT-licensed. Its licence text is retained in `Sentinel/wwwroot/licenses/surveyjs-license.txt`. |
| Optional / operator-managed | SurveyJS Creator 1.9.131 | Demo visual survey-designer override | The standard Sentinel source archive and image do not contain Creator assets. A deployment operator may mount separately obtained assets for a demo deployment and is responsible for the vendor's terms and any required licence. |
| Optional / separately licensed | WebDataRocks 1.4.23 | Interactive report builder and pivot views, loaded from the vendor CDN only after acceptance | The vendor requires attribution and distributes it under a separate EULA. Sentinel keeps the component disabled by default; setup/settings records organisation acceptance and each user accepts the current agreement before its CDN files load. |

Do **not** describe the complete Sentinel distribution as “GPL-only”. The
Sentinel-owned code is GPL-3.0-or-later; the components above retain their own
terms.

## Direct NuGet dependencies

The following licence identifiers are taken from the NuGet package metadata for
the version referenced by `Sentinel/Sentinel.csproj`.

| Package | Version | Declared licence |
|---|---:|---|
| AntDesign | 1.5.1 | MIT |
| ClosedXML | 0.105.1 | MIT |
| CsvHelper | 33.1.0 | MS-PL OR Apache-2.0 (Sentinel distributes under the Apache-2.0 option) |
| Dapper | 2.1.79 | Apache-2.0 |
| MailKit | 4.17.0 | MIT |
| MimeKit | 4.17.0 | MIT |
| Microsoft.AspNetCore.Components.QuickGrid.EntityFrameworkAdapter | 10.0.12 | MIT |
| Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore | 10.0.12 | MIT |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.12 | MIT |
| Microsoft.AspNetCore.Identity.UI | 10.0.12 | MIT |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.12 | MIT |
| Microsoft.EntityFrameworkCore.Tools | 10.0.12 | MIT (development-only package assets) |
| NetTopologySuite | 2.5.0 | BSD-3-Clause |
| NetTopologySuite.IO.GeoJSON | 4.0.0 | BSD-3-Clause |
| NetTopologySuite.IO.ShapeFile | 2.1.0 | LGPL-2.1-or-later |
| nhapi | 3.2.0 | MPL-2.0 |
| QRCoder | 1.8.0 | MIT |
| Serilog.AspNetCore | 10.0.0 | Apache-2.0 |
| Serilog.Enrichers.Environment | 3.0.1 | Apache-2.0 |
| Serilog.Enrichers.Thread | 4.0.0 | Apache-2.0 |
| Serilog.Formatting.Compact | 3.0.0 | Apache-2.0 |
| Serilog.Sinks.File | 7.0.0 | Apache-2.0 |
| Serilog.Sinks.MSSqlServer | 10.0.0 | Apache-2.0 |
| System.Linq.Dynamic.Core | 1.7.1 | Apache-2.0 |

The LGPL and MPL packages have their own notice and source-availability
obligations. They can be used in a GPL-3.0-or-later distribution, but their
licences must continue to accompany their corresponding package material.

## Browser assets and services

- Bootstrap, jQuery, jQuery Validation, and jQuery Validation Unobtrusive are
  committed under `Sentinel/wwwroot/lib` with their upstream licence files.
- SurveyJS Form Library's MIT text is committed under
  `Sentinel/wwwroot/licenses/surveyjs-license.txt`; it does not licence the
  separate Survey Creator files.
- WebDataRocks is loaded from `cdn.webdatarocks.com`, rather than copied into
  the repository. Its required on-screen attribution is retained on Sentinel's
  active report-builder and report-view pages. The scripts are not loaded until
  an organisation administrator and the signed-in user have accepted the
  current vendor agreement; see [the integration record](docs/licensing-webdatarocks.md).

## Maintainer release gate

Before publishing a source archive, container, installer, or binary release:

1. Review this file and `docs/licensing.md`.
2. Run `dotnet list Sentinel/Sentinel.csproj package --include-transitive` and
   review any newly added or upgraded dependency's licence.
3. Preserve required licence texts, notices, attributions, and source-offer
   obligations for both direct and transitive dependencies.
4. Verify that no SurveyJS Creator assets have entered the source archive or
   standard image. If an operator-managed override is distributed, obtain the
   vendor permissions required for that distribution. Verify the WebDataRocks
   acceptance gate and attribution described above before publishing.
