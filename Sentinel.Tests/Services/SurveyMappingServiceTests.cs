using Sentinel.Services;
using Sentinel.Models;
using Sentinel.Models.Lookups;
using Sentinel.Data;
using Sentinel.Services.Reporting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Sentinel.Tests.Services
{
    public class SurveyMappingServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly SurveyMappingService _service;
        private readonly Mock<IReportFieldMetadataService> _mockFieldMetadataService;
        private readonly Mock<ICollectionMappingService> _mockCollectionMappingService;
        private readonly Mock<ILogger<SurveyMappingService>> _mockLogger;

        // Test data IDs
        private readonly Guid _diseaseId = Guid.NewGuid();
        private readonly Guid _taskTemplateId = Guid.NewGuid();
        private readonly Guid _surveyTemplateId = Guid.NewGuid();
        private readonly Guid _diseaseTaskTemplateId = Guid.NewGuid();
        private readonly Guid _taskTypeId = Guid.NewGuid();

        public SurveyMappingServiceTests()
        {
            // Create in-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            // Ensure database schema is created
            _context.Database.EnsureCreated();

            // Setup mocks
            _mockFieldMetadataService = new Mock<IReportFieldMetadataService>();
            _mockCollectionMappingService = new Mock<ICollectionMappingService>();
            _mockLogger = new Mock<ILogger<SurveyMappingService>>();

            _service = new SurveyMappingService(
                _context,
                _mockFieldMetadataService.Object,
                _mockCollectionMappingService.Object,
                _mockLogger.Object
            );

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            // Create a disease
            var disease = new Disease
            {
                Id = _diseaseId,
                Name = "Measles",
                Code = "MEASLES",
                ExportCode = "MEASLES",
                IsActive = true
            };
            _context.Diseases.Add(disease);

            // Create a task type
            var taskType = new TaskType
            {
                Id = _taskTypeId,
                Name = "Interview",
                IsActive = true
            };
            _context.TaskTypes.Add(taskType);

            // Create a task template
            var taskTemplate = new TaskTemplate
            {
                Id = _taskTemplateId,
                Name = "Case Interview",
                TaskTypeId = _taskTypeId,
                SurveyTemplateId = _surveyTemplateId,
                IsActive = true
            };
            _context.TaskTemplates.Add(taskTemplate);

            // Create a survey template
            var surveyTemplate = new SurveyTemplate
            {
                Id = _surveyTemplateId,
                Name = "Measles Case Interview Survey",
                SurveyDefinitionJson = "{}",
                Version = 1,
                IsActive = true
            };
            _context.SurveyTemplates.Add(surveyTemplate);

            // Create a disease task template
            var diseaseTaskTemplate = new DiseaseTaskTemplate
            {
                Id = _diseaseTaskTemplateId,
                DiseaseId = _diseaseId,
                TaskTemplateId = _taskTemplateId,
                Disease = disease,
                TaskTemplate = taskTemplate,
                IsActive = true
            };
            _context.DiseaseTaskTemplates.Add(diseaseTaskTemplate);

            _context.SaveChanges();
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithDiseaseTaskTemplateMapping_ReturnsMappingWithHighestPriority()
        {
            // Arrange
            var diseaseTaskTemplateMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                ConfigurationId = _diseaseTaskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.DiseaseTaskTemplate,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(diseaseTaskTemplateMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Single(result);
            Assert.Equal("fever", result[0].SurveyQuestionName);
            Assert.Equal(MappingConfigurationType.DiseaseTaskTemplate, result[0].ConfigurationType);
            Assert.Equal((int)MappingConfigurationType.DiseaseTaskTemplate, result[0].Priority);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithMultipleLevels_PrioritizesDiseaseTaskTemplateOverOthers()
        {
            // Arrange - Create mappings at all levels for the same question
            var diseaseMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Disease,
                ConfigurationId = _diseaseId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "DiseaseLevelField",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Disease,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            var taskMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Task,
                ConfigurationId = _taskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "TaskLevelField",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Task,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            var surveyMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Survey,
                ConfigurationId = _surveyTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "SurveyLevelField",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Survey,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            var diseaseTaskTemplateMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                ConfigurationId = _diseaseTaskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "DiseaseTaskTemplateField",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.DiseaseTaskTemplate,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            _context.SurveyFieldMappings.AddRange(diseaseMapping, taskMapping, surveyMapping, diseaseTaskTemplateMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Single(result); // Should deduplicate to one mapping
            Assert.Equal("DiseaseTaskTemplateField", result[0].TargetFieldPath); // Should be the DiseaseTaskTemplate mapping
            Assert.Equal(MappingConfigurationType.DiseaseTaskTemplate, result[0].ConfigurationType);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithoutDiseaseTaskTemplate_FallsBackToSurveyLevel()
        {
            // Arrange
            var surveyMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Survey,
                ConfigurationId = _surveyTemplateId,
                SurveyQuestionName = "cough",
                TargetFieldPath = "HasCough",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Survey,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(surveyMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Single(result);
            Assert.Equal("cough", result[0].SurveyQuestionName);
            Assert.Equal(MappingConfigurationType.Survey, result[0].ConfigurationType);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithDifferentQuestions_ReturnsAllMappings()
        {
            // Arrange
            var diseaseTaskTemplateMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                ConfigurationId = _diseaseTaskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.DiseaseTaskTemplate,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            var surveyMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Survey,
                ConfigurationId = _surveyTemplateId,
                SurveyQuestionName = "cough",
                TargetFieldPath = "HasCough",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Survey,
                IsActive = true,
                DisplayOrder = 2,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };

            _context.SurveyFieldMappings.AddRange(diseaseTaskTemplateMapping, surveyMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, m => m.SurveyQuestionName == "fever");
            Assert.Contains(result, m => m.SurveyQuestionName == "cough");
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithInactiveDiseaseTaskTemplate_DoesNotReturnMapping()
        {
            // Arrange
            // Make the disease task template inactive
            var dtt = await _context.DiseaseTaskTemplates.FindAsync(_diseaseTaskTemplateId);
            dtt!.IsActive = false;
            await _context.SaveChangesAsync();

            var diseaseTaskTemplateMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                ConfigurationId = _diseaseTaskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.DiseaseTaskTemplate,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(diseaseTaskTemplateMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Empty(result); // Should not find mapping because DiseaseTaskTemplate is inactive
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithInactiveMapping_DoesNotReturnMapping()
        {
            // Arrange
            var diseaseTaskTemplateMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                ConfigurationId = _diseaseTaskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.DiseaseTaskTemplate,
                IsActive = false, // Inactive mapping
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(diseaseTaskTemplateMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithMissingTaskTemplateId_SkipsDiseaseTaskTemplateCheck()
        {
            // Arrange
            var diseaseMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Disease,
                ConfigurationId = _diseaseId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Disease,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(diseaseMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, null, _diseaseId);

            // Assert
            Assert.Single(result);
            Assert.Equal(MappingConfigurationType.Disease, result[0].ConfigurationType);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_WithMissingDiseaseId_SkipsDiseaseTaskTemplateCheck()
        {
            // Arrange
            var taskMapping = new SurveyFieldMapping
            {
                Id = Guid.NewGuid(),
                ConfigurationType = MappingConfigurationType.Task,
                ConfigurationId = _taskTemplateId,
                SurveyQuestionName = "fever",
                TargetFieldPath = "HasFever",
                TargetFieldType = MappingFieldType.StandardField,
                Priority = (int)MappingConfigurationType.Task,
                IsActive = true,
                DisplayOrder = 1,
                FieldCategory = MappingFieldCategory.Symptom,
                MappingAction = MappingAction.AutoSave,
                BusinessRule = MappingBusinessRule.AlwaysOverwrite
            };
            _context.SurveyFieldMappings.Add(taskMapping);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, null);

            // Assert
            Assert.Single(result);
            Assert.Equal(MappingConfigurationType.Task, result[0].ConfigurationType);
        }

        [Fact]
        public async Task GetActiveMappingsAsync_PriorityOrder_DiseaseTaskTemplate_Survey_Task_Disease()
        {
            // Arrange - Create mappings for different questions at each level
            var mappings = new[]
            {
                new SurveyFieldMapping
                {
                    Id = Guid.NewGuid(),
                    ConfigurationType = MappingConfigurationType.DiseaseTaskTemplate,
                    ConfigurationId = _diseaseTaskTemplateId,
                    SurveyQuestionName = "q1",
                    TargetFieldPath = "Field1",
                    TargetFieldType = MappingFieldType.StandardField,
                    Priority = (int)MappingConfigurationType.DiseaseTaskTemplate, // 1
                    IsActive = true,
                    DisplayOrder = 1,
                    FieldCategory = MappingFieldCategory.Symptom,
                    MappingAction = MappingAction.AutoSave,
                    BusinessRule = MappingBusinessRule.AlwaysOverwrite
                },
                new SurveyFieldMapping
                {
                    Id = Guid.NewGuid(),
                    ConfigurationType = MappingConfigurationType.Survey,
                    ConfigurationId = _surveyTemplateId,
                    SurveyQuestionName = "q2",
                    TargetFieldPath = "Field2",
                    TargetFieldType = MappingFieldType.StandardField,
                    Priority = (int)MappingConfigurationType.Survey, // 2
                    IsActive = true,
                    DisplayOrder = 2,
                    FieldCategory = MappingFieldCategory.Symptom,
                    MappingAction = MappingAction.AutoSave,
                    BusinessRule = MappingBusinessRule.AlwaysOverwrite
                },
                new SurveyFieldMapping
                {
                    Id = Guid.NewGuid(),
                    ConfigurationType = MappingConfigurationType.Task,
                    ConfigurationId = _taskTemplateId,
                    SurveyQuestionName = "q3",
                    TargetFieldPath = "Field3",
                    TargetFieldType = MappingFieldType.StandardField,
                    Priority = (int)MappingConfigurationType.Task, // 3
                    IsActive = true,
                    DisplayOrder = 3,
                    FieldCategory = MappingFieldCategory.Symptom,
                    MappingAction = MappingAction.AutoSave,
                    BusinessRule = MappingBusinessRule.AlwaysOverwrite
                },
                new SurveyFieldMapping
                {
                    Id = Guid.NewGuid(),
                    ConfigurationType = MappingConfigurationType.Disease,
                    ConfigurationId = _diseaseId,
                    SurveyQuestionName = "q4",
                    TargetFieldPath = "Field4",
                    TargetFieldType = MappingFieldType.StandardField,
                    Priority = (int)MappingConfigurationType.Disease, // 4
                    IsActive = true,
                    DisplayOrder = 4,
                    FieldCategory = MappingFieldCategory.Symptom,
                    MappingAction = MappingAction.AutoSave,
                    BusinessRule = MappingBusinessRule.AlwaysOverwrite
                }
            };

            _context.SurveyFieldMappings.AddRange(mappings);
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetActiveMappingsAsync(_surveyTemplateId, _taskTemplateId, _diseaseId);

            // Assert
            Assert.Equal(4, result.Count);
            // Verify priority values are correct
            var q1 = result.First(m => m.SurveyQuestionName == "q1");
            var q2 = result.First(m => m.SurveyQuestionName == "q2");
            var q3 = result.First(m => m.SurveyQuestionName == "q3");
            var q4 = result.First(m => m.SurveyQuestionName == "q4");

            Assert.Equal(1, q1.Priority); // Highest priority
            Assert.Equal(2, q2.Priority);
            Assert.Equal(3, q3.Priority);
            Assert.Equal(4, q4.Priority); // Lowest priority
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context?.Dispose();
        }
    }
}
