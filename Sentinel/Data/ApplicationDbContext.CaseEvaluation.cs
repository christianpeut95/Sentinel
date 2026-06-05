using Microsoft.EntityFrameworkCore;
using Sentinel.Models;
using Sentinel.Services.CaseDefinitionEvaluation;

namespace Sentinel.Data
{
    /// <summary>
    /// Partial class extension for ApplicationDbContext handling case evaluation triggers
    /// </summary>
    public partial class ApplicationDbContext
    {
        private ICaseEvaluationQueue? _evaluationQueue;

        /// <summary>
        /// Set the evaluation queue (called from DI container)
        /// </summary>
        public void SetEvaluationQueue(ICaseEvaluationQueue queue)
        {
            _evaluationQueue = queue;
        }

        /// <summary>
        /// Detect changes to cases, lab results, symptoms, and custom fields
        /// Returns list of (CaseId, ChangeReason) tuples for queueing
        /// Called from SaveChangesAsync BEFORE base.SaveChangesAsync()
        /// </summary>
        protected List<(Guid CaseId, string ChangeReason)> DetectCaseEvaluationChanges()
        {
            var changes = new List<(Guid CaseId, string ChangeReason)>();

            // Track which cases we've already queued to avoid duplicates
            var queuedCases = new HashSet<Guid>();

            // 1. Direct Case entity changes
            var caseEntries = ChangeTracker.Entries<Case>()
                .Where(e => e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in caseEntries)
            {
                var caseId = entry.Entity.Id;

                // Skip if case has manual override active
                if (entry.Entity.ConfirmationStatusManualOverride)
                {
                    continue;
                }

                // Check if ConfirmationStatusId was manually changed
                // If so, DO NOT queue evaluation (user made deliberate change)
                var statusProperty = entry.Property(nameof(Case.ConfirmationStatusId));
                if (statusProperty.IsModified && 
                    statusProperty.OriginalValue != statusProperty.CurrentValue &&
                    !entry.Entity.IsAutoClassified) // Only block if NOT auto-classified
                {
                    // This will be handled by Edit PageModel setting manual override
                    continue;
                }

                // Detect which properties changed
                var changedProperties = new List<string>();

                // Check key clinical fields
                if (entry.Property(nameof(Case.DateOfOnset)).IsModified)
                    changedProperties.Add("date of onset");
                if (entry.Property(nameof(Case.Hospitalised)).IsModified)
                    changedProperties.Add("hospitalization status");
                if (entry.Property(nameof(Case.DateOfAdmission)).IsModified)
                    changedProperties.Add("admission date");
                if (entry.Property(nameof(Case.DateOfDischarge)).IsModified)
                    changedProperties.Add("discharge date");
                if (entry.Property(nameof(Case.DiedDueToDisease)).IsModified)
                    changedProperties.Add("death status");

                if (changedProperties.Any())
                {
                    var reason = $"Case updated: {string.Join(", ", changedProperties)}";
                    changes.Add((caseId, reason));
                    queuedCases.Add(caseId);
                }
            }

            // 2. Lab Result changes
            var labResultEntries = ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in labResultEntries)
            {
                var caseId = entry.Entity.CaseId;

                // Skip if LabResult not yet linked to a Case (HL7 processing scenario)
                if (caseId == null)
                    continue;

                if (queuedCases.Contains(caseId.Value))
                    continue; // Already queued

                // Check if case has manual override
                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var action = entry.State == EntityState.Added ? "added" : "updated";
                // Note: LabResult doesn't have TestName, use generic description
                var reason = $"Lab result {action}";

                changes.Add((caseId.Value, reason));
                queuedCases.Add(caseId.Value);
            }

            // 3. Case Symptom changes
            var symptomEntries = ChangeTracker.Entries<CaseSymptom>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Deleted)
                .ToList();

            foreach (var entry in symptomEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var action = entry.State == EntityState.Added ? "added" : "removed";
                var reason = $"Symptom {action}";

                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            // 4. Custom Field changes (String)
            var customStringEntries = ChangeTracker.Entries<CaseCustomFieldString>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in customStringEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var reason = $"Custom field updated: {entry.Entity.FieldDefinitionId}";
                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            // 5. Custom Field changes (Number)
            var customNumberEntries = ChangeTracker.Entries<CaseCustomFieldNumber>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in customNumberEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var reason = $"Custom field updated: {entry.Entity.FieldDefinitionId}";
                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            // 6. Custom Field changes (Date)
            var customDateEntries = ChangeTracker.Entries<CaseCustomFieldDate>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in customDateEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var reason = $"Custom field updated: {entry.Entity.FieldDefinitionId}";
                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            // 7. Custom Field changes (Boolean)
            var customBoolEntries = ChangeTracker.Entries<CaseCustomFieldBoolean>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in customBoolEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var reason = $"Custom field updated: {entry.Entity.FieldDefinitionId}";
                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            // 8. Custom Field changes (Lookup)
            var customLookupEntries = ChangeTracker.Entries<CaseCustomFieldLookup>()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                .ToList();

            foreach (var entry in customLookupEntries)
            {
                var caseId = entry.Entity.CaseId;
                if (queuedCases.Contains(caseId))
                    continue;

                var caseEntity = Cases.Local.FirstOrDefault(c => c.Id == caseId);
                if (caseEntity?.ConfirmationStatusManualOverride == true)
                    continue;

                var reason = $"Custom field updated: {entry.Entity.FieldDefinitionId}";
                changes.Add((caseId, reason));
                queuedCases.Add(caseId);
            }

            return changes;
        }

        /// <summary>
        /// Queue case evaluation jobs after successful save
        /// Called from SaveChangesAsync AFTER base.SaveChangesAsync()
        /// </summary>
        protected async Task QueueCaseEvaluationsAsync(List<(Guid CaseId, string ChangeReason)> changes)
        {
            if (_evaluationQueue == null || !changes.Any())
                return;

            foreach (var (caseId, reason) in changes)
            {
                await _evaluationQueue.QueueEvaluationAsync(caseId, reason);
            }
        }
    }
}
