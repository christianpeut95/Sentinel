// Report Builder - Collection Queries Module (Part 2)
// This is a continuation of report-builder.js

console.log('[report-builder-collections.js] Loading...');

// Add these functions to the ReportBuilder object

ReportBuilder.addCollectionQuery = function() {
    console.log('[addCollectionQuery] Called');
    const queryId = this.nextCollectionQueryId++;
    const container = document.getElementById('collectionQueries');
    
    const placeholder = container.querySelector('.text-center');
    if (placeholder) {
        container.innerHTML = '';
    }
    
    const entityType = document.getElementById('entityTypeSelector').value;
    const collections = this.getAvailableCollections(entityType);
    
    if (collections.length === 0) {
        alert('No related collections available for this entity type');
        return;
    }
    
    this.addCollectionQueryCard(queryId, collections);
    this.collectionQueries.push({ id: queryId, subFilters: [], displayAsColumn: false });
};

ReportBuilder.addCollectionQueryCard = function(queryId, collections) {
    const container = document.getElementById('collectionQueries');
    
    if (!collections) {
        const entityType = document.getElementById('entityTypeSelector').value;
        collections = this.getAvailableCollections(entityType);
    }
    
    const queryHtml = `
        <div class="list-group-item border-start border-info border-3" id="collection-query-${queryId}" data-query-id="${queryId}">
            <div class="row g-2">
                <div class="col-12 d-flex justify-content-between align-items-center mb-2">
                    <h6 class="mb-0 text-info"><i class="bi bi-diagram-3 me-2"></i>Related Data Query #${queryId}</h6>
                    <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeCollectionQuery(${queryId})">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
                
                <div class="col-12 mb-2">
                    <div class="form-check form-switch">
                        <input class="form-check-input" type="checkbox" id="display-as-column-${queryId}" onchange="ReportBuilder.toggleDisplayMode(${queryId})">
                        <label class="form-check-label fw-bold text-success" for="display-as-column-${queryId}">
                            <i class="bi bi-table me-1"></i> Display as Column (instead of filtering rows)
                        </label>
                    </div>
                    <small class="text-muted ms-4">When checked, adds a column showing the result for each row. When unchecked, filters rows based on the condition.</small>
                </div>
                
                <div class="col-12" id="column-name-container-${queryId}" style="display:none;">
                    <label class="form-label small fw-bold">Column Name:</label>
                    <input type="text" class="form-control form-control-sm" id="column-name-${queryId}" placeholder="e.g., 'Has Positive PCR' or 'Exposure Count'">
                </div>
                
                <div class="col-md-4">
                    <label class="form-label small fw-bold">Collection:</label>
                    <select class="form-select form-select-sm collection-name" id="collection-${queryId}" onchange="ReportBuilder.updateCollectionFields(${queryId})">
                        <option value="">Select collection...</option>
                        ${collections.map(c => `<option value="${c.value}" data-type="${c.entityType}">${c.label}</option>`).join('')}
                    </select>
                </div>
                
                <div class="col-md-4">
                    <label class="form-label small fw-bold">Operation:</label>
                    <select class="form-select form-select-sm collection-operation" id="operation-${queryId}" onchange="ReportBuilder.updateCollectionOperator(${queryId})">
                        <option value="">Select operation...</option>
                        <option value="HasAny">Has Any (at least one)</option>
                        <option value="HasAll">Has All (every one matches)</option>
                        <option value="Count">Count (number of items)</option>
                        <option value="Sum">Sum (total value)</option>
                        <option value="Average">Average</option>
                        <option value="Min">Minimum</option>
                        <option value="Max">Maximum</option>
                    </select>
                </div>
                
                <div class="col-md-4" id="aggregate-field-container-${queryId}" style="display:none;">
                    <label class="form-label small fw-bold">Aggregate Field:</label>
                    <select class="form-select form-select-sm" id="aggregate-field-${queryId}">
                        <option value="">Select field...</option>
                    </select>
                    <small class="text-muted">Field to aggregate (min/max/sum/average)</small>
                </div>
                
                <div class="col-md-4" id="operator-container-${queryId}">
                    <!-- Operator will appear here for Count/Sum/etc -->
                </div>
                
                <div class="col-12">
                    <div class="card card-body bg-light">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <label class="form-label small fw-bold mb-0">Sub-Filters (optional - filter which items to include):</label>
                            <button type="button" class="btn btn-sm btn-outline-primary" onclick="ReportBuilder.addCollectionSubFilter(${queryId})">
                                <i class="bi bi-plus-circle me-1"></i> Add Sub-Filter
                            </button>
                        </div>
                        <div id="subfilters-${queryId}" class="mt-2">
                            <small class="text-muted">No sub-filters added</small>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;
    
    container.insertAdjacentHTML('beforeend', queryHtml);
};

ReportBuilder.getAvailableCollections = function(entityType) {
    const collections = {
        'Case': [
            { value: 'ExposureEvents', label: 'Exposures', entityType: 'ExposureEvent' },
            { value: 'Tasks', label: 'Tasks', entityType: 'Task' },
            { value: 'LabResults', label: 'Lab Results', entityType: 'LabResult' },
            { value: 'Symptoms', label: 'Symptoms', entityType: 'CaseSymptomTracking' },
            { value: 'Contacts', label: 'Contacts', entityType: 'Contact' }
        ],
        'Contact': [
            { value: 'Tasks', label: 'Tasks', entityType: 'Task' },
            { value: 'LabResults', label: 'Lab Results', entityType: 'LabResult' }
        ],
        'Patient': [
            { value: 'Cases', label: 'Cases', entityType: 'Case' },
            { value: 'Contacts', label: 'Contacts (as Contact)', entityType: 'Contact' }
        ],
        'Outbreak': [
            { value: 'OutbreakCases', label: 'Outbreak Cases', entityType: 'OutbreakCase' },
            { value: 'Tasks', label: 'Tasks', entityType: 'Task' }
        ]
    };
    
    return collections[entityType] || [];
};

ReportBuilder.toggleDisplayMode = function(queryId) {
    const checkbox = document.getElementById(`display-as-column-${queryId}`);
    const columnNameContainer = document.getElementById(`column-name-container-${queryId}`);
    const operatorContainer = document.getElementById(`operator-container-${queryId}`);
    
    const query = this.collectionQueries.find(q => q.id === queryId);
    if (query) {
        query.displayAsColumn = checkbox.checked;
    }
    
    if (checkbox.checked) {
        columnNameContainer.style.display = 'block';
        operatorContainer.innerHTML = '';
        operatorContainer.style.display = 'none';
    } else {
        columnNameContainer.style.display = 'none';
        operatorContainer.style.display = 'block';
        this.updateCollectionOperator(queryId);
    }
};

ReportBuilder.updateCollectionFields = async function(queryId) {
    const selectElement = document.getElementById(`collection-${queryId}`);
    const collectionName = selectElement.value;

    if (!collectionName) return;

    const entityType = document.getElementById('entityTypeSelector').value;

    console.log('[updateCollectionFields] Fetching metadata for collection:', collectionName, 'entityType:', entityType);

    try {
        const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
        if (aggregateFieldSelect) {
            aggregateFieldSelect.disabled = true;
            aggregateFieldSelect.innerHTML = '<option value="">Loading...</option>';
        }

        const response = await fetch(`/api/reports/collection-metadata/${entityType}`);
        if (!response.ok) throw new Error('Failed to fetch collection metadata');

        const data = await response.json();

        console.log('[updateCollectionFields] Collection metadata API response:', data);

        if (data.success && data.collections) {
            const query = this.collectionQueries.find(q => q.id === queryId);
            if (query) {
                query.collectionMetadata = data.collections[collectionName];
                console.log('[updateCollectionFields] Stored collectionMetadata:', query.collectionMetadata);
            }
        }

        if (aggregateFieldSelect) {
            aggregateFieldSelect.disabled = false;
        }

        const fieldResponse = await fetch(`/api/reporting/fields/${entityType}/grouped`);
        console.log('[updateCollectionFields] Field response status:', fieldResponse.status);

        if (fieldResponse.ok) {
            // Check if response is actually JSON
            const contentType = fieldResponse.headers.get('content-type');
            console.log('[updateCollectionFields] Response content-type:', contentType);

            if (!contentType || !contentType.includes('application/json')) {
                const text = await fieldResponse.text();
                console.error('[updateCollectionFields] ❌ Expected JSON but got:', text.substring(0, 200));
                throw new Error('API returned non-JSON response');
            }

            const fieldsByCategory = await fieldResponse.json();
            console.log('[updateCollectionFields] Fields by category:', fieldsByCategory);

            let collectionMetadata = null;

            // Search through all categories for a field matching the collection name
            for (const category in fieldsByCategory) {
                console.log(`[updateCollectionFields] Searching category "${category}" for collection "${collectionName}"`);

                const field = fieldsByCategory[category].find(f => {
                    const matches = (f.fieldPath === collectionName || 
                                   f.fieldPath?.toLowerCase() === collectionName.toLowerCase() ||
                                   f.displayName === collectionName) && 
                                  f.isCollection;

                    if (matches) {
                        console.log('[updateCollectionFields] FOUND matching field:', f);
                    }

                    return matches;
                });

                if (field) {
                    collectionMetadata = field;
                    console.log('[updateCollectionFields] Collection metadata found in category:', category);
                    break;
                }
            }

            if (!collectionMetadata) {
                console.warn('[updateCollectionFields] ⚠️ No collection metadata found for:', collectionName);
                console.log('[updateCollectionFields] Available collections:', 
                    Object.values(fieldsByCategory).flat().filter(f => f.isCollection).map(f => f.fieldPath));
            }

            const query = this.collectionQueries.find(q => q.id === queryId);
            if (query && collectionMetadata) {
                query.collectionSubFieldsMetadata = collectionMetadata.collectionSubFieldsMetadata || [];
                query.collectionSubFields = collectionMetadata.collectionSubFields || [];
                query.collectionEntityType = collectionMetadata.collectionElementType;

                console.log('[updateCollectionFields] ✅ Stored sub-field metadata:', {
                    subFields: query.collectionSubFields.length,
                    subFieldsMetadata: query.collectionSubFieldsMetadata.length,
                    entityType: query.collectionEntityType
                });
            } else if (query) {
                console.error('[updateCollectionFields] ❌ No metadata found - fields will default to String type');
            }
        } else {
            console.error('[updateCollectionFields] Field API request failed:', fieldResponse.status);
        }

        await this.updateCollectionOperator(queryId);

    } catch (error) {
        console.error('[updateCollectionFields] ❌ Error:', error);
        alert('Failed to load collection metadata. Please try again.');
    }
};

ReportBuilder.updateCollectionOperator = function(queryId) {
    const operationSelect = document.getElementById(`operation-${queryId}`);
    const operation = operationSelect.value;
    const container = document.getElementById(`operator-container-${queryId}`);
    const displayAsColumnCheckbox = document.getElementById(`display-as-column-${queryId}`);
    
    const aggregateFieldContainer = document.getElementById(`aggregate-field-container-${queryId}`);
    if (!aggregateFieldContainer && ['Min', 'Max', 'Sum', 'Average'].includes(operation)) {
        setTimeout(() => this.updateCollectionOperator(queryId), 100);
        return;
    }
    
    this.updateAggregateFieldOptions(queryId);
    
    if (displayAsColumnCheckbox && displayAsColumnCheckbox.checked) {
        container.innerHTML = '';
        container.style.display = 'none';
        return;
    }
    
    if (['Count', 'Sum', 'Average', 'Min', 'Max'].includes(operation)) {
        container.style.display = 'block';
        container.innerHTML = `
            <label class="form-label small fw-bold">Compare:</label>
            <div class="input-group input-group-sm">
                <select class="form-select collection-comparator" id="comparator-${queryId}">
                    <option value="Equals">= (equals)</option>
                    <option value="NotEquals">≠ (not equals)</option>
                    <option value="GreaterThan">> (greater than)</option>
                    <option value="LessThan">< (less than)</option>
                    <option value="GreaterThanOrEqual">≥ (greater or equal)</option>
                    <option value="LessThanOrEqual">≤ (less or equal)</option>
                </select>
                <input type="number" class="form-control collection-value" id="value-${queryId}" placeholder="Value" step="any">
            </div>
        `;
    } else {
        container.innerHTML = '';
        container.style.display = 'none';
    }
};

ReportBuilder.updateAggregateFieldOptions = function(queryId) {
    const aggregateFieldContainer = document.getElementById(`aggregate-field-container-${queryId}`);
    const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
    const operationSelect = document.getElementById(`operation-${queryId}`);
    
    if (!aggregateFieldContainer || !aggregateFieldSelect) {
        return;
    }
    
    const operation = operationSelect?.value;
    const query = this.collectionQueries.find(q => q.id === queryId);
    const metadata = query?.collectionMetadata;
    
    aggregateFieldSelect.innerHTML = '<option value="">Select field...</option>';
    
    if (!metadata || !metadata.aggregatableFields) {
        aggregateFieldContainer.style.display = 'none';
        return;
    }
    
    if (['Min', 'Max', 'Sum', 'Average'].includes(operation)) {
        const aggregatableFields = metadata.aggregatableFields;
        let hasOptions = false;
        
        for (const [fieldName, fieldInfo] of Object.entries(aggregatableFields)) {
            if (fieldInfo.allowedOperations && fieldInfo.allowedOperations.includes(operation)) {
                const option = document.createElement('option');
                option.value = fieldName;
                option.textContent = fieldInfo.label;
                option.dataset.type = fieldInfo.dataType;
                aggregateFieldSelect.appendChild(option);
                hasOptions = true;
            }
        }
        
        if (hasOptions) {
            aggregateFieldContainer.style.display = 'block';
        } else {
            aggregateFieldContainer.style.display = 'none';
        }
    } else {
        aggregateFieldContainer.style.display = 'none';
    }
};

ReportBuilder.addCollectionSubFilter = function(queryId) {
    const container = document.getElementById(`subfilters-${queryId}`);
    const collectionSelect = document.getElementById(`collection-${queryId}`);

    if (!collectionSelect.value) {
        alert('Please select a collection first');
        return;
    }

    const query = this.collectionQueries.find(q => q.id === queryId);

    if (!query || !query.collectionSubFields || query.collectionSubFields.length === 0) {
        alert('No fields available for this collection type. Please select a collection first.');
        return;
    }

    const fields = query.collectionSubFields;
    const fieldsMetadata = query.collectionSubFieldsMetadata || [];

    console.log('[addCollectionSubFilter] Fields:', fields);
    console.log('[addCollectionSubFilter] Metadata:', fieldsMetadata);

    const placeholder = container.querySelector('small');
    if (placeholder) {
        container.innerHTML = '';
    }

    const subFilterId = Date.now() + Math.random();

    const fieldOptions = fields.map(fieldName => {
        const metadata = fieldsMetadata.find(m => m.fieldPath === fieldName || m.name === fieldName);
        const dataType = metadata?.dataType || 'String';
        const displayName = this.formatFieldName(fieldName);

        console.log(`[addCollectionSubFilter] Field: ${fieldName}, DataType: ${dataType}, Metadata:`, metadata);

        return `<option value="${fieldName}" data-type="${dataType}">${displayName}</option>`;
    }).join('');

    const subFilterHtml = `
        <div class="mb-2 p-2 border rounded bg-white" id="subfilter-${queryId}-${subFilterId}">
            <div class="row g-1">
                <div class="col-md-4">
                    <select class="form-select form-select-sm subfilter-field" data-subfilter-id="${subFilterId}" data-query-id="${queryId}">
                        <option value="">Select field...</option>
                        ${fieldOptions}
                    </select>
                </div>
                <div class="col-md-3">
                    <select class="form-select form-select-sm subfilter-operator" id="subfilter-operator-${queryId}-${subFilterId}">
                        <option value="Equals">Equals</option>
                    </select>
                </div>
                <div class="col-md-4" id="subfilter-value-container-${queryId}-${subFilterId}">
                    <input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">
                </div>
                <div class="col-md-1">
                    <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeCollectionSubFilter(${queryId}, ${subFilterId})">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', subFilterHtml);

    if (query) {
        if (!query.subFilters) query.subFilters = [];
        query.subFilters.push(subFilterId);
    }

    this.setupSubFilterSmartInput(queryId, subFilterId);
};

