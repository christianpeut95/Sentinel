# Task Barriers and Referral Coordination

> **Status:** Proposed feature / technical design (not implemented)  
> **Scope:** Case-task barriers, referral configuration and tracking, reporting, and interfaces for future electronic messaging  
> **Last reviewed:** 14 October 2026

## Feature description

Sentinel should support recording **individual-specific barriers** that interfere with completion of an assigned public-health task **or achievement of its intended outcome**. These are circumstances of the individual to whom the task relates, not workforce, system, or operational faults.

For example, an isolation-advice task may be completed in the sense that the patient received instructions, but the patient may lack safe accommodation and therefore be unable to follow those instructions. The task and its associated barriers must be reportable together.

A task can have zero or more barriers. Each barrier is selected from a configurable lookup, has its own outcome/lifecycle, and can have zero or more referrals. Eligible organisations and services are configured against barrier definitions. If the required questionnaire has **explicitly recorded an affirmative, valid consent response for the specific disclosure**, and other preconditions are satisfied, a subsequent automation component may prepare and electronically send a referral to a selected provider. Messaging, delivery confirmation and response processing depend on a separate electronic messaging capability.

This proposal reuses Sentinel's existing case tasks, survey and mapping infrastructure, organisations, jurisdictions, permissions, audit logs and reporting. It does **not** require associating barrier definitions with `TaskTemplate`, `TaskType` or `DiseaseTaskTemplate`; the instance-level relationship to `CaseTask` is sufficient. It does **not** add `SourceQuestionName` to the barrier table.

### Example

1. An interviewer completes the isolation-advice questionnaire and records that instructions were provided.
2. The questionnaire identifies two barriers: unsuitable accommodation and food insecurity.
3. Two `TaskBarrier` records are created against that `CaseTask`.
4. Suitable services are listed using configured `OrganisationServiceBarrier` associations and service-jurisdiction coverage.
5. The questionnaire explicitly asks for authorisation to disclose specified information for the proposed referral to the identified recipient(s). An unanswered, declined, withdrawn or inapplicable response blocks *automated* referral.
6. The selected service(s), the required referral data, and a validated consent/authorisation record are used to create distinct `BarrierReferral` records.
7. The future electronic messaging capability sends the referrals and records the relevant timestamps/events.
8. Referral outcomes and the mitigation status of each task barrier are maintained independently of task completion.

## Scope and design decisions

- **Task-scoped:** `TaskBarrier.CaseTaskId` is the reporting anchor. Barriers remain independent child records and can progress after the task is completed.
- **No template association:** barrier applicability is established when a survey response or staff action creates the task-barrier instance. Administrative barrier lookup values are globally reusable. Do not introduce `TaskTemplateBarrier` unless a separate future requirement demonstrates the need.
- **No question-name provenance column:** continue retaining the source questionnaire in the existing survey record, including the consent answer. A mapping pipeline can use question keys internally without persisting `SourceQuestionName` on `TaskBarrier`.
- **Multiple cardinalities:** one task → many barriers; one barrier → many referrals; one organisation → many services; one service → many supported barriers; one service → many covered jurisdictions.
- **Eligibility is descriptive initially:** `OrganisationService` should have a human-readable eligibility/description field. Do not build a general machine-readable eligibility-rule engine in this feature. The system can deterministically filter by active status, configured barrier and jurisdiction, but cannot assert that an individual's circumstances meet arbitrary prose eligibility criteria. Selection must be confirmed by an authorised user unless a later explicitly configured automation policy permits deterministic selection.
- **Explicit survey consent gates automation:** no default opt-in; no consent inferred from general task completion or from a generic patient flag. A clearly answered, valid, scoped consent questionnaire item must be persisted and checked before *any automatic referral operation arising from that task barrier*. Data extraction/preparation for external disclosure and transmission must remain blocked until that check succeeds.
- **Referral and barrier outcomes differ:** a delivered referral does not imply barrier mitigation; a barrier may cease to apply without any successful referral.
- **Messaging remains separate:** the referral module records a requested recipient/organisation service, message transport configuration and transmission state, but concrete channels, message format, acknowledgement processing and integrations belong to the shared messaging capability.
- **Historical integrity:** deactivate definitions/services rather than deleting records used in referrals; preserve the service, jurisdiction, template and authorisation applicable at the time of referral.

## Integration with existing Sentinel models

The following existing source files are relevant:

