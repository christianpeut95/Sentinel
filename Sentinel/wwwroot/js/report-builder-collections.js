// Report Builder - Collection Queries Module (Part 2)
// This is a continuation of report-builder.js

// Add these functions to the ReportBuilder object

ReportBuilder.addCollectionQuery = async function() {
    const queryId = this.nextCollectionQueryId++;
    const container = document.getElementById('collectionQueries');

    const placeholder = container.querySelector('.text-center');
    if (placeholder) {
        container.innerHTML = '';
    }

    const entityType = document.getElementById('entityTypeSelector').value;
    const collections = await this.getAvailableCollections(entityType);

    if (collections.length === 0) {
        ReportBuilderNotifications.showToast('No related collections available for this entity type', 'info');
        return;
    }

    this.addCollectionQueryCard(queryId, collections);
    this.collectionQueries.push({ id: queryId, subFilters: [], displayAsColumn: false });
    this.scheduleAutoSave();
};

ReportBuilder.addCollectionQueryCard = function(queryId, collections) {
    const container = document.getElementById('collectionQueries');

    if (!collections) {
        const entityType = document.getElementById('entityTypeSelector').value;
        collections = this.getAvailableCollections(entityType);
    }

    // Remove empty state if present
    const emptyState = container.querySelector('.rb-empty-state');
    if (emptyState) {
        container.innerHTML = '';
    }

    const queryHtml = `
        <div class="rb-collection-query" id="collection-query-${queryId}" data-query-id="${queryId}">
            <div class="rb-collection-header">
                <span class="rb-collection-title">Collection Query #${queryId}</span>
                <button type="button" class="rb-item-action js-remove-collection-query" title="Remove query">×</button>
            </div>
            <div class="rb-collection-body">
                <div class="rb-collection-row">
                    <label>Collection:</label>
                    <select class="rb-collection-select js-collection-select" id="collection-${queryId}">
                        <option value="">Select collection...</option>
                        ${collections.map(c => `<option value="${this.escapeHtml(c.value)}" data-type="${this.escapeHtml(c.entityType)}">${this.escapeHtml(c.label)}</option>`).join('')}
                    </select>
                </div>
                <div class="rb-collection-row">
                    <label>Operation:</label>
                    <select class="rb-collection-select js-collection-operation" id="operation-${queryId}">
                        <option value="">Select operation...</option>
                        <option value="HasAny">Has Any</option>
                        <option value="Count">Count</option>
                        <option value="Sum">Sum</option>
                        <option value="Average">Average</option>
                        <option value="Min">Minimum</option>
                        <option value="Max">Maximum</option>
                    </select>
                </div>
                <div class="rb-collection-row" id="aggregate-field-container-${queryId}" style="display:none;">
                    <label>Aggregate Field:</label>
                    <select class="rb-collection-select" id="aggregate-field-${queryId}">
                        <option value="">Select field...</option>
                    </select>
                </div>
                <div id="operator-container-${queryId}"></div>
                <div class="rb-collection-switch">
                    <input type="checkbox" class="js-collection-display-mode" id="display-as-column-${queryId}">
                    <label for="display-as-column-${queryId}">Display as column (instead of filter)</label>
                </div>
                <div id="column-name-container-${queryId}" style="display:none;" class="rb-collection-row">
                    <label>Column Name:</label>
                    <input type="text" class="rb-collection-input" id="column-name-${queryId}" placeholder="e.g., Has Positive PCR">
                </div>
                <div class="rb-subfilters-section">
                    <div class="rb-subfilters-header">
                        <span>Sub-Filters</span>
                        <button type="button" class="rb-add-btn-sm js-add-collection-subfilter">+ Add</button>
                    </div>
                    <div class="rb-subfilters-container" id="subfilters-${queryId}">
                        <div class="rb-empty-state-sm">No sub-filters</div>
                    </div>
                </div>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', queryHtml);

    const queryCard = document.getElementById(`collection-query-${queryId}`);
    queryCard.querySelector('.js-remove-collection-query').addEventListener('click', () => this.removeCollectionQuery(queryId));
    queryCard.querySelector('.js-collection-select').addEventListener('change', () => this.updateCollectionFields(queryId));
    queryCard.querySelector('.js-collection-operation').addEventListener('change', () => this.updateCollectionOperator(queryId));
    queryCard.querySelector('.js-collection-display-mode').addEventListener('change', () => this.toggleDisplayMode(queryId));
    queryCard.querySelector('.js-add-collection-subfilter').addEventListener('click', () => this.addCollectionSubFilter(queryId));

    this.updateStatusBar();
};

/**
 * Get available collections for an entity type, including nested sub-collections.
 * Fetches collection metadata from API and builds hierarchical options.
 * @param {string} entityType - The entity type (e.g., 'Case', 'Contact')
 * @returns {Promise<Array>} Array of collection options with value, label, entityType
 */
ReportBuilder.getAvailableCollections = async function(entityType) {
    try {
        const response = await fetch(`/api/reports/collection-metadata/${entityType}`);
        if (!response.ok) {
            console.error('Collection metadata request failed.');
            return this.getFallbackCollections(entityType);
        }

        const data = await response.json();

        if (!data.success || !data.collections) {
            console.error('[getAvailableCollections] Invalid API response.');
            return this.getFallbackCollections(entityType);
        }

        const collections = [];

        // Iterate through top-level collections
        for (const [collectionName, metadata] of Object.entries(data.collections)) {
            // Add parent collection
            collections.push({
                value: collectionName,
                label: this.formatCollectionLabel(collectionName),
                entityType: metadata.entityType || metadata.EntityType || collectionName
            });

            // Add nested sub-collections with arrow notation
            const subCollections = metadata.subCollections || metadata.SubCollections;
            if (subCollections) {
                for (const [subCollectionName, subMetadata] of Object.entries(subCollections)) {
                    collections.push({
                        value: `${collectionName}.${subCollectionName}`,
                        label: `${this.formatCollectionLabel(collectionName)} → ${this.formatCollectionLabel(subCollectionName)}`,
                        entityType: subMetadata.entityType || subMetadata.EntityType || subCollectionName,
                        parentCollection: collectionName,
                        subCollection: subCollectionName
                    });
                }
            }
        }

        return collections;

    } catch (error) {
        console.error('Collection metadata could not be loaded.');
        return this.getFallbackCollections(entityType);
    }
};

/**
 * Fallback hardcoded collections if API fetch fails (backward compatibility)
 */
ReportBuilder.getFallbackCollections = function(entityType) {
    console.warn('Using fallback collection metadata.');
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

/**
 * Format collection name into readable label
 */
ReportBuilder.formatCollectionLabel = function(collectionName) {
    // Handle special cases
    const labels = {
        'LabResults': 'Lab Results',
        'ExposureEvents': 'Exposures',
        'OutbreakCases': 'Outbreak Cases',
        'CaseSymptomTracking': 'Symptoms'
    };

    if (labels[collectionName]) {
        return labels[collectionName];
    }

    // Default: Add spaces before capital letters
    return collectionName.replace(/([A-Z])/g, ' $1').trim();
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
    const collectionPath = selectElement.value;

    if (!collectionPath) return;

    const entityType = document.getElementById('entityTypeSelector').value;

    // Parse nested collection path (e.g., "LabResults.Markers")
    const pathParts = collectionPath.split('.');
    const parentCollectionName = pathParts[0];
    const subCollectionName = pathParts.length > 1 ? pathParts[1] : null;

    try {
        const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
        if (aggregateFieldSelect) {
            aggregateFieldSelect.disabled = true;
            aggregateFieldSelect.innerHTML = '<option value="">Loading...</option>';
        }

        const response = await fetch(`/api/reports/collection-metadata/${entityType}`);
        if (!response.ok) throw new Error('Failed to fetch collection metadata');

        const data = await response.json();

        if (data.success && data.collections) {
            const query = this.collectionQueries.find(q => q.id === queryId);
            if (query) {
                // Store parent collection name and sub-collection name
                query.collectionName = parentCollectionName;
                query.subCollectionName = subCollectionName;

                // Get parent metadata
                const parentMetadata = data.collections[parentCollectionName];

                // If nested, navigate to sub-collection metadata
                const subCollections = parentMetadata?.subCollections || parentMetadata?.SubCollections;
                if (subCollectionName && subCollections) {
                    query.collectionMetadata = subCollections[subCollectionName];
                } else {
                    query.collectionMetadata = parentMetadata;
                }
            }
        }

        if (aggregateFieldSelect) {
            aggregateFieldSelect.disabled = false;
        }

        // The collection-metadata endpoint is authoritative for collection fields.
        // The grouped field endpoint intentionally omits collection properties, so
        // looking there meant valid collections had no fields available to filter.
        const query = this.collectionQueries.find(q => q.id === queryId);
        const metadata = query?.collectionMetadata;
        if (query && metadata) {
            const filterableFields = metadata.filterableFields || metadata.FilterableFields || [];

            query.collectionSubFields = filterableFields
                .map(field => field.name || field.Name)
                .filter(Boolean);
            query.collectionSubFieldsMetadata = filterableFields
                .map(field => {
                    const fieldPath = field.name || field.Name;
                    if (!fieldPath) return null;

                    return {
                        fieldPath,
                        name: field.label || field.Label || fieldPath,
                        dataType: field.dataType || field.DataType || 'String'
                    };
                })
                .filter(Boolean);
            query.collectionEntityType = query.subCollectionName || query.collectionName;

        } else if (query) {
            console.error('Collection metadata was not returned.');
        }

        // Clear existing sub-filters since collection type changed
        const subfiltersContainer = document.getElementById(`subfilters-${queryId}`);
        if (subfiltersContainer) {
            subfiltersContainer.innerHTML = '<div class="rb-empty-state-sm">No sub-filters</div>';
            const query = this.collectionQueries.find(q => q.id === queryId);
            if (query) {
                query.subFilters = [];
            }
        }

        await this.updateCollectionOperator(queryId);

    } catch (error) {
        console.error('Collection metadata could not be loaded.');
        ReportBuilderNotifications.showToast('Failed to load collection metadata. Please try again.', 'error', 5000);
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

    // Use collectionMetadata which already points to the correct level
    // (sub-collection metadata if nested, parent metadata otherwise)
    const metadata = query?.collectionMetadata;

    aggregateFieldSelect.innerHTML = '<option value="">Select field...</option>';

    const aggregatableFields = metadata?.aggregatableFields || metadata?.AggregatableFields;
    if (!aggregatableFields) {
        aggregateFieldContainer.style.display = 'none';
        return;
    }

    if (['Min', 'Max', 'Sum', 'Average'].includes(operation)) {
        let hasOptions = false;

        for (const [fieldName, fieldInfo] of Object.entries(aggregatableFields)) {
            const allowedOperations = fieldInfo.allowedOperations || fieldInfo.AllowedOperations || [];
            if (allowedOperations.includes(operation)) {
                const option = document.createElement('option');
                option.value = fieldName;
                option.textContent = fieldInfo.label || fieldInfo.Label || fieldName;
                option.dataset.type = fieldInfo.dataType || fieldInfo.DataType || 'String';
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
        ReportBuilderNotifications.showToast('Please select a collection first', 'warning');
        return;
    }

    const query = this.collectionQueries.find(q => q.id === queryId);

    if (!query || !query.collectionSubFields || query.collectionSubFields.length === 0) {
        ReportBuilderNotifications.showToast('No fields available for this collection type. Please select a collection first.', 'info');
        return;
    }

    // Use fields from the correct metadata level (sub-collection if nested, parent otherwise)
    // These were populated by updateCollectionFields() from collectionMetadata
    const fields = query.collectionSubFields;
    const fieldsMetadata = query.collectionSubFieldsMetadata || [];

    const placeholder = container.querySelector('.rb-empty-state-sm');
    if (placeholder) {
        container.innerHTML = '';
    }

    const subFilterId = Date.now() + Math.random();

    const fieldOptions = fields.map(fieldName => {
        const metadata = fieldsMetadata.find(m => m.fieldPath === fieldName || m.name === fieldName);
        const dataType = metadata?.dataType || 'String';
        const displayName = this.formatFieldName(fieldName);

        return `<option value="${this.escapeHtml(fieldName)}" data-type="${this.escapeHtml(dataType)}">${this.escapeHtml(displayName)}</option>`;
    }).join('');

    const subFilterHtml = `
        <div class="rb-subfilter-row" id="subfilter-${queryId}-${subFilterId}">
            <select class="rb-subfilter-field" data-subfilter-id="${subFilterId}" data-query-id="${queryId}">
                <option value="">Select field...</option>
                ${fieldOptions}
            </select>
            <select class="rb-subfilter-operator" id="subfilter-operator-${queryId}-${subFilterId}">
                <option value="Equals">=</option>
            </select>
            <div class="rb-subfilter-value-container" id="subfilter-value-container-${queryId}-${subFilterId}">
                <input type="text" class="rb-subfilter-value" id="subfilter-value-${queryId}-${subFilterId}" placeholder="Value">
            </div>
            <button type="button" class="rb-item-action js-remove-collection-subfilter" title="Remove sub-filter">×</button>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', subFilterHtml);
    document.getElementById(`subfilter-${queryId}-${subFilterId}`)
        .querySelector('.js-remove-collection-subfilter')
        .addEventListener('click', () => this.removeCollectionSubFilter(queryId, subFilterId));

    if (query) {
        if (!query.subFilters) query.subFilters = [];
        query.subFilters.push(subFilterId);
    }

    this.setupSubFilterSmartInput(queryId, subFilterId);

    // Initialize operators for the default String type
    const operatorSelect = document.getElementById(`subfilter-operator-${queryId}-${subFilterId}`);
    if (operatorSelect) {
        this.updateOperators(operatorSelect, 'String', false);
    }
};

