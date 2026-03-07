/**
 * Collection Filter UI Extension for Report Builder
 * Handles UI for complex collection queries (e.g., Cases with PCR positive results)
 */

const collectionFilterHelper = {
    /**
     * Render collection filter UI when user selects a collection field
     */
    renderCollectionFilterUI: function(filterId, collectionMetadata) {
        const { fieldPath, displayName, collectionElementType, collectionSubFields } = collectionMetadata;
        
        // Store subfields as a data attribute for later retrieval
        const subFieldsData = JSON.stringify(collectionSubFields);
        
        return `
            <div class="collection-filter-container border rounded p-3 mt-2" style="background: #f8f9fa;" data-subfields='${subFieldsData.replace(/'/g, '&apos;')}' data-filter-id="${filterId}">
                <h6 class="mb-3">
                    <i class="bi bi-collection-fill text-primary me-2"></i>
                    Query: ${displayName}
                </h6>
                
                <!-- Collection Operator Selection -->
                <div class="row mb-3">
                    <div class="col-md-6">
                        <label class="form-label small fw-bold">Collection Operator:</label>
                        <select class="form-select form-select-sm collection-operator" data-filter-id="${filterId}">
                            <option value="HasAny" selected>Has Any (at least one matches)</option>
                            <option value="HasAll">Has All (all match)</option>
                            <option value="Count">Count (number of items)</option>
                            <option value="None">None (no items match)</option>
                        </select>
                    </div>
                    <div class="col-md-6 count-value-container" style="display: none;">
                        <label class="form-label small fw-bold">Comparison:</label>
                        <div class="input-group input-group-sm">
                            <select class="form-select count-operator">
                                <option value="GreaterThan">Greater Than</option>
                                <option value="LessThan">Less Than</option>
                                <option value="Equals">Equals</option>
                                <option value="GreaterThanOrEqual">Greater or Equal</option>
                                <option value="LessThanOrEqual">Less or Equal</option>
                            </select>
                            <input type="number" class="form-control count-value" placeholder="Count" value="0">
                        </div>
                    </div>
                </div>
                
                <!-- Sub-Filters Container -->
                <div class="sub-filters-container" data-filter-id="${filterId}">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <label class="form-label small fw-bold mb-0">Filter Conditions:</label>
                        <button type="button" class="btn btn-sm btn-outline-primary add-condition-btn" data-filter-id="${filterId}">
                            <i class="bi bi-plus-circle me-1"></i>Add Condition
                        </button>
                    </div>
                    
                    <!-- Sub-filters will be added here -->
                    <div class="sub-filter-list" id="sub-filter-list-${filterId}">
                        <div class="text-muted small text-center py-2">
                            <i class="bi bi-info-circle me-1"></i>
                            Click "Add Condition" to filter items in this collection
                        </div>
                    </div>
                </div>
                
                <!-- Collection Filter Preview -->
                <div class="alert alert-info alert-sm mb-0 mt-3" style="font-size: 0.875rem;">
                    <i class="bi bi-lightbulb-fill me-1"></i>
                    <strong>Preview:</strong> <span class="collection-filter-preview" id="preview-${filterId}">
                        ${fieldPath}.Any()
                    </span>
                </div>
            </div>
        `;
    },

    /**
     * Add a sub-filter to a collection filter
     */
    addSubFilter: function(filterId, subFields) {
        const subFilterId = `${filterId}-${Date.now()}`;
        const listContainer = document.getElementById(`sub-filter-list-${filterId}`);
        
        // Remove placeholder
        const placeholder = listContainer.querySelector('.text-muted');
        if (placeholder) {
            placeholder.remove();
        }
        
        const subFilterHtml = `
            <div class="sub-filter-item border rounded p-2 mb-2" id="sub-filter-${subFilterId}" data-sub-filter-id="${subFilterId}">
                <div class="row g-2">
                    <div class="col-md-4">
                        <select class="form-select form-select-sm sub-filter-field" required>
                            <option value="">Select field...</option>
                            ${subFields.map(field => `<option value="${field}">${this.formatFieldName(field)}</option>`).join('')}
                        </select>
                    </div>
                    <div class="col-md-3">
                        <select class="form-select form-select-sm sub-filter-operator">
                            <option value="Equals">Equals</option>
                            <option value="NotEquals">Not Equals</option>
                            <option value="Contains">Contains</option>
                            <option value="StartsWith">Starts With</option>
                            <option value="GreaterThan">Greater Than</option>
                            <option value="LessThan">Less Than</option>
                        </select>
                    </div>
                    <div class="col-md-4">
                        <input type="text" class="form-control form-control-sm sub-filter-value" placeholder="Value">
                    </div>
                    <div class="col-md-1">
                        <button class="btn btn-sm btn-outline-danger" onclick="collectionFilterHelper.removeSubFilter('${subFilterId}', ${filterId})">
                            <i class="bi bi-x"></i>
                        </button>
                    </div>
                </div>
            </div>
        `;
        
        listContainer.insertAdjacentHTML('beforeend', subFilterHtml);
        this.updateCollectionFilterPreview(filterId);
        
        // Add listeners to update preview
        const subFilterEl = document.getElementById(`sub-filter-${subFilterId}`);
        subFilterEl.querySelectorAll('select, input').forEach(el => {
            el.addEventListener('change', () => this.updateCollectionFilterPreview(filterId));
            el.addEventListener('input', () => this.updateCollectionFilterPreview(filterId));
        });
    },

    /**
     * Remove a sub-filter
     */
    removeSubFilter: function(subFilterId, filterId) {
        const element = document.getElementById(`sub-filter-${subFilterId}`);
        if (element) {
            element.remove();
        }
        
        // Show placeholder if no sub-filters remain
        const listContainer = document.getElementById(`sub-filter-list-${filterId}`);
        if (listContainer && listContainer.children.length === 0) {
            listContainer.innerHTML = `
                <div class="text-muted small text-center py-2">
                    <i class="bi bi-info-circle me-1"></i>
                    Click "Add Condition" to filter items in this collection
                </div>
            `;
        }
        
        this.updateCollectionFilterPreview(filterId);
    },

    /**
     * Update collection filter preview text
     */
    updateCollectionFilterPreview: function(filterId) {
        const filterEl = document.getElementById(`filter-${filterId}`);
        if (!filterEl) return;
        
        const fieldPath = filterEl.querySelector('.filter-field')?.value || 'Collection';
        const collectionOp = filterEl.querySelector('.collection-operator')?.value || 'HasAny';
        const previewEl = document.getElementById(`preview-${filterId}`);
        
        if (!previewEl) return;
        
        // Get sub-filters
        const subFilters = [];
        filterEl.querySelectorAll('.sub-filter-item').forEach(subFilterEl => {
            const field = subFilterEl.querySelector('.sub-filter-field')?.value;
            const operator = subFilterEl.querySelector('.sub-filter-operator')?.value;
            const value = subFilterEl.querySelector('.sub-filter-value')?.value;
            
            if (field && operator && value) {
                const opSymbol = this.getOperatorSymbol(operator);
                subFilters.push(`${field} ${opSymbol} "${value}"`);
            }
        });
        
        // Build preview
        let preview = '';
        switch (collectionOp) {
            case 'HasAny':
                preview = subFilters.length > 0 
                    ? `${fieldPath}.Any(${subFilters.join(' && ')})` 
                    : `${fieldPath}.Any()`;
                break;
            case 'HasAll':
                preview = subFilters.length > 0 
                    ? `${fieldPath}.All(${subFilters.join(' && ')})` 
                    : `${fieldPath}.All()`;
                break;
            case 'Count':
                const countOp = filterEl.querySelector('.count-operator')?.value || 'GreaterThan';
                const countValue = filterEl.querySelector('.count-value')?.value || '0';
                const countSymbol = this.getOperatorSymbol(countOp);
                preview = `${fieldPath}.Count() ${countSymbol} ${countValue}`;
                break;
            case 'None':
                preview = subFilters.length > 0 
                    ? `!${fieldPath}.Any(${subFilters.join(' && ')})` 
                    : `!${fieldPath}.Any()`;
                break;
        }
        
        previewEl.textContent = preview;
    },

    /**
     * Get operator symbol for preview
     */
    getOperatorSymbol: function(operator) {
        const symbols = {
            'Equals': '==',
            'NotEquals': '!=',
            'Contains': 'contains',
            'StartsWith': 'starts with',
            'GreaterThan': '>',
            'LessThan': '<',
            'GreaterThanOrEqual': '>=',
            'LessThanOrEqual': '<='
        };
        return symbols[operator] || '==';
    },

    /**
     * Format field name for display (e.g., "TestType" -> "Test Type")
     */
    formatFieldName: function(fieldName) {
        return fieldName.replace(/([A-Z])/g, ' $1').trim();
    },

    /**
     * Initialize collection operator listeners
     */
    initCollectionOperatorListener: function(filterId) {
        const filterEl = document.getElementById(`filter-${filterId}`);
        if (!filterEl) return;
        
        const collectionOpSelect = filterEl.querySelector('.collection-operator');
        const countContainer = filterEl.querySelector('.count-value-container');
        
        if (collectionOpSelect && countContainer) {
            collectionOpSelect.addEventListener('change', function() {
                // Show/hide count inputs based on operator
                if (this.value === 'Count') {
                    countContainer.style.display = 'block';
                } else {
                    countContainer.style.display = 'none';
                }
                
                collectionFilterHelper.updateCollectionFilterPreview(filterId);
            });
        }
        
        // Add event listener for "Add Condition" button
        const addConditionBtn = filterEl.querySelector('.add-condition-btn');
        if (addConditionBtn) {
            addConditionBtn.addEventListener('click', function() {
                // Get subfields from data attribute
                const container = filterEl.querySelector('.collection-filter-container');
                if (container) {
                    const subFieldsData = container.getAttribute('data-subfields');
                    if (subFieldsData) {
                        try {
                            const subFields = JSON.parse(subFieldsData);
                            collectionFilterHelper.addSubFilter(filterId, subFields);
                        } catch (e) {                        }
                    }
                }
            });
        }
    },

    /**
     * Extract collection filter data for API submission
     */
    extractCollectionFilterData: function(filterElement) {
        const fieldSelect = filterElement.querySelector('.filter-field');
        const collectionOp = filterElement.querySelector('.collection-operator')?.value || 'HasAny';
        
        // Get sub-filters
        const subFilters = [];
        filterElement.querySelectorAll('.sub-filter-item').forEach(subFilterEl => {
            const field = subFilterEl.querySelector('.sub-filter-field')?.value;
            const operator = subFilterEl.querySelector('.sub-filter-operator')?.value;
            const value = subFilterEl.querySelector('.sub-filter-value')?.value;
            
            if (field && operator) {
                subFilters.push({
                    Field: field,
                    Operator: operator,
                    Value: value || '',
                    DataType: this.inferDataType(field)
                });
            }
        });
        
        const filterData = {
            fieldPath: fieldSelect?.value || '',
            isCollectionQuery: true,
            collectionOperator: collectionOp,
            collectionSubFilters: JSON.stringify(subFilters)
        };
        
        // Add count-specific data
        if (collectionOp === 'Count') {
            filterData.operator = filterElement.querySelector('.count-operator')?.value || 'GreaterThan';
            filterData.value = filterElement.querySelector('.count-value')?.value || '0';
        }
        
        return filterData;
    },

    /**
     * Infer data type from field name
     */
    inferDataType: function(fieldName) {
        const lower = fieldName.toLowerCase();
        if (lower.includes('date') || lower.includes('time')) {
            return 'DateTime';
        }
        if (lower.includes('count') || lower.includes('number') || lower.includes('id')) {
            return 'Int32';
        }
        return 'String';
    }
};

// Export for use in Builder.cshtml
window.collectionFilterHelper = collectionFilterHelper;