- `Sentinel/Models/CaseTask.cs`: `CaseTask` holds `CaseId`, optional `TaskTemplateId`, task status/completion and `SurveyResponseJson`.
- `Sentinel/Models/TaskTemplate.cs`: surveys are configured via template-linked or embedded survey definitions and associated mappings, but **no barrier-definition FK or junction is proposed**.
- `Sentinel/Models/SurveyFieldMapping.cs`: mapping configuration and collection/related-entity facilities provide a plausible extension point for materialising `TaskBarrier` instances.
- `Sentinel/Services/SurveyService.cs`: survey submission persists responses and invokes mapping. It rejects ordinary survey edits on completed/cancelled tasks.
- `Sentinel/Services/TaskWorkflowPolicy.cs`: completed/cancelled tasks are terminal. Barrier and referral updates must **not** reopen or edit terminal `CaseTask` records.
- `Sentinel/Models/Organization.cs`: reuse the existing `Organization` directory rather than creating another organisation entity.
- `Sentinel/Models/Lookups/Jurisdiction.cs` and `Sentinel/Services/JurisdictionService.cs`: reuse existing jurisdiction records and hierarchy.
- `Sentinel/Data/ApplicationDbContext.cs`: add EF Core models and constraints through additive migrations.

These are the intended extension points, not a claim that this feature is already present.

## Proposed entity model

```text
CaseTask
  └── TaskBarrier (0..*)
        ├── BarrierDefinition (1)
        └── BarrierReferral (0..*)
              ├── OrganisationService (1)
              │     ├── Organization (1)
              │     ├── OrganisationServiceBarrier (0..*) ── BarrierDefinition
              │     ├── OrganisationServiceJurisdiction (0..*) ── Jurisdiction
              │     └── ReferralTemplate / version
              ├── ReferralDisclosureAuthorisation (required for automation)
              └── MessagingEndpoint / TransportConfiguration (optional until delivery)
```

Use explicit foreign keys; do not store polymorphic entity IDs in free-text or JSON fields.

### 1. BarrierDefinition (new lookup)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| Id | uniqueidentifier | PK |
| Code | nvarchar(100) | Required, unique stable code |
| Name | nvarchar(200) | Required |
| Description | nvarchar(2000) | Optional |
| IsActive | bit | Required, default true |
| CreatedAt / ModifiedAt | datetime2 | UTC audit timestamps |

Examples: unsuitable accommodation, food insecurity, medication access, lack of transport. No task-template association.

### 2. TaskBarrier (new instance entity)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| Id | uniqueidentifier | PK |
| CaseTaskId | uniqueidentifier | Required FK → CaseTask |
| BarrierDefinitionId | uniqueidentifier | Required FK → BarrierDefinition |
| Status | int | Required, controlled enum |
| IdentifiedAt | datetime2 | Required UTC |
| MitigatedAt | datetime2 | Nullable UTC |
| ClosedAt | datetime2 | Nullable UTC |
| ResolutionNotes | nvarchar(2000) | Optional |
| CreatedAt / ModifiedAt | datetime2 | UTC |
| RowVersion | rowversion | Optimistic concurrency |

Suggested lifecycle: `Identified`, `MitigationInProgress`, `Mitigated`, `Unmitigated`, `NoLongerApplicable`, `EnteredInError`. Define allowed transitions and audit actors. A second occurrence of an identical barrier in a task must have intentional semantics; use application-level upsert/deduplication keyed by submission/definition initially, without an unconditional uniqueness constraint that prevents legitimate reopening or repeat assessment.

**Do not add `SourceQuestionName`.** The survey response remains the source record.

### 3. OrganisationService (new)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| Id | uniqueidentifier | PK |
| OrganizationId | uniqueidentifier | Required FK → existing Organization |
| Name | nvarchar(200) | Required |
| Description | nvarchar(2000) | Human-readable description, including general eligibility |
| EligibilityDescription | nvarchar(4000) | Optional detailed eligibility guidance; prose only |
| ReferralTemplateId | uniqueidentifier | Nullable FK → template/version |
| MessagingEndpointId | uniqueidentifier | Nullable FK → configured outbound messaging destination |
| IsActive | bit | Required |
| Priority | int | Optional service-ordering preference |
| CreatedAt / ModifiedAt | datetime2 | UTC |

`EligibilityDescription` is **advisory**, not executable logic. Do not promise automatic assessment of free-text eligibility. Services without a configured delivery endpoint can still be tracked through a manual referral workflow.

### 4. OrganisationServiceBarrier (new junction)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| OrganisationServiceId | uniqueidentifier | FK → OrganisationService |
| BarrierDefinitionId | uniqueidentifier | FK → BarrierDefinition |

