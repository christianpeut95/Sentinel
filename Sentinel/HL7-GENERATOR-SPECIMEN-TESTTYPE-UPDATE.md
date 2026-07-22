# HL7 Test Generator Enhancement: Specimen Type & Test Type/Method

## Summary
Added **Specimen Type** and **Test Type/Method** fields to the HL7 test generator to provide more realistic and detailed test data generation.

---

## Changes Made

### 1. **Model Updates** (`Sentinel.HL7Generator/Models/HL7MessageRequest.cs`)

#### Added to `HL7MessageRequest`:
```csharp
public string SpecimenType { get; set; } = "URINE"; // URINE, BLOOD, SWAB, etc.
```

#### Added to `BiomarkerResult`:
```csharp
public string TestType { get; set; } = "NAAT"; // NAAT, PCR, Culture, Antibody, Antigen, etc.
```

---

### 2. **UI Updates** (`Components/Pages/Settings/HL7/GenerateTestFiles.razor`)

#### Specimen Type Dropdown (Section 4: Specimen & Order):
Added comprehensive specimen type selector with common options:
- Urine
- Blood
- Swab (generic)
- Vaginal Swab
- Urethral Swab
- Cervical Swab
- Throat Swab
- Nasal Swab
- Rectal Swab
- Serum
- Plasma
- Sputum

#### Test Type/Method Dropdown (Biomarker Cards):
Added test method selector for each biomarker with options:
- **NAAT** (Nucleic Acid Amplification Test)
- **PCR** (Polymerase Chain Reaction)
- **Culture**
- **Antibody**
- **Antigen**
- **ELISA**
- **Rapid Test**
- **Serology**
- **Molecular**

**Layout Change**: Reorganized biomarker fields to 4-4-4-6 column layout:
- Test Code: 4 cols
- LOINC Code: 4 cols
- Test Type: 4 cols
- Result: 4 cols (moved to new row)
- Abnormal Flag: 6 cols

---

### 3. **Code-Behind Updates** (GenerateTestFiles.razor @code)

Updated `AddBiomarker()` method to include default TestType:
```csharp
TestType = "NAAT"
```

Updated `AddCommonTest()` method to include appropriate test types for each common test:
- **CT (Chlamydia)**: NAAT
- **GC (Gonorrhea)**: NAAT
- **COVID-19**: PCR
- **HIV**: ANTIBODY
- **Hepatitis C**: ANTIBODY
- **Syphilis RPR**: SEROLOGY
- **Trichomonas**: NAAT

---

### 4. **HL7 Message Builder Updates** (`Sentinel.HL7Generator/Services/HL7MessageBuilder.cs`)

#### OBR Segment (Specimen Source - OBR-15):
Now includes the specimen type:
```csharp
// Specimen Source (OBR-15)
_message.Append($"{_request.SpecimenType}^^^^^^^^^{_request.SpecimenType}");
```

#### OBX Segment (Observation Method - OBX-17):
Added test type/method to each observation:
```csharp
_message.Append(FieldSeparator);
_message.Append(string.Empty); // Producer's ID (OBX-15)
_message.Append(FieldSeparator);
_message.Append(string.Empty); // Responsible Observer (OBX-16)
_message.Append(FieldSeparator);
// Observation Method (OBX-17)
_message.Append($"{result.TestType}^{result.TestType}");
```

---

## HL7 Standard Compliance

### OBR-15 (Specimen Source)
Format: `Specimen Source Name^Specimen Source Name Alternate^Specimen Source Name Coding System^...`

Example:
```
OBR|...|URINE^^^^^^^^^URINE|...
```

### OBX-17 (Observation Method)
Format: `Method Identifier^Method Text^Coding System^...`

Example:
```
OBX|1|ST|87798-0^CHLAMYDIA TRACHOMATIS NAAT^LN||POSITIVE||NEGATIVE|A||F|||20260625120000|||NAAT^NAAT
```

---

## Example Generated HL7 Message

```hl7
MSH|^~\&|LAB|TESTFACILITY|SENTINEL|HOSPITAL|20260625120000||ORU^R01|MSG20260625120000|P|2.5.1
PID|1||P123456^^^MRN||DOE^JOHN||19900101|M
OBR|1|||87798-0^CHLAMYDIA TRACHOMATIS NAAT^LN|||||20260625120000|||||||URINE^^^^^^^^^URINE|SMITH^JANE^^^^^^^^1234567890||||||F
OBX|1|ST|87798-0^CHLAMYDIA TRACHOMATIS NAAT^LN||POSITIVE||NEGATIVE|A||F|||20260625120000|||NAAT^NAAT
```

---

## Testing Recommendations

1. **Specimen Type Coverage**: Generate tests with different specimen types and verify they appear correctly in:
   - Generated HL7 files (OBR-15)
   - Parsed/extracted data in the database
   - UI displays (monitoring dashboard, lab results)

2. **Test Type/Method Coverage**: Verify test methods appear in:
   - Generated OBX-17 segments
   - Database extraction/mapping
   - Reports and result displays

3. **Common Test Quick-Select**: Verify that selecting common tests (CT, GC, COVID, etc.) populates appropriate:
   - LOINC codes
   - Test names
   - Test types/methods
   - Default results

4. **Template Saving/Loading**: Ensure specimen type and test types are preserved when saving and loading templates

---

## Future Enhancements (Optional)

- Add specimen collection site/body location (OBR-15 component 2-7)
- Map test methods to SNOMED CT codes for OBX-17
- Add specimen condition/quality codes
- Include collection method (e.g., clean catch, venipuncture)
- Support multiple specimen types per order (if needed)

---

## Build Status
✅ **Build Successful** - All changes compile without errors
✅ **Hot Reload Ready** - Can apply changes to running app via hot reload
