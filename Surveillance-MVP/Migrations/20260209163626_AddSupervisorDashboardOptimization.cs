using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Surveillance_MVP.Migrations
{
    /// <inheritdoc />
    public partial class AddSupervisorDashboardOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ================================================================
            // SUPERVISOR DASHBOARD OPTIMIZATION
            // Creates 7 performance indexes and fixes IsInterviewTask flag
            // ================================================================
            
            // Index 1: Main Supervisor Dashboard Query
            // Covers: IsInterviewTask + AssignedToUserId + Status with includes
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_CaseTasks_SupervisorDashboard' 
                               AND object_id = OBJECT_ID('CaseTasks'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_CaseTasks_SupervisorDashboard
                    ON CaseTasks(IsInterviewTask, AssignedToUserId, Status)
                    INCLUDE (Id, Priority, CurrentAttemptCount, LastCallAttempt, 
                            MaxCallAttempts, CaseId, TaskTypeId, Title, CreatedAt)
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 2: Worker Statistics Query
            // Optimizes task count queries grouped by worker
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_CaseTasks_WorkerStats' 
                               AND object_id = OBJECT_ID('CaseTasks'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_CaseTasks_WorkerStats
                    ON CaseTasks(AssignedToUserId, Status)
                    INCLUDE (CreatedAt, CompletedAt, IsInterviewTask)
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 3: Task Call Attempts Query
            // Optimizes call attempt lookups by task
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_TaskCallAttempts_TaskDate' 
                               AND object_id = OBJECT_ID('TaskCallAttempts'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_TaskCallAttempts_TaskDate
                    ON TaskCallAttempts(TaskId, AttemptedAt DESC)
                    INCLUDE (Outcome, DurationSeconds, AttemptedByUserId)
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 4: Today's Call Attempts (for worker statistics)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_TaskCallAttempts_WorkerDate' 
                               AND object_id = OBJECT_ID('TaskCallAttempts'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_TaskCallAttempts_WorkerDate
                    ON TaskCallAttempts(AttemptedByUserId, AttemptedAt DESC)
                    INCLUDE (Outcome, DurationSeconds)
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 5: Unassigned Tasks Query (Filtered Index)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_CaseTasks_Unassigned' 
                               AND object_id = OBJECT_ID('CaseTasks'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_CaseTasks_Unassigned
                    ON CaseTasks(IsInterviewTask, AssignedToUserId, Status, Priority)
                    INCLUDE (CaseId, CreatedAt, Title)
                    WHERE AssignedToUserId IS NULL
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 6: Escalated Tasks Query (Filtered Index)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_CaseTasks_Escalated' 
                               AND object_id = OBJECT_ID('CaseTasks'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_CaseTasks_Escalated
                    ON CaseTasks(IsInterviewTask, EscalationLevel DESC, CreatedAt)
                    INCLUDE (CaseId, Title, Priority, Status)
                    WHERE EscalationLevel > 0
                    WITH (FILLFACTOR = 90);
                END
            ");

            // Index 7: Interview Workers Lookup (Filtered Index)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes 
                               WHERE name = 'IX_AspNetUsers_InterviewWorker' 
                               AND object_id = OBJECT_ID('AspNetUsers'))
                BEGIN
                    CREATE NONCLUSTERED INDEX IX_AspNetUsers_InterviewWorker
                    ON AspNetUsers(IsInterviewWorker, AvailableForAutoAssignment)
                    INCLUDE (FirstName, LastName, PrimaryLanguage, CurrentTaskCapacity)
                    WHERE IsInterviewWorker = 1
                    WITH (FILLFACTOR = 90);
                END
            ");

            // ================================================================
            // FIX ISINTERVIEWTASK FLAG
            // Update tasks assigned to interview workers
            // ================================================================
            
            migrationBuilder.Sql(@"
                UPDATE ct
                SET ct.IsInterviewTask = 1,
                    ct.ModifiedAt = GETUTCDATE()
                FROM CaseTasks ct
                INNER JOIN AspNetUsers u ON ct.AssignedToUserId = u.Id
                WHERE u.IsInterviewWorker = 1
                  AND ct.IsInterviewTask = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop all indexes created by this migration
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_CaseTasks_SupervisorDashboard ON CaseTasks;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_CaseTasks_WorkerStats ON CaseTasks;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_TaskCallAttempts_TaskDate ON TaskCallAttempts;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_TaskCallAttempts_WorkerDate ON TaskCallAttempts;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_CaseTasks_Unassigned ON CaseTasks;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_CaseTasks_Escalated ON CaseTasks;
            ");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_AspNetUsers_InterviewWorker ON AspNetUsers;
            ");
            
            // Note: We do NOT revert the IsInterviewTask fix as that would break functionality
        }
    }
}
