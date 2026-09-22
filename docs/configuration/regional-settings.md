# Organisation regional settings

Sentinel has one organisation-wide regional configuration. It keeps calendar
dates, timestamps, numeric formatting and the user-interface language
predictable for an installation without trusting browser-supplied locale or
time-zone values.

```json
"Organization": {
  "CountryCode": "AU",
  "TimeZoneId": "Australia/Adelaide",
  "Locale": "en-AU"
}
```

Configure these values during setup or later through **Settings →
Organization**. The application validates country, culture and time-zone
identifiers on the server. Sentinel accepts legacy Windows time-zone identifiers
where the runtime supports their conversion, and saves a portable IANA
identifier on the next update.

## Date and time rules

- Persisted timestamps are UTC. SQL Server does not retain `DateTime.Kind`, so
  an unspecified persisted timestamp is treated as UTC for display.
- Date-only values, such as date of birth and symptom onset, are calendar
  dates. They are not shifted between time zones.
- `datetime-local` inputs are shown in the organisation's time zone and stored
  as UTC. Invalid or ambiguous daylight-saving values are rejected rather than
  silently shifted.
- Dynamic periods such as *Today* and *This month*, task due-date filters and
  age calculations use the organisation's current calendar day.
- Client and server presentation uses the configured culture and time zone,
  not the visitor's browser defaults.

Changing the setting changes presentation and future organisation-relative
defaults; it does not rewrite existing UTC timestamps.

## Developer guidance

Use `IApplicationTimeZoneService` for organisation-relative time, formatting
and calendar calculations. The `[OrganizationLocalDateTime]` model-binding
attribute is available for `datetime-local` values. Razor and JavaScript should
use the supplied Sentinel formatting helpers rather than `DateTime.Now`,
`DateTime.Today`, `TimeZoneInfo.Local`, browser `toLocale*` calls or hard-coded
cultures for user-facing regional behaviour.

The trusted organisation setting, rather than an `Accept-Language` request
header, determines request localisation.
