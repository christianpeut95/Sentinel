namespace Sentinel.Tests.Security;

/// <summary>
/// Regression guards for the first-party DOM sink families reviewed during the
/// ASVS assessment. The inventory test deliberately fails closed if a reviewed
/// sink is added, removed, or moved without an explicit security review.
/// </summary>
public sealed class ReviewedDomSinkRegressionTests
{
    [Fact]
    public void FirstPartyDomSinkInventory_MatchesTheReviewedManifest()
    {
        var expected = new[]
        {
            new DomSinkCounts("Pages/Admin/Logs.cshtml", 3, 0, 0, 0),
            new DomSinkCounts("Pages/Cases/Create.cshtml", 0, 0, 0, 1),
            new DomSinkCounts("Pages/Cases/CreateNew.cshtml", 0, 0, 0, 11),
            new DomSinkCounts("Pages/Cases/Details.cshtml", 8, 0, 0, 0),
            new DomSinkCounts("Pages/Cases/Edit.cshtml", 0, 0, 0, 1),
            new DomSinkCounts("Pages/Cases/EditExposure.cshtml", 0, 0, 0, 8),
            new DomSinkCounts("Pages/Contacts/CreateNewContact.cshtml", 0, 0, 0, 11),
            new DomSinkCounts("Pages/Contacts/Details.cshtml", 0, 0, 0, 8),
            new DomSinkCounts("Pages/Dashboard.cshtml", 16, 0, 0, 0),
            new DomSinkCounts("Pages/Dashboard/MyTasks.cshtml", 0, 0, 0, 4),
            new DomSinkCounts("Pages/Dashboard/SuperviseInterviews.cshtml", 1, 0, 0, 0),
            new DomSinkCounts("Pages/DataInbox/Index.cshtml", 6, 0, 0, 0),
            new DomSinkCounts("Pages/DataInbox/Review.cshtml", 2, 0, 0, 0),
            new DomSinkCounts("Pages/Locations/Create.cshtml", 4, 0, 0, 0),
            new DomSinkCounts("Pages/Outbreaks/CaseDefinitions.cshtml", 9, 0, 0, 0),
            new DomSinkCounts("Pages/Outbreaks/LineList.cshtml", 9, 0, 0, 0),
            new DomSinkCounts("Pages/Patients/Create.cshtml", 5, 0, 0, 1),
            new DomSinkCounts("Pages/Patients/Edit.cshtml", 3, 0, 0, 1),
            new DomSinkCounts("Pages/Settings/CaseDefinitions/BuildCriteria.cshtml", 7, 0, 4, 0),
            new DomSinkCounts("Pages/Settings/HL7/Diagnostics.cshtml", 3, 0, 0, 0),
            new DomSinkCounts("Pages/Settings/HL7/Testing.cshtml", 2, 0, 0, 0),
            new DomSinkCounts("Pages/Settings/Mappings/EditMapping.cshtml", 0, 0, 0, 2),
            new DomSinkCounts("Pages/Settings/Surveys/DesignSurvey.cshtml", 0, 0, 0, 18),
            new DomSinkCounts("Pages/Tasks/CompleteSurvey.cshtml", 2, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/autocomplete.js", 2, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/collection-filter-helper.js", 1, 0, 1, 0),
            new DomSinkCounts("wwwroot/js/feedback-widget.js", 2, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/geocoding.js", 2, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/report-builder-actions.js", 10, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/report-builder-collections.js", 15, 0, 2, 0),
            new DomSinkCounts("wwwroot/js/report-builder-notifications.js", 2, 0, 0, 0),
            new DomSinkCounts("wwwroot/js/report-builder.js", 25, 0, 8, 0),
            new DomSinkCounts("wwwroot/js/review-queue.js", 1, 0, 0, 0)
        };

        var sentinelRoot = Path.Combine(GetRepositoryRoot(), "Sentinel");
        var roots = new[]
        {
            (Path.Combine(sentinelRoot, "Pages"), "*.cshtml"),
            (Path.Combine(sentinelRoot, "wwwroot", "js"), "*.js"),
            (Path.Combine(sentinelRoot, "Components"), "*.razor")
        };

        var actual = roots
            .Where(entry => Directory.Exists(entry.Item1))
            .SelectMany(entry => Directory.EnumerateFiles(entry.Item1, entry.Item2, SearchOption.AllDirectories))
            .Select(path => new DomSinkCounts(
                Path.GetRelativePath(sentinelRoot, path).Replace(Path.DirectorySeparatorChar, '/'),
                Count(path, "innerHTML\\s*="),
                Count(path, "outerHTML\\s*="),
                Count(path, "insertAdjacentHTML"),
                Count(path, "\\.html\\s*\\(")))
            .Where(entry => entry.Total > 0)
            .OrderBy(entry => entry.Path, StringComparer.Ordinal)
            .ToArray();

        var expectedOrdered = expected.OrderBy(entry => entry.Path, StringComparer.Ordinal).ToArray();
        Assert.Equal(expectedOrdered, actual);
    }

    [Fact]
    public void ReviewedDomSinkManifest_HasAnExplicitSafetyContractForEverySource()
    {
        var reviewedContracts = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Pages/Admin/Logs.cshtml"] = ["escapeHtml(log.Message)", "return levelMap[level] ||"],
            ["Pages/Cases/Create.cshtml"] = ["$('#exposureMessage').html('<strong>This disease requires exposure data."],
            ["Pages/Cases/CreateNew.cshtml"] = ["${escapeHtml(result.testTypeName || 'Lab Test')}", "${escapeHtml(selectedPatient.name)}"],
            ["Pages/Cases/Details.cshtml"] = ["return response.text();", "encodeURIComponent(taskId)"],
            ["Pages/Cases/Edit.cshtml"] = ["$dropdown.html('<div class=\"autocomplete-item disabled\">No results found</div>')"],
            ["Pages/Cases/EditExposure.cshtml"] = [".html('').append(option).trigger('change')"],
            ["Pages/Contacts/CreateNewContact.cshtml"] = ["${escapeHtml(selectedPatient.name)}", "+ escapeHtml(exposureDesc) +"],
            ["Pages/Contacts/Details.cshtml"] = ["const userName = escapeHtml(attempt.attemptedByUser?.displayName || 'Unknown User')"],
            ["Pages/Dashboard.cshtml"] = ["safeRelativeUrl(result.url)", "escapeHtml(item.displayText)"],
            ["Pages/Dashboard/MyTasks.cshtml"] = ["${escapeHtml(attempt.notes)}"],
            ["Pages/Dashboard/SuperviseInterviews.cshtml"] = ["button.textContent = label"],
            ["Pages/DataInbox/Index.cshtml"] = ["toast.appendChild(document.createTextNode(message))"],
            ["Pages/DataInbox/Review.cshtml"] = ["btn.innerHTML = '<span class=\"spinner-border spinner-border-sm me-2\"></span>Reprocessing...'"],
            ["Pages/Locations/Create.cshtml"] = ["+ escapeHtml(placeName) +", "text.innerText = pred.description || ''"],
            ["Pages/Outbreaks/CaseDefinitions.cshtml"] = ["${escapeHtml(criterion.input)}", "option.textContent = `${field.label} (${field.fieldType})`"],
            ["Pages/Outbreaks/LineList.cshtml"] = ["${escapeHtml(f.fieldPath)}", "Number.isSafeInteger(configId)"],
            ["Pages/Patients/Create.cshtml"] = ["text.innerText = pred.description || ''", "${escapeHtml(patient.name)}"],
            ["Pages/Patients/Edit.cshtml"] = ["+ escapeHtml(result.name || 'Address') +"],
            ["Pages/Settings/CaseDefinitions/BuildCriteria.cshtml"] = ["label.textContent = name ?? ''", "operatorSelect.insertAdjacentHTML('beforeend', '<option value=\"Contains\">Contains</option>')"],
            ["Pages/Settings/HL7/Diagnostics.cshtml"] = ["${escapeHtml(mapping.pathogenName)}", "${escapeHtml(report.recommendation)}"],
            ["Pages/Settings/HL7/Testing.cshtml"] = ["${escapeHtml(data.messageControlId || '-')}", "${escapeHtml(data.rawContent || 'No raw content available')}"],
            ["Pages/Settings/Mappings/EditMapping.cshtml"] = ["var descriptions = {", "descriptions[complexity] || descriptions['1']"],
            ["Pages/Settings/Surveys/DesignSurvey.cshtml"] = ["${escapeHtml(question.name)}", "sanitizeSurveyDefinition"],
            ["Pages/Tasks/CompleteSurvey.cshtml"] = ["alert.appendChild(document.createTextNode(message))", "btn.innerHTML = '<i class=\"bi bi-check-circle\"></i>Complete'"],
            ["wwwroot/js/autocomplete.js"] = ["innerHTML = '<div class=\"autocomplete-item no-results\">No results found</div>'"],
            ["wwwroot/js/collection-filter-helper.js"] = ["element.textContent = value == null ? '' : String(value)", "escapeAttribute(JSON.stringify(collectionSubFields || []))"],
            ["wwwroot/js/feedback-widget.js"] = ["alert.textContent = message", "modal.innerHTML = `"],
            ["wwwroot/js/geocoding.js"] = ["Address autocomplete disabled (using Nominatim).", "Requires Google Maps"],
            ["wwwroot/js/report-builder-actions.js"] = ["container.innerHTML = '<div id=\"wdr-preview-pivot\"></div>'", "recordCountSpan.textContent"],
            ["wwwroot/js/report-builder-collections.js"] = ["${this.escapeHtml(c.value)}", "option.textContent = fieldInfo.label || fieldInfo.Label || fieldName"],
            ["wwwroot/js/report-builder-notifications.js"] = ["${escapeNotificationHtml(message)}", "element.textContent = value ?? ''"],
            ["wwwroot/js/report-builder.js"] = ["data-field-path=\"${this.escapeHtml(field.fieldPath)}\"", "value=\"${this.escapeHtml(subFilter.value || '')}\""],
            ["wwwroot/js/review-queue.js"] = ["Object.entries(this.shortcuts)", "toggleBtn.textContent = isExpanded"]
        };

        foreach (var (relativePath, requiredFragments) in reviewedContracts)
        {
            var source = ReadSentinelFile(relativePath);
            foreach (var fragment in requiredFragments)
            {
                Assert.Contains(fragment, source, StringComparison.Ordinal);
            }
        }

        Assert.Equal(33, reviewedContracts.Count);
    }

