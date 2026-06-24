using Sentinel.Services;
using Sentinel.Services.Reporting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;

namespace Sentinel.Tests.Services
{
    public class SurveyFieldExtractionTests
    {
        private readonly SurveyMappingService _service;
        private readonly Mock<Data.ApplicationDbContext> _mockContext;
        private readonly Mock<IReportFieldMetadataService> _mockFieldMetadataService;
        private readonly Mock<ICollectionMappingService> _mockCollectionMappingService;
        private readonly Mock<ILogger<SurveyMappingService>> _mockLogger;

        public SurveyFieldExtractionTests()
        {
            _mockContext = new Mock<Data.ApplicationDbContext>(
                new Microsoft.EntityFrameworkCore.DbContextOptions<Data.ApplicationDbContext>());
            _mockFieldMetadataService = new Mock<IReportFieldMetadataService>();
            _mockCollectionMappingService = new Mock<ICollectionMappingService>();
            _mockLogger = new Mock<ILogger<SurveyMappingService>>();

            _service = new SurveyMappingService(
                _mockContext.Object,
                _mockFieldMetadataService.Object,
                _mockCollectionMappingService.Object,
                _mockLogger.Object
            );
        }

        #region Simple Question Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithSimpleTextQuestion_ExtractsCorrectly()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""text"",
                        ""name"": ""patient_name"",
                        ""title"": ""Patient Name"",
                        ""isRequired"": true
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Single(questions);
            var question = questions[0];
            Assert.Equal("patient_name", question.Name);
            Assert.Equal("Patient Name", question.Title);
            Assert.Equal("text", question.Type);
            Assert.True(question.IsRequired);
            Assert.False(question.IsCalculated);
            Assert.Null(question.ParentMatrix);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_IgnoresHtmlElements()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [
                        {
                            ""type"": ""html"",
                            ""name"": ""intro_text"",
                            ""html"": ""<p>Introduction</p>""
                        },
                        {
                            ""type"": ""text"",
                            ""name"": ""actual_question"",
                            ""title"": ""Real Question""
                        }
                    ]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Single(questions);
            Assert.Equal("actual_question", questions[0].Name);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithDropdown_ExtractsChoices()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""dropdown"",
                        ""name"": ""gender"",
                        ""title"": ""Gender"",
                        ""choices"": [""Male"", ""Female"", ""Other""]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            var question = questions[0];
            Assert.NotNull(question.Choices);
            Assert.Equal(3, question.Choices.Count);
            Assert.Contains("Male", question.Choices);
            Assert.Contains("Female", question.Choices);
            Assert.Contains("Other", question.Choices);
        }

        #endregion

        #region Matrix Question Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMatrixDropdown_ExtractsAllCells()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""matrixdropdown"",
                        ""name"": ""symptoms"",
                        ""title"": ""Symptoms"",
                        ""columns"": [
                            {
                                ""name"": ""present"",
                                ""title"": ""Present?"",
                                ""cellType"": ""dropdown""
                            },
                            {
                                ""name"": ""onset_date"",
                                ""title"": ""Date of onset"",
                                ""cellType"": ""text""
                            }
                        ],
                        ""rows"": [
                            { ""value"": ""fever"", ""text"": ""Fever"" },
                            { ""value"": ""cough"", ""text"": ""Cough"" }
                        ]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            // Should have 2 rows × 2 columns = 4 fields
            Assert.Equal(4, questions.Count);

            // Check fever.present
            var feverPresent = questions.FirstOrDefault(q => q.FieldPath == "symptoms.fever.present");
            Assert.NotNull(feverPresent);
            Assert.Equal("Fever - Present?", feverPresent.DisplayName);
            Assert.Equal("matrix_dropdown", feverPresent.Type);
            Assert.Equal("symptoms", feverPresent.ParentMatrix);
            Assert.False(feverPresent.IsArray);

