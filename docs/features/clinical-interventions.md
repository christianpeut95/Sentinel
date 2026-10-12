# Clinical interventions module

## Status and purpose

**Draft feature specification.** This document records the proposed scope and
data model for an Immunisations and Treatments capability. It does not describe
implemented Sentinel behaviour.

The module must support surveillance of immunisation, post-exposure prophylaxis
and treatment. It should not be implemented as a vaccine-only list because the
same surveillance need can involve vaccine courses, multi-dose treatment, and
alternative acceptable medicines.

The user-facing name may be **Immunisations and Treatments**. The internal
model uses the broader term **clinical intervention**.

## Goals

- Record clinically relevant immunisations, prophylaxis and treatments for
  cases and contacts.
- Model products separately from their antigens and active ingredients.
- Allow teams to configure disease-specific schedules and protocols, such as
  primary courses, boosters and treatment courses.
- Show an explainable status, including why a course is complete, incomplete,
  overdue or requires review.
- Link operational tasks and surveys to an intervention plan without treating a
  completed task or survey response as proof that treatment was delivered.
- Reuse Sentinel's existing case/contact access controls, disease-hierarchy
  restrictions, audit history and protected attachment handling.

## Out of scope for the first delivery

- Clinical decision support or replacement of practitioner judgement.
- Direct integration with national immunisation registers or prescribing
  systems.
- Jurisdiction-specific catch-up calculations beyond the configured rules.
- Automatic acceptance of ambiguous survey or imported intervention data.

## Core design decisions

1. A clinical product is a particular marketed formulation. Its components are
   not stored as free text on a patient event.
2. An intervention event belongs to a patient history. It may be recorded in a
   case or contact context, but may later support more than one surveillance
   episode.
3. An intervention episode records a disease-specific plan or course being
   tracked for a person. It is separate from the actual intervention events.
4. Published schedule and protocol versions are immutable. Historic results
   retain the version and event evidence used at the time of evaluation.
5. A task can refer to an intervention episode, but task completion does not
   record administration, prescription or treatment completion.
6. Rules are represented by structured fields and relationships, not arbitrary
   expressions or executable logic.

## Relationship overview

```text
Clinical product ──< Product component >── Clinical component
       │
       └──< Intervention event >── Patient
                                      │
Disease ──< Disease intervention rule >── Protocol ──< Protocol version
                                                │
Patient + case/contact context ──< Intervention episode >── Evaluation snapshot
                                                │                    │
                                                └── Existing task     └── Evidence events
```

In the current Sentinel model, the patient is the clinical recipient. A case or
contact record supplies the surveillance context. The eventual implementation
should use the existing case/contact keys rather than introduce a generic
polymorphic entity reference.

## Reference data

### Clinical products

`ClinicalProducts` represents the actual vaccine or medicine formulation that
can be selected when recording an event.

| Field | Purpose |
|---|---|
| `Id` | Stable identifier. |
| `Name` | Product or formulation name. |
| `BrandName` | Brand where distinct from the formulation name. |
| `Kind` | `Vaccine`, `Medicine`, `Prophylaxis`, or other controlled value. |
| `Formulation` | Presentation or formulation details. |
| `DefaultRoute` | Usual administration route where relevant. |
| `IsActive` | Allows retirement without deleting historical records. |
| Audit and concurrency fields | Existing Sentinel conventions. |

`ClinicalProductCodes` stores one or more recognised codes for a product:
`ProductId`, `CodeSystem` and `Code`.

A material formulation change should create a new product record and retire the
old one. Component history must never be silently overwritten.

### Clinical components

`ClinicalComponents` represents a reusable canonical component:

| Field | Purpose |
|---|---|
| `Id` | Stable identifier. |
| `Name` | Canonical component name. |
| `Kind` | `Antigen` or `ActiveIngredient`. |
| Standard codes | Optional terminology mappings. |
| `IsActive` | Allows retirement without historical loss. |

`ClinicalProductComponents` links a product to one or more components. It may
also record quantity and unit when they are relevant to a treatment protocol.

Examples:

| Product | Components |
|---|---|
| MMR vaccine | measles, mumps, rubella |
| MMRV vaccine | measles, mumps, rubella, varicella |
| Infanrix hexa | diphtheria, tetanus, pertussis, polio, Hib, hepatitis B |
| Ceftriaxone product | ceftriaxone active ingredient |

## Schedules and treatment protocols

### Protocols and versions

`InterventionProtocols` is the named reusable definition, for example:

- MMR primary course;
- meningococcal clearance antibiotics;
- syphilis treatment course;
- pertussis booster current.

Key fields include `Id`, `Name`, `Purpose` (`Immunisation`, `Treatment` or
`Prophylaxis`), `Description`, `IsActive` and the current published version.

`InterventionProtocolVersions` stores draft, published and superseded versions.
It contains the version number, effective dates, publication metadata and
immutable requirements. A published version is never edited in place.

### Requirements and acceptable options

`ProtocolRequirements` represents a dose, course or booster condition within a
protocol version.

| Field | Purpose |
|---|---|
| `Sequence` and `Name` | Human-readable order and explanation. |
| `RequiredCount` | Number of qualifying events required. |
| `MinimumIntervalDays` | Minimum interval between qualifying events. |
| `MaximumIntervalDays` | Optional maximum interval where clinically required. |
| `ReferenceDateType` | Date used to calculate due or recency rules, such as date of birth, exposure date, case notification date or prior event date. |
| `QualifyingEventState` | Event state that counts, such as administered or completed. |

