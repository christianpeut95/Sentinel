# HL7 Test Codes Reference Guide

## What Should the Test Code Be?

**Short Answer:** Use **LOINC codes** for realism, or **lab-specific codes** for testing specific lab configurations.

---

## LOINC Codes (Recommended)

LOINC (Logical Observation Identifiers Names and Codes) is the **universal standard** for lab test identification in healthcare. It's what real HL7 messages should use.

### Format
- Full format: `87798-0` (5-digit number + dash + check digit)
- HL7 format: Often used without dash: `87798`

### Common LOINC Codes for STI Testing

| Disease | LOINC Code | Test Name | Test Method |
|---------|------------|-----------|-------------|
| **Chlamydia** | `87798-0` | Chlamydia trachomatis DNA | NAA with probe detection |
| **Gonorrhea** | `87491-0` | Neisseria gonorrhoeae DNA | NAA with probe detection |
| **CT/GC Combo** | `87800-4` | CT/GC combo test | NAA with probe detection |
| **Trichomonas** | `87661-7` | Trichomonas vaginalis DNA | NAA with probe detection |
| **HIV 1+2** | `75622-1` | HIV 1+2 antibody screen | Immunoassay |
| **HIV RNA** | `62469-2` | HIV 1 RNA | NAA with probe detection |
| **Hepatitis C** | `16128-1` | Hepatitis C antibody | Immunoassay |
| **Hepatitis B** | `5196-1` | Hepatitis B surface Ag | Immunoassay |
| **Syphilis RPR** | `20507-0` | Syphilis RPR | Agglutination |
| **Syphilis TP** | `11084-1` | Treponema pallidum Ab | Immunoassay |
| **Herpes Simplex 1** | `43030-6` | HSV 1 IgG | Immunoassay |
| **Herpes Simplex 2** | `43031-4` | HSV 2 IgG | Immunoassay |
| **COVID-19** | `94500-6` | SARS-CoV-2 RNA | NAA with probe detection |
| **COVID Antigen** | `94558-4` | SARS-CoV-2 Ag | Immunoassay |

### Common LOINC Codes for Other Tests

