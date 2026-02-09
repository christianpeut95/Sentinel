# DEPLOY NOW - Quick Instructions

## ? Run This SQL Script RIGHT NOW

### Step 1: Open SQL Server Connection

**Option A: Visual Studio**
1. View ? SQL Server Object Explorer
2. Expand `(localdb)\MSSQLLocalDB` ? Databases ? `aspnet-Surveillance-MVP`
3. Right-click ? New Query

**Option B: SQL Server Management Studio (SSMS)**
1. Connect to: `(localdb)\MSSQLLocalDB`
2. Select database: `aspnet-Surveillance-MVP`
3. New Query

### Step 2: Run the Script

Copy and paste this file:
```
Surveillance-MVP\Migrations\ManualScripts\DeploySupervisorDashboardOptimization_COMBINED.sql
```

Click **Execute** (F5)

### Step 3: Verify Success

You should see output like:
```
? Created IX_CaseTasks_SupervisorDashboard
? Created IX_CaseTasks_WorkerStats
? Created IX_TaskCallAttempts_TaskDate
? Created IX_TaskCallAttempts_WorkerDate
? Created IX_CaseTasks_Unassigned (filtered index)
? Created IX_CaseTasks_Escalated (filtered index)
? Created IX_AspNetUsers_InterviewWorker (filtered index)
? Updated X tasks to IsInterviewTask = 1
Status: ? READY FOR TESTING
```

### Step 4: Test

1. Stop your application (if running)
2. Start your application
3. Navigate to: `/Dashboard/SuperviseInterviews`
4. Should load in <500ms
5. Test filters, pagination, sorting

---

## ?? For Production Deployment

**Do NOT run manual scripts in production!**

### Use the Entity Framework Migration Instead

1. Copy this file to your Migrations folder:
```
Surveillance-MVP\Migrations\AddSupervisorDashboardOptimization_TEMPLATE.cs
```

2. Rename to:
```
Surveillance-MVP\Migrations\20260207XXXXXX_AddSupervisorDashboardOptimization.cs
```
(Replace XXXXXX with timestamp)

3. Update `ApplicationDbContext` model snapshot if needed

4. On deployment, the migration runs automatically

---

## Summary

**NOW (Development)**: Run manual SQL script  
**LATER (Production)**: Use EF migration  

**Script Location**:  
`Surveillance-MVP\Migrations\ManualScripts\DeploySupervisorDashboardOptimization_COMBINED.sql`

**Status**: Ready to execute!
