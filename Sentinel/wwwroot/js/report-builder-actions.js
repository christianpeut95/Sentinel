// Report Builder - Data Collection & Actions Module (Part 3)
// This is a continuation of report-builder.js

console.log('[report-builder-actions.js] Loading...');

// Add these functions to the ReportBuilder object

ReportBuilder.getFilters = function() {
    const filterElements = document.querySelectorAll('#filters .rb-list-item');
    const filters = [];

    console.log('[getFilters] Found', filterElements.length, 'filter elements');

    filterElements.forEach((el, i) => {
        const fieldSelect = el.querySelector('.rb-filter-field');
        const field = fieldSelect?.value;
        let operator = el.querySelector('.rb-filter-operator')?.value;

        console.log(`[getFilters] Filter ${i}:`, { field, operator });

        if (field && operator) {
            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption?.dataset.type || 'String';

            let value = '';
            let isDynamicDate = false;
            let dynamicDateType = null;
            let dynamicDateOffset = null;
            let dynamicDateOffsetUnit = null;

            // Check if this is a date field with combined dropdown UI
            const valueContainer = el.querySelector('.rb-filter-value-container');
            const combinedDateSelect = valueContainer?.querySelector('.filter-date-combined');

            if (dataType.includes('Date') && combinedDateSelect) {
                const combinedValue = combinedDateSelect.value;
                console.log(`[getFilters] Date filter combined value:`, combinedValue);

                if (combinedValue && combinedValue !== '') {
                    if (combinedValue === 'static') {
                        // User selected "Pick a specific date..." - read from date input
                        const dateInput = valueContainer.querySelector('.filter-value');
                        value = dateInput?.value || '';
                        isDynamicDate = false;
                    } else if (combinedValue === 'custom') {
                        // User selected custom condition
                        const customCondition = valueContainer.querySelector('.filter-custom-condition');

                        // Check if dynamic or static
                        const isDynamic = customCondition?.querySelector('input[value="dynamic"]:checked') !== null;

                        if (isDynamic) {
                            // Read dynamic date inputs
                            const offsetValue = customCondition?.querySelector('.filter-dynamic-offset-value')?.value;
                            const offsetUnit = customCondition?.querySelector('.filter-dynamic-offset-unit')?.value;
                            const direction = customCondition?.querySelector('.filter-dynamic-offset-direction')?.value;

                            // Read the operator from custom condition
                            const customOperator = customCondition?.querySelector('.filter-operator')?.value;
                            operator = customOperator || operator;

                            dynamicDateOffset = offsetValue ? parseInt(offsetValue) : null;
                            dynamicDateOffsetUnit = offsetUnit || 'Days';
                            isDynamicDate = true;

                            // Map direction + unit to dynamic date type based on operator
                            // For range operators (InLast, InNext), use those directly
                            // For comparison operators (GreaterThan, LessThan, etc.), use PastDays/NextDays/PastWeeks/etc.
                            if (operator === 'InLast' || operator === 'InNext') {
                                // Range operators - keep operator as dynamicDateType
                                dynamicDateType = operator;
                            } else {
                                // Comparison operators - need point-in-time dynamic date types
                                const capitalizedDirection = direction === 'past' ? 'Past' : 'Next';
                                const capitalizedUnit = offsetUnit.charAt(0).toUpperCase() + offsetUnit.slice(1);
                                dynamicDateType = capitalizedDirection + capitalizedUnit;
                            }

                            value = offsetValue || '';
                        } else {
                            // Static custom date
                            const staticDateInput = customCondition?.querySelector('.filter-custom-static-value');
                            value = staticDateInput?.value || '';
                            isDynamicDate = false;

                            // Read the operator from custom condition
                            const customOperator = customCondition?.querySelector('.filter-operator')?.value;
                            operator = customOperator || operator;
                        }
                    } else {
                        // Preset value like "InLast|7" or "Equals|Today"
                        const parts = combinedValue.split('|');
                        if (parts.length === 2) {
                            operator = parts[0];
                            const presetValue = parts[1];

                            // Check if it's a numeric offset (InLast/InNext)
                            if (operator === 'InLast' || operator === 'InNext') {
                                dynamicDateType = operator;
                                dynamicDateOffset = parseInt(presetValue);
                                dynamicDateOffsetUnit = 'Days';
                                isDynamicDate = true;
                                value = presetValue;
                            } else {
                                // Named dynamic dates like Today, Yesterday, StartOfWeek, etc.
                                dynamicDateType = presetValue;
                                isDynamicDate = true;
                                value = '';
                            }
                                                }
                                            }
                                        }
                                    } else if (operator === 'IsNull' || operator === 'IsNotNull') {
                value = '';
            } else {
                // Regular non-date field or field without combined UI
                value = el.querySelector('.rb-filter-value')?.value || '';
            }

            console.log(`[getFilters] Adding filter:`, { 
                field, 
                operator, 
                value, 
                dataType, 
                isDynamicDate, 
                dynamicDateType, 
                dynamicDateOffset, 
                dynamicDateOffsetUnit 
            });

            filters.push({
                fieldPath: field,
                operator: operator,
                value: value,
                dataType: dataType,
                displayOrder: i,
                isCustomField: false,
                customFieldDefinitionId: null,
                logicOperator: 'AND',
                groupId: null,
                groupLogicOperator: 'AND',
                isDynamicDate: isDynamicDate,
                dynamicDateType: dynamicDateType,
                dynamicDateOffset: dynamicDateOffset,
                dynamicDateOffsetUnit: dynamicDateOffsetUnit
            });
        }
    });

    console.log('[getFilters] Returning', filters.length, 'filters:', filters);
    return filters;
};