    [Fact]
    public void DashboardDynamicWidgets_EncodeTextAndConstrainNavigationTokens()
    {
        var source = ReadSentinelFile("Pages/Dashboard.cshtml");

        Assert.Contains("safeCssToken(item.status", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(item.activityType)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(item.displayText)", source, StringComparison.Ordinal);
        Assert.Contains("safeRelativeUrl(result.url)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(result.title)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LineListConfigurationRendering_EncodesFieldsAndValidatesConfigurationIds()
    {
        var source = ReadSentinelFile("Pages/Outbreaks/LineList.cshtml");

        Assert.Contains("escapeHtml(f.fieldPath)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(f.displayName)", source, StringComparison.Ordinal);
        Assert.Contains("Number.isSafeInteger(configId)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(c.name)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Hl7Diagnostics_EncodesApiValuesBeforeRenderingResults()
    {
        var source = ReadSentinelFile("Pages/Settings/HL7/Diagnostics.cshtml");

        Assert.Contains("escapeHtml(mapping.pathogenName)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(report.recommendation)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(marker.result)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(issue)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminLogs_EncodesLogContentAndAllowsOnlyKnownPresentationClasses()
    {
        var source = ReadSentinelFile("Pages/Admin/Logs.cshtml");

        Assert.Contains("const levelMap", source, StringComparison.Ordinal);
        Assert.Contains("return levelMap[level] ||", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(log.Message)", source, StringComparison.Ordinal);
        Assert.Contains("escapeHtml(log.Exception)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportBuilder_UsesAttributeSafeEncodingForDynamicFieldMetadata()
    {
        var source = ReadSentinelFile("wwwroot/js/report-builder.js");

        Assert.Contains(".replace(/&/g, '&amp;')", source, StringComparison.Ordinal);
        Assert.Contains(".replace(/\"/g, '&quot;')", source, StringComparison.Ordinal);
        Assert.Contains(".replace(/'/g, '&#39;')", source, StringComparison.Ordinal);
        Assert.Contains("data-field-path=\"${this.escapeHtml(field.fieldPath)}\"", source, StringComparison.Ordinal);
        Assert.Contains("value=\"${this.escapeHtml(subFilter.value || '')}\"", source, StringComparison.Ordinal);
        Assert.Contains("groupId: this.toSafeOptionalInteger(filter.groupId)", source, StringComparison.Ordinal);
        Assert.Contains("data-group-id=\"${groupId}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportBuilderCollectionsAndNotifications_KeepDynamicValuesOutOfMarkupContexts()
    {
        var collections = ReadSentinelFile("wwwroot/js/report-builder-collections.js");
        var notifications = ReadSentinelFile("wwwroot/js/report-builder-notifications.js");

        Assert.Contains("<option value=\"${this.escapeHtml(c.value)}\" data-type=\"${this.escapeHtml(c.entityType)}\">${this.escapeHtml(c.label)}</option>", collections, StringComparison.Ordinal);
        Assert.Contains("option.textContent = fieldInfo.label || fieldInfo.Label || fieldName", collections, StringComparison.Ordinal);
        Assert.Contains("element.textContent = value ?? ''", notifications, StringComparison.Ordinal);
        Assert.Contains("${escapeNotificationHtml(message)}", notifications, StringComparison.Ordinal);
        Assert.Contains("${escapeNotificationHtml(title)}", notifications, StringComparison.Ordinal);
    }

    [Fact]
    public void LegacyCollectionFilterUi_SeparatesTextAndAttributeEncoding()
    {
        var source = ReadSentinelFile("wwwroot/js/collection-filter-helper.js");

        Assert.Contains("element.textContent = value == null ? '' : String(value)", source, StringComparison.Ordinal);
        Assert.Contains("return element.innerHTML", source, StringComparison.Ordinal);
        Assert.Contains("escapeAttribute(JSON.stringify(collectionSubFields || []))", source, StringComparison.Ordinal);
        Assert.Contains("value=\"${this.escapeAttribute(field)}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SurveyCompletion_UsesTextNodesForDynamicAlertsAndStaticMarkupForButtonState()
    {
        var source = ReadSentinelFile("Pages/Tasks/CompleteSurvey.cshtml");

        Assert.Contains("alert.appendChild(document.createTextNode(message))", source, StringComparison.Ordinal);
        Assert.Contains("headingElement.textContent = heading", source, StringComparison.Ordinal);
        Assert.Contains("btn.innerHTML = '<span class=\"spinner-border spinner-border-sm me-2\"></span>Saving...'", source, StringComparison.Ordinal);
        Assert.Contains("btn.innerHTML = '<i class=\"bi bi-check-circle\"></i>Complete'", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SurveyDesigner_MappingRowsEscapeSurveyAndCustomFieldValuesInTextAndAttributes()
    {
        var source = ReadSentinelFile("Pages/Settings/Surveys/DesignSurvey.cshtml");

        Assert.Contains("${escapeHtml(question.name)}", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(question.title)}", source, StringComparison.Ordinal);
        Assert.Contains("data-question=\"${escapeHtml(question.name)}\"", source, StringComparison.Ordinal);
        Assert.Contains("value=\"${escapeHtml(customFieldName)}\"", source, StringComparison.Ordinal);
        Assert.Contains(".replace(/\"/g, '&quot;')", source, StringComparison.Ordinal);
        Assert.Contains(".replace(/'/g, '&#39;')", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OutbreakCriteriaEditor_UsesTextNodesOrEscapingForCustomFieldAndCriterionValues()
    {
        var source = ReadSentinelFile("Pages/Outbreaks/CaseDefinitions.cshtml");

        Assert.Contains("option.textContent = `${field.label} (${field.fieldType})`", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(customField.label)}", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(v.id)}", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(v.displayText)}", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(criterion.input)}", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CaseAndContactSummaries_EscapeApiAndFormValuesBeforeDynamicMarkup()
    {
        var caseSource = ReadSentinelFile("Pages/Cases/CreateNew.cshtml");
        var contactSource = ReadSentinelFile("Pages/Contacts/CreateNewContact.cshtml");
        var contactDetailsSource = ReadSentinelFile("Pages/Contacts/Details.cshtml");

        Assert.Contains("${escapeHtml(result.testTypeName || 'Lab Test')}", caseSource, StringComparison.Ordinal);
        Assert.Contains("data-result-id=\"${escapeHtml(result.id)}\"", caseSource, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(location)}", caseSource, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(selectedPatient.name)}", caseSource, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(selectedSourceCase.patientName)}", contactSource, StringComparison.Ordinal);
        Assert.Contains("+ escapeHtml(exposureDesc) +", contactSource, StringComparison.Ordinal);
        Assert.Contains("const userName = escapeHtml(attempt.attemptedByUser?.displayName || 'Unknown User')", contactDetailsSource, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(attempt.notes)}", contactDetailsSource, StringComparison.Ordinal);
    }

    [Fact]
    public void DashboardCallHistory_EscapesUserAndNotesWhileUsingFixedOutcomeClasses()
    {
        var source = ReadSentinelFile("Pages/Dashboard/MyTasks.cshtml");

        Assert.Contains("const userName = escapeHtml(attempt.attemptedByUser?.displayName || 'Unknown User')", source, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(attempt.notes)}", source, StringComparison.Ordinal);
        Assert.Contains("case 0: outcomeText = 'Completed'; outcomeClass = 'success';", source, StringComparison.Ordinal);
        Assert.Contains(".replace(/\"/g, '&quot;')", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SharedClientWidgets_UseStaticMarkupOrDomNodesForDynamicValues()
    {
        var reviewQueue = ReadSentinelFile("wwwroot/js/review-queue.js");
        var autocomplete = ReadSentinelFile("wwwroot/js/autocomplete.js");
        var superviseInterviews = ReadSentinelFile("Pages/Dashboard/SuperviseInterviews.cshtml");

        Assert.Contains("Object.entries(this.shortcuts)", reviewQueue, StringComparison.Ordinal);
        Assert.Contains("innerHTML = '<div class=\"autocomplete-item no-results\">No results found</div>'", autocomplete, StringComparison.Ordinal);
        Assert.Contains("paginationEl.innerHTML = ''", superviseInterviews, StringComparison.Ordinal);
        Assert.Contains("button.textContent = label", superviseInterviews, StringComparison.Ordinal);
    }

    [Fact]
    public void Hl7TestingAndCriteriaBuilder_EncodeOrUseTextNodesForConfiguredValues()
    {
        var testing = ReadSentinelFile("Pages/Settings/HL7/Testing.cshtml");
        var criteria = ReadSentinelFile("Pages/Settings/CaseDefinitions/BuildCriteria.cshtml");

        Assert.Contains("${escapeHtml(data.messageControlId || '-')}", testing, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(marker.testName || '-')}", testing, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(data.rawContent || 'No raw content available')}", testing, StringComparison.Ordinal);
        Assert.Contains("label.textContent = name ?? ''", criteria, StringComparison.Ordinal);
        Assert.Contains("button.textContent = '×'", criteria, StringComparison.Ordinal);
    }

    [Fact]
    public void AddressAutocompleteAndInbox_RenderExternalOrWorkflowValuesSafely()
    {
        var patientCreate = ReadSentinelFile("Pages/Patients/Create.cshtml");
        var patientEdit = ReadSentinelFile("Pages/Patients/Edit.cshtml");
        var locationCreate = ReadSentinelFile("Pages/Locations/Create.cshtml");
        var inbox = ReadSentinelFile("Pages/DataInbox/Index.cshtml");

        Assert.Contains("text.innerText = pred.description || ''", patientCreate, StringComparison.Ordinal);
        Assert.Contains("+ escapeHtml(placeName) +", patientCreate, StringComparison.Ordinal);
        Assert.Contains("${escapeHtml(patient.name)}", patientCreate, StringComparison.Ordinal);
        Assert.Contains("+ escapeHtml(result.name || 'Address') +", patientEdit, StringComparison.Ordinal);
        Assert.Contains("text.innerText = pred.description || ''", locationCreate, StringComparison.Ordinal);
        Assert.Contains("+ escapeHtml(placeName) +", locationCreate, StringComparison.Ordinal);
        Assert.Contains("toast.appendChild(document.createTextNode(message))", inbox, StringComparison.Ordinal);
    }

    [Fact]
    public void FirstPartyClientCode_DoesNotUseDynamicCodeExecution()
    {
        var sourceRoots = new[]
        {
            Path.Combine(GetRepositoryRoot(), "Sentinel", "Pages"),
            Path.Combine(GetRepositoryRoot(), "Sentinel", "wwwroot", "js"),
            Path.Combine(GetRepositoryRoot(), "Sentinel", "Components")
        };

        var patterns = new[] { "eval(", "new Function(", "document.write" };
        var files = sourceRoots.Where(Directory.Exists).SelectMany(root => Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                        || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase));

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            foreach (var pattern in patterns)
            {
                Assert.DoesNotContain(pattern, source, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void SurveyDesignerPreview_SanitizesDefinitionAndEscapesMarkdownBeforeRendering()
    {
        var source = ReadSentinelFile("Pages/Settings/Surveys/DesignSurvey.cshtml");

        Assert.Contains("surveyCreator.JSON = sanitizeSurveyDefinition(initialSurveyJson)", source, StringComparison.Ordinal);
        Assert.Contains("const surveyJson = sanitizeSurveyDefinition(surveyCreator.JSON)", source, StringComparison.Ordinal);
        Assert.Contains("options.html = escapeHtml(options.text).replace", source, StringComparison.Ordinal);
        Assert.Contains("previewModal.one('shown.bs.modal'", source, StringComparison.Ordinal);
        Assert.Contains("survey.render(\"surveyPreviewContainer\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SurveyRenderingBoundaries_RemoveEverySurveyJsHtmlPropertyBeforeRendering()
    {
        var surveyRenderers = new[]
        {
            "Pages/Settings/Surveys/DesignSurvey.cshtml",
            "Pages/Tasks/CompleteSurvey.cshtml",
            "Pages/Tasks/ViewSurveyResult.cshtml"
        };

        foreach (var relativePath in surveyRenderers)
        {
            var source = ReadSentinelFile(relativePath);

            Assert.Contains("node.type.toLowerCase() === 'html'", source, StringComparison.Ordinal);
            Assert.Contains("'htmlcontent', 'completedhtml', 'completedbeforehtml', 'loadinghtml'", source, StringComparison.Ordinal);
            Assert.Contains(".includes(key.toLowerCase())", source, StringComparison.Ordinal);
        }
    }

    private static string ReadSentinelFile(string relativePath) =>
        File.ReadAllText(Path.Combine(GetRepositoryRoot(), "Sentinel", relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static int Count(string path, string pattern) =>
        System.Text.RegularExpressions.Regex.Matches(File.ReadAllText(path), pattern).Count;

    private static string GetRepositoryRoot()
    {
        var candidates = new[]
        {
            new DirectoryInfo(Directory.GetCurrentDirectory()),
            new DirectoryInfo(AppContext.BaseDirectory)
        };

        foreach (var candidate in candidates)
        {
            for (var current = candidate; current is not null; current = current.Parent)
            {
                if (Directory.Exists(Path.Combine(current.FullName, "Sentinel", "Pages")))
                {
                    return current.FullName;
                }
            }
        }

        throw new DirectoryNotFoundException("Could not locate the Sentinel repository root for DOM sink checks.");
    }

    private sealed record DomSinkCounts(string Path, int InnerHtml, int OuterHtml, int InsertAdjacentHtml, int JQueryHtml)
    {
        public int Total => InnerHtml + OuterHtml + InsertAdjacentHtml + JQueryHtml;
    }
}