`RequirementOptions` represents an acceptable way to satisfy a requirement.
It supports a specific product, a set of products, or a product containing a
required set of components. Its child tables are:

- `RequirementOptionProducts` — eligible products;
- `RequirementOptionComponents` — required components;
- `ComponentMatchMode` — `All` or `Any` components must be present.

This supports different brands and formulations without duplicating every
schedule.

| Protocol | Structured requirement |
|---|---|
| MMR primary course | Two qualifying events at least 42 days apart; each event uses a product containing all of measles, mumps and rubella. |
| Meningococcal clearance antibiotics | One qualifying completed event using ceftriaxone, rifampicin or ciprofloxacin. |
| Syphilis treatment course | Three qualifying benzathine benzylpenicillin events at configured intervals. |
| Pertussis booster | One qualifying event within a configured recency period. |

## Patient intervention history

### Intervention events

`InterventionEvents` is the patient-level record of what happened.

| Field | Purpose |
|---|---|
| `Id`, `PatientId` | Required event identity and clinical recipient. |
| `ProductId` | Selected clinical product. |
| `UnmappedProductName` | Restricted manual fallback, requiring review before it can count automatically. |
| `OccurredAt` | Date and optional time of the event. |
| `State` | Planned, offered, prescribed, administered, completed, declined, contraindicated or unknown. |
| `Dose`, `DoseUnit`, `Route`, `AdministrationSite` | Optional administration details. |
| `BatchNumber`, `ExpiryDate` | Optional traceability details. |
| `Provider`, `Location`, `Source` | Provenance. |
| `RecordedInCaseId` | Optional case/contact context in which it was recorded. |
| Audit and concurrency fields | Existing Sentinel conventions. |

An event is not owned by a single intervention episode. This permits a historic
MMR dose to be evaluated against later relevant surveillance contexts without
duplicating the clinical record.

## Episodes, evaluations and evidence

### Intervention episodes

`InterventionEpisodes` represents the disease-specific plan, recommendation or
course being tracked for an individual.

| Field | Purpose |
|---|---|
| `PatientId` | Clinical recipient. |
| `ContextCaseId` | Case or contact context. |
| `DiseaseId` | Disease that made the episode relevant. |
| `ProtocolVersionId` | Exact rules being evaluated. |
| `Status` | Operational episode status. |
| `DueDate`, `Priority`, `Reason` | Follow-up information. |
| `CreatedByRuleId` | Optional provenance when created by a disease rule. |

Examples include “MMR catch-up recommended for this contact” and
“Meningococcal clearance antibiotics required.”

### Evaluations

`InterventionEvaluations` stores an immutable result for an episode:

- `EpisodeId`;
- `ProtocolVersionId`;
- `EvaluatedAt`;
- `Status`: not started, incomplete, complete, current, overdue, declined,
  contraindicated, unknown or requires review;
- `NextDueDate` where applicable;
- concise explanation for the user.

`InterventionEvaluationEvidence` links the evaluation to individual events and
requirements. It records whether an event counted, did not count, or requires
review, together with the reason. This makes schedule status explainable and
auditable.

## Disease rules and task integration

`DiseaseInterventionRules` links a disease workflow to a protocol. It includes:

- `DiseaseId`;
- `ProtocolId`;
- whether the rule applies to a case, contact or other supported context;
- default due offset and priority;
- whether an episode may be created automatically;
- optional existing task-template reference.

The existing task table should gain a nullable `InterventionEpisodeId`. A task
may then direct a user to contact a person, organise follow-up or confirm a
dose. Completing that task records the operational work only. It must never
mark a clinical intervention as delivered.

Surveys may collect intervention information. A mapping from a survey answer to
an intervention event must use the existing trusted/review model. Ambiguous or
sensitive values should create a reviewable proposal, not silently alter the
patient's clinical history.

## Security and data-integrity requirements

- Require intervention-specific permissions in addition to existing
  case/contact and disease-hierarchy access.
- Use foreign keys, controlled enums, allow-listed products and structured
  protocol rules; do not use executable user-defined expressions.
- Retire or soft-delete reference data rather than deleting records referenced
  by events, evaluations or protocols.
- Preserve audit history for all clinical events and configuration changes.
- Use optimistic concurrency protection for editable configuration and event
  records.
- Apply existing protected file-storage rules to supporting documents.
- Validate all clinical dates, intervals, identifiers and references on the
  server.

## Initial delivery scope

1. Product, component and code catalogue.
2. Patient intervention-event recording in case and contact contexts.
3. Configurable protocols with qualifying products/components, required counts
   and minimum intervals.
4. Explainable episode evaluation and evidence history.
5. Disease-linked intervention episodes and optional task links.
6. Basic line-list and aggregate reporting.

## Later scope

- HL7, FHIR or immunisation-register import.
- Advanced age, catch-up and jurisdiction-specific dose-validity rules.
- Automatic episode creation from exposure, classification or contact events.
- Reminder and escalation workflows for due or overdue interventions.
- Coverage reporting by disease, outbreak, geography and demographic group.