ReportBuilder.getCollectionQueries = function() {
    const queryElements = document.querySelectorAll('[id^="collection-query-"]');
    const queries = [];

    queryElements.forEach((el) => {
        const queryId = parseInt(el.dataset.queryId);
        const collectionPath = document.getElementById(`collection-${queryId}`)?.value;
        const operation = document.getElementById(`operation-${queryId}`)?.value;
        const displayAsColumn = document.getElementById(`display-as-column-${queryId}`)?.checked || false;
        const columnName = document.getElementById(`column-name-${queryId}`)?.value || '';
        const aggregateField = document.getElementById(`aggregate-field-${queryId}`)?.value || null;

        if (!collectionPath || !operation) return;

        // Parse nested collection path (e.g., "LabResults.Markers")
        const pathParts = collectionPath.split('.');
        const collectionName = pathParts[0];
        const subCollectionName = pathParts.length > 1 ? pathParts[1] : null;

        const query = {
            collectionName: collectionName,
            subCollectionName: subCollectionName,  // NEW: capture sub-collection
            operation: operation,
            aggregateField: aggregateField,
            displayAsColumn: displayAsColumn,
            columnName: columnName,
            subFilters: []
        };

        console.log('[getCollectionQueries] Serializing query:', {
            queryId,
            collectionName,
            subCollectionName,
            isNested: !!subCollectionName
        });

        if (!displayAsColumn && ['Count', 'Sum', 'Average', 'Min', 'Max'].includes(operation)) {
            query.comparator = document.getElementById(`comparator-${queryId}`)?.value || 'GreaterThan';
            const valueInput = document.getElementById(`value-${queryId}`);
            query.value = valueInput ? parseFloat(valueInput.value) || 0 : 0;
        }
        
        const subFilterElements = el.querySelectorAll('[id^="subfilter-"]');
        subFilterElements.forEach((subEl) => {
            const fieldSelect = subEl.querySelector('.rb-subfilter-field');
            const field = fieldSelect?.value;
            let operator = subEl.querySelector('.rb-subfilter-operator')?.value;

            if (field && operator) {
                const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
                const dataType = selectedOption.dataset.type || 'String';

                let value = '';
                let isDynamicDate = false;
                let dynamicDateType = null;
                let dynamicDateOffset = null;
                let dynamicDateOffsetUnit = null;

                const combinedSelect = subEl.querySelector('.filter-date-combined');
                if (combinedSelect) {
                    const combinedValue = combinedSelect.value;
                    const customCondition = subEl.querySelector('.filter-custom-condition');
                    const isCustomConditionVisible = customCondition && customCondition.style.display !== 'none';

                    if (combinedValue === 'static') {
                        value = subEl.querySelector('.rb-subfilter-value')?.value || '';
                        isDynamicDate = false;
                    } else if (combinedValue === 'custom' || isCustomConditionVisible) {
                        const customOperator = customCondition?.querySelector('.filter-operator')?.value;
                        const dateTypeRadio = customCondition?.querySelector('input[name^="custom-date-type-"]:checked');
                        const dateType = dateTypeRadio?.value || 'dynamic';

                        if (dateType === 'static') {
                            const staticDateInput = customCondition?.querySelector('.filter-custom-static-value');
                            const staticDateValue = staticDateInput?.value;

                            if (customOperator && staticDateValue) {
                                operator = customOperator;
                                value = staticDateValue;
                                isDynamicDate = false;
                            }
                        } else {
                            const offsetValue = customCondition?.querySelector('.filter-dynamic-offset-value')?.value;
                            const offsetUnit = customCondition?.querySelector('.filter-dynamic-offset-unit')?.value || 'Days';
                            const direction = customCondition?.querySelector('.filter-dynamic-offset-direction')?.value || 'past';

                            if (customOperator && offsetValue) {
                                operator = customOperator;
                                isDynamicDate = true;
                                dynamicDateOffset = parseInt(offsetValue);
                                dynamicDateOffsetUnit = offsetUnit;
                                const capitalizedDirection = direction.charAt(0).toUpperCase() + direction.slice(1);
                                const capitalizedUnit = offsetUnit.charAt(0).toUpperCase() + offsetUnit.slice(1);
                                dynamicDateType = capitalizedDirection + capitalizedUnit;
                                value = '';
                            }
                        }
                    } else if (combinedValue && combinedValue.includes('|')) {
                        const [presetOperator, presetValue] = combinedValue.split('|');

                        if (presetOperator) {
                            operator = presetOperator;
                        }

                        if (presetOperator === 'InLast' || presetOperator === 'InNext') {
                            // InLast/InNext with numeric values are dynamic date filters
                            isDynamicDate = true;
                            dynamicDateOffset = parseInt(presetValue);
                            dynamicDateOffsetUnit = 'Days'; // Default to days for InLast/InNext
                            const direction = presetOperator === 'InLast' ? 'Past' : 'Next';
                            dynamicDateType = direction + 'Days';
                            value = '';
                        } else if (!isNaN(parseInt(presetValue))) {
                            value = presetValue;
                            isDynamicDate = false;
                        } else {
                            isDynamicDate = true;
                            dynamicDateType = presetValue;

                            const presetMatch = presetValue.match(/^(Past|Next)(\d+)(Days|Weeks|Months)$/);
                            if (presetMatch) {
                                const [, direction, num, unit] = presetMatch;
                                dynamicDateOffset = parseInt(num);
                                dynamicDateOffsetUnit = unit;
                                dynamicDateType = direction + unit;
                            }
                            value = '';
                        }
                    } else {
                        value = subEl.querySelector('.rb-subfilter-value')?.value || '';
                        isDynamicDate = false;
                    }
                } else {
                    value = subEl.querySelector('.rb-subfilter-value')?.value || '';
                }

                query.subFilters.push({
                    field: field,
                    operator: operator,
                    value: value,
                    dataType: dataType,
                    isDynamicDate: isDynamicDate,
                    dynamicDateType: dynamicDateType,
                    dynamicDateOffset: dynamicDateOffset,
                    dynamicDateOffsetUnit: dynamicDateOffsetUnit
                });
            }
        });
        
        queries.push(query);
    });
    
    return queries;
};