| Test Type | LOINC Code | Test Name |
|-----------|------------|-----------|
| **Blood glucose** | `2345-7` | Glucose [Mass/volume] in Serum or Plasma |
| **Hemoglobin A1c** | `4548-4` | Hemoglobin A1c/Hemoglobin.total in Blood |
| **Complete Blood Count (CBC)** | `58410-2` | CBC panel - Blood by Automated count |
| **White Blood Cell** | `6690-2` | Leukocytes [#/volume] in Blood |
| **Red Blood Cell** | `789-8` | Erythrocytes [#/volume] in Blood |
| **Platelet** | `777-3` | Platelets [#/volume] in Blood |
| **Creatinine** | `2160-0` | Creatinine [Mass/volume] in Serum or Plasma |
| **BUN** | `3094-0` | Urea nitrogen [Mass/volume] in Serum or Plasma |
| **ALT** | `1742-6` | Alanine aminotransferase [Enzymatic activity/volume] in Serum or Plasma |
| **AST** | `1920-8` | Aspartate aminotransferase [Enzymatic activity/volume] in Serum or Plasma |

---

## Lab-Specific Test Codes

Different labs use their own internal coding systems. These are useful when testing **specific lab configurations**.

### Quest Diagnostics Style
```
87798    (LOINC without dash)
183064   (Quest internal code)
CT-NAAT  (Mnemonic code)
```

### LabCorp Style
```
183064   (LabCorp test code)
CT-PCR   (Mnemonic)
CHLAM    (Short code)
```

### Generic Hospital/Reference Lab
```
CHLAM
GONOR
COVID
HIV
HEPC
SYPHILIS
```

---

## How Test Codes Appear in HL7

In an **OBX (Observation/Result) segment**:

```
OBX|1|ST|87798^CHLAMYDIA NAAT^LN||POSITIVE|||A|||F|||20260125120000
```

**Breaking it down:**
- `OBX` = Segment identifier (observation/result)
- `1` = Set ID (sequence number)
- `ST` = Value type (String/Text)
- `87798` = **Test Code** (LOINC without dash)
- `CHLAMYDIA NAAT` = **Test Name** (human-readable description)
- `LN` = **Coding System** (LN = LOINC)
  - `LN` = LOINC
  - `L` = Lab-specific
  - `SNM` = SNOMED
  - `CPT4` = CPT codes
- `POSITIVE` = **Result value**
- `A` = **Abnormal flag**

**Alternative with full LOINC:**
```
OBX|1|ST|87798-0^CHLAMYDIA NAAT^LN||POSITIVE|||A|||F|||20260125120000
```

---

## Quick-Select Feature Added to Generator

I've added a **"Common Tests" dropdown** to the UI with pre-populated LOINC codes:

### Available Quick-Select Tests:

| Selection | Code | Test Name | Typical Result |
|-----------|------|-----------|----------------|
| **Chlamydia (CT)** | 87798-0 | CHLAMYDIA TRACHOMATIS NAAT | POSITIVE |
| **Gonorrhea (GC)** | 87491-0 | NEISSERIA GONORRHOEAE NAAT | POSITIVE |
| **COVID-19** | 94500-6 | SARS-COV-2 RNA NAA | DETECTED |
| **HIV 1+2 Antibody** | 75622-1 | HIV 1+2 ANTIBODY | REACTIVE |
| **Hepatitis C** | 16128-1 | HEPATITIS C ANTIBODY | REACTIVE |
| **Syphilis RPR** | 20507-0 | SYPHILIS RPR | REACTIVE |
| **Trichomonas** | 87661-7 | TRICHOMONAS VAGINALIS NAAT | POSITIVE |

### Usage:
1. Click the **"Or add common test..."** dropdown
2. Select a disease
3. Biomarker is automatically added with:
   - ✅ Proper LOINC code
   - ✅ Standard test name
   - ✅ Appropriate result value
   - ✅ Correct abnormal flag
   - ✅ Reference range

---

## Recommendations for Testing

### For Configuration Validation
Use **LOINC codes** that match your disease matching rules:
- If your system maps LOINC `87798-0` → Chlamydia disease
- Use that exact code in test messages

### For Parser Testing
Mix different code formats:
- `87798-0` (with dash)
- `87798` (without dash)
- `CT-NAAT` (mnemonic)
- Lab-specific internal codes

### For Workflow Testing
Use **realistic LOINC codes** so the test closely mimics production:
- Messages parse correctly
- Disease matching works
- Case creation follows expected flow
- Reporting/analytics see realistic data

---

## LOINC Resources

**Official LOINC Database:**
- Website: https://loinc.org
- Search: https://search.loinc.org
- Free account for browsing and searching codes

**Common LOINC Subsets:**
- Laboratory: Codes 1-99999
- Clinical: Codes 10000-99999
- Survey instruments: Special codes

---

## Best Practices

### ✅ DO:
- Use full LOINC codes with dash (`87798-0`) for maximum compatibility
- Include LOINC code in the `LOINCCode` field even if it's also the test code
- Use standard test names that match LOINC descriptions
- Test with codes your production system actually receives

### ❌ DON'T:
- Mix coding systems in the same field (e.g., LOINC + internal codes concatenated)
- Invent LOINC-like codes that don't exist (they won't validate)
- Use outdated/deprecated LOINC codes without checking current status
- Forget to set the coding system identifier (`LN`, `L`, etc.)

---

## Example Test Messages

### Chlamydia Positive (LOINC)
```
Test Code: 87798-0
Test Name: CHLAMYDIA TRACHOMATIS NAAT
LOINC Code: 87798-0
Result: POSITIVE
Reference Range: NEGATIVE
Abnormal Flag: A
```

### COVID-19 Detected (LOINC)
```
Test Code: 94500-6
Test Name: SARS-COV-2 RNA NAA
LOINC Code: 94500-6
Result: DETECTED
Reference Range: NOT DETECTED
Abnormal Flag: A
```

### HIV Reactive (LOINC)
```
Test Code: 75622-1
Test Name: HIV 1+2 ANTIBODY
LOINC Code: 75622-1
Result: REACTIVE
Reference Range: NONREACTIVE
Abnormal Flag: A
```

---

**Summary:**
- ✅ **Use LOINC codes** for realistic test messages
- ✅ **Use the quick-select dropdown** for common STI tests
- ✅ **Match your disease mapping rules** for validation testing
- ✅ **Verify codes** at https://loinc.org if unsure

*Last Updated: 2026-01-25*
