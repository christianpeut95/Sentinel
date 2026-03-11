# View Column Comparison - CaseContactTasksFlattened

## Migration SQL (Migrations/20260304060957_AddReportingViews.cs lines 54-106)
```
1. CaseGuid (c.Id)
2. CaseNumber (c.FriendlyId)
3. CaseTypeEnum (c.Type)
4. CaseType (CASE c.Type...)
5. GenerationNumber (0)
6. TransmissionChainPath (c.FriendlyId)
7. TransmittedByCase ('')
8. DateOfOnset (c.DateOfOnset)
9. DateOfNotification (c.DateOfNotification)
10. CaseStatus (cs.Name)
11. PatientId (p.Id)
12. PatientName (CONCAT(p.GivenName, ' ', p.FamilyName))
13. PatientFirstName (p.GivenName)
14. PatientLastName (p.FamilyName)
15. PatientDOB (p.DateOfBirth)
16. AgeAtOnset (DATEDIFF(YEAR, p.DateOfBirth, COALESCE(c.DateOfOnset, GETDATE())))
17. PatientSuburb (p.City)
18. PatientState (p.State)
19. PatientMobile (p.MobilePhone)
20. PatientEmail (p.EmailAddress)
21. DiseaseName (d.Name)
22. DiseaseCode (d.Code)
23. Jurisdiction1 (j1.Name)
24. Jurisdiction2 (j2.Name)
25. Jurisdiction3 (j3.Name)
26. ExposureEventId (CAST(NULL AS UNIQUEIDENTIFIER))
27. ExposureType ('Unknown')
28. ExposureStatusDisplay ('')
29. ExposureDate (CAST(NULL AS DATETIME2))  ?? LINE 83
30. ExposureLocation ('')  ?? LINE 84
31. ContactClassification ('')
32. ConfidenceLevel ('')
33. TaskId (CAST(NULL AS UNIQUEIDENTIFIER))
34. TaskTitle ('')
35. TaskType ('')
36. TaskStatus ('NotStarted')
37. TaskDueDate (CAST(NULL AS DATETIME2))
38. TaskCompletedDate (CAST(NULL AS DATETIME2))  ?? LINE 92
39. TaskCreatedAt (GETDATE())
40. AssignedToName ('')
41. AssignedToEmail ('')
42. AssignmentType ('User')
43. CaseCreatedAt (GETDATE())
44. CaseUpdatedAt (GETDATE())
```
**TOTAL: 44 columns**

## C# Model (Models/Views/CaseContactTaskFlattened.cs)
```
1. CaseGuid ?
2. CaseNumber ?
3. CaseTypeEnum ?
4. CaseType ?
5. GenerationNumber ?
6. TransmissionChainPath ?
7. TransmittedByCase ?
8. DateOfOnset ?
9. DateOfNotification ?
10. CaseStatus ?
11. PatientId ?
12. PatientName ?
13. PatientFirstName ?
14. PatientLastName ?
15. PatientDOB ?
16. AgeAtOnset ?
17. PatientSuburb ?
18. PatientState ?
19. PatientMobile ?
20. PatientEmail ?
21. DiseaseName ?
22. DiseaseCode ?
23. Jurisdiction1 ?
24. Jurisdiction2 ?
25. Jurisdiction3 ?
26. ExposureEventId ?
27. ExposureType ?
28. ExposureStatusDisplay ?
29. ExposureDate ?  ?? EXISTS IN MIGRATION BUT NOT IN DATABASE
30. ExposureLocation ?  ?? EXISTS IN MIGRATION BUT NOT IN DATABASE
31. ContactClassification ?
32. ConfidenceLevel ?
33. TaskId ?
34. TaskTitle ?
35. TaskType ?
36. TaskStatus ?
37. TaskDueDate ?
38. TaskCompletedDate ?  ?? EXISTS IN MIGRATION BUT NOT IN DATABASE
39. TaskCreatedAt ?
40. AssignedToName ?
41. AssignedToEmail ?
42. AssignmentType ?
43. CaseCreatedAt ?
44. CaseUpdatedAt ?
```
**TOTAL: 44 properties - PERFECT MATCH**

## Database (YOUR ACTUAL DATABASE)
? **Missing 3 columns:**
- ExposureDate
- ExposureLocation  
- TaskCompletedDate

## DIAGNOSIS:
? Migration SQL is CORRECT (has all 44 columns)
? C# Model is CORRECT (has all 44 properties)
? DATABASE is OUT OF SYNC (missing 3 columns)

## SOLUTION:
Your database was NOT updated with the migration. You need to:

### Option 1: Run the RecreateReportingViews.sql script
Execute `Scripts/RecreateReportingViews.sql` in your database to manually create the views.

### Option 2: Reapply the migration
```bash
dotnet ef database update
```

### Option 3: Check migration history
```sql
SELECT * FROM __EFMigrationsHistory 
WHERE MigrationId = '20260304060957_AddReportingViews';
```

If it shows the migration was applied, then DROP and recreate the views manually using the SQL script.

The issue is NOT with the C# code - it's that your database doesn't have the updated view definition!