ReportBuilder.preview = async function() {
    console.log('[preview] Starting preview...');

    if (this.selectedFields.length === 0) {
        alert('Please select at least one field');
        return;
    }

    // Show loading in preview container
    const container = document.getElementById('previewContainer');
    if (!container) {
        console.error('[preview] previewContainer not found');
        return;
    }

    container.innerHTML = `
        <div class="rb-empty-state">
            <div class="rb-empty-state-icon">⏳</div>
            <div class="rb-empty-state-title">Loading Preview...</div>
            <div class="rb-empty-state-text">Fetching data from server</div>
        </div>
    `;

    try {
        const filters = this.getFilters();
        console.log('[preview] Selected fields:', this.selectedFields.length);
        console.log('[preview] Filters:', filters.length);

        const response = await fetch('/api/reports/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                entityType: document.getElementById('entityTypeSelector').value,
                fields: this.selectedFields.map((f, i) => ({
                    fieldPath: f.fieldPath,
                    displayName: f.displayName,
                    dataType: f.dataType,
                    displayOrder: i,
                    isCustomField: f.isCustom || false,
                    customFieldDefinitionId: f.customId ? parseInt(f.customId) : null
                })),
                filters: filters,
                collectionQueries: this.getCollectionQueries()
            })
        });

        console.log('[preview] Response status:', response.status);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('[preview] Server error:', errorText);
            throw new Error(`Server error: ${response.status}`);
        }

        const result = await response.json();
        console.log('[preview] Result:', result.success ? `${result.data?.length || 0} rows` : 'failed');

        if (result.success) {
            this.renderPreview(result.data, filters);
        } else {
            container.innerHTML = `
                <div class="rb-empty-state">
                    <div class="rb-empty-state-icon">⚠️</div>
                    <div class="rb-empty-state-title">Preview Failed</div>
                    <div class="rb-empty-state-text">${result.error || 'Unknown error'}</div>
                </div>
            `;
        }
    } catch (error) {
        console.error('[preview] Error:', error);
        container.innerHTML = `
            <div class="rb-empty-state">
                <div class="rb-empty-state-icon">❌</div>
                <div class="rb-empty-state-title">Error Loading Preview</div>
                <div class="rb-empty-state-text">${error.message}</div>
            </div>
        `;
    }
};