Composite primary key `(OrganisationServiceId, BarrierDefinitionId)`. This avoids duplicating service records when the same service assists with multiple barrier types. An organisation may have multiple independently configured services.

### 5. OrganisationServiceJurisdiction (new junction)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| OrganisationServiceId | uniqueidentifier | FK → OrganisationService |
| JurisdictionId | int | FK → existing Jurisdiction |

Composite PK `(OrganisationServiceId, JurisdictionId)`. Specify whether parent-jurisdiction coverage includes child jurisdictions. The matching location must be explicitly defined (usually where support is to be delivered), rather than assuming it equals the case-management jurisdiction.

### 6. ReferralTemplate (new or reuse an existing suitable template facility)

A versioned structured schema describing the data required by the receiving service. Proposed fields: `Id`, `Name`, `Version`, `SchemaJson`, `IsActive`, timestamps. Prefer reusing existing questionnaire schema/validation components where feasible, but distinguish the **recipient data contract** from the interview questionnaire. Existing referral records must retain the exact template version and values used.

### 7. BarrierReferral (new instance entity)

| Field | SQL Server type | Requirement |
| --- | --- | --- |
| Id | uniqueidentifier | PK |
| TaskBarrierId | uniqueidentifier | FK → TaskBarrier |
| OrganisationServiceId | uniqueidentifier | FK → OrganisationService |
| ReferralTemplateId / Version | uniqueidentifier / int | Template snapshot reference |
| ReferralDataJson | nvarchar(max) | Validated payload snapshot; protect as sensitive |
| DisclosureAuthorisationId | uniqueidentifier | Required before automated preparation/sending |
| Status | int | Draft, AwaitingConsent, Ready, Sent, Received, Accepted, Declined, Completed, Cancelled, Failed |
| Outcome | int | Nullable; separate from barrier outcome |
| MessagingEndpointId | uniqueidentifier | Snapshot of selected delivery endpoint; nullable |
| OutboundMessageId | uniqueidentifier | FK to future messaging record; nullable |
| SentAt / ReceivedAt / CompletedAt | datetime2 | Nullable UTC timestamps |
| CreatedAt / ModifiedAt | datetime2 | UTC |
| RowVersion | rowversion | Concurrency |

Create `ReferralStatusHistory` or reuse an adequate existing immutable event/audit subsystem so each transition can be reconstructed. `ReceivedAt` is populated only from reliable recipient acknowledgement or an explicitly recorded manual event, **not** merely from successful dispatch.

A single barrier can have multiple referrals, including sequential referrals after decline or failure. No referral is required for a barrier that the public-health team addresses without an external provider.

### 8. ReferralDisclosureAuthorisation (new or reuse an equivalent existing entity)

Store a structured record linked to the task and captured from an **explicit consent question in the survey**. At minimum:

- `Id`, `CaseTaskId`, `SurveySubmissionId` (or durable submission reference), `Response` (Yes/No/Unanswered/Withdrawn), `AnsweredAt`, `RecordedByUserId`.
- `RecipientOrganizationId` or agreed recipient scope; `DisclosurePurpose`; `PermittedDataScope`; `ExpiresAt`/`WithdrawnAt` as relevant.
- A reference to the underlying stored questionnaire/answer, not an unverified consent flag. Preserve consent wording/version so its meaning is auditable.

**Automation invariant:** no automatic service selection resulting in referral preparation, disclosure or sending may execute unless the explicit survey question was answered affirmatively and the response remains valid for that recipient, purpose and data scope. Missing responses, ambiguous scope and revocation must fail closed. If the questionnaire is completed before the recipient is selected, consent must expressly cover the later selected recipient or a subsequent recipient-specific consent must be obtained.

Capture permission to share through the survey; whether alternative statutory authority permits a separate non-consent workflow is a different future governance decision and must **not** bypass this feature's explicit consent gate.

## Survey integration requirements

1. Keep `CaseTask.SurveyResponseJson` as the source questionnaire record.
2. Extend existing mapping infrastructure to support task-barrier creation. Appending a new `MappingFieldCategory.TaskBarrier` enum value is one plausible approach; inspect the mapping handlers before implementing.
3. Support multi-select answers producing multiple `TaskBarrier` children against one `CaseTask`.
4. Make mapping idempotent and retryable; resubmission or transient errors must not create duplicate barrier instances or referrals.
5. Distinguish `not assessed`, `assessed with no barriers` and `barriers identified`. A missing child row is not proof of a negative assessment.
6. Do not silently change `TaskBarrier` outcome when a survey answer changes. Specify correction and reconciliation semantics, especially if referrals already exist.
7. Explicitly map and persist the consent response, scope and source submission; validate it independently before any automated action.
8. Preserve the current survey persistence/review behaviour on mapping failures. A mapping failure should be visible and recoverable without losing questionnaire responses.

