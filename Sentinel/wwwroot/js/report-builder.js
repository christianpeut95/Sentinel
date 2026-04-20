// Report Builder - Main Module
console.log('[report-builder.js] Loading...');

const ReportBuilder = {
    selectedFields: [],
    filters: [],
    filterGroups: [],
    collectionQueries: [],
    nextFilterId: 1,
    nextGroupId: 1,
    nextCollectionQueryId: 1,

    // Loading UI helpers
    showLoading(message = 'Loading...', subtext = '') {
        const overlay = document.getElementById('report-loading-overlay');
        const loadingText = overlay?.querySelector('.loading-text');
        const loadingSubtext = overlay?.querySelector('.loading-subtext');

        if (overlay) {
            overlay.classList.remove('hidden');
            if (loadingText) loadingText.textContent = message;
            if (loadingSubtext) loadingSubtext.textContent = subtext;
        }
    },

    hideLoading() {
        const overlay = document.getElementById('report-loading-overlay');
        if (overlay) {
            overlay.classList.add('hidden');
        }
    },

    updateLoadingProgress(message) {
        const progress = document.getElementById('loading-progress');
        if (progress) {
            progress.textContent = message;
        }
    },

    // Initialize with data passed from Razor page
    init(savedReport) {
        console.log('[ReportBuilder.init] Called with savedReport:', savedReport);

        if (savedReport) {
            this.showLoading('Loading Report', 'Restoring filters and collection queries...');
        }

        this.setupDragDrop();
        this.setupEventListeners();
        this.setupFieldSearch();

        if (savedReport) {
            this.loadSavedReport(savedReport);
        } else {
            // No saved report, hide loading immediately
            this.hideLoading();
        }

        console.log('[ReportBuilder.init] Initialization complete');
    },
    
    loadSavedReport(savedReport) {
        // Load saved fields
        if (savedReport.fields && savedReport.fields.length > 0) {
            this.updateLoadingProgress(`Loading ${savedReport.fields.length} selected fields...`);
            savedReport.fields.forEach(field => {
                this.selectedFields.push({
                    fieldPath: field.fieldPath,
                    displayName: field.displayName,
                    dataType: field.dataType,
                    isCustom: field.isCustomField,
                    customId: field.customFieldDefinitionId
                });
            });
            this.renderSelectedFields();
        }

        // Load saved filters
        if (savedReport.filters && savedReport.filters.length > 0) {
            this.updateLoadingProgress(`Restoring ${savedReport.filters.length} filters...`);
            savedReport.filters.forEach((filter, index) => {
                this.addFilter();
                setTimeout(() => {
                    this.restoreFilter(filter, index);
                }, (index + 1) * 100);
            });
        }

        // Load saved collection queries (async)
        if (savedReport.collectionQueries && savedReport.collectionQueries.length > 0) {
            const queryCount = savedReport.collectionQueries.length;
            this.updateLoadingProgress(`Restoring ${queryCount} collection ${queryCount === 1 ? 'query' : 'queries'}...`);

            // Use setTimeout to ensure DOM is ready and handle async restoration
            setTimeout(async () => {
                try {
                    let completed = 0;
                    for (const query of savedReport.collectionQueries) {
                        try {
                            completed++;
                            this.updateLoadingProgress(`Restoring collection query ${completed} of ${queryCount}...`);
                            await this.restoreCollectionQuery(query);
                            // Wait between queries to ensure proper DOM updates
                            await new Promise(resolve => setTimeout(resolve, 300));
                        } catch (error) {
                            console.error('[loadSavedReport] Failed to restore collection query:', error);
                        }
                    }

                    // All done!
                    this.updateLoadingProgress('✓ Report loaded successfully');
                    setTimeout(() => {
                        this.hideLoading();
                        console.log('[loadSavedReport] ✅ All restoration complete - loading overlay hidden');
                    }, 500);
                } catch (error) {
                    console.error('[loadSavedReport] Error during restoration:', error);
                    this.hideLoading();
                }
            }, 500);
        } else {
            // No collection queries, hide loading after a short delay
            setTimeout(() => {
                this.hideLoading();
                console.log('[loadSavedReport] ✅ Report loaded (no collection queries) - loading overlay hidden');
            }, 800);
        }
    },
    
    restoreFilter(filter, filterIndex) {
        const filterElements = document.querySelectorAll('#filters .list-group-item');
        const filterEl = filterElements[filterIndex];
        
        if (!filterEl) return;
        
        const fieldSelect = filterEl.querySelector('.filter-field');
        if (fieldSelect) {
            fieldSelect.value = filter.fieldPath;
            fieldSelect.dispatchEvent(new Event('change'));
        }
        
        // Wait for field change to create the combined date dropdown
        setTimeout(() => {
            const combinedSelect = filterEl.querySelector('.filter-date-combined');
            
            // If no combined dropdown exists, this is not a date field - restore as regular filter
            if (!combinedSelect) {
                const operatorSelect = filterEl.querySelector('.filter-operator');
                if (operatorSelect) {
                    operatorSelect.value = filter.operator;
                    operatorSelect.dispatchEvent(new Event('change'));
                }
                
                setTimeout(() => {
                    const valueInput = filterEl.querySelector('.filter-value');
                    if (valueInput) valueInput.value = filter.value;
                }, 50);
                return;
            }
            
            // DATE FIELD RESTORATION
            let restoredPreset = null;

            // Case 1: InLast/InNext operators
            if (filter.operator === 'InLast' || filter.operator === 'InNext') {
                // Use dynamicDateOffset (new format) or fall back to value (old format for backward compatibility)
                const offsetValue = filter.dynamicDateOffset || filter.value;
                restoredPreset = `${filter.operator}|${offsetValue}`;
            }
            // Case 2: Has offset
            else if (filter.dynamicDateOffset && filter.dynamicDateType) {
                const direction = filter.dynamicDateType.startsWith('Past') ? 'Past' : 
                                 filter.dynamicDateType.startsWith('Next') ? 'Next' : null;
                
                if (direction) {
                    const inLastPreset = `InLast|${filter.dynamicDateOffset}`;
                    const inNextPreset = `InNext|${filter.dynamicDateOffset}`;
                    
                    if (direction === 'Past' && this.hasPreset(combinedSelect, inLastPreset)) {
                        restoredPreset = inLastPreset;
                    } else if (direction === 'Next' && this.hasPreset(combinedSelect, inNextPreset)) {
                        restoredPreset = inNextPreset;
                    } else {
                        this.restoreCustomCondition(filterEl, filter);
                        return;
                    }
                }
            }
            // Case 3: Dynamic date without offset
            else if (filter.isDynamicDate && filter.dynamicDateType) {
                restoredPreset = `${filter.operator}|${filter.dynamicDateType}`;
                if (!this.hasPreset(combinedSelect, restoredPreset)) {
                    restoredPreset = null;
                }
            }
            // Case 4: Static date value
            else if (filter.value) {
                if (filter.operator !== 'Equals') {
                    this.restoreCustomCondition(filterEl, filter);
                    return;
                } else {
                    combinedSelect.value = 'static';
                    combinedSelect.dispatchEvent(new Event('change'));
                    
                    setTimeout(() => {
                        const dateInput = filterEl.querySelector('.filter-value');
                        if (dateInput) dateInput.value = filter.value;
                    }, 50);
                    return;
                }
            }
            
            if (restoredPreset) {
                combinedSelect.value = restoredPreset;
            }
        }, 50);
        
        // Restore group ID if present
        if (filter.groupId) {
            filterEl.dataset.groupId = filter.groupId;
        }
    },
    
    hasPreset(selectElement, presetValue) {
        return Array.from(selectElement.options).some(opt => opt.value === presetValue);
    },
    
    restoreCustomCondition(filterEl, filter) {
        const combinedSelect = filterEl.querySelector('.filter-date-combined');
        combinedSelect.value = 'custom';
        combinedSelect.dispatchEvent(new Event('change'));
        
        setTimeout(() => {
            const customCondition = filterEl.querySelector('.filter-custom-condition');
            const isStaticDate = filter.value && !filter.dynamicDateOffset;
            
            if (isStaticDate) {
                const staticRadio = customCondition?.querySelector('input[name^="custom-date-type-"][value="static"]');
                if (staticRadio) {
                    staticRadio.checked = true;
                    staticRadio.dispatchEvent(new Event('change'));
                }
                
                const customOperator = customCondition?.querySelector('.filter-operator');
                if (customOperator) customOperator.value = filter.operator;
                
                const staticDateInput = customCondition?.querySelector('.filter-custom-static-value');
                if (staticDateInput) staticDateInput.value = filter.value;
            } else {
                const dynamicRadio = customCondition?.querySelector('input[name^="custom-date-type-"][value="dynamic"]');
                if (dynamicRadio) {
                    dynamicRadio.checked = true;
                    dynamicRadio.dispatchEvent(new Event('change'));
                }
                
                const customOperator = customCondition?.querySelector('.filter-operator');
                if (customOperator) customOperator.value = filter.operator;
                
                const direction = filter.dynamicDateType?.startsWith('Past') ? 'past' :
                                 filter.dynamicDateType?.startsWith('Next') ? 'next' : 'past';
                
                const directionSelect = customCondition?.querySelector('.filter-dynamic-offset-direction');
                if (directionSelect) directionSelect.value = direction;
                
                const offsetInput = customCondition?.querySelector('.filter-dynamic-offset-value');
                if (offsetInput) offsetInput.value = filter.dynamicDateOffset;
                
                const unitSelect = customCondition?.querySelector('.filter-dynamic-offset-unit');
                if (unitSelect) unitSelect.value = filter.dynamicDateOffsetUnit;
            }
        }, 50);
    },
    
    async restoreCollectionQuery(query) {
        const queryId = this.nextCollectionQueryId++;
        const newQuery = {
            id: queryId,
            collectionName: query.collectionName,
            operation: query.operation,
            displayAsColumn: query.displayAsColumn,
            columnName: query.columnName,
            comparator: query.comparator,
            value: query.value,
            aggregateField: query.aggregateField,
            subFilters: [],
            collectionSubFields: []
        };
        
        this.collectionQueries.push(newQuery);
        
        const container = document.getElementById('collectionQueries');
        if (container.children.length === 1 && container.children[0].querySelector('.text-center')) {
            container.innerHTML = '';
        }
        
        this.addCollectionQueryCard(queryId);
        
        const collectionSelect = document.getElementById(`collection-${queryId}`);
        if (collectionSelect && query.collectionName) {
            collectionSelect.value = query.collectionName;
        }
        
        await this.updateCollectionFields(queryId);
        
        if (query.subFilters && query.subFilters.length > 0) {
            setTimeout(() => {
                query.subFilters.forEach(subFilter => {
                    this.restoreCollectionSubFilter(queryId, subFilter);
                });
            }, 100);
        }
        
        document.getElementById(`operation-${queryId}`).value = query.operation;
        this.updateCollectionOperator(queryId);
        
        if (query.aggregateField && ['Min', 'Max', 'Sum', 'Average'].includes(query.operation)) {
            setTimeout(() => {
                const aggregateFieldSelect = document.getElementById(`aggregate-field-${queryId}`);
                if (aggregateFieldSelect) {
                    aggregateFieldSelect.value = query.aggregateField;
                }
            }, 100);
        }
        
        if (query.displayAsColumn) {
            document.getElementById(`display-as-column-${queryId}`).checked = true;
            document.getElementById(`column-name-${queryId}`).value = query.columnName || '';
            this.toggleDisplayMode(queryId);
        }
        
        if (!query.displayAsColumn && query.comparator) {
            document.getElementById(`comparator-${queryId}`).value = query.comparator;
            document.getElementById(`value-${queryId}`).value = query.value || '';
        }
    },
    
    restoreCollectionSubFilter(queryId, subFilter) {
        const query = this.collectionQueries.find(q => q.id === queryId);
        if (!query) return;

        const subFilterId = Date.now() + Math.random();
        query.subFilters.push(subFilterId);

        const container = document.getElementById(`subfilters-${queryId}`);
        if (!container) return;

        if (container.querySelector('.text-muted')) {
            container.innerHTML = '';
        }

        const fields = query.collectionSubFields;
        const fieldsMetadata = query.collectionSubFieldsMetadata || [];

        const fieldOptions = fields.map(fieldName => {
            const metadata = fieldsMetadata.find(m => m.fieldPath === fieldName || m.name === fieldName);
            const dataType = metadata?.dataType || subFilter.dataType || 'String';
            const displayName = this.formatFieldName(fieldName);
            const selected = fieldName === subFilter.field ? 'selected' : '';
            return `<option value="${fieldName}" data-type="${dataType}" ${selected}>${displayName}</option>`;
        }).join('');

        const operatorOptions = [
            { value: 'Equals', label: 'Equals' },
            { value: 'NotEquals', label: 'Not Equals' },
            { value: 'Contains', label: 'Contains' },
            { value: 'StartsWith', label: 'Starts With' },
            { value: 'EndsWith', label: 'Ends With' },
            { value: 'GreaterThan', label: 'Greater Than' },
            { value: 'LessThan', label: 'Less Than' }
        ].map(op => {
            const selected = op.value === subFilter.operator ? 'selected' : '';
            return `<option value="${op.value}" ${selected}>${op.label}</option>`;
        }).join('');

        container.insertAdjacentHTML('beforeend', `
            <div class="card mb-2" id="subfilter-${queryId}-${subFilterId}">
                <div class="card-body p-2">
                    <div class="row g-2 align-items-center">
                        <div class="col-md-4">
                            <select class="form-select form-select-sm subfilter-field" data-subfilter-id="${subFilterId}" data-query-id="${queryId}">
                                ${fieldOptions}
                            </select>
                        </div>
                        <div class="col-md-3">
                            <select class="form-select form-select-sm subfilter-operator" id="subfilter-operator-${queryId}-${subFilterId}">
                                ${operatorOptions}
                            </select>
                        </div>
                        <div class="col-md-4" id="subfilter-value-container-${queryId}-${subFilterId}">
                            <input type="text" class="form-control form-control-sm subfilter-value" 
                                   placeholder="Value" value="${subFilter.value || ''}">
                        </div>
                        <div class="col-md-1">
                            <button type="button" class="btn btn-sm btn-outline-danger" 
                                    onclick="ReportBuilder.removeCollectionSubFilter(${queryId}, ${subFilterId})">
                                <i class="bi bi-x"></i>
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `);

        this.setupSubFilterSmartInput(queryId, subFilterId);
    },
    
    setupDragDrop() {
        document.querySelectorAll('.field-item').forEach(item => {
            item.addEventListener('dragstart', (e) => {
                const fieldData = {
                    fieldPath: item.dataset.fieldPath,
                    displayName: item.dataset.displayName,
                    dataType: item.dataset.dataType,
                    isCustom: item.dataset.isCustom === 'true',
                    customId: item.dataset.customId || null
                };
                e.dataTransfer.setData('application/json', JSON.stringify(fieldData));
                e.dataTransfer.setData('drag-type', 'add-field');
                e.dataTransfer.effectAllowed = 'copy';
            });
        });
        
        const dropZone = document.getElementById('selectedFields');
        
        dropZone.addEventListener('dragover', (e) => {
            e.preventDefault();
            const dragType = e.dataTransfer.types.includes('drag-type') ? 'add-field' : 'reorder';
            e.dataTransfer.dropEffect = dragType === 'add-field' ? 'copy' : 'move';
        });

        dropZone.addEventListener('drop', (e) => {
            const jsonData = e.dataTransfer.getData('application/json');
            const dragType = e.dataTransfer.types.includes('drag-type') ? e.dataTransfer.getData('drag-type') : null;

            if (jsonData && dragType === 'add-field') {
                e.preventDefault();
                e.stopPropagation();
                const fieldData = JSON.parse(jsonData);
                this.addField(fieldData);
            }
        });
    },
    
    setupEventListeners() {
        document.getElementById('btnPreview').addEventListener('click', () => this.preview());
        document.getElementById('btnSave').addEventListener('click', () => this.save());
        document.getElementById('btnAddFilter').addEventListener('click', () => this.addFilter());
        document.getElementById('btnAddGroup').addEventListener('click', () => this.addFilterGroup());
        document.getElementById('btnAddCollectionQuery').addEventListener('click', () => this.addCollectionQuery());
        document.getElementById('btnLoadDefaults').addEventListener('click', () => this.loadDefaultFields());
        
        document.getElementById('entityTypeSelector').addEventListener('change', (e) => {
            const reportId = e.target.dataset.reportId;
            
            if (reportId && reportId !== 'null') {
                if (!confirm('Changing the entity type will clear all fields, filters, and collection queries. Are you sure?')) {
                    e.target.value = e.target.dataset.originalValue;
                    return;
                }
            }
            
            window.location.href = '?entityType=' + e.target.value;
        });
    },
    
    setupFieldSearch() {
        document.getElementById('fieldSearch').addEventListener('input', (e) => {
            const search = e.target.value.toLowerCase();
            document.querySelectorAll('.field-item').forEach(item => {
                const text = item.textContent.toLowerCase();
                item.style.display = text.includes(search) ? '' : 'none';
            });
        });
    },

    addField(field) {
        if (this.selectedFields.some(f => f.fieldPath === field.fieldPath)) {
            alert('Field already added');
            return;
        }

        this.selectedFields.push(field);
        this.renderSelectedFields();
    },
    
    removeField(fieldPath) {
        this.selectedFields = this.selectedFields.filter(f => f.fieldPath !== fieldPath);
        this.renderSelectedFields();
    },
    
    renderSelectedFields() {
        const container = document.getElementById('selectedFields');
        
        if (this.selectedFields.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-4">
                    <i class="bi bi-arrow-left-circle fs-1"></i>
                    <p class="mt-2">Drag fields from the left panel to add them to your report</p>
                </div>
            `;
            return;
        }
        
        container.innerHTML = this.selectedFields.map((field, index) => `
            <div class="list-group-item selected-field-item" draggable="true" data-index="${index}" data-field-path="${field.fieldPath}">
                <div class="d-flex justify-content-between align-items-center">
                    <div class="d-flex align-items-center flex-grow-1">
                        <i class="bi bi-grip-vertical text-muted me-2" style="cursor: move;"></i>
                        <div>
                            <strong>${field.displayName}</strong>
                            ${field.isCustom ? '<span class="badge bg-success ms-2">Custom</span>' : ''}
                            <br><small class="text-muted">${field.fieldPath} (${field.dataType})</small>
                        </div>
                    </div>
                    <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeField('${field.fieldPath}')">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            </div>
        `).join('');
        
        this.setupFieldReordering();
    },
    
    setupFieldReordering() {
        const items = document.querySelectorAll('.selected-field-item');
        let draggedItem = null;
        let draggedIndex = null;

        items.forEach(item => {
            item.addEventListener('dragstart', (e) => {
                draggedItem = item;
                draggedIndex = parseInt(item.dataset.index);
                item.classList.add('dragging');
                e.dataTransfer.effectAllowed = 'move';
                e.dataTransfer.setData('drag-type', 'reorder');
                e.dataTransfer.setData('text/plain', '');
            });

            item.addEventListener('dragend', (e) => {
                item.classList.remove('dragging');
                draggedItem = null;
                draggedIndex = null;
            });

            item.addEventListener('dragover', (e) => {
                if (draggedItem && draggedItem !== item) {
                    e.preventDefault();
                    e.dataTransfer.dropEffect = 'move';

                    const container = item.parentElement;
                    const mouseY = e.clientY;
                    const itemRect = item.getBoundingClientRect();
                    const itemMiddle = itemRect.top + itemRect.height / 2;

                    if (mouseY < itemMiddle) {
                        container.insertBefore(draggedItem, item);
                    } else {
                        container.insertBefore(draggedItem, item.nextSibling);
                    }
                }
            });

            item.addEventListener('drop', (e) => {
                if (draggedItem) {
                    e.preventDefault();
                    e.stopPropagation();

                    const items = document.querySelectorAll('.selected-field-item');
                    const newOrder = [];
                    items.forEach(item => {
                        const fieldPath = item.dataset.fieldPath;
                        const field = this.selectedFields.find(f => f.fieldPath === fieldPath);
                        if (field) newOrder.push(field);
                    });

                    this.selectedFields = newOrder;
                }
            });
        });
    },

    // ==================== FILTER FUNCTIONS ====================

    addFilter() {
        const filterId = this.nextFilterId++;
        const container = document.getElementById('filters');

        if (this.selectedFields.length === 0) {
            alert('Please add fields first');
            return;
        }

        if (container.querySelector('.text-center')) {
            container.innerHTML = '';
        }

        const filterHtml = `
            <div class="list-group-item" id="filter-${filterId}">
                <div class="row g-2">
                    <div class="col-md-4">
                        <select class="form-select form-select-sm filter-field" data-filter-id="${filterId}">
                            <option value="">Select field...</option>
                            ${this.selectedFields.map(f => `<option value="${f.fieldPath}" data-type="${f.dataType}">${f.displayName}</option>`).join('')}
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select form-select-sm filter-operator" id="operator-${filterId}">
                            <option value="Equals">Equals</option>
                            <option value="Contains">Contains</option>
                            <option value="GreaterThan">Greater Than</option>
                            <option value="LessThan">Less Than</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <input type="text" class="form-control form-control-sm filter-value" id="value-${filterId}" placeholder="Value">
                    </div>
                    <div class="col-md-1">
                        <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeFilter(${filterId})">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', filterHtml);
        this.setupSmartFilter(filterId);
    },

    removeFilter(filterId) {
        document.getElementById(`filter-${filterId}`)?.remove();
        const container = document.getElementById('filters');
        if (container.children.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-4">
                    <i class="bi bi-funnel fs-1"></i>
                    <p class="mt-2">No filters added yet</p>
                </div>
            `;
        }
    },

    setupSmartFilter(filterId) {
        const fieldSelect = document.querySelector(`.filter-field[data-filter-id="${filterId}"]`);
        const operatorSelect = document.getElementById(`operator-${filterId}`);
        const valueInput = document.getElementById(`value-${filterId}`);

        if (!fieldSelect || !operatorSelect || !valueInput) return;

        fieldSelect.addEventListener('change', (e) => {
            const selectedOption = e.target.options[e.target.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            this.updateOperators(operatorSelect, dataType);
            this.updateValueInput(valueInput, dataType, operatorSelect.value);
        });

        operatorSelect.addEventListener('change', (e) => {
            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';
            this.updateValueInput(valueInput, dataType, e.target.value);
        });
    },

    updateOperators(selectElement, dataType) {
        const operatorsByType = {
            'String': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'Contains', label: 'Contains' },
                { value: 'NotContains', label: 'Does Not Contain' },
                { value: 'StartsWith', label: 'Starts With' },
                { value: 'EndsWith', label: 'Ends With' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' },
                { value: 'IsEmpty', label: 'Is Empty' },
                { value: 'IsNotEmpty', label: 'Is Not Empty' }
            ],
            'Int32': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'GreaterThan', label: 'Greater Than (>)' },
                { value: 'LessThan', label: 'Less Than (<)' },
                { value: 'GreaterThanOrEqual', label: 'Greater Than or Equal (≥)' },
                { value: 'LessThanOrEqual', label: 'Less Than or Equal (≤)' },
                { value: 'Between', label: 'Between' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ],
            'Decimal': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'GreaterThan', label: 'Greater Than (>)' },
                { value: 'LessThan', label: 'Less Than (<)' },
                { value: 'GreaterThanOrEqual', label: 'Greater Than or Equal (≥)' },
                { value: 'LessThanOrEqual', label: 'Less Than or Equal (≤)' },
                { value: 'Between', label: 'Between' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ],
            'DateTime': [
                { value: 'Equals', label: 'On Date' },
                { value: 'NotEquals', label: 'Not On Date' },
                { value: 'GreaterThan', label: 'After' },
                { value: 'LessThan', label: 'Before' },
                { value: 'GreaterThanOrEqual', label: 'On or After' },
                { value: 'LessThanOrEqual', label: 'On or Before' },
                { value: 'Between', label: 'Between Dates' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' },
                { value: 'InLast', label: 'In Last X Days' },
                { value: 'InNext', label: 'In Next X Days' }
            ],
            'Boolean': [
                { value: 'Equals', label: 'Is' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ]
        };

        const normalizedType = dataType.includes('Int') || dataType.includes('Number') ? 'Int32' :
                               dataType.includes('Decimal') || dataType.includes('Double') ? 'Decimal' :
                               dataType.includes('Date') ? 'DateTime' :
                               dataType.includes('Bool') ? 'Boolean' : 'String';

        const operators = operatorsByType[normalizedType] || operatorsByType['String'];

        selectElement.innerHTML = operators.map(op => 
            `<option value="${op.value}">${op.label}</option>`
        ).join('');
    },

    updateValueInput(inputElement, dataType, operator) {
        const parentCol = inputElement.closest('.col-md-4');
        if (!parentCol) return;

        const uniqueId = inputElement.id || `filter-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

        if (operator === 'Between') {
            const inputType = dataType.includes('Date') ? 'date' : 'number';
            parentCol.innerHTML = `
                <input type="${inputType}" class="form-control form-control-sm filter-value mb-1" 
                       placeholder="From" style="width: 100%;">
                <input type="${inputType}" class="form-control form-control-sm filter-value-end" 
                       placeholder="To" style="width: 100%;">
            `;
            return;
        }

        if (operator === 'InLast' || operator === 'InNext') {
            parentCol.innerHTML = `
                <input type="number" class="form-control form-control-sm filter-value" 
                       placeholder="Number of days" min="1" style="width: 100%;">
            `;
            return;
        }

        if (operator === 'IsNull' || operator === 'IsNotNull' || operator === 'IsEmpty' || operator === 'IsNotEmpty') {
            parentCol.innerHTML = `
                <input type="text" class="form-control form-control-sm filter-value" 
                       placeholder="(no value needed)" disabled style="width: 100%;">
            `;
            return;
        }

        const normalizedType = dataType.includes('Date') ? 'date' :
                               dataType.includes('Int') || dataType.includes('Number') ? 'number' :
                               dataType.includes('Bool') ? 'checkbox' : 'text';

        const filterRow = parentCol.closest('.row');
        const operatorCol = filterRow?.querySelector('.col-md-3');

        if (normalizedType === 'checkbox') {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <div class="form-check mt-2">
                    <input type="checkbox" class="form-check-input filter-value" id="value-${uniqueId}">
                    <label class="form-check-label" for="value-${uniqueId}">True</label>
                </div>
            `;
        } else if (normalizedType === 'date') {
            if (operatorCol) operatorCol.style.display = 'none';

            parentCol.innerHTML = `
                <select class="form-control form-control-sm filter-date-combined" style="width: 100%;">
                    <option value="">Select date condition...</option>
                    <optgroup label="Specific Date">
                        <option value="static">Pick a specific date...</option>
                        <option value="Equals|Today">is today</option>
                        <option value="Equals|Yesterday">is yesterday</option>
                        <option value="Equals|Tomorrow">is tomorrow</option>
                    </optgroup>
                    <optgroup label="Relative Dates">
                        <option value="Equals|StartOfWeek">is start of this week</option>
                        <option value="Equals|EndOfWeek">is end of this week</option>
                        <option value="Equals|StartOfMonth">is start of this month</option>
                        <option value="Equals|EndOfMonth">is end of this month</option>
                    </optgroup>
                    <optgroup label="Within Last...">
                        <option value="InLast|7">is within the last 7 days</option>
                        <option value="InLast|30">is within the last 30 days</option>
                        <option value="InLast|90">is within the last 90 days</option>
                        <option value="InLast|180">is within the last 180 days</option>
                        <option value="InLast|365">is within the last 365 days</option>
                    </optgroup>
                    <optgroup label="More Than...Ago">
                        <option value="LessThan|Past7Days">is more than 7 days ago</option>
                        <option value="LessThan|Past30Days">is more than 30 days ago</option>
                        <option value="LessThan|Past3Months">is more than 3 months ago</option>
                        <option value="LessThan|Past6Months">is more than 6 months ago</option>
                        <option value="LessThan|Past12Months">is more than 12 months ago</option>
                    </optgroup>
                    <optgroup label="Within Next...">
                        <option value="InNext|7">is within the next 7 days</option>
                        <option value="InNext|30">is within the next 30 days</option>
                        <option value="InNext|90">is within the next 90 days</option>
                    </optgroup>
                    <optgroup label="Less Than...Away">
                        <option value="GreaterThan|Next7Days">is less than 7 days away</option>
                        <option value="GreaterThan|Next30Days">is less than 30 days away</option>
                    </optgroup>
                    <optgroup label="Custom">
                        <option value="custom">Custom condition...</option>
                    </optgroup>
                </select>
                <input type="date" class="form-control form-control-sm filter-value mt-1" 
                       placeholder="Select date" style="width: 100%; display: none;">
                <div class="filter-custom-condition mt-1" style="display: none;">
                    <div class="btn-group btn-group-sm w-100 mb-1" role="group">
                        <input type="radio" class="btn-check" name="custom-date-type-${uniqueId}" id="custom-dynamic-${uniqueId}" value="dynamic" checked>
                        <label class="btn btn-outline-primary" for="custom-dynamic-${uniqueId}">
                            <i class="bi bi-clock-history"></i> Dynamic Date
                        </label>
                        <input type="radio" class="btn-check" name="custom-date-type-${uniqueId}" id="custom-static-${uniqueId}" value="static">
                        <label class="btn btn-outline-primary" for="custom-static-${uniqueId}">
                            <i class="bi bi-calendar-date"></i> Static Date
                        </label>
                    </div>

                    <select class="form-select form-select-sm filter-operator mb-1">
                        <option value="Equals">is exactly</option>
                        <option value="GreaterThan">is after</option>
                        <option value="LessThan">is before</option>
                        <option value="GreaterThanOrEqual">is on or after</option>
                        <option value="LessThanOrEqual">is on or before</option>
                    </select>

                    <div class="filter-custom-dynamic">
                        <div class="input-group input-group-sm">
                            <input type="number" class="form-control filter-dynamic-offset-value" 
                                   placeholder="#" min="1" style="max-width: 70px;">
                            <select class="form-select filter-dynamic-offset-unit" style="max-width: 100px;">
                                <option value="Days">days</option>
                                <option value="Weeks">weeks</option>
                                <option value="Months">months</option>
                                <option value="Years">years</option>
                            </select>
                            <select class="form-select filter-dynamic-offset-direction" style="max-width: 90px;">
                                <option value="past">ago</option>
                                <option value="next">from now</option>
                            </select>
                        </div>
                    </div>

                    <div class="filter-custom-static" style="display: none;">
                        <input type="date" class="form-control form-control-sm filter-custom-static-value">
                    </div>
                </div>
            `;

            setTimeout(() => {
                const combinedSelect = parentCol.querySelector('.filter-date-combined');
                const dateInput = parentCol.querySelector('.filter-value');
                const customCondition = parentCol.querySelector('.filter-custom-condition');

                if (combinedSelect && dateInput && customCondition) {
                    combinedSelect.addEventListener('change', (e) => {
                        const value = e.target.value;

                        if (value === 'static') {
                            dateInput.style.display = '';
                            customCondition.style.display = 'none';
                        } else if (value === 'custom') {
                            dateInput.style.display = 'none';
                            customCondition.style.display = '';
                        } else {
                            dateInput.style.display = 'none';
                            customCondition.style.display = 'none';
                        }
                    });

                    const customDynamicControls = customCondition.querySelector('.filter-custom-dynamic');
                    const customStaticControls = customCondition.querySelector('.filter-custom-static');
                    const dateTypeRadios = customCondition.querySelectorAll('input[name^="custom-date-type-"]');

                    dateTypeRadios.forEach(radio => {
                        radio.addEventListener('change', (e) => {
                            if (e.target.value === 'dynamic') {
                                customDynamicControls.style.display = '';
                                customStaticControls.style.display = 'none';
                            } else {
                                customDynamicControls.style.display = 'none';
                                customStaticControls.style.display = '';
                            }
                        });
                    });
                }
            }, 0);
        } else if (normalizedType === 'number') {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <input type="number" class="form-control form-control-sm filter-value" 
                       placeholder="Enter number" step="any" style="width: 100%;">
            `;
        } else {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <input type="text" class="form-control form-control-sm filter-value" 
                       placeholder="Enter value" style="width: 100%;">
            `;
        }
    },

    // ==================== FILTER GROUP FUNCTIONS ====================

    addFilterGroup() {
        const groupId = this.nextGroupId++;
        const groupHtml = `
            <div class="filter-group mb-3 p-3 border border-primary rounded" id="group-${groupId}" data-group-id="${groupId}">
                <div class="d-flex justify-content-between align-items-center mb-2">
                    <div>
                        <strong><i class="bi bi-parentheses"></i> Filter Group ${groupId}</strong>
                    </div>
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-sm btn-outline-primary" onclick="ReportBuilder.addFilterToGroup(${groupId})">
                            <i class="bi bi-plus"></i> Add Filter
                        </button>
                        <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeGroup(${groupId})">
                            <i class="bi bi-x"></i> Remove Group
                        </button>
                    </div>
                </div>
                <div class="group-filters" data-group-id="${groupId}">
                    <div class="text-muted small py-2 text-center">
                        <i class="bi bi-arrow-down-circle"></i> Add filters to this group
                    </div>
                </div>
                <div class="mt-2">
                    <div class="btn-group btn-group-sm" role="group">
                        <input type="radio" class="btn-check" name="group-logic-${groupId}" id="group-and-${groupId}" value="AND" checked>
                        <label class="btn btn-outline-primary" for="group-and-${groupId}">AND</label>
                        <input type="radio" class="btn-check" name="group-logic-${groupId}" id="group-or-${groupId}" value="OR">
                        <label class="btn btn-outline-primary" for="group-or-${groupId}">OR</label>
                    </div>
                    <small class="text-muted ms-2">with next group</small>
                </div>
            </div>
        `;

        const container = document.getElementById('filters');
        if (container.querySelector('.text-center.text-muted')) {
            container.innerHTML = '';
        }
        container.insertAdjacentHTML('beforeend', groupHtml);
        this.filterGroups.push({ id: groupId, filters: [] });
    },

    addFilterToGroup(groupId) {
        const filterId = this.nextFilterId++;

        if (this.selectedFields.length === 0) {
            alert('Please add fields first');
            return;
        }

        const filterHtml = `
            <div class="list-group-item mb-2" id="filter-${filterId}" data-filter-id="${filterId}" data-group-id="${groupId}">
                <div class="row g-2">
                    <div class="col-md-4">
                        <select class="form-select form-select-sm filter-field" data-filter-id="${filterId}">
                            <option value="">Select field...</option>
                            ${this.selectedFields.map(f => `<option value="${f.fieldPath}" data-type="${f.dataType}">${f.displayName}</option>`).join('')}
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select form-select-sm filter-operator" id="operator-${filterId}">
                            <option value="Equals">Equals</option>
                            <option value="Contains">Contains</option>
                            <option value="GreaterThan">Greater Than</option>
                            <option value="LessThan">Less Than</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <input type="text" class="form-control form-control-sm filter-value" id="value-${filterId}" placeholder="Value">
                    </div>
                    <div class="col-md-1">
                        <button class="btn btn-sm btn-outline-danger" onclick="ReportBuilder.removeFilterFromGroup(${filterId}, ${groupId})">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                </div>
                <div class="mt-1">
                    <div class="btn-group btn-group-sm" role="group">
                        <input type="radio" class="btn-check" name="logic-${filterId}" id="and-${filterId}" value="AND" checked>
                        <label class="btn btn-outline-secondary" for="and-${filterId}">AND</label>
                        <input type="radio" class="btn-check" name="logic-${filterId}" id="or-${filterId}" value="OR">
                        <label class="btn btn-outline-secondary" for="or-${filterId}">OR</label>
                    </div>
                    <small class="text-muted ms-2">with next filter in group</small>
                </div>
            </div>
        `;

        const groupContainer = document.querySelector(`.group-filters[data-group-id="${groupId}"]`);
        const placeholder = groupContainer.querySelector('.text-center');
        if (placeholder) {
            groupContainer.innerHTML = '';
        }
        groupContainer.insertAdjacentHTML('beforeend', filterHtml);

        const group = this.filterGroups.find(g => g.id === groupId);
        if (group) {
            group.filters.push(filterId);
        }

        this.setupSmartFilter(filterId);
    },

    removeGroup(groupId) {
        document.getElementById(`group-${groupId}`)?.remove();
        this.filterGroups = this.filterGroups.filter(g => g.id !== groupId);

        const container = document.getElementById('filters');
        if (container.children.length === 0) {
            container.innerHTML = `
                <div class="text-center text-muted py-4">
                    <i class="bi bi-funnel fs-1"></i>
                    <p class="mt-2">No filters added yet</p>
                </div>
            `;
        }
    },

    removeFilterFromGroup(filterId, groupId) {
        document.getElementById(`filter-${filterId}`)?.remove();

        const group = this.filterGroups.find(g => g.id === groupId);
        if (group) {
            group.filters = group.filters.filter(f => f !== filterId);
        }

        const groupContainer = document.querySelector(`.group-filters[data-group-id="${groupId}"]`);
        if (groupContainer && groupContainer.children.length === 0) {
            groupContainer.innerHTML = `
                <div class="text-muted small py-2 text-center">
                    <i class="bi bi-arrow-down-circle"></i> Add filters to this group
                </div>
            `;
        }
    },

    // Smart filtering functions
    setupSmartFilter(filterId) {
        const fieldSelect = document.querySelector(`.filter-field[data-filter-id="${filterId}"]`);
        const operatorSelect = document.getElementById(`operator-${filterId}`);
        const valueInput = document.getElementById(`value-${filterId}`);

        if (!fieldSelect || !operatorSelect || !valueInput) return;

        fieldSelect.addEventListener('change', (e) => {
            const selectedOption = e.target.options[e.target.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            this.updateOperators(operatorSelect, dataType);
            this.updateValueInput(valueInput, dataType, operatorSelect.value);
        });

        operatorSelect.addEventListener('change', (e) => {
            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';
            this.updateValueInput(valueInput, dataType, e.target.value);
        });
    },

    setupSubFilterSmartInput(queryId, subFilterId) {
        const fieldSelect = document.querySelector(`[data-subfilter-id="${subFilterId}"][data-query-id="${queryId}"]`);
        const operatorSelect = document.getElementById(`subfilter-operator-${queryId}-${subFilterId}`);
        const valueContainer = document.getElementById(`subfilter-value-container-${queryId}-${subFilterId}`);

        if (!fieldSelect || !operatorSelect || !valueContainer) {
            console.warn(`Sub-filter elements not found for query ${queryId}, subfilter ${subFilterId}`);
            return;
        }

        fieldSelect.addEventListener('change', (e) => {
            const selectedOption = e.target.options[e.target.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            console.log(`[SubFilter] Field changed to ${e.target.value}, dataType: ${dataType}`);

            this.updateOperators(operatorSelect, dataType);

            valueContainer.innerHTML = '<input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">';
            const tempInput = valueContainer.querySelector('input');

            this.updateValueInput(tempInput, dataType, operatorSelect.value);
        });

        operatorSelect.addEventListener('change', (e) => {
            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            console.log(`[SubFilter] Operator changed to ${e.target.value}, dataType: ${dataType}`);

            valueContainer.innerHTML = '<input type="text" class="form-control form-control-sm subfilter-value" placeholder="Value">';
            const tempInput = valueContainer.querySelector('input');

            this.updateValueInput(tempInput, dataType, e.target.value);
        });
    },

    updateOperators(selectElement, dataType) {
        const operatorsByType = {
            'String': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'Contains', label: 'Contains' },
                { value: 'NotContains', label: 'Does Not Contain' },
                { value: 'StartsWith', label: 'Starts With' },
                { value: 'EndsWith', label: 'Ends With' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' },
                { value: 'IsEmpty', label: 'Is Empty' },
                { value: 'IsNotEmpty', label: 'Is Not Empty' }
            ],
            'Int32': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'GreaterThan', label: 'Greater Than (>)' },
                { value: 'LessThan', label: 'Less Than (<)' },
                { value: 'GreaterThanOrEqual', label: 'Greater Than or Equal (≥)' },
                { value: 'LessThanOrEqual', label: 'Less Than or Equal (≤)' },
                { value: 'Between', label: 'Between' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ],
            'Decimal': [
                { value: 'Equals', label: 'Equals' },
                { value: 'NotEquals', label: 'Not Equals' },
                { value: 'GreaterThan', label: 'Greater Than (>)' },
                { value: 'LessThan', label: 'Less Than (<)' },
                { value: 'GreaterThanOrEqual', label: 'Greater Than or Equal (≥)' },
                { value: 'LessThanOrEqual', label: 'Less Than or Equal (≤)' },
                { value: 'Between', label: 'Between' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ],
            'DateTime': [
                { value: 'Equals', label: 'On Date' },
                { value: 'NotEquals', label: 'Not On Date' },
                { value: 'GreaterThan', label: 'After' },
                { value: 'LessThan', label: 'Before' },
                { value: 'GreaterThanOrEqual', label: 'On or After' },
                { value: 'LessThanOrEqual', label: 'On or Before' },
                { value: 'Between', label: 'Between Dates' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' },
                { value: 'InLast', label: 'In Last X Days' },
                { value: 'InNext', label: 'In Next X Days' }
            ],
            'Boolean': [
                { value: 'Equals', label: 'Is' },
                { value: 'IsNull', label: 'Is Null' },
                { value: 'IsNotNull', label: 'Is Not Null' }
            ]
        };

        const normalizedType = dataType.includes('Int') || dataType.includes('Number') ? 'Int32' :
                               dataType.includes('Decimal') || dataType.includes('Double') ? 'Decimal' :
                               dataType.includes('Date') ? 'DateTime' :
                               dataType.includes('Bool') ? 'Boolean' : 'String';

        const operators = operatorsByType[normalizedType] || operatorsByType['String'];

        selectElement.innerHTML = operators.map(op =>
            `<option value="${op.value}">${op.label}</option>`
        ).join('');

        // For date fields, hide the operator column because the combined dropdown handles everything
        if (normalizedType === 'DateTime') {
            const filterRow = selectElement.closest('.row');
            const operatorCol = selectElement.closest('.col-md-3');
            if (operatorCol) {
                operatorCol.style.display = 'none';
            }
        }
    },

    updateValueInput(inputElement, dataType, operator) {
        const parentCol = inputElement.closest('.col-md-4');
        if (!parentCol) return;

        const uniqueId = inputElement.id || `filter-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

        if (operator === 'Between') {
            const inputType = dataType.includes('Date') ? 'date' : 'number';
            parentCol.innerHTML = `
                <input type="${inputType}" class="form-control form-control-sm filter-value mb-1" 
                       placeholder="From" style="width: 100%;">
                <input type="${inputType}" class="form-control form-control-sm filter-value-end" 
                       placeholder="To" style="width: 100%;">
            `;
            return;
        }

        // For date fields, InLast/InNext are handled by the combined dropdown, not a number input
        // Only create number input for InLast/InNext if it's NOT a date field
        if ((operator === 'InLast' || operator === 'InNext') && !dataType.includes('Date')) {
            parentCol.innerHTML = `
                <input type="number" class="form-control form-control-sm filter-value" 
                       placeholder="Number of days" min="1" style="width: 100%;">
            `;
            return;
        }

        if (operator === 'IsNull' || operator === 'IsNotNull' || operator === 'IsEmpty' || operator === 'IsNotEmpty') {
            parentCol.innerHTML = `
                <input type="text" class="form-control form-control-sm filter-value" 
                       placeholder="(no value needed)" disabled style="width: 100%;">
            `;
            return;
        }

        const normalizedType = dataType.includes('Date') ? 'date' :
                               dataType.includes('Int') || dataType.includes('Number') ? 'number' :
                               dataType.includes('Bool') ? 'checkbox' : 'text';

        const filterRow = parentCol.closest('.row');
        const operatorCol = filterRow?.querySelector('.col-md-3');

        if (normalizedType === 'checkbox') {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <div class="form-check mt-2">
                    <input type="checkbox" class="form-check-input filter-value" id="value-${uniqueId}">
                    <label class="form-check-label" for="value-${uniqueId}">True</label>
                </div>
            `;
        } else if (normalizedType === 'date') {
            if (operatorCol) operatorCol.style.display = 'none';

            parentCol.innerHTML = this.getDateFilterHTML(uniqueId);

            setTimeout(() => {
                this.setupDateFilterListeners(parentCol, uniqueId);
            }, 0);
        } else if (normalizedType === 'number') {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <input type="number" class="form-control form-control-sm filter-value" 
                       placeholder="Enter number" step="any" style="width: 100%;">
            `;
        } else {
            if (operatorCol) operatorCol.style.display = '';

            parentCol.innerHTML = `
                <input type="text" class="form-control form-control-sm filter-value" 
                       placeholder="Enter value" style="width: 100%;">
            `;
        }
    },

    getDateFilterHTML(uniqueId) {
        return `
            <select class="form-control form-control-sm filter-date-combined" style="width: 100%;">
                <option value="">Select date condition...</option>
                <optgroup label="Specific Date">
                    <option value="static">Pick a specific date...</option>
                    <option value="Equals|Today">is today</option>
                    <option value="Equals|Yesterday">is yesterday</option>
                    <option value="Equals|Tomorrow">is tomorrow</option>
                </optgroup>
                <optgroup label="Relative Dates">
                    <option value="Equals|StartOfWeek">is start of this week</option>
                    <option value="Equals|EndOfWeek">is end of this week</option>
                    <option value="Equals|StartOfMonth">is start of this month</option>
                    <option value="Equals|EndOfMonth">is end of this month</option>
                </optgroup>
                <optgroup label="Within Last...">
                    <option value="InLast|7">is within the last 7 days</option>
                    <option value="InLast|30">is within the last 30 days</option>
                    <option value="InLast|90">is within the last 90 days</option>
                    <option value="InLast|180">is within the last 180 days</option>
                    <option value="InLast|365">is within the last 365 days</option>
                </optgroup>
                <optgroup label="More Than...Ago">
                    <option value="LessThan|Past7Days">is more than 7 days ago</option>
                    <option value="LessThan|Past30Days">is more than 30 days ago</option>
                    <option value="LessThan|Past3Months">is more than 3 months ago</option>
                    <option value="LessThan|Past6Months">is more than 6 months ago</option>
                    <option value="LessThan|Past12Months">is more than 12 months ago</option>
                </optgroup>
                <optgroup label="Within Next...">
                    <option value="InNext|7">is within the next 7 days</option>
                    <option value="InNext|30">is within the next 30 days</option>
                    <option value="InNext|90">is within the next 90 days</option>
                </optgroup>
                <optgroup label="Less Than...Away">
                    <option value="GreaterThan|Next7Days">is less than 7 days away</option>
                    <option value="GreaterThan|Next30Days">is less than 30 days away</option>
                </optgroup>
                <optgroup label="Custom">
                    <option value="custom">Custom condition...</option>
                </optgroup>
            </select>
            <input type="date" class="form-control form-control-sm filter-value mt-1" 
                   placeholder="Select date" style="width: 100%; display: none;">
            <div class="filter-custom-condition mt-1" style="display: none;">
                <div class="btn-group btn-group-sm w-100 mb-1" role="group">
                    <input type="radio" class="btn-check" name="custom-date-type-${uniqueId}" id="custom-dynamic-${uniqueId}" value="dynamic" checked>
                    <label class="btn btn-outline-primary" for="custom-dynamic-${uniqueId}">
                        <i class="bi bi-clock-history"></i> Dynamic Date
                    </label>
                    <input type="radio" class="btn-check" name="custom-date-type-${uniqueId}" id="custom-static-${uniqueId}" value="static">
                    <label class="btn btn-outline-primary" for="custom-static-${uniqueId}">
                        <i class="bi bi-calendar-date"></i> Static Date
                    </label>
                </div>

                <select class="form-select form-select-sm filter-operator mb-1">
                    <option value="Equals">is exactly</option>
                    <option value="GreaterThan">is after</option>
                    <option value="LessThan">is before</option>
                    <option value="GreaterThanOrEqual">is on or after</option>
                    <option value="LessThanOrEqual">is on or before</option>
                </select>

                <div class="filter-custom-dynamic">
                    <div class="input-group input-group-sm">
                        <input type="number" class="form-control filter-dynamic-offset-value" 
                               placeholder="#" min="1" style="max-width: 70px;">
                        <select class="form-select filter-dynamic-offset-unit" style="max-width: 100px;">
                            <option value="Days">days</option>
                            <option value="Weeks">weeks</option>
                            <option value="Months">months</option>
                            <option value="Years">years</option>
                        </select>
                        <select class="form-select filter-dynamic-offset-direction" style="max-width: 90px;">
                            <option value="past">ago</option>
                            <option value="next">from now</option>
                        </select>
                    </div>
                </div>

                <div class="filter-custom-static" style="display: none;">
                    <input type="date" class="form-control form-control-sm filter-custom-static-value">
                </div>
            </div>
        `;
    },

    setupDateFilterListeners(parentCol, uniqueId) {
        const combinedSelect = parentCol.querySelector('.filter-date-combined');
        const dateInput = parentCol.querySelector('.filter-value');
        const customCondition = parentCol.querySelector('.filter-custom-condition');

        if (combinedSelect && dateInput && customCondition) {
            combinedSelect.addEventListener('change', (e) => {
                const value = e.target.value;

                if (value === 'static') {
                    dateInput.style.display = '';
                    customCondition.style.display = 'none';
                } else if (value === 'custom') {
                    dateInput.style.display = 'none';
                    customCondition.style.display = '';
                } else {
                    dateInput.style.display = 'none';
                    customCondition.style.display = 'none';
                }
            });

            const customDynamicControls = customCondition.querySelector('.filter-custom-dynamic');
            const customStaticControls = customCondition.querySelector('.filter-custom-static');
            const dateTypeRadios = customCondition.querySelectorAll('input[name^="custom-date-type-"]');

            dateTypeRadios.forEach(radio => {
                radio.addEventListener('change', (e) => {
                    if (e.target.value === 'dynamic') {
                        customDynamicControls.style.display = '';
                        customStaticControls.style.display = 'none';
                    } else {
                        customDynamicControls.style.display = 'none';
                        customStaticControls.style.display = '';
                    }
                });
            });
        }
    },

    formatFieldName(fieldPath) {
        if (fieldPath.includes('.')) {
            const parts = fieldPath.split('.');
            return parts.map(part => this.splitPascalCase(part)).join(': ');
        }

        return this.splitPascalCase(fieldPath);
    },

    splitPascalCase(text) {
        return text.replace(/([a-z])([A-Z])/g, '$1 $2')
                  .replace(/([A-Z])([A-Z][a-z])/g, '$1 $2');
    }
};

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    // ReportBuilder will be initialized from Builder.cshtml with saved data
});
