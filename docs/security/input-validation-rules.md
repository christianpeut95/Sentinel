# Input validation rules

## Purpose

Sentinel validates input on the server before data is saved, evaluated, imported or used to make a security decision. Client-side validation improves usability only; it is never an authorization or integrity control.

## General rules

- Bind a dedicated input/view model, not a database entity, for create and edit operations.
- Use data annotations for basic shape rules and service-layer checks for business rules.
- Treat route IDs, query IDs, form IDs and JSON IDs as untrusted. Confirm the referenced record exists, is active where relevant, and is accessible to the caller.
- Use allow-lists for fixed choices, enum values, sortable fields, report fields, file types and logical operators.
- Validate dates, numeric ranges and required field combinations before a workflow transition.
- Reject invalid input with a safe validation response; do not expose database, filesystem or implementation details.

## High-risk input categories

| Area | Required server-side checks |
|---|---|
| Identity and users | Valid email structure, configured password policy, common-password deny-list, valid role/permission IDs, protected account state changes |
| Patient, contact and case forms | Dedicated input model, required demographic/date formats, valid disease/case/patient references, permitted disease hierarchy and logical state transitions |
| Tasks and surveys | Valid task/survey IDs, task not already complete, caller can access the linked case, survey/version fields match the active definition |
| Case definitions and mappings | Allow-listed criterion type/operator/field identifiers, referenced marker/test/result exists, group structure is valid, mutation requires anti-forgery and settings authorization |
| Reports and exports | Allow-listed data views, fields, operators and sorting; filter values are parameterized; caller has both report access and underlying entity/data access |
| CSV, population and occupation imports | File size/type/content checks, headers allow-listed, row limits, typed parsing with explicit errors, reject malformed or duplicate identifiers according to the workflow |
| Shapefile imports | Expected sidecars and file count, archive/path limits, GeoJSON/shapefile parse validation, file size limit appropriate to the jurisdiction workflow |
| HL7 | Safe parser, message/segment shape validation, laboratory configuration match, field mappings only from configured mapping choices, invalid patient/result data sent to review or rejected safely |
| Attachments | Protected-storage service, generated storage key, extension/content allow-list, signature/archive/XML/text validation, configured size limit and authorised download |

## Validation sequence

1. Authenticate and authorise the request.
2. Bind only expected fields.
3. Validate primitive shape: required, length, type, range and format.
4. Validate references and tenant/disease/object access.
5. Validate business state and cross-field combinations in the service layer.
6. Save the complete change atomically where partial success would be unsafe.
7. Audit the change without recording secrets or unnecessary sensitive values.

## Manual regression samples

- Submit missing, overlong, malformed and out-of-range values directly, bypassing browser validation.
- Change an ID in a route/body to a record under a restricted parent or child disease.
- Submit an unknown permission, role, report field, sort column or case-definition operator.
- Replay a completed survey/task action and repeat an import confirmation.
- Submit a renamed executable, malformed Office archive, invalid UTF-8 CSV and incomplete shapefile set.

When adding a new high-risk workflow, add its validation expectations to this document and a representative automated or manual regression test.