ReportBuilder.setupSubFilterSmartInput = function(queryId, subFilterId) {
    const fieldSelect = document.querySelector(`[data-subfilter-id="${subFilterId}"][data-query-id="${queryId}"]`);
    const operatorSelect = document.getElementById(`subfilter-operator-${queryId}-${subFilterId}`);
    const valueContainer = document.getElementById(`subfilter-value-container-${queryId}-${subFilterId}`);

    if (!fieldSelect || !operatorSelect || !valueContainer) {
        console.warn(`Sub-filter elements not found for query ${queryId}, subfilter ${subFilterId}`);
        return;
    }

    console.log('[setupSubFilterSmartInput] Setting up smart input for query', queryId, 'subfilter', subFilterId);

    fieldSelect.addEventListener('change', (e) => {
        const selectedOption = e.target.options[e.target.selectedIndex];
        const dataType = selectedOption.dataset.type || 'String';

        console.log('[SubFilter] Field changed to', e.target.value, 'DataType:', dataType, 'Option dataset:', selectedOption.dataset);

        this.updateOperators(operatorSelect, dataType);

        valueContainer.innerHTML = '<input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">';
        const tempInput = valueContainer.querySelector('input');

        this.updateValueInput(tempInput, dataType, operatorSelect.value);
    });

    operatorSelect.addEventListener('change', (e) => {
        const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
        const dataType = selectedOption.dataset.type || 'String';

        console.log('[SubFilter] Operator changed to', e.target.value, 'DataType:', dataType);

        valueContainer.innerHTML = '<input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">';
        const tempInput = valueContainer.querySelector('input');

        this.updateValueInput(tempInput, dataType, e.target.value);
    });
};

