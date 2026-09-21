# WebDataRocks integration and acceptance

Sentinel-owned code is GPL-3.0-or-later. WebDataRocks is a separate,
browser-side reporting component supplied by its vendor under the
[WebDataRocks Licence Agreement](https://www.webdatarocks.com/license-agreement/).
This document records how Sentinel keeps the optional component distinct from
the GPL-covered core.

## Release design

- Sentinel does not copy WebDataRocks source or bundled assets into the
  standard source archive or Docker image.
- When enabled, the browser obtains version `1.4.23` from
  `cdn.webdatarocks.com`.
- Sentinel keeps the required on-screen WebDataRocks attribution on active
  pivot views.
- Standard report tables do not require WebDataRocks and remain available when
  the component is disabled.

Loading a third-party CDN asset discloses the user's network request to that
CDN. Sentinel does not send report rows, patient data, or configuration to the
WebDataRocks vendor as part of loading the library; report data is provided to
the already-loaded component in the user's browser.

## Consent controls

The current agreement revision is recorded in code as `2024-04-18`.

1. During initial setup, an organisation representative can leave interactive
   pivots disabled, or enable them by explicitly accepting the agreement.
2. Existing installations use **Settings → Interactive Reports** to make the
   same organisation-level decision. Disabling the feature removes the
   organisation acceptance; a future re-enable requires a fresh confirmation.
3. Before the report builder loads, and before a report view loads pivot
   controls, Sentinel checks that the organisation has accepted the current
   revision and that the signed-in user has accepted it too.
4. The acceptance timestamp and agreement revision are stored for the
   organisation and the individual user. Updating the recorded agreement
   revision in `WebDataRocksLicenseService` makes the prior acceptances stale,
   requiring both levels to accept the replacement terms.

This flow records acceptance but is not legal advice. Deployment operators and
distributors remain responsible for confirming that their intended use complies
with the vendor agreement and any applicable law.

## Maintainer checks

Before a release:

1. Verify the vendor's current agreement and update the recorded revision when
   it changes.
2. Confirm the source and Docker image still do not bundle vendor proprietary
   files or remove the required attribution.
3. Check that disabled and unaccepted states do not include a
   `cdn.webdatarocks.com` CSS, JavaScript, or toolbar request.
4. Keep this document, [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md), and
   [docs/licensing.md](licensing.md) with the release material.
