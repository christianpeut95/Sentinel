# Sentinel documentation

This directory contains the maintained documentation that ships with Sentinel.
It is deliberately separate from implementation history, incident notes and
one-off debugging records; Git history is the source for those records.

## Operator and configuration guidance

- The repository [README](../README.md) is the starting point for evaluation,
  Docker deployment and initial setup.
- [Regional settings](configuration/regional-settings.md) explains the
  organisation-wide country, locale and time-zone settings.
- [HL7 mapping and test messages](integrations/hl7-mapping-and-testing.md)
  describes the supported configuration and safe validation workflow.
- [Case definitions](features/case-definitions.md) describes the current
  case-definition builder and review workflow.
- [Background geocoding](features/background-geocoding.md) documents the
  asynchronous HL7 address-geocoding behaviour and its limitations.

## Maintainer guidance

- [Migrations](development/migrations.md) covers normal EF Core migration
  maintenance and the limited legacy manual scripts.
- [Release checklist](releasing.md) covers a source or container release.
- [Licensing](licensing.md) and [WebDataRocks acceptance](licensing-webdatarocks.md)
  cover release conditions for Sentinel-owned and separately licensed material.
- [Optional SurveyJS Creator deployment](deployment/survey-designer.md)
  describes the operator-managed designer override.

## Security

The [security documentation](security/README.md) records Sentinel's security
controls, operating assumptions and release checks. It must not contain
credentials, setup tokens, patient data, raw production logs or telemetry
payloads.

## Keeping this documentation current

Update the relevant document in the same change as an externally observable
configuration, deployment, permission or workflow change. Do not add new
``fix summary``, ``complete``, ``restart now`` or incident-log Markdown files
to the application tree. A concise changelog entry and the Git commit provide
the durable record for implementation work.
