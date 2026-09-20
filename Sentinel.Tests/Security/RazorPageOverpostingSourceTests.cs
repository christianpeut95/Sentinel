namespace Sentinel.Tests.Security;

/// <summary>
/// Guards pages remediated from attaching an HTTP-bound entity as Modified.
/// The page may still use a bound input object for form validation, but it must
/// load the persisted entity and explicitly copy the fields rendered by the
/// page before saving.
/// </summary>
public sealed class RazorPageOverpostingSourceTests
{
    private static readonly string RepositoryRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("Pages/Events/Edit.cshtml.cs", "_context.Attach(Event).State = EntityState.Modified", "eventToUpdate")]
    [InlineData("Pages/Locations/Edit.cshtml.cs", "_context.Attach(Location).State = EntityState.Modified", "locationToUpdate")]
    [InlineData("Pages/Settings/Pathogens/Edit.cshtml.cs", "_context.Attach(Pathogen).State = EntityState.Modified", "pathogenToUpdate")]
    [InlineData("Pages/Organizations/Edit.cshtml.cs", "_context.Attach(Organization).State = EntityState.Modified", "existingOrganization")]
    [InlineData("Pages/Contacts/Edit.cshtml.cs", "_context.Attach(Case).State = EntityState.Modified", "caseToUpdate")]
    [InlineData("Pages/Settings/Diseases/Edit.cshtml.cs", "_context.Attach(Disease).State = EntityState.Modified", "diseaseToUpdate")]
    [InlineData("Pages/Settings/Ancestries/Edit.cshtml.cs", "_context.Attach(Ancestry).State = EntityState.Modified", "ancestryToUpdate")]
    [InlineData("Pages/Settings/CaseStatuses/Edit.cshtml.cs", "_context.Attach(CaseStatus).State = EntityState.Modified", "caseStatusToUpdate")]
    [InlineData("Pages/Settings/Countries/Edit.cshtml.cs", "_context.Attach(Country).State = EntityState.Modified", "countryToUpdate")]
    [InlineData("Pages/Settings/Genders/Edit.cshtml.cs", "_context.Attach(Gender).State = EntityState.Modified", "genderToUpdate")]
    [InlineData("Pages/Settings/Languages/Edit.cshtml.cs", "_context.Attach(Language).State = EntityState.Modified", "languageToUpdate")]
    [InlineData("Pages/Settings/Occupations/Edit.cshtml.cs", "_context.Attach(Occupation).State = EntityState.Modified", "occupationToUpdate")]
    [InlineData("Pages/Settings/States/Edit.cshtml.cs", "_context.Attach(State).State = EntityState.Modified", "stateToUpdate")]
    [InlineData("Pages/Settings/SexAtBirths/Edit.cshtml.cs", "_context.Attach(SexAtBirth).State = EntityState.Modified", "sexAtBirthToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditEventType.cshtml.cs", "_context.Attach(EventType).State = EntityState.Modified", "eventTypeToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditOrganizationType.cshtml.cs", "_context.Attach(OrganizationType).State = EntityState.Modified", "organizationTypeToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditResultUnit.cshtml.cs", "_context.Attach(ResultUnit).State = EntityState.Modified", "resultUnitToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditSpecimenType.cshtml.cs", "_context.Attach(SpecimenType).State = EntityState.Modified", "specimenTypeToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditTestResult.cshtml.cs", "_context.Attach(TestResult).State = EntityState.Modified", "testResultToUpdate")]
    [InlineData("Pages/Settings/Lookups/EditTestMethod.cshtml.cs", "_context.Attach(TestMethod).State = EntityState.Modified", "testMethodToUpdate")]
    public void BoundEditPages_LoadPersistedEntityAndDoNotMarkThePostedEntityModified(
        string relativePath,
        string forbiddenAttachment,
        string persistedEntityVariable)
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.DoesNotContain(forbiddenAttachment, source, StringComparison.Ordinal);
        Assert.Contains($"var {persistedEntityVariable} = await _context", source, StringComparison.Ordinal);
        Assert.Contains($"if ({persistedEntityVariable} == null)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DiseaseBasicInformationForm_UsesItsDeclaredPostHandler()
    {
        var view = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Pages",
            "Settings",
            "Diseases",
            "Edit.cshtml"));
        var pageModel = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Pages",
            "Settings",
            "Diseases",
            "Edit.cshtml.cs"));

        Assert.Contains("asp-page-handler=\"SaveBasic\"", view, StringComparison.Ordinal);
        Assert.Contains("OnPostSaveBasicAsync", pageModel, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Pages/Organizations/Create.cshtml.cs", "Organization", "organizationToCreate")]
    [InlineData("Pages/Locations/Create.cshtml.cs", "Location", "locationToCreate")]
    [InlineData("Pages/Events/Create.cshtml.cs", "Event", "eventToCreate")]
    [InlineData("Pages/Contacts/Create.cshtml.cs", "Case", "caseToCreate")]
    [InlineData("Pages/Cases/AddExposure.cshtml.cs", "Exposure", "exposureToCreate")]
    [InlineData("Pages/Cases/AddLabResult.cshtml.cs", "LabResult", "labResultToCreate")]
    [InlineData("Pages/Cases/Create.cshtml.cs", "Case", "caseToCreate")]
    [InlineData("Pages/Patients/Create.cshtml.cs", "Patient", "patientToCreate")]
    [InlineData("Pages/Settings/HL7/FieldMappings/Create.cshtml.cs", "Mapping", "mappingToCreate")]
    public void BoundCreatePages_ConstructANewPersistedEntityFromAnAllowList(
        string relativePath,
        string boundEntityName,
        string persistedEntityVariable)
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            relativePath.Replace('/', Path.DirectorySeparatorChar)));

        Assert.Contains($"var {persistedEntityVariable} = new", source, StringComparison.Ordinal);
        Assert.Contains($".Add({persistedEntityVariable})", source, StringComparison.Ordinal);
        Assert.DoesNotContain($".Add({boundEntityName})", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Hl7FieldMappingHandlers_ValidatePostedReferencesAgainstActiveConfiguration()
    {
        foreach (var relativePath in new[]
        {
            "Pages/Settings/HL7/FieldMappings/Create.cshtml.cs",
            "Pages/Settings/HL7/FieldMappings/Edit.cshtml.cs"
        })
        {
            var source = File.ReadAllText(Path.Combine(
                RepositoryRoot,
                "Sentinel",
                relativePath.Replace('/', Path.DirectorySeparatorChar)));

            Assert.Contains("ValidateReferencesAsync", source, StringComparison.Ordinal);
            Assert.Contains("d.Id == Mapping.DiseaseId && d.IsActive", source, StringComparison.Ordinal);
            Assert.Contains("cf.Id == Mapping.CustomFieldDefinitionId && cf.IsActive && cf.ShowOnCaseForm", source, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void PatientEdit_DoesNotExposeCustomFieldExceptionsOrRethrowConcurrencyFailures()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Pages",
            "Patients",
            "Edit.cshtml.cs"));

        Assert.DoesNotContain("cfEx.Message", source, StringComparison.Ordinal);
        Assert.Contains("some custom fields could not be saved", source, StringComparison.Ordinal);
        Assert.Contains("catch (DbUpdateConcurrencyException)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("throw;", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CaseEdit_StagesSymptomChangesUntilTheEnclosingUnitOfWorkIsSaved()
    {
        var source = File.ReadAllText(Path.Combine(
            RepositoryRoot,
            "Sentinel",
            "Pages",
            "Cases",
            "Edit.cshtml.cs"));
        var methodStart = source.IndexOf(
            "private async Task<DateTime?> PrepareSymptomChangesAsync()",
            StringComparison.Ordinal);
        var nextMethod = source.IndexOf(
            "public async Task<JsonResult> OnGetSearchSymptomsAsync",
            methodStart,
            StringComparison.Ordinal);

        Assert.True(methodStart >= 0 && nextMethod > methodStart, "Could not locate the symptom staging method.");
        var methodSource = source[methodStart..nextMethod];

        Assert.DoesNotContain("SaveChangesAsync", methodSource, StringComparison.Ordinal);
        Assert.Contains("ONE ATOMIC SAVE", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        foreach (var candidate in new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        })
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel")) &&
                    Directory.Exists(Path.Combine(current.FullName, "Sentinel.Tests")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root.");
    }
}
