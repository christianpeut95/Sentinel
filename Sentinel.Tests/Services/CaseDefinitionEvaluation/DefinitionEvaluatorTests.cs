using Microsoft.EntityFrameworkCore;
using Sentinel.Data;
using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Models.Pathogens;
using Sentinel.Services.CaseDefinitionEvaluation;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class DefinitionEvaluatorTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly DefinitionEvaluator _evaluator;
        private readonly CriteriaGroupEvaluator _groupEvaluator;
        private readonly CriterionEvaluator _criterionEvaluator;

        public DefinitionEvaluatorTests()
        {
            // Setup in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Setup evaluator chain
            var operatorEvaluator = new OperatorEvaluator();
            var fieldResolver = new FieldResolver();
            _criterionEvaluator = new CriterionEvaluator(operatorEvaluator, fieldResolver, NullLogger<CriterionEvaluator>.Instance);
            _groupEvaluator = new CriteriaGroupEvaluator(_criterionEvaluator, NullLogger<CriteriaGroupEvaluator>.Instance);
            _evaluator = new DefinitionEvaluator(_context, _groupEvaluator, NullLogger<DefinitionEvaluator>.Instance);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        #region Single Definition Evaluation Tests

        [Fact]
        public async Task EvaluateDefinitionAsync_ValidDefinition_ReturnsResult()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Confirmed Measles",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 1,
                        CriterionType = CriterionType.Laboratory,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Lab confirmation",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            specimenTypeIds = new[] { 5 },
                            pathogenNames = new[] { "Measles Virus" },
                            testMethodIds = new[] { 10 },
                            resultValues = new[] { "Positive" }
                        })
                    }
                }
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id,
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
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(caseEntity.Id, result.CaseId);
            Assert.Equal(definition.Id, result.CaseDefinitionId);
            Assert.Equal(definition.Name, result.CaseDefinitionName);
            Assert.True(result.IsMatch);
            Assert.Equal(RecommendedAction.AutoClassify, result.RecommendedAction);
            Assert.Contains("Matched criteria", result.Rationale);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_DefinitionNotFound_ReturnsNoneAction()
        {
            // Arrange
            var caseEntity = new Case { Id = Guid.NewGuid() };

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, 999);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RecommendedAction.None, result.RecommendedAction);
            Assert.Contains("not found", result.Rationale);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_InactiveDefinition_ReturnsNoneAction()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Draft Definition",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Draft, // Not Current
                Criteria = new List<CaseDefinitionCriteria>()
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RecommendedAction.None, result.RecommendedAction);
            Assert.Contains("not active", result.Rationale);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_WrongDisease_ReturnsNoneAction()
        {
            // Arrange
            var disease1 = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var disease2 = new Disease { Id = Guid.NewGuid(), Name = "Rubella" };

            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Measles Definition",
                DiseaseId = disease1.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>()
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease2.Id // Different disease
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.AddRange(disease1, disease2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RecommendedAction.None, result.RecommendedAction);
            Assert.Contains("different disease", result.Rationale);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_NoCriteria_ReturnsNoneAction()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Empty Definition",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>() // No criteria
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(RecommendedAction.None, result.RecommendedAction);
            Assert.Contains("no criteria", result.Rationale);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_NoMatch_ReturnsNoneAction()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Lab Confirmation",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 1,
                        CriterionType = CriterionType.Laboratory,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Lab confirmation",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            specimenTypeIds = new[] { 5 },
                            pathogenNames = new[] { "Measles Virus" },
                            resultValues = new[] { "Positive" }
                        })
                    }
                }
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id,
                LabResults = new List<LabResult>() // No lab results
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsMatch);
            Assert.Equal(RecommendedAction.None, result.RecommendedAction);
            Assert.Contains("does not meet criteria", result.Rationale);
        }

        #endregion

        #region Recommended Action Tests

        [Fact]
        public async Task EvaluateDefinitionAsync_LabConfirmation_ReturnsAutoClassify()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = CreateDefinitionWithLabCriteria(disease.Id);
            var caseEntity = CreateCaseWithLabResult(disease.Id);

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal(RecommendedAction.AutoClassify, result.RecommendedAction);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_MultipleSymptoms_ReturnsFlagForReview()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Clinical Criteria",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 1,
                        CriterionType = CriterionType.Clinical,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Fever",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            symptomIds = new[] { 1 },
                            requireAll = false
                        })
                    },
                    new CaseDefinitionCriteria
                    {
                        Id = 2,
                        CriterionType = CriterionType.Clinical,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Rash",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            symptomIds = new[] { 2 },
                            requireAll = false
                        })
                    }
                }
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id,
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } },
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" } }
                }
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal(RecommendedAction.FlagForReview, result.RecommendedAction);
        }

        [Fact]
        public async Task EvaluateDefinitionAsync_OnlyClinical_ReturnsFlagForReview()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = new CaseDefinition
            {
                Id = 1,
                Name = "Clinical Only",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 1,
                        CriterionType = CriterionType.Clinical,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Clinical symptoms",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            symptomIds = new[] { 1 },
                            requireAll = false
                        })
                    }
                }
            };

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = disease.Id,
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                }
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var result = await _evaluator.EvaluateDefinitionAsync(caseEntity, definition.Id);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal(RecommendedAction.FlagForReview, result.RecommendedAction);
        }

        #endregion

        #region Multiple Definitions Tests

        [Fact]
        public async Task EvaluateAllDefinitionsAsync_MultipleDefinitions_ReturnsAllResults()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };

            var definition1 = new CaseDefinition
            {
                Id = 1,
                Name = "Definition 1",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 100,
                        CriterionType = CriterionType.Laboratory,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Lab confirmation",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            specimenTypeIds = new[] { 5 },
                            pathogenNames = new[] { "Measles Virus" },
                            testMethodIds = new[] { 10 },
                            resultValues = new[] { "Positive" }
                        })
                    }
                }
            };

            var definition2 = new CaseDefinition
            {
                Id = 2,
                Name = "Definition 2",
                DiseaseId = disease.Id,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 200,
                        CriterionType = CriterionType.Laboratory,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Lab confirmation",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            specimenTypeIds = new[] { 5 },
                            pathogenNames = new[] { "Measles Virus" },
                            testMethodIds = new[] { 10 },
                            resultValues = new[] { "Positive" }
                        })
                    }
                }
            };

            var caseEntity = CreateCaseWithLabResult(disease.Id);

            _context.CaseDefinitions.AddRange(definition1, definition2);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var results = await _evaluator.EvaluateAllDefinitionsAsync(caseEntity);

            // Assert
            Assert.Equal(2, results.Count);
            Assert.All(results, r => Assert.True(r.IsMatch));
        }

        [Fact]
        public async Task EvaluateAllDefinitionsAsync_NoCaseDisease_ReturnsAllActiveDefinitions()
        {
            // Arrange
            var disease = new Disease { Id = Guid.NewGuid(), Name = "Measles" };
            var definition = CreateDefinitionWithLabCriteria(disease.Id);

            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = null, // No disease set
                LabResults = new List<LabResult>()
            };

            _context.CaseDefinitions.Add(definition);
            _context.Diseases.Add(disease);
            await _context.SaveChangesAsync();

            // Act
            var results = await _evaluator.EvaluateAllDefinitionsAsync(caseEntity);

            // Assert
            Assert.Single(results);
        }

        #endregion

        #region Helper Methods

        private CaseDefinition CreateDefinitionWithLabCriteria(Guid diseaseId)
        {
            return new CaseDefinition
            {
                Id = 1,
                Name = "Lab Confirmation",
                DiseaseId = diseaseId,
                Status = CaseDefinitionStatus.Current,
                Criteria = new List<CaseDefinitionCriteria>
                {
                    new CaseDefinitionCriteria
                    {
                        Id = 1,
                        CriterionType = CriterionType.Laboratory,
                        LogicalOperator = LogicalOperator.AND,
                        DisplayText = "Lab confirmation",
                        ValueJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            specimenTypeIds = new[] { 5 },
                            pathogenNames = new[] { "Measles Virus" },
                            testMethodIds = new[] { 10 },
                            resultValues = new[] { "Positive" }
                        })
                    }
                }
            };
        }

        private Case CreateCaseWithLabResult(Guid diseaseId)
        {
            return new Case
            {
                Id = Guid.NewGuid(),
                DiseaseId = diseaseId,
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
                                Pathogen = new Pathogen { Name = "Measles Virus" },
                                TestMethodId = 10,
                                QualitativeResultText = "Positive"
                            }
                        }
                    }
                }
            };
        }

        #endregion
    }
}
