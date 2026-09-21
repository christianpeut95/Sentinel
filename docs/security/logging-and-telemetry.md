# Logging and telemetry safety

## Logging rule

Logs are for operational diagnosis, not for copying application data. Do not log passwords, reset/setup tokens, connection strings, cookies, authorization headers, full request bodies, patient demographics, clinical notes, attachments, complete HL7 payloads or complete telemetry payloads.

When an exception reaches a user-facing endpoint, log the full exception server-side with a request/correlation identifier. Return only a generic message and that identifier to the user. Do not return `Exception.Message`, stack traces, provider errors or local paths.

## Optional remote telemetry

Remote usage/error reporting is disabled by default. A Sentinel administrator must explicitly enable the single **Anonymous Usage Statistics & Automatic Error Reports** setting; the choice can be withdrawn at any time. The feedback widget is separately disabled by default.

When enabled, remote usage/error reporting is one configuration choice. It is limited to operationally useful, non-identifying data:

- installation/report identifiers;
- Sentinel version and safe runtime characteristics;
- semantic page identifiers and aggregate counts;
- coarse operational error classification/fingerprint.

Automated error telemetry must not include exception message, stack trace, raw route/query values, headers, trace IDs, credentials, patient data or raw request/response payloads. Feedback, usage and error clients must log only safe submission metadata, never complete serialized payloads.

## Review procedure

Before release and after logging/telemetry changes, search for `Console.WriteLine`, `LogInformation`, `LogWarning`, `LogError`, `Serialize`, `Request.Body`, `Exception.Message` and `StackTrace`. Review each use for unnecessary sensitive data. Test a controlled error in page, AJAX and JSON contexts and inspect both the user response and server log.

If a report of unexpected telemetry is received, use installation/report identifiers and timestamps to correlate server-side logs. Do not ask the reporting site to send patient records, raw logs or secrets by email.
