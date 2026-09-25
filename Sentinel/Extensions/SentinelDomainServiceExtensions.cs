using AntDesign;
using Microsoft.AspNetCore.DataProtection;
using Sentinel.Services;

namespace Sentinel.Extensions;

/// <summary>
/// Registers Sentinel's domain, integration, and background services.
/// Infrastructure that controls Identity, cookies, database access, and rate
/// limits deliberately remains visible in Program.cs because its order and
/// security settings are particularly sensitive.
/// </summary>
public static class SentinelDomainServiceExtensions
{
    public static IServiceCollection AddSentinelDomainServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        // Data Protection for encrypting sensitive configuration. Docker provides a
        // protected, named volume through DataProtection:KeyRingPath so cookies and
        // encrypted settings survive a container replacement.
        var dataProtection = services.AddDataProtection().SetApplicationName("Sentinel");
        var dataProtectionKeyRingPath = configuration["DataProtection:KeyRingPath"];
        if (!string.IsNullOrWhiteSpace(dataProtectionKeyRingPath))
        {
            Directory.CreateDirectory(dataProtectionKeyRingPath);
            dataProtection.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeyRingPath));
        }

        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddSingleton<ISetupTokenFileService, SetupTokenFileService>();
        services.AddScoped<ISetupWizardState, SetupWizardState>();
        services.AddScoped<IWebDataRocksLicenseService, WebDataRocksLicenseService>();
        services.AddSingleton<IApplicationVersionProvider, ApplicationVersionProvider>();

        services.AddSingleton<Sentinel.Services.Telemetry.ITelemetryService, Sentinel.Services.Telemetry.TelemetryService>();
        services.AddSingleton<Sentinel.Services.Telemetry.ActivityTracker>();
        services.AddScoped<Sentinel.Services.Telemetry.UsageSnapshotBuilder>();
        services.AddHttpClient<Sentinel.Services.Telemetry.UsageReportClient>();
        services.AddSingleton<Sentinel.Services.Telemetry.UsageMonitoringHostedService>();
        services.AddHostedService(provider => provider.GetRequiredService<Sentinel.Services.Telemetry.UsageMonitoringHostedService>());
        services.AddSingleton<Sentinel.Services.Telemetry.BreadcrumbTracker>();
        services.AddHttpClient<Sentinel.Services.Telemetry.ErrorReportClient>();

        if (environment.IsDevelopment())
        {
            services.AddScoped<Sentinel.Services.Email.IEmailService, Sentinel.Services.Email.MockEmailService>();
        }
        else
        {
            services.AddScoped<Sentinel.Services.Email.IEmailService, Sentinel.Services.Email.SmtpEmailService>();
        }

        services.AddScoped<IPatientDuplicateCheckService, PatientDuplicateCheckService>();
        services.AddScoped<ILocationDuplicateCheckService, LocationDuplicateCheckService>();
        services.AddScoped<IExposureRequirementService, ExposureRequirementService>();
        services.AddScoped<IOccupationImportService, OccupationImportService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IPatientCustomFieldService, PatientCustomFieldService>();
        services.AddScoped<IPatientMergeService, PatientMergeService>();
        services.AddScoped<IBackupService, BackupService>();
        services.AddScoped<IPatientIdGeneratorService, PatientIdGeneratorService>();
        services.AddScoped<ICaseIdGeneratorService, CaseIdGeneratorService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IDiseaseAccessService, DiseaseAccessService>();
        services.AddScoped<ICaseAccessService, CaseAccessService>();
        services.AddScoped<IOutbreakAccessService, OutbreakAccessService>();
        services.AddSingleton<IProtectedFileStorageService, ProtectedFileStorageService>();
        services.AddHostedService<ProtectedFileStorageMigrationService>();
        services.AddScoped<CustomFieldService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskAssignmentService, TaskAssignmentService>();
        services.AddScoped<ISurveyService, SurveyService>();
        services.AddScoped<IOutbreakService, OutbreakService>();
        services.AddScoped<ILineListService, LineListService>();
        services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();
        services.AddScoped<IJurisdictionService, JurisdictionService>();
        services.AddScoped<Sentinel.Services.Reporting.IReportFieldMetadataService, Sentinel.Services.Reporting.ReportFieldMetadataService>();
        services.AddScoped<Sentinel.Services.Reporting.IReportDataAccessService, Sentinel.Services.Reporting.ReportDataAccessService>();
        services.AddScoped<Sentinel.Services.Reporting.IReportDataService, Sentinel.Services.Reporting.ReportDataService>();
        services.AddScoped<Sentinel.Services.Reporting.CollectionQueryFilterBuilder>();
        services.AddScoped<Sentinel.Services.Telemetry.SystemInfoProvider>();
        services.AddScoped<Sentinel.Services.Feedback.DiagnosticsBuilder>();
        services.AddHttpClient<Sentinel.Services.Feedback.FeedbackApiClient>();
        services.AddScoped<Sentinel.Services.Reporting.IReportFolderService, Sentinel.Services.Reporting.ReportFolderService>();
        services.AddScoped<Sentinel.Services.Reporting.ICollectionMetadataService, Sentinel.Services.Reporting.CollectionMetadataService>();
        services.AddScoped<Sentinel.Services.Reporting.IDynamicDateResolver, Sentinel.Services.Reporting.DynamicDateResolver>();
        services.AddScoped<IDataReviewService, DataReviewService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddSingleton<IApplicationTimeZoneService, ApplicationTimeZoneService>();

        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.OperatorEvaluator>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.FieldResolver>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriterionEvaluator>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.CriteriaGroupEvaluator>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.DefinitionEvaluator>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.TreeBasedCriteriaEvaluator>();
        services.AddScoped<Sentinel.Services.CaseDefinitionEvaluation.ICaseDefinitionEvaluationService, Sentinel.Services.CaseDefinitionEvaluation.CaseDefinitionEvaluationService>();

        services.AddScoped<Sentinel.Services.HL7.IHL7ParserService, Sentinel.Services.HL7.HL7ParserService>();
        services.AddScoped<Sentinel.Services.HL7.IDuplicateDetectionService, Sentinel.Services.HL7.DuplicateDetectionService>();
        services.AddScoped<Sentinel.Services.HL7.IHL7FieldMappingService, Sentinel.Services.HL7.HL7FieldMappingService>();
        services.AddScoped<Sentinel.Services.HL7.IHL7DataExtractionService, Sentinel.Services.HL7.HL7DataExtractionService>();
        services.AddScoped<Sentinel.Services.HL7.IHL7MarkerResolutionService, Sentinel.Services.HL7.HL7MarkerResolutionService>();
        services.AddScoped<Sentinel.Services.HL7.ICaseDefinitionMatchingService, Sentinel.Services.HL7.CaseDefinitionMatchingService>();
        services.AddScoped<Sentinel.Services.HL7.ICaseMatchingService, Sentinel.Services.HL7.CaseMatchingService>();
        services.AddScoped<Sentinel.Services.HL7.CaseDefinitionSpecificityScorer>();
        services.AddScoped<Sentinel.Services.HL7.HL7DiagnosticService>();
        services.AddScoped<Sentinel.Services.HL7.HL7ReviewService>();
        services.AddScoped<Sentinel.Services.HL7.IHL7TestMessageService, Sentinel.Services.HL7.HL7TestMessageService>();
        services.AddSingleton<Sentinel.Services.HL7.IHL7FileMonitorService, Sentinel.Services.HL7.HL7FileMonitorService>();
        services.AddHostedService<Sentinel.Services.HL7.HL7FileMonitorHostedService>();

        services.AddSingleton<Sentinel.Services.CaseDefinitionEvaluation.ICaseEvaluationQueue, Sentinel.Services.CaseDefinitionEvaluation.CaseEvaluationQueue>();
        services.AddHostedService<Sentinel.Services.CaseDefinitionEvaluation.CaseEvaluationWorker>();
        services.AddScoped<ISurveyMappingService, SurveyMappingService>();
        services.AddScoped<ICollectionMappingService, CollectionMappingService>();
        services.AddScoped<CollectionMappingValidationService>();
        services.AddScoped<IPatientAddressService, PatientAddressService>();
        services.AddScoped<TestDataGeneratorService>();
        services.AddScoped<Sentinel.Helpers.PermissionHelper>();
        services.AddHttpContextAccessor();
        services.AddAntDesign();

        var geocodingProvider = configuration["Geocoding:Provider"]?.ToLowerInvariant() ?? "google";
        if (geocodingProvider == "nominatim")
        {
            services.AddHttpClient<ILocationLookupService, NominatimLocationLookupService>(client =>
            {
                client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
                client.DefaultRequestHeaders.Add("User-Agent", "SurveillanceMVP/1.0");
            });
        }
        else
        {
            services.AddHttpClient<ILocationLookupService, GoogleLocationLookupService>();
        }

        services.AddScoped<IGeocodingService, GoogleGeocodingService>();
        services.AddSingleton<IGeocodingQueueService, GeocodingQueueService>();
        services.AddHostedService<GeocodingBackgroundService>();

        return services;
    }
}
