// Report Builder - Data Collection & Actions Module (Part 3)
// This is a continuation of report-builder.js

console.log('[report-builder-actions.js] Loading...');

// Add these functions to the ReportBuilder object

ReportBuilder.getFilters = function() {
    const filterElements = document.querySelectorAll('#filters .list-group-item');
    const filters = [];
    
    filterElements.forEach((el, i) => {
        const fieldSelect = el.querySelector('.filter-field');
        const field = fieldSelect?.value;
        let operator = el.querySelector('.filter-operator')?.value;

        if (field && operator) {
            const filterId = el.dataset.filterId;
            const groupId = el.dataset.groupId ? parseInt(el.dataset.groupId) : null;

            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption?.dataset.type || 'String';
            const isCustomField = selectedOption?.dataset.iscustom === 'true';
            const customFieldId = selectedOption?.dataset.customid ? parseInt(selectedOption.dataset.customid) : null;

            let value = '';
            let isDynamicDate = false;
            let dynamicDateType = null;
            let dynamicDateOffset = null;
            let dynamicDateOffsetUnit = null;

            if (operator === 'Between') {
                const value1 = el.querySelector('.filter-value')?.value || '';
                const value2 = el.querySelector('.filter-value-end')?.value || '';
                value = `${value1}|${value2}`;
            } else if (operator === 'IsNull' || operator === 'IsNotNull' || operator === 'IsEmpty' || operator === 'IsNotEmpty') {
                value = '';
            } else {
                const combinedSelect = el.querySelector('.filter-date-combined');
                if (combinedSelect) {
                    const combinedValue = combinedSelect.value;

                    const customCondition = el.querySelector('.filter-custom-condition');
                    const isCustomConditionVisible = customCondition && customCondition.style.display !== 'none';

                    if (combinedValue === 'static') {
                        value = el.querySelector('.filter-value')?.value || '';
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

                        if (presetOperator && el.querySelector('.filter-operator')) {
                            el.querySelector('.filter-operator').value = presetOperator;
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
                        value = el.querySelector('.filter-value')?.value || '';
                        isDynamicDate = false;
                    }
                } else {
                    value = el.querySelector('.filter-value')?.value || '';
                }
            }

            const logicOperator = el.querySelector(`input[name="logic-${filterId}"]:checked`)?.value || 'AND';

            let groupLogicOperator = 'AND';
            if (groupId) {
                const groupElement = el.closest('.filter-group');
                groupLogicOperator = groupElement?.querySelector(`input[name="group-logic-${groupId}"]:checked`)?.value || 'AND';
            }

            filters.push({
                fieldPath: field,
                operator: operator,
                value: value,
                dataType: dataType,
                displayOrder: i,
                isCustomField: isCustomField,
                customFieldDefinitionId: customFieldId,
                logicOperator: logicOperator,
                groupId: groupId,
                groupLogicOperator: groupLogicOperator,
                isDynamicDate: isDynamicDate,
                dynamicDateType: dynamicDateType,
                dynamicDateOffset: dynamicDateOffset,
                dynamicDateOffsetUnit: dynamicDateOffsetUnit
            });
        }
    });

    return filters;
};

ReportBuilder.getCollectionQueries = function() {
    const queryElements = document.querySelectorAll('[id^="collection-query-"]');
    const queries = [];
    
    queryElements.forEach((el) => {
        const queryId = parseInt(el.dataset.queryId);
        const collectionName = document.getElementById(`collection-${queryId}`)?.value;
        const operation = document.getElementById(`operation-${queryId}`)?.value;
        const displayAsColumn = document.getElementById(`display-as-column-${queryId}`)?.checked || false;
        const columnName = document.getElementById(`column-name-${queryId}`)?.value || '';
        const aggregateField = document.getElementById(`aggregate-field-${queryId}`)?.value || null;
        
        if (!collectionName || !operation) return;
        
        const query = {
            collectionName: collectionName,
            operation: operation,
            aggregateField: aggregateField,
            displayAsColumn: displayAsColumn,
            columnName: columnName,
            subFilters: []
        };
        
        if (!displayAsColumn && ['Count', 'Sum', 'Average', 'Min', 'Max'].includes(operation)) {
            query.comparator = document.getElementById(`comparator-${queryId}`)?.value || 'GreaterThan';
            const valueInput = document.getElementById(`value-${queryId}`);
            query.value = valueInput ? parseFloat(valueInput.value) || 0 : 0;
        }
        
        const subFilterElements = el.querySelectorAll('[id^="subfilter-"]');
        subFilterElements.forEach((subEl) => {
            const fieldSelect = subEl.querySelector('.subfilter-field');
            const field = fieldSelect?.value;
            let operator = subEl.querySelector('.subfilter-operator')?.value;

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
                        value = subEl.querySelector('.subfilter-value')?.value || '';
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
                        value = subEl.querySelector('.subfilter-value')?.value || '';
                        isDynamicDate = false;
                    }
                } else {
                    value = subEl.querySelector('.subfilter-value')?.value || '';
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
    if (this.selectedFields.length === 0) {
        alert('Please select at least one field');
        return;
    }

    try {
        const filters = this.getFilters();

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

        if (!response.ok) {
            throw new Error(`Server error: ${response.status}`);
        }

        const result = await response.json();

        if (result.success) {
            this.renderPreview(result.data, filters);
        } else {
            alert('Error: ' + result.error);
        }
    } catch (error) {
        alert('Failed to preview report: ' + error.message);
    }
};

ReportBuilder.renderPreview = function(data, filters) {
    const container = document.getElementById('previewContainer');

    if (!data || data.length === 0) {
        container.innerHTML = `
            <div class="alert alert-info">
                <i class="bi bi-info-circle me-2"></i>
                <strong>No data found</strong> - Your filters returned 0 rows. Try adjusting your filter criteria.
            </div>
        `;
        return;
    }

    const filterSummary = filters && filters.length > 0 
        ? `<div class="mt-2 small text-muted">
               <strong>Filters applied:</strong> ${filters.length} filter(s) active
           </div>`
        : '<div class="mt-2 small text-muted">No filters applied - showing all records</div>';

    const banner = `
        <div class="alert alert-success mb-3">
            <i class="bi bi-check-circle me-2"></i>
            <strong>${data.length} rows</strong> returned
            ${filterSummary}
        </div>
    `;

    if (window.pivotInstance) {
        window.pivotInstance.dispose();
        window.pivotInstance = null;
    }

    container.innerHTML = banner + '<div id="wdr-pivot"></div>';

    const savedPivotConfig = this.savedPivotConfiguration;
    let reportConfig;

    if (savedPivotConfig && savedPivotConfig.length > 0) {
        try {
            reportConfig = JSON.parse(savedPivotConfig);
            reportConfig.dataSource = { data: data };
        } catch (e) {
            reportConfig = this.getDefaultPivotConfig(data);
        }
    } else {
        reportConfig = this.getDefaultPivotConfig(data);
    }

    window.pivotInstance = new WebDataRocks({
        container: "#wdr-pivot",
        toolbar: true,
        height: 600,
        report: reportConfig,
        customizeCell: function(cell, data) {
            if (data.type === "value" && typeof data.value === "string" && /^\d{4}-\d{2}-\d{2}T/.test(data.value)) {
                cell.text = data.value.split('T')[0];
            }
        }
    });
};

ReportBuilder.getDefaultPivotConfig = function(data) {
    const dataKeys = Object.keys(data[0] || {});

    const allMeasures = dataKeys.map(key => ({
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
    const name = document.getElementById('reportName').value;
    if (!name) {
        alert('Please enter a report name');
        return;
    }
    
    if (this.selectedFields.length === 0) {
        alert('Please select at least one field');
        return;
    }
    
    try {
        let pivotConfig = null;
        if (window.pivotInstance) {
            try {
                const report = window.pivotInstance.getReport();
                pivotConfig = JSON.stringify(report);
            } catch (e) {
                console.error('Failed to get pivot configuration', e);
            }
        }
        
        const response = await fetch('/api/reports/save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                reportId: this.reportId,
                name: name,
                description: document.getElementById('reportDescription').value,
                entityType: document.getElementById('entityTypeSelector').value,
                category: document.getElementById('reportCategory').value,
                isPublic: document.getElementById('reportPublic').checked,
                pivotConfiguration: pivotConfig,
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
            })
        });
        
        const result = await response.json();
        
        if (result.success) {
            alert('Report saved successfully!');
            window.location.href = '/Reports/Index';
        } else {
            alert('Error saving report: ' + result.error);
        }
    } catch (error) {
        alert('Failed to save report: ' + error.message);
    }
};

ReportBuilder.loadDefaultFields = async function() {
    const entityType = document.getElementById('entityTypeSelector').value;
    const btnLoadDefaults = document.getElementById('btnLoadDefaults');
    
    if (!entityType) {
        alert('Please select an entity type first');
        return;
    }
    
    if (this.selectedFields.length > 0) {
        if (!confirm('This will replace your current field selection. Continue?')) {
            return;
        }
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
            alert('Failed to load default fields: ' + (result.error || 'Unknown error'));
        }
    } catch (error) {
        alert('Failed to load default fields: ' + error.message);
    } finally {
        btnLoadDefaults.disabled = false;
        btnLoadDefaults.innerHTML = '<i class="bi bi-magic me-1"></i> Load Default Fields';
    }
};