The **task** anchors reporting; the survey collects data; `TaskBarrier` represents the structured finding. There is no need for a permanent link from a barrier definition to a task template or a question-name provenance column on the instance.

## Referral selection and automation boundaries

A proposed `IBarrierServiceEligibility` service retrieves active services that are linked to a barrier through `OrganisationServiceBarrier` and geographically applicable through `OrganisationServiceJurisdiction`.

Filtering can be machine-executable for **active status, configured barrier and jurisdiction**. Human-readable service descriptions/eligibility conditions remain visible for user assessment. Do not build a rules engine for prose eligibility in this feature.

An automation coordinator may be introduced later. Its preconditions are:

1. A persisted `TaskBarrier` exists.
2. The appropriate consent question in the task survey has an explicit affirmative response with valid recipient/purpose/data scope.
3. The intended service is active and configured for the barrier and service jurisdiction.
4. An eligible recipient has been selected deterministically under an approved configuration or explicitly by an authorised user.
5. The required referral data validates against the recipient template.
6. A usable messaging destination/transport configuration exists for automated sending.
7. No equivalent referral has already been initiated for the same trigger and intended recipient; retries use stable idempotency keys.

If any condition fails, do not transmit. Surface a pending/manual-review state instead. Consent must be rechecked immediately before release to the messaging subsystem, not only when a barrier is first identified.

Use separate, injectable services for barrier persistence, service eligibility, authorisation validation, referral lifecycle and message handoff. Referral creation must not send messages synchronously inside a database transaction.

## Electronic messaging dependency (future cross-cutting feature)

Automated external referrals require a general-purpose electronic messaging capability, **which is not specified by this feature**. The database must nevertheless identify the outbound channel and endpoint selected for a service and correlate each referral to its outbound communication.

Minimum future abstractions:

- `MessagingEndpoint`: `Id`, `OrganizationId` and/or `OrganisationServiceId`, `ChannelType`, `DestinationConfigurationReference`, `IsActive`, and configuration/audit metadata. Secrets or credentials must be held in a secure secret store, not plaintext rows.
- `OutboundMessage` (or a shared message/outbox record): `Id`, `MessageType`, `CorrelationId` (referral ID), `MessagingEndpointId`, status, retry/idempotency information, creation/send timestamps and delivery/acknowledgement metadata.
- `BarrierReferral.OutboundMessageId` links the referral to delivery state. Consider a one-to-many `ReferralMessage` table if multiple attempts/messages need independent records.
- Reliable background delivery with an outbox pattern, retry policy, duplicate suppression, audit trail and terminal failure handling.

Channel choices (e.g. secure email, API, FHIR, other electronic messaging) and recipient response mechanisms are deferred to the interoperability/messaging design. Do **not** interpret provider acknowledgement or barrier mitigation as equivalent to technical message delivery.

The referral schema should remain transport-neutral.

## Task and referral lifecycle

`CaseTask` completion/cancellation remains governed by `TaskWorkflowPolicy`. Barriers and referrals evolve independently, including after task completion, provided the user's case permissions permit the action. They must not mutate or reopen the completed parent task.

- **Task state:** existing Sentinel state.
- **Barrier state:** identified, in progress, mitigated, unmitigated, no longer applicable, entered in error.
- **Referral state:** draft/awaiting consent/ready/sent/received/accepted/declined/completed/cancelled/failed.
- **Messaging state:** queued/dispatched/delivery confirmed/failed, owned by the shared messaging component.

Historical referral service, template, jurisdiction and disclosure scope must be retained or snapshotted so later configuration changes cannot retrospectively alter the record.

## Reporting: required `vwTaskBarrierReporting`

Implement a SQL Server view named **`vwTaskBarrierReporting`** as a stable reporting source.

**Grain: exactly one row per `TaskBarrier`**. Include:

- TaskBarrier ID, barrier definition code/name, status, identified/mitigated/closed timestamps and elapsed-time measures.
- Parent CaseTask ID, task type/title, status, completion timestamp, case ID.
- Existing case/disease and relevant jurisdiction identifiers for authorised downstream reporting.
- Number of referrals, number sent, number accepted/completed, number declined/failed and first/last referral timestamps.
- Optional computed indication of any outstanding referral and whether the barrier remains unresolved.