ReportBuilder.renderPreview = function(data, filters) {
    const container = document.getElementById('previewContainer');
    const recordCountSpan = document.getElementById('previewRecordCount');

    if (!data || data.length === 0) {
        container.innerHTML = `
            <div class="rb-empty-state">
                <div class="rb-empty-state-icon">📊</div>
                <div class="rb-empty-state-title">No Data Found</div>
                <div class="rb-empty-state-text">Your filters returned 0 rows. Try adjusting your filter criteria.</div>
            </div>
        `;
        if (recordCountSpan) recordCountSpan.textContent = '(0 rows)';
        return;
    }

    // Update record count in toolbar
    if (recordCountSpan) {
        const filterText = filters && filters.length > 0 ? ` · ${filters.length} filter(s)` : '';
        recordCountSpan.textContent = `(${data.length} rows${filterText})`;
    }

    // Dispose preview-specific pivot instance
    if (window.previewPivotInstance) {
        try {
            window.previewPivotInstance.dispose();
        } catch (e) {
            console.warn('[renderPreview] Error disposing previous instance:', e);
        }
        window.previewPivotInstance = null;
    }

    container.innerHTML = '<div id="wdr-preview-pivot"></div>';

    // Use the saved preview configuration, not the pivot configuration
    const savedPreviewConfig = this.savedPreviewConfiguration;
    let reportConfig;

    if (savedPreviewConfig && savedPreviewConfig.length > 0) {
        try {
            reportConfig = JSON.parse(savedPreviewConfig);
            reportConfig.dataSource = { data: data };
            console.log('[renderPreview] Using saved preview configuration');
        } catch (e) {
            console.warn('[renderPreview] Failed to parse saved config, using default:', e);
            reportConfig = this.getDefaultPivotConfig(data);
        }
    } else {
        console.log('[renderPreview] No saved configuration, using default');
        reportConfig = this.getDefaultPivotConfig(data);
    }

    // Calculate available height - preview is in a split pane
    const containerHeight = container.offsetHeight || 600;
    const pivotHeight = Math.max(400, containerHeight - 10);

    window.previewPivotInstance = new WebDataRocks({
        container: "#wdr-preview-pivot",
        toolbar: true,
        height: pivotHeight,
        report: reportConfig,
        global: {
            localization: {
                grid: {
                    blankMember: "(blank)"
                }
            }
        },
        customizeCell: function(cell, data) {
            if (data.type === "value" && typeof data.value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(data.value)) {
                cell.text = data.value.split('T')[0];
            }
        },
        reportcomplete: function() {
            console.log('[WebDataRocks Preview] Report rendered');
            // Capture the configuration whenever the report is updated
            try {
                const currentReport = window.previewPivotInstance.getReport();
                ReportBuilder.savedPreviewConfiguration = JSON.stringify(currentReport);
                console.log('[WebDataRocks Preview] Configuration captured');
            } catch (e) {
                console.warn('[WebDataRocks Preview] Failed to capture configuration:', e);
            }
        }
    });
};


