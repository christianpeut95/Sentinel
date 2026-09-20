# File handling and import safety

## Storage and downloads

Attachments and generated protected files must use Sentinel's protected storage service. Store them outside `wwwroot` using generated storage keys; never derive a disk path from a browser-supplied filename. All downloads require server-side object/case access checks and should be sent as attachments with `nosniff` and no-store headers where applicable.

Legacy `/uploads` and timeline paths must remain blocked before static-file middleware. New features must not create a public static path for sensitive files.

## Upload rules

- Enforce endpoint-appropriate request and file size limits. Standard attachments are limited by the protected-file configuration; documented large jurisdiction shapefile workflows may use their higher explicit limit.
- Allow-list expected extensions and validate content/signatures, not extension alone.
- Validate text encoding and archive structure; XML parsers must prohibit DTD processing and external resolution.
- Bound archive entry count, extracted size and path traversal; reject malformed archives and missing required shapefile sidecars.
- Generate a safe server filename/key and retain the original display name only as metadata after normalization.
- Reject unsafe files with a generic validation error and log safe diagnostic metadata only.

## Import checks

CSV/population/occupation imports require expected headers, bounded row/file sizes and typed per-row parsing. Shapefile imports require the expected component set and valid geographic parse. A failed import must not leave partial records unless the workflow explicitly supports a reviewed partial result.

## Regression tests

Test each upload/import page with a valid allowed file plus: renamed binary, oversize file, malformed Office/zip archive, invalid UTF-8 CSV, zip-bomb-shaped input, traversal filename, missing shapefile sidecar and a record targeting a restricted disease/case. Verify valid files remain uploadable/downloadable for authorised users.