ReportBuilder.removeCollectionQuery = function(queryId) {
    document.getElementById(`collection-query-${queryId}`)?.remove();
    this.collectionQueries = this.collectionQueries.filter(q => q.id !== queryId);
    
    const container = document.getElementById('collectionQueries');
    if (container.children.length === 0) {
        container.innerHTML = `
            <div class="text-center text-muted py-4">
                <i class="bi bi-diagram-3 fs-1"></i>
                <p class="mt-2">No collection queries added yet</p>
                <small class="text-muted">Use collection queries to filter on related data like exposures, tasks, or lab results</small>
            </div>
        `;
    }
};

ReportBuilder.removeCollectionSubFilter = function(queryId, subFilterId) {
    document.getElementById(`subfilter-${queryId}-${subFilterId}`)?.remove();
    
    const query = this.collectionQueries.find(q => q.id === queryId);
    if (query) {
        query.subFilters = query.subFilters.filter(f => f !== subFilterId);
    }
    
    const container = document.getElementById(`subfilters-${queryId}`);
    if (container.children.length === 0) {
        container.innerHTML = '<small class="text-muted">No sub-filters added</small>';
    }
};

ReportBuilder.extractDateFilterValue = function(filterElement) {
    const combinedSelect = filterElement.querySelector('.filter-date-combined');
    if (!combinedSelect) {
        return filterElement.querySelector('.subfilter-value')?.value || '';
    }

    const combinedValue = combinedSelect.value;

    if (combinedValue === 'static') {
        return filterElement.querySelector('.filter-value')?.value || '';
    } else if (combinedValue === 'custom') {
        const customCondition = filterElement.querySelector('.filter-custom-condition');
        const dateTypeRadio = customCondition?.querySelector('input[name^="custom-date-type-"]:checked');
        const dateType = dateTypeRadio?.value || 'dynamic';

        if (dateType === 'static') {
            return customCondition?.querySelector('.filter-custom-static-value')?.value || '';
        } else {
            return customCondition?.querySelector('.filter-dynamic-offset-value')?.value || '';
        }
    } else if (combinedValue && combinedValue.includes('|')) {
        return combinedValue.split('|')[1];
    }

    return '';
};