Use aggregated subqueries or `OUTER APPLY` to calculate referral metrics. **Do not directly join raw multiple-referral rows** into the one-row-per-barrier view: that would multiply tasks/barriers and distort reporting. Provide a separate referral-grain view (e.g. `vwBarrierReferralReporting`) if individual referral-level analysis is required.

Expose the view to Sentinel's reporting configuration with the same case/jurisdiction/disease access controls as its source records. SQL views themselves do not automatically enforce application permissions.

A report must be able to answer: **Of people given isolation advice, how many had a barrier preventing effective isolation, what barriers were reported, how many were referred, and how many barriers were ultimately mitigated?** Ensure the denominator uses a structured isolation-advice outcome/assessment indicator, not an assumption that every completed task delivered instructions. Similarly, retain explicit negative questionnaire answers so `not assessed` cannot be confused with `no barriers`.

## Application UI and permissions

Extend existing interfaces rather than creating a parallel case management module:

- **Task details:** read/write barrier instances, resolution and referral history without changing terminal parent-task state.
- **Barrier lookup settings:** configure names, codes and descriptions.
- **Organisation details:** configure one or more services, supported barriers via the junction, jurisdiction coverage, referral template and messaging endpoint.
- **Survey mapping editor:** support identifying multiple barriers and structured consent mapping.
- **Referrals:** prepare, inspect, validate, authorise and review each referral and its transmission/receipt events.
- **Reports:** register `vwTaskBarrierReporting` as a supported report data source.

Suggested permission capabilities: `TaskBarrier.View`, `TaskBarrier.Manage`, `BarrierDefinition.Manage`, `OrganisationService.Manage`, `Referral.View`, `Referral.Create`, `Referral.Manage`. Apply existing case-level access checks; do not rely solely on route or UI visibility.

Audit barrier updates, configuration changes, service selection, consent validation, referral creation and each transmission state change. Protect referral payloads, which can contain sensitive personal data. Avoid storing plaintext credentials or full referral payloads in application logs.

## Implementation sequence

1. **Data model and migration:** add barrier definitions, instances, organisation services, service/barrier and jurisdiction junctions, referral templates/records, and consent records. Define FK constraints, indexes, soft-deactivation and concurrency behaviour.
2. **Survey materialisation:** extend mapping execution to create/update multiple barriers idempotently, track assessment completeness, and persist explicit consent responses in structured form.
3. **Configuration/UI:** add barrier lookup administration and organisation service/barrier/jurisdiction configuration; add task-level barrier and referral screens.
4. **Internal referral workflow:** implement eligible-service queries, human selection, descriptive eligibility review, payload validation, consent gating and independent referral lifecycle (manual tracking without electronic transport).
5. **Reporting:** create/register `vwTaskBarrierReporting`; add a separate referral-grain view as required; verify count cardinality and access controls.
6. **Messaging integration:** once shared electronic messaging infrastructure exists, configure recipient endpoints, outbox delivery, message correlation, acknowledgements and failure recovery; enable automatic handoff only after consent and validation.

## Acceptance criteria

- One task can have multiple distinct barriers; one barrier can have multiple independent referrals.
- No `BarrierDefinition` → `TaskTemplate` relationship and no `TaskBarrier.SourceQuestionName` column are required.
- One organisation service can support multiple barrier definitions through `OrganisationServiceBarrier`.
- Service coverage uses existing jurisdictions; non-structured eligibility is human-readable guidance, not claimed to be automatically evaluated.
- The originating survey persists an **explicitly answered** consent question, and absent/negative/withdrawn/out-of-scope consent prevents all automatic referral processing and transmission.
- Mapping can retry safely without duplicate barrier or referral records.
- Task completion remains independent of barrier/referral completion and existing terminal-task protections remain intact.
- `vwTaskBarrierReporting` exists, maintains one row per barrier, and permits accurate task/barrier/referral aggregation.
- Electronic referral requires an identifiable configured messaging endpoint and linked outbound message record; no unsupported claim of receipt is made at send time.
- Historical data remains interpretable after configuration changes, and new permissions/audit coverage are tested.

## Deferred decisions and dependencies

- Electronic messaging protocol, transport adapters, security and acknowledgement semantics.
- Whether external organisations can log into Sentinel versus respond through integrated systems.
- Whether jurisdiction coverage automatically expands to descendants.
- Advanced machine-readable provider eligibility and automatic provider ranking.
- Referral consent wording, granular recipient scoping, expiry and revocation policies (these require governance review before deployment).
- Whether barrier reassessment can reopen a previously closed barrier or creates a new occurrence.
- Whether the reporting system requires a dedicated data-source adapter for SQL views.

These decisions should be resolved before production automation, but need not block the relational model and manual referral workflow.