ReportBuilder.getDefaultPivotConfig = function(data) {
    if (!data || data.length === 0) {
        return {
            dataSource: { data: [] },
            slice: { rows: [], columns: [], measures: [] }
        };
    }

    const dataKeys = Object.keys(data[0] || {});

    // Filter to only include selected display fields
    const selectedFieldPaths = this.selectedFields.map(f => f.fieldPath);
    const filteredKeys = dataKeys.filter(key => selectedFieldPaths.includes(key));

    // If no fields are selected, fall back to all keys (but this shouldn't happen)
    const keysToUse = filteredKeys.length > 0 ? filteredKeys : dataKeys;

    const allMeasures = keysToUse.map(key => ({
        uniqueName: key,
        aggregation: "none"
    }));

    return {
        dataSource: {
            data: data
        },
        slice: {
            rows: [],
            columns: [{ uniqueName: "Measures" }],
            measures: allMeasures
        },
        options: {
            grid: {
                type: "flat",
                showTotals: false,
                showGrandTotals: "off"
            },
            configuratorActive: false,
            showAggregationLabels: false
        },
        formats: [{
            name: "",
            thousandsSeparator: ",",
            decimalSeparator: ".",
            decimalPlaces: 2
        }]
    };
};

ReportBuilder.save = async function() {
    console.log('[save] Starting save process...');

    const name = document.getElementById('reportName').value;
    console.log('[save] Report name:', name);

    if (!name) {
        ReportBuilderNotifications.showToast('Please enter a report name', 'warning');
        return;
    }

    if (this.selectedFields.length === 0) {
        ReportBuilderNotifications.showToast('Please select at least one field', 'warning');
        return;
    }

    try {
        let pivotConfig = null;
        if (window.pivotGridInstance) {
            try {
                const report = window.pivotGridInstance.getReport();
                pivotConfig = JSON.stringify(report);
            } catch (e) {
                console.error('Failed to get pivot configuration', e);
            }
        }

        let previewConfig = null;
        if (window.previewPivotInstance) {
            try {
                const report = window.previewPivotInstance.getReport();
                previewConfig = JSON.stringify(report);
            } catch (e) {
                console.error('Failed to get preview configuration', e);
            }
        }

        const payload = {
            reportId: this.reportId,
            name: name,
            description: document.getElementById('reportDescription').value,
            entityType: document.getElementById('entityTypeSelector').value,
            category: document.getElementById('reportCategory').value,
            isPublic: document.getElementById('reportPublic').checked,
            pivotConfiguration: pivotConfig,
            previewConfiguration: previewConfig,
            fields: this.selectedFields.map((f, i) => ({
                fieldPath: f.fieldPath,
                displayName: f.displayName,
                dataType: f.dataType,
                displayOrder: i,
                isCustomField: f.isCustom || false,
                customFieldDefinitionId: f.customId ? parseInt(f.customId) : null
            })),
            filters: this.getFilters(),
            collectionQueries: this.getCollectionQueries()
        };

        console.log('[save] Payload:', payload);
        console.log('[save] Sending to /api/reports/save...');

        const response = await fetch('/api/reports/save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });

        console.log('[save] Response status:', response.status);

        const result = await response.json();
        console.log('[save] Result:', result);

        if (result.success) {
            // Clear auto-saved draft since we explicitly saved
            ReportBuilder.clearAutoSavedDraft();
            ReportBuilderNotifications.showToast('Report saved successfully!', 'success', 2000);
            setTimeout(() => {
                window.location.href = '/Reports/Index';
            }, 2000);
        } else {
            ReportBuilderNotifications.showToast('Error saving report: ' + result.error, 'error', 5000);
        }
    } catch (error) {
        console.error('[save] Exception:', error);
        ReportBuilderNotifications.showToast('Failed to save report: ' + error.message, 'error', 5000);
    }
};

// ==================== PIVOT FUNCTIONS ====================

ReportBuilder.loadPivot = async function() {
    console.log('[loadPivot] Starting pivot load...');

    if (this.selectedFields.length === 0) {
        ReportBuilderNotifications.showToast('Please select at least one field', 'warning');
        return;
    }

    // Switch to pivot tab
    const pivotTab = document.querySelector('.rb-tab[data-tab="pivot"]');
    if (pivotTab && !pivotTab.classList.contains('active')) {
        pivotTab.click();
    }

    // Show loading in pivot container
    const container = document.getElementById('wdr-pivot-grid');
    if (!container) {
        console.error('[loadPivot] wdr-pivot-grid container not found');
        return;
    }

    container.innerHTML = `
        <div class="rb-empty-state">
            <div class="rb-empty-state-icon">⏳</div>
            <div class="rb-empty-state-title">Loading Pivot...</div>
            <div class="rb-empty-state-text">Fetching data from server</div>
        </div>
    `;

    try {
        const filters = this.getFilters();
        console.log('[loadPivot] Selected fields:', this.selectedFields.length);
        console.log('[loadPivot] Filters:', filters.length);

        const response = await fetch('/api/reports/preview', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                entityType: document.getElementById('entityTypeSelector').value,
                fields: this.selectedFields.map((f, i) => ({
                    fieldPath: f.fieldPath,
                    displayName: f.displayName,
                    dataType: f.dataType,
                    displayOrder: i,
                    isCustomField: f.isCustom || false,
                    customFieldDefinitionId: f.customId ? parseInt(f.customId) : null
                })),
                filters: filters,
                collectionQueries: this.getCollectionQueries()
            })
        });

        console.log('[loadPivot] Response status:', response.status);

        if (!response.ok) {
            const errorText = await response.text();
            console.error('[loadPivot] Server error:', errorText);
            throw new Error(`Server error: ${response.status}`);
        }

        const result = await response.json();
        console.log('[loadPivot] Result:', result.success ? `${result.data?.length || 0} rows` : 'failed');

        if (result.success) {
            this.renderPivot(result.data, filters);
        } else {
            container.innerHTML = `
                <div class="rb-empty-state">
                    <div class="rb-empty-state-icon">⚠️</div>
                    <div class="rb-empty-state-title">Pivot Failed</div>
                    <div class="rb-empty-state-text">${result.error || 'Unknown error'}</div>
                </div>
            `;
        }
    } catch (error) {
        console.error('[loadPivot] Error:', error);
        container.innerHTML = `
            <div class="rb-empty-state">
                <div class="rb-empty-state-icon">❌</div>
                <div class="rb-empty-state-title">Error Loading Pivot</div>
                <div class="rb-empty-state-text">${error.message}</div>
            </div>
        `;
    }
};

