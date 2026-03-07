using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Sentinel.Data;
using Sentinel.Models;

namespace Sentinel.Services;

/// <summary>
/// Interceptor that automatically creates tasks based on multiple triggers:
/// - New Cases created (AutoCreateOnCaseCreation)
/// - New Contacts created (AutoCreateOnContactCreation) - Cases with Type=Contact
/// - Lab Results confirmed (AutoCreateOnLabConfirmation)
/// This ensures tasks are created regardless of how entities are created (UI, survey mapping, API, etc.)
/// </summary>
public class CaseCreationInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CaseCreationInterceptor> _logger;

    public CaseCreationInterceptor(IServiceProvider serviceProvider, ILogger<CaseCreationInterceptor> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext context)
        {
            // === TRIGGER 1 & 2: Detect new Cases and Contacts ===
            var newCases = context.ChangeTracker.Entries<Case>()
                .Where(e => e.State == EntityState.Added && e.Entity.DiseaseId.HasValue)
                .Select(e => new { 
                    e.Entity.Id, 
                    e.Entity.Type,
                    e.Entity.DiseaseId, 
                    e.Entity.FriendlyId 
                })
                .ToList();

            if (newCases.Any())
            {
                var caseCount = newCases.Count(c => c.Type == CaseType.Case);
                var contactCount = newCases.Count(c => c.Type == CaseType.Contact);
                
                _logger.LogInformation(
                    "?? CaseCreationInterceptor: Detected {CaseCount} new cases and {ContactCount} new contacts",
                    caseCount,
                    contactCount
                );

                // Store for post-save processing
                context.SetCasesAwaitingTaskCreation(newCases.Select(c => new CaseTaskTrigger 
                { 
                    CaseId = c.Id, 
                    Type = c.Type 
                }).ToList());
            }

            // === TRIGGER 3: Detect lab result confirmations ===
            var confirmedLabs = context.ChangeTracker.Entries<LabResult>()
                .Where(e => e.State == EntityState.Modified && 
                           e.Entity.CaseId != Guid.Empty &&
                           IsLabConfirmed(e))
                .Select(e => e.Entity.CaseId)
                .Distinct()
                .ToList();

            if (confirmedLabs.Any())
            {
                _logger.LogInformation(
                    "?? CaseCreationInterceptor: Detected {Count} lab confirmations",
                    confirmedLabs.Count
                );

                context.SetLabConfirmationsAwaitingTaskCreation(confirmedLabs);
            }
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is ApplicationDbContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<ITaskService>();

            // === Process Case/Contact Creation Triggers ===
            var caseTriggers = context.GetCasesAwaitingTaskCreation();
            if (caseTriggers != null && caseTriggers.Any())
            {
                _logger.LogInformation(
                    "? Entities saved. Processing {Count} case/contact creation triggers...",
                    caseTriggers.Count
                );

                foreach (var trigger in caseTriggers)
                {
                    try
                    {
                        List<CaseTask> tasksCreated;
                        
                        if (trigger.Type == CaseType.Contact)
                        {
                            // Use Contact creation trigger
                            tasksCreated = await taskService.CreateTasksForCase(
                                trigger.CaseId, 
                                TaskTrigger.OnContactCreation
                            );
                            
                            if (tasksCreated.Any())
                            {
                                _logger.LogInformation(
                                    "? Auto-created {TaskCount} tasks for contact {CaseId}",
                                    tasksCreated.Count,
                                    trigger.CaseId
                                );
                            }
                        }
                        else
                        {
                            // Use Case creation trigger (corresponds to AutoCreateOnCaseCreation)
                            tasksCreated = await taskService.CreateTasksForCase(
                                trigger.CaseId,
                                TaskTrigger.OnCaseCreation
                            );
                            
                            if (tasksCreated.Any())
                            {
                                _logger.LogInformation(
                                    "?? Auto-created {TaskCount} tasks for case {CaseId}",
                                    tasksCreated.Count,
                                    trigger.CaseId
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "? Error auto-creating tasks for {Type} {CaseId}: {Message}",
                            trigger.Type,
                            trigger.CaseId,
                            ex.Message
                        );
                    }
                }

                context.ClearCasesAwaitingTaskCreation();
            }

            // === Process Lab Confirmation Triggers ===
            var labCaseIds = context.GetLabConfirmationsAwaitingTaskCreation();
            if (labCaseIds != null && labCaseIds.Any())
            {
                _logger.LogInformation(
                    "?? Processing {Count} lab confirmation triggers...",
                    labCaseIds.Count
                );

                foreach (var caseId in labCaseIds)
                {
                    try
                    {
                        var tasksCreated = await taskService.CreateTasksForCase(
                            caseId, 
                            TaskTrigger.OnLabConfirmation
                        );
                        
                        if (tasksCreated.Any())
                        {
                            _logger.LogInformation(
                                "?? Auto-created {TaskCount} tasks for lab-confirmed case {CaseId}",
                                tasksCreated.Count,
                                caseId
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "? Error auto-creating lab confirmation tasks for case {CaseId}: {Message}",
                            caseId,
                            ex.Message
                        );
                    }
                }

                context.ClearLabConfirmationsAwaitingTaskCreation();
            }
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    /// <summary>
    /// Determines if a lab result has been newly confirmed
    /// </summary>
    private bool IsLabConfirmed(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<LabResult> entry)
    {
        // Check if TestResultId changed to a "positive" or "confirmed" result
        // This is a simplified check - you may want to add more sophisticated logic
        var originalResultId = entry.OriginalValues.GetValue<int?>(nameof(LabResult.TestResultId));
        var currentResultId = entry.CurrentValues.GetValue<int?>(nameof(LabResult.TestResultId));
        
        // Trigger if TestResultId was null/unset and is now set
        return originalResultId == null && currentResultId != null;
    }
}

/// <summary>
/// Helper class to track case/contact creation triggers
/// </summary>
public class CaseTaskTrigger
{
    public Guid CaseId { get; set; }
    public CaseType Type { get; set; }
}

/// <summary>
/// Extension methods to store state between SavingChanges and SavedChanges
/// </summary>
public static class ApplicationDbContextExtensions
{
    // Static storage for pending triggers (keyed by context instance)
    private static readonly Dictionary<ApplicationDbContext, List<CaseTaskTrigger>> _pendingCaseTriggers = new();
    private static readonly Dictionary<ApplicationDbContext, List<Guid>> _pendingLabConfirmations = new();

    // === Case/Contact Creation Triggers ===
    
    public static void SetCasesAwaitingTaskCreation(this ApplicationDbContext context, List<CaseTaskTrigger> triggers)
    {
        _pendingCaseTriggers[context] = triggers;
    }

    public static List<CaseTaskTrigger>? GetCasesAwaitingTaskCreation(this ApplicationDbContext context)
    {
        return _pendingCaseTriggers.TryGetValue(context, out var triggers) ? triggers : null;
    }

    public static void ClearCasesAwaitingTaskCreation(this ApplicationDbContext context)
    {
        _pendingCaseTriggers.Remove(context);
    }

    // === Lab Confirmation Triggers ===
    
    public static void SetLabConfirmationsAwaitingTaskCreation(this ApplicationDbContext context, List<Guid> caseIds)
    {
        _pendingLabConfirmations[context] = caseIds;
    }

    public static List<Guid>? GetLabConfirmationsAwaitingTaskCreation(this ApplicationDbContext context)
    {
        return _pendingLabConfirmations.TryGetValue(context, out var caseIds) ? caseIds : null;
    }

    public static void ClearLabConfirmationsAwaitingTaskCreation(this ApplicationDbContext context)
    {
        _pendingLabConfirmations.Remove(context);
    }
}
