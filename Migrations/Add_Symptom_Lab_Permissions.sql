-- =====================================================================
-- Add Symptom and Laboratory Permissions
-- =====================================================================
USE [aspnet-Surveillance_MVP-a57ac8ba-ccc6-4d7a-b380-485d37f1148d]
GO

PRINT 'Adding Symptom and Laboratory Permissions...';
PRINT '';

-- Laboratory Permissions
IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Laboratory.View')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (7, 0, 'Laboratory.View', 'View laboratory results');
    PRINT '   ? Added Laboratory.View';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Laboratory.Create')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (7, 1, 'Laboratory.Create', 'Create laboratory results');
    PRINT '   ? Added Laboratory.Create';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Laboratory.Edit')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (7, 2, 'Laboratory.Edit', 'Edit laboratory results');
    PRINT '   ? Added Laboratory.Edit';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Laboratory.Delete')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (7, 3, 'Laboratory.Delete', 'Delete laboratory results');
    PRINT '   ? Added Laboratory.Delete';
END

-- Symptom Permissions
IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Symptom.View')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (8, 0, 'Symptom.View', 'View symptom data on cases');
    PRINT '   ? Added Symptom.View';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Symptom.Create')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (8, 1, 'Symptom.Create', 'Add symptoms to cases');
    PRINT '   ? Added Symptom.Create';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Symptom.Edit')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (8, 2, 'Symptom.Edit', 'Edit symptom data on cases');
    PRINT '   ? Added Symptom.Edit';
END

IF NOT EXISTS (SELECT * FROM Permissions WHERE Name = 'Symptom.Delete')
BEGIN
    INSERT INTO Permissions ([Module], [Action], [Name], [Description])
    VALUES (8, 3, 'Symptom.Delete', 'Delete symptom data from cases');
    PRINT '   ? Added Symptom.Delete';
END

PRINT '';
PRINT '? Symptom and Laboratory permissions added successfully!';
PRINT '';
PRINT 'Note: Module values are: 7=Laboratory, 8=Symptom';
PRINT 'Action values are: 0=View, 1=Create, 2=Edit, 3=Delete';
GO