ReportBuilder.renderPivot = function(data, filters) {
    const container = document.getElementById('wdr-pivot-grid');
    const recordCountSpan = document.getElementById('pivotRecordCount');

    if (!container) {
        console.error('[renderPivot] wdr-pivot-grid container not found');
        return;
    }

    if (!data || data.length === 0) {
        container.innerHTML = `
            <div class="rb-empty-state">
                <div class="rb-empty-state-icon">📊</div>
                <div class="rb-empty-state-title">No Data Found</div>
                <div class="rb-empty-state-text">Your filters returned 0 rows. Try adjusting your filter criteria.</div>
            </div>
        `;
        if (recordCountSpan) recordCountSpan.textContent = '(0 rows)';
        return;
    }

    // Update record count in toolbar
    if (recordCountSpan) {
        const filterText = filters && filters.length > 0 ? ` · ${filters.length} filter(s)` : '';
        recordCountSpan.textContent = `(${data.length} rows${filterText})`;
    }

    if (window.pivotGridInstance) {
        try {
            window.pivotGridInstance.dispose();
        } catch (e) {
            console.warn('[renderPivot] Error disposing previous instance:', e);
        }
        window.pivotGridInstance = null;
    }

    container.innerHTML = '<div id="wdr-pivot-actual"></div>';

    const savedPivotConfig = this.savedPivotConfiguration;
    let reportConfig;

    if (savedPivotConfig && savedPivotConfig.length > 0) {
        try {
            reportConfig = JSON.parse(savedPivotConfig);
            reportConfig.dataSource = { data: data };
        } catch (e) {
            reportConfig = this.getPivotConfig(data);
        }
    } else {
        reportConfig = this.getPivotConfig(data);
    }

    // Calculate available height for the pivot
    // Account for toolbar (40px) and some margin
    const containerHeight = container.offsetHeight || 600;
    const pivotHeight = Math.max(500, containerHeight - 10);

    window.pivotGridInstance = new WebDataRocks({
        container: "#wdr-pivot-actual",
        toolbar: true,
        height: pivotHeight,
        report: reportConfig,
        global: {
            localization: {
                grid: {
                    blankMember: "(blank)"
                }
            }
        },
        customizeCell: function(cell, data) {
            if (data.type === "value" && typeof data.value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(data.value)) {
                cell.text = data.value.split('T')[0];
            }
        },
        beforetoolbarcreated: function(toolbar) {
            // Ensure toolbar is properly initialized
            console.log('[WebDataRocks] Toolbar created');
        },
        reportcomplete: function() {
            console.log('[WebDataRocks] Report rendered');
            // Capture the configuration whenever the report is updated
            try {
                const currentReport = window.pivotGridInstance.getReport();
                ReportBuilder.savedPivotConfiguration = JSON.stringify(currentReport);
                console.log('[WebDataRocks] Configuration captured');
            } catch (e) {
                console.warn('[WebDataRocks] Failed to capture configuration:', e);
            }
        }
    });

    console.log('[renderPivot] Pivot rendered with', data.length, 'rows at height', pivotHeight);
};

