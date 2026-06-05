using Sentinel.Models;
using Sentinel.Models.CaseDefinitions;
using Sentinel.Models.Lookups;
using Sentinel.Services.CaseDefinitionEvaluation;
using System.Text.Json;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;

namespace Sentinel.Tests.Services.CaseDefinitionEvaluation
{
    public class CriteriaGroupEvaluatorTests
    {
        private readonly CriteriaGroupEvaluator _evaluator;
        private readonly CriterionEvaluator _criterionEvaluator;

        public CriteriaGroupEvaluatorTests()
        {
            var operatorEvaluator = new OperatorEvaluator();
            var fieldResolver = new FieldResolver();
            _criterionEvaluator = new CriterionEvaluator(operatorEvaluator, fieldResolver, NullLogger<CriterionEvaluator>.Instance);
            _evaluator = new CriteriaGroupEvaluator(_criterionEvaluator, NullLogger<CriteriaGroupEvaluator>.Instance);
        }

        #region AND Logic Tests

        [Fact]
        public async Task EvaluateGroupAsync_ANDLogic_AllMatch_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } },
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" } }
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal(2, result.ChildResults.Count);
            Assert.All(result.ChildResults, r => Assert.True(r.IsMatch));
            Assert.Equal(LogicalOperator.AND, result.LogicalOperator);
        }

        [Fact]
        public async Task EvaluateGroupAsync_ANDLogic_OneDoesNotMatch_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                    // Missing symptom 2
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.False(result.IsMatch);
            Assert.Equal(2, result.ChildResults.Count);
            Assert.True(result.ChildResults[0].IsMatch); // First matches
            Assert.False(result.ChildResults[1].IsMatch); // Second doesn't match
        }

        #endregion

        #region OR Logic Tests

        [Fact]
        public async Task EvaluateGroupAsync_ORLogic_OneMatches_ReturnsTrue()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                    // Missing symptom 2
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Equal(2, result.ChildResults.Count);
            Assert.Equal(LogicalOperator.OR, result.LogicalOperator);
        }

        [Fact]
        public async Task EvaluateGroupAsync_ORLogic_NoneMatch_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>() // No symptoms
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.False(result.IsMatch);
            Assert.Equal(2, result.ChildResults.Count);
            Assert.All(result.ChildResults, r => Assert.False(r.IsMatch));
        }

        #endregion

        #region Nested Group Tests

        [Fact]
        public async Task EvaluateGroupAsync_NestedGroups_CorrectlyEvaluates()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } },
                    new CaseSymptom { SymptomId = 2, Symptom = new Symptom { Name = "Rash" } }
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                // Group parent
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Group",
                    DisplayOrder = 0,
                    ValueJson = "{}"
                },
                // Children of group (OR logic within group)
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = 1,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 3,
                    ParentCriteriaId = 1,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.OR,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Single(result.ChildResults); // One group
            Assert.True(result.ChildResults[0].IsMatch);
            Assert.Equal(2, result.ChildResults[0].ChildResults.Count); // Group has 2 children
        }

        [Fact]
        public async Task EvaluateGroupAsync_DeepNesting_CorrectlyEvaluates()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                // Level 1 - Root group
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Root Group",
                    DisplayOrder = 0,
                    ValueJson = "{}"
                },
                // Level 2 - Nested group
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = 1,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Nested Group",
                    DisplayOrder = 0,
                    ValueJson = "{}"
                },
                // Level 3 - Actual criterion
                new CaseDefinitionCriteria
                {
                    Id = 3,
                    ParentCriteriaId = 2,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Single(result.ChildResults); // Root has 1 child (nested group)
            Assert.Single(result.ChildResults[0].ChildResults); // Nested group has 1 child (criterion)
            Assert.True(result.ChildResults[0].ChildResults[0].IsMatch);
        }

        #endregion

        #region Edge Cases

        [Fact]
        public async Task EvaluateGroupAsync_EmptyGroup_ReturnsFalse()
        {
            // Arrange
            var caseEntity = new Case { Id = Guid.NewGuid() };
            var criteria = new List<CaseDefinitionCriteria>(); // Empty

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.False(result.IsMatch);
            Assert.Contains("No criteria", result.ActualValue);
        }

        [Fact]
        public async Task EvaluateGroupAsync_SingleCriterion_CorrectlyEvaluates()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.True(result.IsMatch);
            Assert.Single(result.ChildResults);
        }

        [Fact]
        public async Task EvaluateGroupAsync_ActualValueDescribesResults()
        {
            // Arrange
            var caseEntity = new Case
            {
                Id = Guid.NewGuid(),
                CaseSymptoms = new List<CaseSymptom>
                {
                    new CaseSymptom { SymptomId = 1, Symptom = new Symptom { Name = "Fever" } }
                }
            };

            var criteria = new List<CaseDefinitionCriteria>
            {
                new CaseDefinitionCriteria
                {
                    Id = 1,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Fever",
                    DisplayOrder = 0,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 1 },
                        requireAll = false
                    })
                },
                new CaseDefinitionCriteria
                {
                    Id = 2,
                    ParentCriteriaId = null,
                    CriterionType = CriterionType.Clinical,
                    LogicalOperator = LogicalOperator.AND,
                    DisplayText = "Rash",
                    DisplayOrder = 1,
                    ValueJson = JsonSerializer.Serialize(new
                    {
                        symptomIds = new[] { 2 },
                        requireAll = false
                    })
                }
            };

            // Act
            var result = await _evaluator.EvaluateGroupAsync(caseEntity, criteria);

            // Assert
            Assert.Contains("1 of 2", result.ActualValue);
            Assert.Contains("ALL 2 criteria must match", result.ExpectedValue);
        }

        #endregion
    }
}