            // Check cough.onset_date
            var coughOnset = questions.FirstOrDefault(q => q.FieldPath == "symptoms.cough.onset_date");
            Assert.NotNull(coughOnset);
            Assert.Equal("Cough - Date of onset", coughOnset.DisplayName);
            Assert.Equal("matrix_text", coughOnset.Type);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMatrixDynamic_ExtractsSameAsMatrixDropdown()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""matrixdynamic"",
                        ""name"": ""exposures"",
                        ""columns"": [
                            { ""name"": ""location"", ""title"": ""Location"" },
                            { ""name"": ""date"", ""title"": ""Date"" }
                        ],
                        ""rows"": [
                            { ""value"": ""work"", ""text"": ""Workplace"" }
                        ]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(2, questions.Count);
            var workLocation = questions.FirstOrDefault(q => q.FieldPath == "exposures.work.location");
            Assert.NotNull(workLocation);
            Assert.Equal("exposures", workLocation.ParentMatrix);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMatrixStringRows_HandlesCorrectly()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""matrixdropdown"",
                        ""name"": ""risk_factors"",
                        ""columns"": [{ ""name"": ""status"", ""title"": ""Status"" }],
                        ""rows"": [""travel"", ""contact"", ""occupation""]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(3, questions.Count);
            var travel = questions.FirstOrDefault(q => q.FieldPath == "risk_factors.travel.status");
            Assert.NotNull(travel);
            Assert.Equal("travel - Status", travel.DisplayName);
        }

        #endregion

        #region Panel Dynamic Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithPanelDynamic_ExtractsWithArrayNotation()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""paneldynamic"",
                        ""name"": ""household_contacts"",
                        ""title"": ""Household Contacts"",
                        ""templateElements"": [
                            {
                                ""type"": ""text"",
                                ""name"": ""first_name"",
                                ""title"": ""First Name""
                            },
                            {
                                ""type"": ""text"",
                                ""name"": ""date_of_birth"",
                                ""title"": ""Date of Birth"",
                                ""inputType"": ""date""
                            }
                        ]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(2, questions.Count);

            var firstName = questions.FirstOrDefault(q => q.FieldPath == "household_contacts[].first_name");
            Assert.NotNull(firstName);
            Assert.Equal("Household Contacts - First Name", firstName.DisplayName);
            Assert.Equal("array_text", firstName.Type);
            Assert.True(firstName.IsArray);
            Assert.Equal("household_contacts", firstName.ParentMatrix);

            var dob = questions.FirstOrDefault(q => q.FieldPath == "household_contacts[].date_of_birth");
            Assert.NotNull(dob);
            Assert.True(dob.IsArray);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithNestedPanelDynamic_ExtractsAllFields()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""paneldynamic"",
                        ""name"": ""trips"",
                        ""title"": ""Travel History"",
                        ""templateElements"": [
                            { ""type"": ""text"", ""name"": ""country"", ""title"": ""Country"" },
                            { ""type"": ""text"", ""name"": ""departure_date"", ""title"": ""Departure"" },
                            { ""type"": ""text"", ""name"": ""return_date"", ""title"": ""Return"" }
                        ]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(3, questions.Count);
            Assert.All(questions, q => Assert.True(q.IsArray));
            Assert.All(questions, q => Assert.Contains("[]", q.FieldPath));
        }

        #endregion

        #region Calculated Values Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithCalculatedValues_ExtractsCorrectly()
        {
            // Arrange
            var surveyJson = @"{
                ""calculatedValues"": [
                    {
                        ""name"": ""measles_rash_onset_date"",
                        ""expression"": ""{measles_symptoms.rash.onset_date}""
                    },
                    {
                        ""name"": ""infectious_period_start"",
                        ""expression"": ""addDays({measles_rash_onset_date}, -4)""
                    }
                ],
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""text"",
                        ""name"": ""case_date_of_onset"",
                        ""title"": ""Date of Onset""
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(3, questions.Count); // 1 regular + 2 calculated

            var calculatedFields = questions.Where(q => q.IsCalculated).ToList();
            Assert.Equal(2, calculatedFields.Count);

            var rashOnset = calculatedFields.FirstOrDefault(q => q.Name == "measles_rash_onset_date");
            Assert.NotNull(rashOnset);
            Assert.Equal("calculated", rashOnset.Type);
            Assert.True(rashOnset.IsCalculated);
            Assert.Equal("Measles Rash Onset Date", rashOnset.DisplayName);

            var infectiousStart = calculatedFields.FirstOrDefault(q => q.Name == "infectious_period_start");
            Assert.NotNull(infectiousStart);
            Assert.True(infectiousStart.IsCalculated);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithOnlyCalculatedValues_ReturnsOnlyCalculated()
        {
            // Arrange
            var surveyJson = @"{
                ""calculatedValues"": [
                    { ""name"": ""calc1"", ""expression"": ""{field1} + {field2}"" },
                    { ""name"": ""calc2"", ""expression"": ""min({field3}, {field4})"" }
                ],
                ""pages"": []
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(2, questions.Count);
            Assert.All(questions, q => Assert.True(q.IsCalculated));
            Assert.All(questions, q => Assert.Equal("calculated", q.Type));
        }

        #endregion

        #region Complex Integration Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMixedTypes_ExtractsAllCorrectly()
        {
            // Arrange - Full measles survey structure
            var surveyJson = @"{
                ""calculatedValues"": [
                    {
                        ""name"": ""earliest_symptom_onset"",
                        ""expression"": ""min({symptoms.fever.onset_date}, {symptoms.cough.onset_date})""
                    }
                ],
                ""pages"": [{
                    ""elements"": [
                        {
                            ""type"": ""text"",
                            ""name"": ""case_id"",
                            ""title"": ""Case ID""
                        },
                        {
                            ""type"": ""matrixdropdown"",
                            ""name"": ""symptoms"",
                            ""columns"": [
                                { ""name"": ""present"", ""title"": ""Present?"" },
                                { ""name"": ""onset_date"", ""title"": ""Date"" }
                            ],
                            ""rows"": [
                                { ""value"": ""fever"", ""text"": ""Fever"" },
                                { ""value"": ""cough"", ""text"": ""Cough"" }
                            ]
                        },
                        {
                            ""type"": ""paneldynamic"",
                            ""name"": ""contacts"",
                            ""templateElements"": [
                                { ""type"": ""text"", ""name"": ""name"", ""title"": ""Name"" }
                            ]
                        }
                    ]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            // 1 simple + 4 matrix cells (2x2) + 1 panel dynamic + 1 calculated = 7 total
            Assert.Equal(7, questions.Count);

            // Check each category
            var simpleQuestions = questions.Where(q => !q.IsCalculated && q.ParentMatrix == null && !q.IsArray).ToList();
            Assert.Single(simpleQuestions);

            var matrixQuestions = questions.Where(q => q.ParentMatrix == "symptoms").ToList();
            Assert.Equal(4, matrixQuestions.Count);

            var arrayQuestions = questions.Where(q => q.IsArray).ToList();
            Assert.Single(arrayQuestions);

            var calculatedQuestions = questions.Where(q => q.IsCalculated).ToList();
            Assert.Single(calculatedQuestions);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMultiplePages_CombinesAllQuestions()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [
                    {
                        ""name"": ""page1"",
                        ""elements"": [
                            { ""type"": ""text"", ""name"": ""field1"", ""title"": ""Field 1"" }
                        ]
                    },
                    {
                        ""name"": ""page2"",
                        ""elements"": [
                            { ""type"": ""text"", ""name"": ""field2"", ""title"": ""Field 2"" },
                            { ""type"": ""text"", ""name"": ""field3"", ""title"": ""Field 3"" }
                        ]
                    }
                ]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Equal(3, questions.Count);
            Assert.Contains(questions, q => q.Name == "field1");
            Assert.Contains(questions, q => q.Name == "field2");
            Assert.Contains(questions, q => q.Name == "field3");
        }

        #endregion

        #region Format Display Name Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_FormatsCalculatedDisplayNames()
        {
            // Arrange
            var surveyJson = @"{
                ""calculatedValues"": [
                    { ""name"": ""snake_case_name"", ""expression"": ""1+1"" },
                    { ""name"": ""camelCaseName"", ""expression"": ""1+1"" },
                    { ""name"": ""PascalCaseName"", ""expression"": ""1+1"" }
                ],
                ""pages"": []
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            var snakeCase = questions.FirstOrDefault(q => q.Name == "snake_case_name");
            Assert.NotNull(snakeCase);
            Assert.Equal("Snake Case Name", snakeCase.DisplayName);

            var camelCase = questions.FirstOrDefault(q => q.Name == "camelCaseName");
            Assert.NotNull(camelCase);
            Assert.Equal("Camel Case Name", camelCase.DisplayName);

            var pascalCase = questions.FirstOrDefault(q => q.Name == "PascalCaseName");
            Assert.NotNull(pascalCase);
            Assert.Equal("Pascal Case Name", pascalCase.DisplayName);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithInvalidJson_ReturnsEmptyList()
        {
            // Arrange
            var invalidJson = "{ invalid json }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(invalidJson);

            // Assert
            Assert.Empty(questions);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithEmptyJson_ReturnsEmptyList()
        {
            // Arrange
            var emptyJson = "{}";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(emptyJson);

            // Assert
            Assert.Empty(questions);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMissingColumns_SkipsMatrix()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""matrixdropdown"",
                        ""name"": ""symptoms"",
                        ""rows"": [{ ""value"": ""fever"" }]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Empty(questions);
        }

        [Fact]
        public async Task GetSurveyQuestionsAsync_WithMissingRows_SkipsMatrix()
        {
            // Arrange
            var surveyJson = @"{
                ""pages"": [{
                    ""elements"": [{
                        ""type"": ""matrixdropdown"",
                        ""name"": ""symptoms"",
                        ""columns"": [{ ""name"": ""status"" }]
                    }]
                }]
            }";

            // Act
            var questions = await _service.GetSurveyQuestionsAsync(surveyJson);

            // Assert
            Assert.Empty(questions);
        }

        #endregion
    }
}
