-- ========================================================================
-- Seed Default Task Types
-- ========================================================================
-- Run after adding TaskTypes table
-- ========================================================================

-- Delete existing data if re-running
DELETE FROM TaskTypes;

-- Insert default task types
INSERT INTO TaskTypes (Id, Name, Code, Description, IconClass, ColorClass, DisplayOrder, IsActive)
VALUES
    (NEWID(), 'Isolation', 'ISO', 'Tasks related to patient isolation and quarantine', 'bi-house-lock', 'bg-danger', 10, 1),
    (NEWID(), 'Medication', 'MED', 'Tasks related to medication administration (e.g., prophylaxis)', 'bi-capsule', 'bg-primary', 20, 1),
    (NEWID(), 'Monitoring', 'MON', 'Tasks related to ongoing monitoring and symptom checks', 'bi-heart-pulse', 'bg-info', 30, 1),
    (NEWID(), 'Survey/Questionnaire', 'SUR', 'Tasks related to completing surveys and questionnaires', 'bi-clipboard-data', 'bg-warning', 40, 1),
    (NEWID(), 'Laboratory Test', 'LAB', 'Tasks related to specimen collection and laboratory testing', 'bi-droplet', 'bg-success', 50, 1),
    (NEWID(), 'Education', 'EDU', 'Tasks related to providing information and education', 'bi-book', 'bg-secondary', 60, 1),
    (NEWID(), 'Contact Tracing', 'CT', 'Tasks related to identifying and documenting contacts', 'bi-people', 'bg-dark', 70, 1),
    (NEWID(), 'Follow-Up', 'FU', 'Tasks related to follow-up appointments and care', 'bi-calendar-check', 'bg-info', 80, 1),
    (NEWID(), 'Documentation', 'DOC', 'Tasks related to completing required documentation', 'bi-file-text', 'bg-secondary', 90, 1),
    (NEWID(), 'Notification', 'NOT', 'Tasks related to notifying authorities or other parties', 'bi-bell', 'bg-warning', 100, 1);

PRINT 'Default task types seeded successfully';
