# HL7 mapping and test messages

Sentinel processes HL7 laboratory messages through per-laboratory
configurations. Configure and validate a laboratory against representative,
appropriately de-identified messages before enabling it for operational use.

## Configure a laboratory

Use **Settings → HL7** to create a laboratory configuration, select its file
drop location and manage its mappings. The field-mapping workflow begins at
`/Settings/HL7/FieldMappings/SelectLab` and requires `HL7.Configure`.

Mappings are configuration-driven and can be used when a laboratory places a
supported value in a non-default HL7 field. Sentinel retains parser fallbacks
for supported standard layouts, but a fallback is not proof that a new lab
format is correctly configured. Validate every field that drives patient
matching, laboratory-result storage or disease matching.

For disease matching, configure the biomarker/pathogen and the allowed test
result values, test methods and specimen types in Sentinel. Match test messages
to those configuration values; do not treat a static list of LOINC or SNOMED
codes in project documentation as an authoritative clinical terminology
reference.

## Generate synthetic test messages

The built-in generator is available at
`/settings/hl7/generate-test-files` to users with `HL7.GenerateTestFiles`.
It supports Generic HL7 2.5.1, Quest Diagnostics, LabCorp, Hospital Lab and
Reference Lab templates, with random, existing or manually entered test
patients and providers. A message may include multiple observations and can be
written to a configured drop location or an explicitly supplied test path.

The generator also supports saved test templates, cloning or regenerating a
message, processing-history inspection and batches with varied identifiers.

## Safe test workflow

1. Use a dedicated non-production configuration or a controlled test drop
   directory. A generated file sent to an active production configuration can
   be processed like any other incoming message.
2. Use synthetic patient data and unique message-control and accession values.
3. Exercise the expected result, a negative/not-detected result, unconfigured
   codes, an existing-patient update, and any multiplex scenario the laboratory
   will send.
4. Inspect the stored HL7 message, extracted laboratory results, case linkage,
   review queue and case-definition outcome.
5. Keep a reusable generator template for each critical configuration test.

## Permissions

`HL7.Configure` controls configuration and mapping changes.
`HL7.View` controls read-only monitoring and message views.
`HL7.Process` controls processing actions and review-queue operations.
`HL7.GenerateTestFiles` controls the synthetic message generator. Assign the
smallest set needed for the user's role.
