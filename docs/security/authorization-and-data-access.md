# Authorization and data access model

## Core rule

Every server-side read or mutation must prove both:

1. the caller holds the required functional permission; and
2. the caller may access the specific object and its disease hierarchy.

Hiding a button, filtering a dropdown or trusting an ID supplied by the browser is not authorization.

## Data scope

- Disease access is hierarchy-aware. A restriction on a parent disease applies to descendant diseases.
- Case access is the primary authority for case-linked records: labs, exposures, notes, attachments, tasks, surveys and associated patient information.
- Patient visibility follows the configured patient-access setting; when case-scoped access is enabled, a visible patient must have at least one accessible case.
- Reports and exports require report permission plus access to the requested underlying entity/data scope.
- Administrative disease-access management may deliberately bypass normal disease filtering so an authorised administrator can grant/revoke access; that exception must be explicit and limited to administration.

## Endpoint implementation rules

- Apply an explicit authorization policy to every Razor Page, controller action and Minimal API mutation.
- Perform object access checks before materialising sensitive data or mutating it.
- For destructive lab-result/exposure operations, require Case.Edit and the dedicated delete permission.
- Require anti-forgery validation for browser-cookie state-changing Minimal APIs.
- Validate Event and Location references as active and accessible before linking them to a case/contact workflow.
- Do not use `IgnoreQueryFilters()` in a user-request path without a documented compensating access check.

## Regression tests

Use synthetic data and a non-administrator user restricted from a parent disease. For each page/API/export/download:

1. request an accessible record;
2. substitute an ID from a restricted child disease;
3. attempt the same request with the relevant permission removed;
4. attempt the state-changing request without a CSRF token where cookie authentication applies;
5. confirm no data is returned or changed and no sensitive metadata leaks in an error.

Repeat for cases, patients, contacts, notes, attachments, laboratory results, exposures, tasks, surveys, reports, outbreaks, HL7 data and exports.
