using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;
using Sentinel.Services.CaseDefinitionEvaluation;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class CriterionEvaluatorTests
    {
        private readonly CriterionEvaluator _evaluator;
        private readonly OperatorEvaluator _operatorEvaluator;
        private readonly FieldResolver _fieldResolver;

        public CriterionEvaluatorTests()
        {
            _operatorEvaluator = new OperatorEvaluator();
            _fieldResolver = new FieldResolver();
            _evaluator = new CriterionEvaluator(_operatorEvaluator, _fieldResolver, NullLogger<CriterionEvaluator>.Instance);
        }

        #region Laboratory Criterion Tests

        [Fact]
        public async Task EvaluateLaboratory_MatchingLabResult_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DateOfOnset = new DateTime(2024, 1, 15),
                LabResults = new List<LabResult>
                {
                    new LabResult
                    {
                        Id = Guid.NewGuid(),
                        SpecimenTypeId = 5,
                        SpecimenCollectionDate = new DateTime(2024, 1, 16),
                        Markers = new List<LabResultMarker>
                        {
                            new LabResultMarker
                            {
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = (object?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateLaboratory_NoMatchingSpecimenType_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                LabResults = new List<LabResult>
                {
                    new LabResult
                    {
                        Id = Guid.NewGuid(),
                        SpecimenTypeId = 99, // Different specimen type
                        Markers = new List<LabResultMarker>
                        {
                            new LabResultMarker
                            {
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = (object?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateLaboratory_WithTimeConstraint_WithinRange_ReturnsTrue()
        {
            // Arrange
            var symptomOnset = new DateTime(2024, 1, 15);
            var collectionDate = new DateTime(2024, 1, 20); // 5 days after onset

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DateOfOnset = symptomOnset,
                LabResults = new List<LabResult>
                {
                    new LabResult
                    {
                        Id = Guid.NewGuid(),
                        SpecimenTypeId = 5,
                        SpecimenCollectionDate = collectionDate,
                        Markers = new List<LabResultMarker>
                        {
                            new LabResultMarker
                            {
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = new
                    {
                        days = 10,
                        relativeTo = "SymptomOnsetDate",
                        direction = "after"
                    }
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateLaboratory_WithTimeConstraint_OutsideRange_ReturnsFalse()
        {
            // Arrange
            var symptomOnset = new DateTime(2024, 1, 15);
            var collectionDate = new DateTime(2024, 1, 30); // 15 days after onset (outside range)

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DateOfOnset = symptomOnset,
                LabResults = new List<LabResult>
                {
                    new LabResult
                    {
                        Id = Guid.NewGuid(),
                        SpecimenTypeId = 5,
                        SpecimenCollectionDate = collectionDate,
                        Markers = new List<LabResultMarker>
                        {
                            new LabResultMarker
                            {
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = new
                    {
                        days = 10,
                        relativeTo = "SymptomOnsetDate",
                        direction = "after"
                    }
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateLaboratory_MultiplePathogenNames_AnyMatch_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                LabResults = new List<LabResult>
                {
                    new LabResult
                    {
                        Id = Guid.NewGuid(),
                        SpecimenTypeId = 5,
                        Markers = new List<LabResultMarker>
                        {
                            new LabResultMarker
                            {
                                Pathogen = new Pathogen { Name = "Rubella Virus" }, // Matches one of the pathogens
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus", "Rubella Virus", "Mumps Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = (object?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateLaboratory_NoLabResults_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                LabResults = new List<LabResult>() // No lab results
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" },
                    timeConstraint = (object?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        #endregion

        #region Clinical Criterion Tests

        [Fact]
        public async Task EvaluateClinical_RequireAll_AllSymptomsPresent_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" }, Severity = "Moderate" },
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" }, Severity = "Mild" },
                    new CaseSymptom { SymptomId = 3, Symptom = new Symptom { Name = "Cough" }, Severity = "Severe" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = true,
                    minCount = (int?)null,
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_RequireAll_MissingOneSymptom_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } },
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" } }
                    // Missing symptom 3
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = true,
                    minCount = (int?)null,
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_MinCount_MeetsRequirement_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } },
                    new CaseSymptom { SymptomId = 3, Symptom = new Symptom { Name = "Cough" } }
                    // 2 out of 3 symptoms
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = false,
                    minCount = 2,
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_MinCount_DoesNotMeetRequirement_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                    // Only 1 out of 3 symptoms
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = false,
                    minCount = 2,
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_ANY_AtLeastOnePresent_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" } }
                    // Only 1 symptom present
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = false,
                    minCount = (int?)null, // ANY mode (no minCount)
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_WithMinSeverity_MeetsSeverity_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" }, Severity = "Severe" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1 },
                    requireAll = false,
                    minCount = (int?)null,
                    severityFilter = "Moderate" // Requires at least Moderate, patient has Severe
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_WithMinSeverity_DoesNotMeetSeverity_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" }, Severity = "Mild" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1 },
                    requireAll = false,
                    minCount = (int?)null,
                    severityFilter = "Moderate" // Requires at least Moderate, patient has only Mild
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateClinical_NoSymptoms_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>() // No symptoms
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Clinical,
                DisplayText = "Clinical criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    symptomIds = new[] { 1, 2, 3 },
                    requireAll = false,
                    minCount = (int?)null,
                    minSeverity = (string?)null
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        #endregion

        #region Custom Field Criterion Tests

        [Fact]
        public async Task EvaluateCustomField_StringField_MatchesEquals_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CustomFieldStrings = new List<CaseCustomFieldString>
                {
                    new CaseCustomFieldString { FieldDefinitionId = 5, Value = "Test Value" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.CustomField,
                DisplayText = "Custom field criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    customFieldId = 5,
                    @operator = "Equals",
                    value = "Test Value"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal("Test Value", result.ActualValue);
        }

        [Fact]
        public async Task EvaluateCustomField_NumericField_GreaterThan_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CustomFieldNumbers = new List<CaseCustomFieldNumber>
                {
                    new CaseCustomFieldNumber { FieldDefinitionId = 10, Value = 150 }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.CustomField,
                DisplayText = "Custom field criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    customFieldId = 10,
                    @operator = "GreaterThan",
                    value = "100"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal("150", result.ActualValue);
        }

        [Fact]
        public async Task EvaluateCustomField_CustomFieldNotFound_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CustomFieldStrings = new List<CaseCustomFieldString>()
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.CustomField,
                DisplayText = "Custom field criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    customFieldId = 999, // Non-existent field
                    @operator = "Equals",
                    value = "Test Value"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateCustomField_BooleanField_Equals_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CustomFieldBooleans = new List<CaseCustomFieldBoolean>
                {
                    new CaseCustomFieldBoolean { FieldDefinitionId = 7, Value = true }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.CustomField,
                DisplayText = "Custom field criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    customFieldId = 7,
                    @operator = "Equals",
                    value = "true"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        #endregion

        #region Demographic Criterion Tests

        [Fact]
        public async Task EvaluateDemographic_PatientAge_GreaterThan_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                Patient = new Patient
                {
                    DateOfBirth = new DateTime(1990, 1, 1) // Clearly over 18
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Demographic,
                DisplayText = "Demographic criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    fieldPath = "Patient.DateOfBirth",
                    @operator = "LessThan",
                    value = DateTime.Now.AddYears(-18).ToString("yyyy-MM-dd") // DOB before 18 years ago = over 18
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateDemographic_PatientGender_Equals_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                Patient = new Patient
                {
                    Gender = new Gender { Name = "Female" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Demographic,
                DisplayText = "Demographic criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    fieldPath = "Patient.Gender.Name",
                    @operator = "Equals",
                    value = "Female"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.True(result.IsMatch);
        }

        [Fact]
        public async Task EvaluateDemographic_InvalidFieldPath_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                Patient = new Patient
                {
                    Gender = new Gender { Name = "Female" }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Demographic,
                DisplayText = "Demographic criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    fieldPath = "Patient.NonExistentField",
                    @operator = "Equals",
                    value = "Test"
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task Evaluate_InvalidJson_ReturnsErrorResult()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                LabResults = new List<LabResult>
                {
                    new LabResult 
                    { 
                        Id = Guid.NewGuid(),
                        Markers = new List<LabResultMarker>()
                    }
                }
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Invalid criterion",
                ValueJson = "not valid json at all"
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
            Assert.NotNull(result.ErrorMessage);
            Assert.Contains("error", result.ErrorMessage.ToLower());
        }

        [Fact]
        public async Task Evaluate_NullCase_ReturnsErrorResult()
        {
            // Arrange
            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = CriterionType.Laboratory,
                DisplayText = "Lab criterion",
                ValueJson = JsonSerializer.Serialize(new
                {
                    specimenTypeIds = new[] { 5 },
                    pathogenNames = new[] { "Measles Virus" },
                    testMethodIds = new[] { 10 },
                    resultValues = new[] { "Positive" }
                })
            };

            // Act
            var result = await _evaluator.EvaluateAsync(null!, criterion);

            // Assert
            Assert.False(result.IsMatch);
            Assert.NotNull(result.ErrorMessage);
        }

        [Fact]
        public async Task Evaluate_UnknownCriterionType_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid()
            };

            var criterion = new CaseDefinitionCriteria
            {
                Id = 1,
                CriterionType = (CriterionType)999, // Invalid type
                DisplayText = "Unknown criterion",
                ValueJson = "{}"
            };

            // Act
            var result = await _evaluator.EvaluateAsync(caseEntity, criterion);

            // Assert
            Assert.False(result.IsMatch);
        }

        #endregion
    }
}