/**
 * Restore a saved collection query
 * @param {Object} query - The saved collection query object
 */
ReportBuilder.restoreCollectionQuery = async function(query) {
    console.log('[restoreCollectionQuery] Restoring query:', query);

    try {
        // Normalize property names (handle both PascalCase from C# and camelCase from JS)
        const collectionName = query.collectionName || query.CollectionName;
        const operation = query.operation || query.Operation;
        const displayAsColumn = query.displayAsColumn ?? query.DisplayAsColumn ?? false;
        const columnName = query.columnName || query.ColumnName;
        const aggregateField = query.aggregateField || query.AggregateField;
        const comparator = query.comparator || query.Comparator;
        const value = query.value ?? query.Value;
        const subFilters = query.subFilters || query.SubFilters || [];

        console.log('[restoreCollectionQuery] Normalized values:', {
            collectionName, operation, displayAsColumn, columnName, aggregateField, comparator, value,
            subFiltersCount: subFilters.length
        });

        // Verify container exists
        const container = document.getElementById('collectionQueries');
        if (!container) {
            console.error('[restoreCollectionQuery] Container not found');
            return;
        }

        // Add the collection query card
        const queryId = this.nextCollectionQueryId++;
        const entityType = document.getElementById('entityTypeSelector').value;
        const collections = this.getAvailableCollections(entityType);

        console.log('[restoreCollectionQuery] Creating card with queryId:', queryId);
        this.addCollectionQueryCard(queryId, collections);

        // Store query object
        this.collectionQueries.push({
            id: queryId,
            subFilters: [],
            displayAsColumn: displayAsColumn,
            collectionName: collectionName
        });

        // Wait for DOM to be ready
        await new Promise(resolve => setTimeout(resolve, 200));

        // Verify the card was created
        const queryCard = document.getElementById(`collection-query-${queryId}`);
        if (!queryCard) {
            console.error('[restoreCollectionQuery] Query card not created for queryId:', queryId);
            return;
        }

        // Set collection name
        const collectionSelect = document.getElementById(`collection-${queryId}`);
        if (collectionSelect && collectionName) {
            console.log('[restoreCollectionQuery] Setting collection to:', collectionName);
            collectionSelect.value = collectionName;

            // Trigger change to load collection metadata
            try {
                await this.updateCollectionFields(queryId);
                await new Promise(resolve => setTimeout(resolve, 300));
            } catch (error) {
                console.error('[restoreCollectionQuery] updateCollectionFields failed:', error);
                // Continue anyway - the collection might still work
            }
        }

        // Set operation
        const operationSelect = document.getElementById(`operation-${queryId}`);
        if (operationSelect && operation) {
            console.log('[restoreCollectionQuery] Setting operation to:', operation);
            operationSelect.value = operation;
            operationSelect.dispatchEvent(new Event('change'));
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        // Set display as column mode
        const displayAsColumnCheckbox = document.getElementById(`display-as-column-${queryId}`);
        if (displayAsColumnCheckbox && displayAsColumn) {
            console.log('[restoreCollectionQuery] Enabling display as column');
            displayAsColumnCheckbox.checked = true;
            this.toggleDisplayMode(queryId);
            await new Promise(resolve => setTimeout(resolve, 100));

            // Set column name if provided
            if (columnName) {
                const columnNameInput = document.getElementById(`column-name-${queryId}`);
                if (columnNameInput) {
                    columnNameInput.value = columnName;
                    console.log('[restoreCollectionQuery] Set column name to:', columnName);
                }
            }
        }

        // Set aggregate field if applicable
        if (aggregateField && ['Min', 'Max', 'Sum', 'Average'].includes(operation)) {
            await new Promise(resolve => setTimeout(resolve, 200));
            const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
            if (aggregateFieldSelect) {
                aggregateFieldSelect.value = aggregateField;
                console.log('[restoreCollectionQuery] Set aggregate field to:', aggregateField);
            }
        }

        // Set comparator and value for filter mode
        if (!displayAsColumn && comparator) {
            const comparatorSelect = document.getElementById(`comparator-${queryId}`);
            if (comparatorSelect) {
                comparatorSelect.value = comparator;
                console.log('[restoreCollectionQuery] Set comparator to:', comparator);
            }

            if (value !== undefined && value !== null) {
                const valueInput = document.getElementById(`value-${queryId}`);
                if (valueInput) {
                    valueInput.value = value;
                    console.log('[restoreCollectionQuery] Set value to:', value);
                }
            }
        }

        // Restore sub-filters
        if (subFilters && subFilters.length > 0) {
            console.log('[restoreCollectionQuery] Restoring', subFilters.length, 'sub-filters');
            for (const subFilter of subFilters) {
                try {
                    await this.restoreCollectionSubFilter(queryId, subFilter);
                    await new Promise(resolve => setTimeout(resolve, 200));
                } catch (error) {
                    console.error('[restoreCollectionQuery] Failed to restore sub-filter:', error, subFilter);
                }
            }
        }

        console.log('[restoreCollectionQuery] ✅ Query restored successfully');
    } catch (error) {
        console.error('[restoreCollectionQuery] ❌ Failed to restore query:', error, query);
        throw error;
    }
};

/**
 * Restore a collection sub-filter
 * @param {number} queryId - The collection query ID
 * @param {Object} subFilter - The sub-filter object
 */
ReportBuilder.restoreCollectionSubFilter = async function(queryId, subFilter) {
    console.log('[restoreCollectionSubFilter] Restoring sub-filter:', subFilter);

    // Normalize property names (handle both PascalCase from C# and camelCase from JS)
    const field = subFilter.field || subFilter.Field;
    const operator = subFilter.operator || subFilter.Operator;
    const value = subFilter.value ?? subFilter.Value;
    const dataType = subFilter.dataType || subFilter.DataType || 'String';
    const isDynamicDate = subFilter.isDynamicDate || subFilter.IsDynamicDate || false;
    const dynamicDateType = subFilter.dynamicDateType || subFilter.DynamicDateType;
    const dynamicDateOffset = subFilter.dynamicDateOffset || subFilter.DynamicDateOffset;
    const dynamicDateOffsetUnit = subFilter.dynamicDateOffsetUnit || subFilter.DynamicDateOffsetUnit;

    if (!field || !operator) {
        console.error('[restoreCollectionSubFilter] Missing required fields:', { field, operator });
        return;
    }

    console.log('[restoreCollectionSubFilter] Normalized:', { 
        field, operator, value, dataType, isDynamicDate, dynamicDateType, dynamicDateOffset, dynamicDateOffsetUnit 
    });

    // Add sub-filter
    this.addCollectionSubFilter(queryId);

    // Wait for DOM - increased to ensure elements are ready
    await new Promise(resolve => setTimeout(resolve, 250));

    // Find the last added sub-filter
    const subFilterElements = document.querySelectorAll(`[id^="subfilter-${queryId}-"]`);
    const subFilterEl = subFilterElements[subFilterElements.length - 1];

    if (!subFilterEl) {
        console.error('[restoreCollectionSubFilter] Could not find sub-filter element');
        return;
    }

    // Extract subFilterId from element ID (format: "subfilter-{queryId}-{subFilterId}")
    const subFilterElId = subFilterEl.id;
    const subFilterId = subFilterElId.replace(`subfilter-${queryId}-`, '');

    const fieldSelect = subFilterEl.querySelector('.subfilter-field');
    const operatorSelect = document.getElementById(`subfilter-operator-${queryId}-${subFilterId}`);
    const valueContainer = document.getElementById(`subfilter-value-container-${queryId}-${subFilterId}`);

    if (!fieldSelect || !operatorSelect || !valueContainer) {
        console.error('[restoreCollectionSubFilter] Missing sub-filter elements. Found:', {
            fieldSelect: !!fieldSelect,
            operatorSelect: !!operatorSelect,
            valueContainer: !!valueContainer,
            subFilterEl: subFilterElId,
            subFilterId: subFilterId
        });
        return;
    }

    // Set field value
    fieldSelect.value = field;
    console.log('[restoreCollectionSubFilter] Set field to:', field);

    // Wait a bit before getting the data type
    await new Promise(resolve => setTimeout(resolve, 50));

    // Get data type from the selected option
    const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
    const actualDataType = selectedOption?.dataset.type || dataType;

    console.log('[restoreCollectionSubFilter] Field data type:', actualDataType, 'from option:', selectedOption);

    // Update operators based on data type
    this.updateOperators(operatorSelect, actualDataType);
    await new Promise(resolve => setTimeout(resolve, 50));

    // For date fields, ensure operator column stays hidden (updateOperators should handle this, but enforce it)
    const isDateField = actualDataType && actualDataType.includes('Date');
    if (isDateField) {
        const operatorCol = operatorSelect.closest('.col-md-3');
        if (operatorCol) {
            operatorCol.style.display = 'none';
        }
    }

    // For non-date fields, set the operator value
    // For date fields, the operator will be set via the combined dropdown later
    if (!isDateField) {
        operatorSelect.value = operator;
        console.log('[restoreCollectionSubFilter] Set operator to:', operator);
    } else {
        console.log('[restoreCollectionSubFilter] Skipping operator set for date field - will use combined dropdown');
    }

    // Clear the value container and create appropriate input based on data type and operator
    valueContainer.innerHTML = '';

    // Create a temporary input element
    const tempInput = document.createElement('input');
    tempInput.type = 'text';
    tempInput.className = 'form-control form-control-sm subfilter-value';
    tempInput.placeholder = 'Value';
    valueContainer.appendChild(tempInput);

    // Update the value input to the correct type (date picker, number, etc.)
    // Pass subFilterId as uniqueId for date filter HTML generation
    this.updateValueInput(tempInput, actualDataType, operator, subFilterId);
    await new Promise(resolve => setTimeout(resolve, 150));

    // Handle dynamic date restoration
    // Check for InLast/InNext - now properly saved as dynamic dates with offset
    const isInLastOrNext = (operator === 'InLast' || operator === 'InNext') && 
                          (dynamicDateOffset || (value && !isNaN(parseInt(value))));

    if ((isDynamicDate && (dynamicDateType || dynamicDateOffset)) || isInLastOrNext) {
        console.log('[restoreCollectionSubFilter] Restoring dynamic date:', { 
            operator, value, isDynamicDate, dynamicDateType, dynamicDateOffset, dynamicDateOffsetUnit, isInLastOrNext 
        });

        const combinedSelect = valueContainer.querySelector('.filter-date-combined');
        if (combinedSelect) {
            let matchedPreset = false;

            // First check for InLast/InNext using offset (new format)
            if (isInLastOrNext && dynamicDateOffset) {
                const presetValue = `${operator}|${dynamicDateOffset}`;
                const presetOption = Array.from(combinedSelect.options).find(opt => opt.value === presetValue);

                if (presetOption) {
                    combinedSelect.value = presetValue;
                    console.log('[restoreCollectionSubFilter] Set to InLast/InNext preset (offset):', presetValue);
                    matchedPreset = true;
                }
            }

            // Fallback: check for InLast/InNext with numeric value (old format - for backward compatibility)
            if (!matchedPreset && isInLastOrNext && value && !isNaN(parseInt(value))) {
                const presetValue = `${operator}|${value}`;
                const presetOption = Array.from(combinedSelect.options).find(opt => opt.value === presetValue);

                if (presetOption) {
                    combinedSelect.value = presetValue;
                    console.log('[restoreCollectionSubFilter] Set to InLast/InNext preset (legacy):', presetValue);
                    matchedPreset = true;
                }
            }

            // Try to match against standard presets using offset
            if (!matchedPreset && dynamicDateOffset && dynamicDateOffsetUnit === 'Days') {
                const standardDays = [7, 30, 90, 180, 365];
                if (standardDays.includes(dynamicDateOffset)) {
                    // Try InLast|{days} format
                    const inLastValue = `InLast|${dynamicDateOffset}`;
                    const inLastOption = Array.from(combinedSelect.options).find(opt => opt.value === inLastValue);

                    if (inLastOption) {
                        combinedSelect.value = inLastValue;
                        console.log('[restoreCollectionSubFilter] Set to InLast preset:', inLastValue);
                        matchedPreset = true;
                    }

                    // Also try InNext|{days} format if operator is InNext
                    if (!matchedPreset && operator === 'InNext') {
                        const inNextValue = `InNext|${dynamicDateOffset}`;
                        const inNextOption = Array.from(combinedSelect.options).find(opt => opt.value === inNextValue);

                        if (inNextOption) {
                            combinedSelect.value = inNextValue;
                            console.log('[restoreCollectionSubFilter] Set to InNext preset:', inNextValue);
                            matchedPreset = true;
                        }
                    }
                }
            }

            // Try matching against dynamic date type presets (Past30Days, Next7Days, etc.)
            if (!matchedPreset && dynamicDateType) {
                const presetValue = `${operator}|${dynamicDateType}`;
                const presetOption = Array.from(combinedSelect.options).find(opt => opt.value === presetValue);

                if (presetOption) {
                    combinedSelect.value = presetValue;
                    console.log('[restoreCollectionSubFilter] Set to dynamic type preset:', presetValue);
                    matchedPreset = true;
                }
            }

            // If no preset matched, use custom condition
            if (!matchedPreset && dynamicDateOffset && dynamicDateOffsetUnit) {
                combinedSelect.value = 'custom';

                await new Promise(resolve => setTimeout(resolve, 100));

                const customCondition = subFilterEl.querySelector('.filter-custom-condition');
                if (customCondition) {
                    customCondition.style.display = 'block';

                    // Set custom operator
                    const customOperatorSelect = customCondition.querySelector('.filter-operator');
                    if (customOperatorSelect) {
                        customOperatorSelect.value = operator;
                    }

                    // Set to dynamic date type
                    const dynamicRadio = customCondition.querySelector('input[value="dynamic"]');
                    if (dynamicRadio) {
                        dynamicRadio.checked = true;

                        // Show dynamic fields
                        const dynamicFields = customCondition.querySelector('.filter-custom-dynamic-fields');
                        const staticField = customCondition.querySelector('.filter-custom-static-field');
                        if (dynamicFields) dynamicFields.style.display = 'block';
                        if (staticField) staticField.style.display = 'none';

                        // Set offset value
                        const offsetInput = customCondition.querySelector('.filter-dynamic-offset-value');
                        if (offsetInput) {
                            offsetInput.value = dynamicDateOffset;
                        }

                        // Set offset unit
                        const unitSelect = customCondition.querySelector('.filter-dynamic-offset-unit');
                        if (unitSelect) {
                            unitSelect.value = dynamicDateOffsetUnit;
                        }

                        // Set direction (Past or Next)
                        const directionSelect = customCondition.querySelector('.filter-dynamic-offset-direction');
                        if (directionSelect) {
                            const direction = dynamicDateType.toLowerCase().startsWith('past') ? 'past' : 'next';
                            directionSelect.value = direction;
                        }

                        console.log('[restoreCollectionSubFilter] Set custom dynamic date:', { 
                            offset: dynamicDateOffset, 
                            unit: dynamicDateOffsetUnit, 
                            direction: dynamicDateType 
                        });
                    }
                }
            }
        }
    } else if (value !== undefined && value !== null && value !== '') {
        // Set static value
        const combinedSelect = valueContainer.querySelector('.filter-date-combined');
        if (combinedSelect) {
            // For date fields with a static value, set to 'static' mode
            combinedSelect.value = 'static';

            await new Promise(resolve => setTimeout(resolve, 50));

            const actualValueInput = valueContainer.querySelector('.subfilter-value');
            if (actualValueInput) {
                actualValueInput.value = value;
                console.log('[restoreCollectionSubFilter] Set static date value to:', value);
            }
        } else {
            // For non-date fields, just set the value
            const actualValueInput = valueContainer.querySelector('.subfilter-value');
            if (actualValueInput) {
                actualValueInput.value = value;
                console.log('[restoreCollectionSubFilter] Set value to:', value);
            } else {
                console.warn('[restoreCollectionSubFilter] Value input not found after update');
            }
        }
    }

    console.log('[restoreCollectionSubFilter] ✅ Sub-filter restored with smart filtering and dynamic date support');
};