ReportBuilder.getPivotConfig = function(data) {
    if (!data || data.length === 0) {
        return {
            dataSource: { data: [] },
            slice: { rows: [], columns: [], measures: [] }
        };
    }

    const dataKeys = Object.keys(data[0] || {});

    // Filter to only include selected display fields
    const selectedFieldPaths = this.selectedFields.map(f => f.fieldPath);
    const filteredKeys = dataKeys.filter(key => selectedFieldPaths.includes(key));

    // If no fields are selected, fall back to all keys
    const keysToUse = filteredKeys.length > 0 ? filteredKeys : dataKeys;

    return {
        dataSource: {
            data: data
        },
        slice: {
            rows: [],
            columns: [{ uniqueName: "Measures" }],
            measures: keysToUse.slice(0, 4).map(key => ({
                uniqueName: key,
                aggregation: "count"
            }))
        },
        options: {
            grid: {
                type: "compact",
                showTotals: true,
                showGrandTotals: "on"
            },
            configuratorActive: true
        }
    };
};

ReportBuilder.loadDefaultFields = async function() {
    const entityType = document.getElementById('entityTypeSelector').value;
    const btnLoadDefaults = document.getElementById('btnLoadDefaults');
    
    if (!entityType) {
        ReportBuilderNotifications.showToast('Please select an entity type first', 'warning');
        return;
    }

    if (this.selectedFields.length > 0) {
        // Capture 'this' context for use in callback
        const self = this;

        ReportBuilderNotifications.confirm(
            'This will replace your current field selection.',
            () => {
                self.applyDefaultFieldsImpl(entityType);
            },
            null,
            { title: 'Replace Field Selection?', confirmText: 'Replace' }
        );
        return;
    }
    
    try {
        btnLoadDefaults.disabled = true;
        btnLoadDefaults.innerHTML = '<i class="bi bi-hourglass-split me-1"></i> Loading...';
        
        const response = await fetch('/Reports/Builder?handler=GetDefaultFields', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
            },
            body: JSON.stringify({ entityType: entityType })
        });
        
        const result = await response.json();
        
        if (result.success && result.fields) {
            this.selectedFields = [];
            const selectedFieldsContainer = document.getElementById('selectedFields');
            selectedFieldsContainer.innerHTML = '';
            
            result.fields.forEach(field => {
                this.addField({
                    fieldPath: field.fieldPath,
                    displayName: field.displayName,
                    dataType: field.dataType,
                    isCustom: field.isCustomField,
                    customId: field.customFieldDefinitionId
                });
            });
            
            const successMsg = document.createElement('div');
            successMsg.className = 'alert alert-success alert-dismissible fade show mt-2';
            successMsg.innerHTML = `
                <i class="bi bi-check-circle me-2"></i>
                <strong>Loaded ${result.fields.length} default fields</strong>
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;
            selectedFieldsContainer.insertAdjacentElement('beforebegin', successMsg);
            
            setTimeout(() => {
                successMsg.remove();
            }, 3000);
            
        } else {
            ReportBuilderNotifications.showToast('Failed to load default fields: ' + (result.error || 'Unknown error'), 'error', 5000);
        }
    } catch (error) {
        ReportBuilderNotifications.showToast('Failed to load default fields: ' + error.message, 'error', 5000);
    } finally {
        btnLoadDefaults.disabled = false;
        btnLoadDefaults.innerHTML = '<i class="bi bi-magic me-1"></i> Load Default Fields';
    }
};