ReportBuilder.setupSubFilterSmartInput = function(queryId, subFilterId) {
    const fieldSelect = document.querySelector(`[data-subfilter-id="${subFilterId}"][data-query-id="${queryId}"]`);
    const operatorSelect = document.getElementById(`subfilter-operator-${queryId}-${subFilterId}`);
    const valueContainer = document.getElementById(`subfilter-value-container-${queryId}-${subFilterId}`);

    if (!fieldSelect || !operatorSelect || !valueContainer) {
        console.warn('Collection sub-filter elements were not found.');
        return;
    }

    fieldSelect.addEventListener('change', (e) => {
        const selectedOption = e.target.options[e.target.selectedIndex];
        const dataType = selectedOption.dataset.type || 'String';

        // Pass false to keep operator dropdown visible for sub-filters
        this.updateOperators(operatorSelect, dataType, false);

        valueContainer.innerHTML = '<input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">';
        const tempInput = valueContainer.querySelector('input');

        this.updateValueInput(tempInput, dataType, operatorSelect.value);
    });

    operatorSelect.addEventListener('change', (e) => {
        const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
        const dataType = selectedOption.dataset.type || 'String';

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
            <div class="rb-empty-state">
                <div class="rb-empty-state-text">No collection queries</div>
            </div>
        `;
    }
    this.updateStatusBar();
};

ReportBuilder.removeCollectionSubFilter = function(queryId, subFilterId) {
    document.getElementById(`subfilter-${queryId}-${subFilterId}`)?.remove();

    const query = this.collectionQueries.find(q => q.id === queryId);
    if (query) {
        query.subFilters = query.subFilters.filter(f => f !== subFilterId);
    }

    const container = document.getElementById(`subfilters-${queryId}`);
    if (container.children.length === 0) {
        container.innerHTML = '<div class="rb-empty-state-sm">No sub-filters</div>';
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
    try {
        // Normalize property names (handle both PascalCase from C# and camelCase from JS)
        const collectionName = query.collectionName || query.CollectionName;
        const subCollectionName = query.subCollectionName || query.SubCollectionName;
        const operation = query.operation || query.Operation;
        const displayAsColumn = query.displayAsColumn ?? query.DisplayAsColumn ?? false;
        const columnName = query.columnName || query.ColumnName;
        const aggregateField = query.aggregateField || query.AggregateField;
        const comparator = query.comparator || query.Comparator;
        const value = query.value ?? query.Value;
        const subFilters = query.subFilters || query.SubFilters || [];

        // Reconstruct full collection path for dropdown
        const collectionPath = subCollectionName 
            ? `${collectionName}.${subCollectionName}` 
            : collectionName;

        // Verify container exists
        const container = document.getElementById('collectionQueries');
        if (!container) {
            console.error('[restoreCollectionQuery] Container not found');
            return;
        }

        // Add the collection query card
        const queryId = this.nextCollectionQueryId++;
        const entityType = document.getElementById('entityTypeSelector').value;
        const collections = await this.getAvailableCollections(entityType);

        this.addCollectionQueryCard(queryId, collections);

        // Store query object
        this.collectionQueries.push({
            id: queryId,
            subFilters: [],
            displayAsColumn: displayAsColumn,
            collectionName: collectionName,
            subCollectionName: subCollectionName  // Store sub-collection for nested queries
        });

        // Wait for DOM to be ready
        await new Promise(resolve => setTimeout(resolve, 200));

        // Verify the card was created
        const queryCard = document.getElementById(`collection-query-${queryId}`);
        if (!queryCard) {
            console.error('Collection query card could not be created.');
            return;
        }

        // Set collection path (use full path for nested collections)
        const collectionSelect = document.getElementById(`collection-${queryId}`);
        if (collectionSelect && collectionPath) {
            collectionSelect.value = collectionPath;

            // Trigger change to load collection metadata
            try {
                await this.updateCollectionFields(queryId);
                await new Promise(resolve => setTimeout(resolve, 300));
            } catch (error) {
                console.error('Collection metadata could not be restored.');
                // Continue anyway - the collection might still work
            }
        }

        // Set operation
        const operationSelect = document.getElementById(`operation-${queryId}`);
        if (operationSelect && operation) {
            operationSelect.value = operation;
            operationSelect.dispatchEvent(new Event('change'));
            await new Promise(resolve => setTimeout(resolve, 100));
        }

        // Set display as column mode
        const displayAsColumnCheckbox = document.getElementById(`display-as-column-${queryId}`);
        if (displayAsColumnCheckbox && displayAsColumn) {
            displayAsColumnCheckbox.checked = true;
            this.toggleDisplayMode(queryId);
            await new Promise(resolve => setTimeout(resolve, 100));

            // Set column name if provided
            if (columnName) {
                const columnNameInput = document.getElementById(`column-name-${queryId}`);
                if (columnNameInput) {
                    columnNameInput.value = columnName;
                }
            }
        }

        // Set aggregate field if applicable
        if (aggregateField && ['Min', 'Max', 'Sum', 'Average'].includes(operation)) {
            await new Promise(resolve => setTimeout(resolve, 200));
            const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
            if (aggregateFieldSelect) {
                aggregateFieldSelect.value = aggregateField;
            }
        }

        // Set comparator and value for filter mode
        if (!displayAsColumn && comparator) {
            const comparatorSelect = document.getElementById(`comparator-${queryId}`);
            if (comparatorSelect) {
                comparatorSelect.value = comparator;
            }

            if (value !== undefined && value !== null) {
                const valueInput = document.getElementById(`value-${queryId}`);
                if (valueInput) {
                    valueInput.value = value;
                }
            }
        }

        // Restore sub-filters
        if (subFilters && subFilters.length > 0) {
            for (const subFilter of subFilters) {
                try {
                    await this.restoreCollectionSubFilter(queryId, subFilter);
                    await new Promise(resolve => setTimeout(resolve, 200));
                } catch (error) {
                    console.error('Collection sub-filter could not be restored.');
                }
            }
        }

    } catch (error) {
        console.error('Collection query could not be restored.');
        throw error;
    }
};

/**
 * Restore a collection sub-filter
 * @param {number} queryId - The collection query ID
 * @param {Object} subFilter - The sub-filter object
 */
ReportBuilder.restoreCollectionSubFilter = async function(queryId, subFilter) {
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
        console.error('Collection sub-filter is missing required settings.');
        return;
    }

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
        console.error('Collection sub-filter elements were not found.');
        return;
    }

    // Set field value
    fieldSelect.value = field;
    // Wait a bit before getting the data type
    await new Promise(resolve => setTimeout(resolve, 50));

    // Get data type from the selected option
    const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
    const actualDataType = selectedOption?.dataset.type || dataType;

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
        const combinedSelect = valueContainer.querySelector('.filter-date-combined');
        if (combinedSelect) {
            let matchedPreset = false;

            // First check for InLast/InNext using offset (new format)
            if (isInLastOrNext && dynamicDateOffset) {
                const presetValue = `${operator}|${dynamicDateOffset}`;
                const presetOption = Array.from(combinedSelect.options).find(opt => opt.value === presetValue);

                if (presetOption) {
                    combinedSelect.value = presetValue;
                    matchedPreset = true;
                }
            }

            // Fallback: check for InLast/InNext with numeric value (old format - for backward compatibility)
            if (!matchedPreset && isInLastOrNext && value && !isNaN(parseInt(value))) {
                const presetValue = `${operator}|${value}`;
                const presetOption = Array.from(combinedSelect.options).find(opt => opt.value === presetValue);

                if (presetOption) {
                    combinedSelect.value = presetValue;
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
                        matchedPreset = true;
                    }

                    // Also try InNext|{days} format if operator is InNext
                    if (!matchedPreset && operator === 'InNext') {
                        const inNextValue = `InNext|${dynamicDateOffset}`;
                        const inNextOption = Array.from(combinedSelect.options).find(opt => opt.value === inNextValue);

                        if (inNextOption) {
                            combinedSelect.value = inNextValue;
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
            }
        } else {
            // For non-date fields, just set the value
            const actualValueInput = valueContainer.querySelector('.subfilter-value');
            if (actualValueInput) {
                actualValueInput.value = value;
            } else {
                console.warn('[restoreCollectionSubFilter] Value input not found after update');
            }
        }
    }

};
