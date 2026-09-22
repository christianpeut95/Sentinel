# Case definitions

Case definitions are configured per disease and are evaluated against the
evidence stored on a case. They support laboratory, clinical and other
configured criteria, including grouped logical structures where the builder
offers them.

## Configure a definition

1. Go to **Settings → Case Definitions** and create or open a definition.
2. Build the criteria while the definition is a draft. Laboratory criteria use
   the configured biomarker/pathogen, result, specimen and test-method
   reference data rather than relying on a test-name text match.
3. Use the builder's grouping and operator controls to express the required
   logic. Review the rendered structure before publishing.
4. Use the review page to confirm the outcome, automatic-classification
   behaviour and the definition's status before making it active.

The main builder and review routes are under
`/Settings/CaseDefinitions/BuildCriteria` and
`/Settings/CaseDefinitions/Review`. They require the `Settings.Edit`
permission. The browser UI uses protected internal endpoints; those endpoints
are not a public integration contract.

## Laboratory evidence and disease hierarchy

An incoming laboratory result may contribute evidence to an existing case. The
case evaluator considers the evidence held by the case, allowing a later
typing/serovar result to refine a generic parent disease when the complete
configured definition is satisfied. Definitions should therefore contain the
core evidence required for their own disease as well as any subtype-specific
criterion; do not assume a subtype result alone proves every required parent
criterion.

When several eligible definitions match, the evaluator applies the configured
specificity rules. Keep definitions unambiguous and test any overlapping
criteria deliberately, especially for parent/child diseases and multiplex
laboratory messages.

## Validate safely

Use a dedicated non-production HL7 configuration and synthetic patient data to
test positive, negative, multiplex and result-order scenarios. See [HL7 mapping
and test messages](../integrations/hl7-mapping-and-testing.md). Record the
expected disease, case definition, classification and laboratory-result
attachment before testing a change.
