-- Seed data for Test Results (Qualitative Results)
-- Run this script to populate initial test result options

-- Standard Test Results
INSERT INTO TestResults (Name, Description, SnomedCode, SnomedDisplay, Hl7Code, ExportCode, DisplayOrder, IsActive, TestTypeId)
VALUES 
('Positive', 'Presence of target pathogen or antibody detected', '10828004', 'Positive (qualifier value)', 'POS', 'P', 10, 1, NULL),
('Negative', 'Target pathogen or antibody not detected', '260385009', 'Negative (qualifier value)', 'NEG', 'N', 20, 1, NULL),
('Indeterminate', 'Result cannot be definitively determined', '82334004', 'Indeterminate (qualifier value)', 'IND', 'I', 30, 1, NULL),
('Borderline', 'Result is at the threshold of detection', '260325005', 'Borderline (qualifier value)', 'B', 'B', 40, 1, NULL),
('Equivocal', 'Result is uncertain or ambiguous', '419984006', 'Equivocal (qualifier value)', 'EQV', 'E', 50, 1, NULL);

-- Serology-specific Results
INSERT INTO TestResults (Name, Description, SnomedCode, SnomedDisplay, Hl7Code, ExportCode, DisplayOrder, IsActive, TestTypeId)
VALUES 
('Reactive', 'Antibody or antigen detected (serology)', '11214006', 'Reactive (qualifier value)', 'R', 'R', 60, 1, NULL),
('Non-Reactive', 'No antibody or antigen detected (serology)', '131194007', 'Non-reactive (qualifier value)', 'NR', 'NR', 70, 1, NULL);

-- Culture-specific Results
INSERT INTO TestResults (Name, Description, SnomedCode, SnomedDisplay, Hl7Code, ExportCode, DisplayOrder, IsActive, TestTypeId)
VALUES 
('Detected', 'Pathogen detected in sample', '260373001', 'Detected (qualifier value)', 'D', 'D', 80, 1, NULL),
('Not Detected', 'Pathogen not detected in sample', '260415000', 'Not detected (qualifier value)', 'ND', 'ND', 90, 1, NULL),
('Growth', 'Bacterial or fungal growth observed', NULL, NULL, 'G', 'G', 100, 1, NULL),
('No Growth', 'No bacterial or fungal growth observed', NULL, NULL, 'NG', 'NG', 110, 1, NULL);

-- Additional Results
INSERT INTO TestResults (Name, Description, SnomedCode, SnomedDisplay, Hl7Code, ExportCode, DisplayOrder, IsActive, TestTypeId)
VALUES 
('Presumptive Positive', 'Preliminary positive result pending confirmation', NULL, NULL, 'PP', 'PP', 120, 1, NULL),
('Confirmed Positive', 'Positive result confirmed by additional testing', NULL, NULL, 'CP', 'CP', 130, 1, NULL),
('Inconclusive', 'Test results are inconclusive', '373066001', 'Inconclusive (qualifier value)', 'INC', 'INC', 140, 1, NULL),
('Invalid', 'Test is invalid and needs to be repeated', '455371000124106', 'Invalid result (qualifier value)', 'INV', 'INV', 150, 0, NULL);

GO

-- Verify inserted records
SELECT 
    Id,
    Name,
    SnomedCode,
    SnomedDisplay,
    Hl7Code,
    DisplayOrder,
    IsActive
FROM TestResults
ORDER BY DisplayOrder;
