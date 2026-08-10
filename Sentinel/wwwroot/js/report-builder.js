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
    autoSaveEnabled: true,
    autoSaveTimer: null,
    lastAutoSave: null,

    // Auto-save functionality
    AUTO_SAVE_KEY: 'sentinel_report_draft',
    AUTO_SAVE_DELAY: 2000, // 2 seconds after last change

    scheduleAutoSave() {
        if (!this.autoSaveEnabled) return;

        // Clear existing timer
        if (this.autoSaveTimer) {
            clearTimeout(this.autoSaveTimer);
        }

        // Schedule new save
        this.autoSaveTimer = setTimeout(() => {
            this.performAutoSave();
        }, this.AUTO_SAVE_DELAY);
    },

    performAutoSave() {
        if (!this.autoSaveEnabled) return;

        try {
            const entityType = document.getElementById('entityTypeSelector')?.value || 'Case';
            const reportName = document.getElementById('reportName')?.value || '';

            // Get actual filter configuration from DOM, not just IDs
            const serializedFilters = this.getFilters ? this.getFilters() : [];

            const draftData = {
                timestamp: new Date().toISOString(),
                entityType: entityType,
                reportName: reportName,
                selectedFields: this.selectedFields,
                filters: serializedFilters,
                collectionQueries: this.collectionQueries,
                nextFilterId: this.nextFilterId,
                nextCollectionQueryId: this.nextCollectionQueryId
            };

            localStorage.setItem(this.AUTO_SAVE_KEY, JSON.stringify(draftData));
            this.lastAutoSave = new Date();
            this.updateAutoSaveStatus();
            console.log('[AutoSave] Draft saved at', this.lastAutoSave.toLocaleTimeString());
        } catch (error) {
            console.error('[AutoSave] Error saving draft:', error);
        }
    },

    loadAutoSavedDraft() {
        try {
            const draftJson = localStorage.getItem(this.AUTO_SAVE_KEY);
            if (!draftJson) return null;

            const draft = JSON.parse(draftJson);
            console.log('[AutoSave] Found draft from', draft.timestamp);
            return draft;
        } catch (error) {
            console.error('[AutoSave] Error loading draft:', error);
            return null;
        }
    },

    clearAutoSavedDraft() {
        try {
            localStorage.removeItem(this.AUTO_SAVE_KEY);
            this.lastAutoSave = null;
            this.updateAutoSaveStatus();
            console.log('[AutoSave] Draft cleared');
        } catch (error) {
            console.error('[AutoSave] Error clearing draft:', error);
        }
    },

    updateAutoSaveStatus() {
        const statusEl = document.getElementById('autoSaveStatus');
        if (!statusEl) return;

        if (this.lastAutoSave) {
            const timeAgo = this.getTimeAgo(this.lastAutoSave);
            statusEl.textContent = `Auto-saved ${timeAgo}`;
            statusEl.style.opacity = '1';
        } else {
            statusEl.textContent = 'Auto-save enabled';
            statusEl.style.opacity = '0.6';
        }
    },

    getTimeAgo(date) {
        const seconds = Math.floor((new Date() - date) / 1000);
        if (seconds < 10) return 'just now';
        if (seconds < 60) return `${seconds}s ago`;
        const minutes = Math.floor(seconds / 60);
        if (minutes < 60) return `${minutes}m ago`;
        const hours = Math.floor(minutes / 60);
        return `${hours}h ago`;
    },

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

    updateStatusBar() {
        const entityType = document.getElementById('entityTypeSelector')?.value || 'Case';
        const statusEntity = document.getElementById('statusEntity');
        const statusFields = document.getElementById('statusFields');
        const statusFilters = document.getElementById('statusFilters');
        const statusCollections = document.getElementById('statusCollections');
        const fieldCount = document.getElementById('fieldCount');
        const filterCount = document.getElementById('filterCount');
        const collectionCount = document.getElementById('collectionCount');

        if (statusEntity) statusEntity.textContent = entityType;
        if (statusFields) statusFields.textContent = this.selectedFields.length;
        if (statusFilters) statusFilters.textContent = this.filters.length;
        if (statusCollections) statusCollections.textContent = this.collectionQueries.length;
        if (fieldCount) fieldCount.textContent = this.selectedFields.length;
        if (filterCount) filterCount.textContent = this.filters.length;
        if (collectionCount) collectionCount.textContent = this.collectionQueries.length;

        // Update query summary
        this.updateQuerySummary();
    },

    updateQuerySummary() {
        const querySummary = document.getElementById('querySummary');
        const summaryCount = document.getElementById('summaryCount');
        if (!querySummary) return;

        const entityType = document.getElementById('entityTypeSelector')?.value || 'Case';
        const conditionCount = this.filters.length + this.collectionQueries.length;

        if (summaryCount) {
            summaryCount.textContent = conditionCount === 0 ? 'No conditions' : 
                                      conditionCount === 1 ? '1 condition' : 
                                      `${conditionCount} conditions`;
        }

        let summary = `<span class="kw">SELECT</span> `;
        if (this.selectedFields.length === 0) {
            summary += `<span class="val">*</span>`;
        } else {
            summary += `<span class="val">${this.selectedFields.length} field${this.selectedFields.length !== 1 ? 's' : ''}</span>`;
        }
        summary += ` <span class="kw">FROM</span> <span class="val">${entityType}</span>`;

        if (this.filters.length > 0) {
            summary += ` <span class="kw">WHERE</span> <span class="val">${this.filters.length} filter${this.filters.length !== 1 ? 's' : ''}</span>`;
        }

        if (this.collectionQueries.length > 0) {
            summary += ` <span class="kw">WITH</span> <span class="val">${this.collectionQueries.length} collection quer${this.collectionQueries.length !== 1 ? 'ies' : 'y'}</span>`;
        }

        querySummary.innerHTML = summary;
    },

    // Initialize with data passed from Razor page
    init(savedReport) {
        console.log('[ReportBuilder.init] Called with savedReport:', savedReport);

        try {
            // Store reportId if present
            if (savedReport && savedReport.reportId) {
                this.reportId = savedReport.reportId;
                console.log('[ReportBuilder.init] Stored reportId:', this.reportId);
            }

            // Check for auto-saved draft FIRST
            const draft = this.loadAutoSavedDraft();
            const hasSavedReport = savedReport && savedReport.reportId;

            if (draft && !hasSavedReport) {
                // Found a draft and no explicit saved report - offer to restore
                const draftDate = new Date(draft.timestamp);
                const timeAgo = this.getTimeAgo(draftDate);

                // Capture 'this' context for use in callbacks
                const self = this;

                ReportBuilderNotifications.confirm(
                    `Found an auto-saved draft from ${timeAgo} (${draftDate.toLocaleString()}).\n\nWould you like to restore this draft?`,
                    () => {
                        console.log('[ReportBuilder.init] Restoring auto-saved draft');
                        const draft = self.loadAutoSavedDraft();
                        if (draft) {
                            // Continue initialization with the draft as savedReport
                            self.continueInitialization(draft);
                        }
                    },
                    () => {
                        console.log('[ReportBuilder.init] User declined draft restore');
                        self.clearAutoSavedDraft();
                        // Continue initialization without draft
                        self.continueInitialization(null);
                    },
                    { title: 'Restore Draft?' }
                );
                return; // Exit early since restore is async
            }

            this.continueInitialization(savedReport);
        } catch (error) {
            console.error('[ReportBuilder.init] Error during initialization:', error);
            this.hideLoading();
            ReportBuilderNotifications.showToast('Error initializing report builder: ' + error.message, 'error', 5000);
        }
    },

    continueInitialization(savedReport) {
        try {
            if (savedReport) {
                this.showLoading('Loading Report', 'Restoring filters and collection queries...');
            }

            console.log('[ReportBuilder.init] Setting up drag/drop');
            this.setupDragDrop();

            console.log('[ReportBuilder.init] Setting up event listeners');
            this.setupEventListeners();

            console.log('[ReportBuilder.init] Setting up field search');
            this.setupFieldSearch();

            // Load available fields for the current entity type
            const entityType = document.getElementById('entityTypeSelector')?.value || 'Case';
            console.log('[ReportBuilder.init] Loading fields for entity type:', entityType);
            this.loadAvailableFields(entityType);

            if (savedReport) {
                console.log('[ReportBuilder.init] Loading saved report');
                this.loadSavedReport(savedReport);
            } else {
                // No saved report, hide loading immediately
                this.hideLoading();
            }

            // Initial status bar update
            this.updateStatusBar();

            // Start auto-save timer updates
            setInterval(() => {
                this.updateAutoSaveStatus();
            }, 10000); // Update every 10 seconds

            console.log('[ReportBuilder.init] Initialization complete');
        } catch (error) {
            console.error('[ReportBuilder.init] Error during initialization:', error);
            this.hideLoading();
            ReportBuilderNotifications.showToast('Error initializing report builder: ' + error.message, 'error', 5000);
        }
    },

    async loadAvailableFields(entityType) {
        console.log('[loadAvailableFields] Loading fields for:', entityType);

        try {
            // Load both recommended and all fields
            const [recommendedResponse, groupedResponse] = await Promise.all([
                fetch(`/api/reporting/fields/${entityType}/recommended`),
                fetch(`/api/reporting/fields/${entityType}/grouped`)
            ]);

            if (!recommendedResponse.ok || !groupedResponse.ok) {
                throw new Error(`Failed to load fields`);
            }

            const recommendedFields = await recommendedResponse.json();
            const fieldsByCategory = await groupedResponse.json();

            console.log('[loadAvailableFields] Loaded recommended:', recommendedFields.length);
            console.log('[loadAvailableFields] Loaded categories:', Object.keys(fieldsByCategory).length);

            this.renderFieldCategories(fieldsByCategory, recommendedFields);
        } catch (error) {
            console.error('[loadAvailableFields] Error:', error);
            const container = document.getElementById('fieldCategories');
            if (container) {
                container.innerHTML = `
                    <div class="rb-empty-state">
                        <div class="rb-empty-state-text">Failed to load fields: ${error.message}</div>
                    </div>
                `;
            }
        }
    },

    renderFieldCategories(fieldsByCategory, recommendedFields = []) {
        const container = document.getElementById('fieldCategories');
        if (!container) return;

        container.innerHTML = '';

        // Render Recommended Fields section first if we have any
        if (recommendedFields && recommendedFields.length > 0) {
            const recommendedHtml = `
                <div class="rb-field-category rb-recommended-category">
                    <div class="rb-field-category-header" data-category="category-recommended">
                        <span class="rb-field-category-icon">▼</span>
                        <span class="rb-field-category-name">⭐ Recommended</span>
                        <span class="rb-field-category-count">${recommendedFields.length}</span>
                    </div>
                    <div class="rb-field-category-body" id="category-recommended">
                        ${recommendedFields.map(field => `
                            <div class="rb-field-item field-item" 
                                 data-field-path="${field.fieldPath}"
                                 data-display-name="${field.displayName}"
                                 data-data-type="${field.dataType}"
                                 data-is-custom="${field.isCustomField || false}"
                                 data-custom-id="${field.customFieldDefinitionId || ''}"
                                 title="${field.fieldPath}">
                                <span class="rb-field-icon">${this.getFieldIcon(field.dataType)}</span>
                                <span class="rb-field-name">${field.displayName}</span>
                                <span class="rb-field-type">${field.dataType}</span>
                                <button class="rb-field-add-btn" title="Add field">+</button>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;
            container.insertAdjacentHTML('beforeend', recommendedHtml);

            // Add separator
            container.insertAdjacentHTML('beforeend', `
                <div class="rb-section-header" style="margin-top: 16px;">
                    <span>All Fields</span>
                </div>
            `);
        }

        // Render all other categories (collapsed by default)
        Object.entries(fieldsByCategory).forEach(([categoryName, fields], index) => {
            const categoryId = `category-${index}`;
            const isExpanded = false; // All categories collapsed by default now

            const categoryHtml = `
                <div class="rb-field-category">
                    <div class="rb-field-category-header" data-category="${categoryId}">
                        <span class="rb-field-category-icon">${isExpanded ? '▼' : '▶'}</span>
                        <span class="rb-field-category-name">${categoryName}</span>
                        <span class="rb-field-category-count">${fields.length}</span>
                    </div>
                    <div class="rb-field-category-body ${isExpanded ? '' : 'collapsed'}" id="${categoryId}">
                        ${fields.map(field => `
                            <div class="rb-field-item field-item" 
                                 data-field-path="${field.fieldPath}"
                                 data-display-name="${field.displayName}"
                                 data-data-type="${field.dataType}"
                                 data-is-custom="${field.isCustomField || false}"
                                 data-custom-id="${field.customFieldDefinitionId || ''}"
                                 title="${field.fieldPath}">
                                <span class="rb-field-icon">${this.getFieldIcon(field.dataType)}</span>
                                <span class="rb-field-name">${field.displayName}</span>
                                <span class="rb-field-type">${field.dataType}</span>
                                <button class="rb-field-add-btn" title="Add field">+</button>
                            </div>
                        `).join('')}
                    </div>
                </div>
            `;

            container.insertAdjacentHTML('beforeend', categoryHtml);
        });

        // Setup category toggle handlers
        container.querySelectorAll('.rb-field-category-header').forEach(header => {
            header.addEventListener('click', () => {
                const categoryId = header.dataset.category;
                const body = document.getElementById(categoryId);
                const icon = header.querySelector('.rb-field-category-icon');

                if (body) {
                    body.classList.toggle('collapsed');
                    icon.textContent = body.classList.contains('collapsed') ? '▶' : '▼';
                }
            });
        });

        // Setup + button click handlers
        container.querySelectorAll('.rb-field-add-btn').forEach(btn => {
            btn.addEventListener('click', (e) => {
                e.stopPropagation();
                const fieldItem = btn.closest('.rb-field-item');
                const field = {
                    fieldPath: fieldItem.dataset.fieldPath,
                    displayName: fieldItem.dataset.displayName,
                    dataType: fieldItem.dataset.dataType,
                    isCustom: fieldItem.dataset.isCustom === 'true',
                    customId: fieldItem.dataset.customId || null
                };
                this.addField(field);
            });
        });

        // Re-setup drag and drop for new field items
        this.setupDragDrop();
    },

    getFieldIcon(dataType) {
        const iconMap = {
            'String': '📝',
            'Int32': '#',
            'Int64': '#',
            'Decimal': '1.2',
            'Double': '1.2',
            'Boolean': '☑',
            'DateTime': '📅',
            'Guid': '🔑',
            'Enum': '⚙',
        };
        return iconMap[dataType] || '•';
    },

    loadSavedReport(savedReport) {
        // Store savedReport for use in restoreFilter
        this.currentSavedReport = savedReport;

        // If this is an autosaved draft (has timestamp), clear preview config to avoid stale state
        if (savedReport.timestamp) {
            this.savedPreviewConfiguration = null;
            console.log('[loadSavedReport] Draft detected - cleared preview configuration');
        } else {
            // Store pivot and preview configurations if present (server saved report)
            if (savedReport.pivotConfiguration) {
                this.savedPivotConfiguration = savedReport.pivotConfiguration;
                console.log('[loadSavedReport] Stored pivot configuration');
            }
            if (savedReport.previewConfiguration) {
                this.savedPreviewConfiguration = savedReport.previewConfiguration;
                console.log('[loadSavedReport] Stored preview configuration');
            }
        }

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

            // Identify unique group IDs
            const uniqueGroupIds = [...new Set(savedReport.filters.map(f => f.groupId).filter(id => id != null))];

            // Create filter groups if needed
            if (uniqueGroupIds.length > 0) {
                uniqueGroupIds.sort((a, b) => a - b); // Sort to maintain order
                uniqueGroupIds.forEach(groupId => {
                    // Ensure our internal counter is at least this high
                    if (groupId >= this.nextGroupId) {
                        this.nextGroupId = groupId + 1;
                    }

                    // Create the group HTML
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

                    // Restore group logic operator if saved
                    const filtersInGroup = savedReport.filters.filter(f => f.groupId === groupId);
                    if (filtersInGroup.length > 0 && filtersInGroup[0].groupLogicOperator) {
                        const groupLogic = filtersInGroup[0].groupLogicOperator;
                        const radioButton = document.getElementById(`group-${groupLogic.toLowerCase()}-${groupId}`);
                        if (radioButton) {
                            radioButton.checked = true;
                        }
                    }
                });
            }

            // Now restore each filter
            savedReport.filters.forEach((filter, index) => {
                if (filter.groupId) {
                    // Add filter to group
                    this.addFilterToGroup(filter.groupId);
                } else {
                    // Add standalone filter
                    this.addFilter();
                }

                // Restore filter details after a delay
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
        // Find the correct filter element - need to account for groups
        let filterEl;
        if (filter.groupId) {
            // Filter is in a group - find it within that group's container
            const groupContainer = document.querySelector(`.group-filters[data-group-id="${filter.groupId}"]`);
            if (groupContainer) {
                const filterElements = groupContainer.querySelectorAll('.rb-list-item');
                // Calculate the index within this group
                const filtersBeforeThisInGroup = this.currentSavedReport.filters
                    .slice(0, filterIndex)
                    .filter(f => f.groupId === filter.groupId).length;
                filterEl = filterElements[filtersBeforeThisInGroup];
            }
        } else {
            // Standalone filter
            const filterElements = document.querySelectorAll('#filters > .rb-list-item');
            const filtersBeforeThisStandalone = this.currentSavedReport.filters
                .slice(0, filterIndex)
                .filter(f => !f.groupId).length;
            filterEl = filterElements[filtersBeforeThisStandalone];
        }

        if (!filterEl) {
            console.warn(`[restoreFilter] Could not find filter element for index ${filterIndex}, groupId: ${filter.groupId}`);
            return;
        }

        const fieldSelect = filterEl.querySelector('.rb-filter-field');
        if (fieldSelect) {
            fieldSelect.value = filter.fieldPath;
            fieldSelect.dispatchEvent(new Event('change'));
        }

        // Restore logic operator radio button (AND/OR with next filter)
        if (filter.logicOperator) {
            setTimeout(() => {
                const logicRadio = filterEl.querySelector(`input[name^="logic-"][value="${filter.logicOperator}"]`);
                if (logicRadio) {
                    logicRadio.checked = true;
                }
            }, 100);
        }

        // Wait for field change to create the combined date dropdown or value input
        setTimeout(() => {
            const valueContainer = filterEl.querySelector('.rb-filter-value-container');
            const combinedSelect = valueContainer?.querySelector('.filter-date-combined');

            // If no combined dropdown exists, this is not a date field - restore as regular filter
            if (!combinedSelect) {
                const operatorSelect = filterEl.querySelector('.rb-filter-operator');
                if (operatorSelect) {
                    operatorSelect.value = filter.operator;
                    operatorSelect.dispatchEvent(new Event('change'));
                }

                setTimeout(() => {
                    const valueInput = filterEl.querySelector('.rb-filter-value');
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
                // Dispatch change event to ensure underlying operator and hidden fields are populated
                combinedSelect.dispatchEvent(new Event('change'));
            }
        }, 50);
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
        // Drag and drop removed - using + buttons instead
        console.log('[setupDragDrop] Skipped (using + buttons)');
    },

    setupEventListeners() {
        console.log('[setupEventListeners] Starting');

        const btnPreview = document.getElementById('btnPreview');
        const btnSave = document.getElementById('btnSave');
        const btnAddFilter = document.getElementById('btnAddFilter');
        const btnAddGroup = document.getElementById('btnAddGroup');
        const btnAddCollection = document.getElementById('btnAddCollection');
        const btnAddFilterInline = document.getElementById('btnAddFilterInline');
        const btnAddCollectionInline = document.getElementById('btnAddCollectionInline');
        const btnLoadDefaults = document.getElementById('btnLoadDefaults');
        const entityTypeSelector = document.getElementById('entityTypeSelector');

        if (btnPreview) {
            btnPreview.addEventListener('click', () => { 
                console.log('[btnPreview] Clicked'); 
                this.preview(); 
            });
            console.log('[setupEventListeners] btnPreview wired');
        } else {
            console.warn('[setupEventListeners] btnPreview not found');
        }

        if (btnSave) {
            btnSave.addEventListener('click', () => { 
                console.log('[btnSave] Clicked'); 
                this.save(); 
            });
            console.log('[setupEventListeners] btnSave wired');
        } else {
            console.warn('[setupEventListeners] btnSave not found');
        }

        if (btnAddFilter) {
            btnAddFilter.addEventListener('click', () => { 
                console.log('[btnAddFilter] Clicked'); 
                this.addFilter(); 
            });
            console.log('[setupEventListeners] btnAddFilter wired');
        } else {
            console.warn('[setupEventListeners] btnAddFilter not found');
        }

        if (btnAddGroup) {
            btnAddGroup.addEventListener('click', () => { 
                console.log('[btnAddGroup] Clicked'); 
                this.addFilterGroup(); 
            });
            console.log('[setupEventListeners] btnAddGroup wired');
        }

        if (btnAddCollection) {
            btnAddCollection.addEventListener('click', () => { 
                console.log('[btnAddCollection] Clicked'); 
                this.addCollectionQuery(); 
            });
            console.log('[setupEventListeners] btnAddCollection wired');
        } else {
            console.warn('[setupEventListeners] btnAddCollection not found');
        }

        // Wire up inline "Add" buttons for Filters and Collection Queries
        if (btnAddFilterInline) {
            btnAddFilterInline.addEventListener('click', () => { 
                console.log('[btnAddFilterInline] Clicked'); 
                this.addFilter(); 
            });
            console.log('[setupEventListeners] btnAddFilterInline wired');
        }

        if (btnAddCollectionInline) {
            btnAddCollectionInline.addEventListener('click', () => { 
                console.log('[btnAddCollectionInline] Clicked'); 
                this.addCollectionQuery(); 
            });
            console.log('[setupEventListeners] btnAddCollectionInline wired');
        }

        if (btnLoadDefaults) {
            btnLoadDefaults.addEventListener('click', () => { 
                console.log('[btnLoadDefaults] Clicked'); 
                this.loadDefaultFields(); 
            });
            console.log('[setupEventListeners] btnLoadDefaults wired');
        } else {
            console.warn('[setupEventListeners] btnLoadDefaults not found');
        }

        const btnClearAll = document.getElementById('btnClearAll');
        if (btnClearAll) {
            btnClearAll.addEventListener('click', () => { 
                console.log('[btnClearAll] Clicked'); 
                this.clearAll(); 
            });
            console.log('[setupEventListeners] btnClearAll wired');
        } else {
            console.warn('[setupEventListeners] btnClearAll not found');
        }

        if (entityTypeSelector) {
            entityTypeSelector.addEventListener('change', (e) => {
                const reportId = e.target.dataset.reportId;

                if (reportId && reportId !== 'null') {
                    const newValue = e.target.value;
                    const originalValue = e.target.dataset.originalValue;

                    ReportBuilderNotifications.confirm(
                        'Changing the entity type will clear all fields, filters, and collection queries.',
                        () => {
                            window.location.href = '?entityType=' + newValue;
                        },
                        () => {
                            e.target.value = originalValue;
                        },
                        { 
                            title: 'Change Entity Type?',
                            danger: true,
                            confirmText: 'Change & Clear All'
                        }
                    );
                    return;
                }

                window.location.href = '?entityType=' + e.target.value;
            });
            console.log('[setupEventListeners] entityTypeSelector wired');
        }

        console.log('[setupEventListeners] Complete');
    },

    setupFieldSearch() {
        const searchInput = document.getElementById('fieldSearch');
        if (!searchInput) {
            console.warn('[setupFieldSearch] fieldSearch input not found');
            return;
        }

        searchInput.addEventListener('input', (e) => {
            const search = e.target.value.toLowerCase();
            document.querySelectorAll('.field-item').forEach(item => {
                const text = item.textContent.toLowerCase();
                item.style.display = text.includes(search) ? '' : 'none';
            });
        });

        console.log('[setupFieldSearch] Field search initialized');
    },

    addField(field) {
        if (this.selectedFields.some(f => f.fieldPath === field.fieldPath)) {
            console.log('[addField] Field already added:', field.fieldPath);
            return;
        }

        this.selectedFields.push(field);
        this.renderSelectedFields();
    },
    
    removeField(fieldPath) {
        this.selectedFields = this.selectedFields.filter(f => f.fieldPath !== fieldPath);
        this.renderSelectedFields();
        this.scheduleAutoSave();
    },

    renderSelectedFields() {
        const container = document.getElementById('selectedFields');

        if (this.selectedFields.length === 0) {
            container.innerHTML = `
                <div class="rb-empty-state">
                    <div class="rb-empty-state-text">No fields selected. Use Add Fields button to select fields.</div>
                </div>
            `;
            return;
        }

        container.innerHTML = this.selectedFields.map((field, index) => `
            <div class="rb-list-item" data-index="${index}" data-field-path="${field.fieldPath}">
                <div class="rb-item-content">
                    <div class="rb-item-label">${field.displayName}</div>
                    <div class="rb-item-detail">${field.fieldPath} · ${field.dataType}</div>
                </div>
                <button class="rb-item-action" onclick="ReportBuilder.removeField('${field.fieldPath}')" title="Remove field">×</button>
            </div>
        `).join('');

        this.updateStatusBar();
        this.scheduleAutoSave();
    },

    setupFieldReordering() {
        // Reordering removed for simplicity
        console.log('[setupFieldReordering] Skipped');
    },

    // ==================== FILTER FUNCTIONS ====================

    addFilter() {
        const filterId = this.nextFilterId++;
        const container = document.getElementById('filters');

        if (this.selectedFields.length === 0) {
            ReportBuilderNotifications.showToast('Please add fields before creating filters', 'warning');
            return;
        }

        const emptyState = container.querySelector('.rb-empty-state');
        if (emptyState) {
            container.innerHTML = '';
        }

        const filterHtml = `
            <div class="rb-list-item" id="filter-${filterId}">
                <div class="rb-filter-row">
                    <select class="rb-filter-field" data-filter-id="${filterId}">
                        <option value="">Select field...</option>
                        ${this.selectedFields.map(f => `<option value="${f.fieldPath}" data-type="${f.dataType}">${f.displayName}</option>`).join('')}
                    </select>
                    <select class="rb-filter-operator" id="operator-${filterId}">
                        <option value="Equals">=</option>
                        <option value="NotEquals">!=</option>
                        <option value="Contains">contains</option>
                        <option value="StartsWith">starts with</option>
                        <option value="GreaterThan">&gt;</option>
                        <option value="LessThan">&lt;</option>
                        <option value="GreaterThanOrEqual">&gt;=</option>
                        <option value="LessThanOrEqual">&lt;=</option>
                        <option value="IsNull">is null</option>
                        <option value="IsNotNull">is not null</option>
                    </select>
                    <input type="text" class="rb-filter-value" id="value-${filterId}" placeholder="Value">
                    <button class="rb-item-action" onclick="ReportBuilder.removeFilter(${filterId})" title="Remove filter">×</button>
                </div>
            </div>
        `;

        container.insertAdjacentHTML('beforeend', filterHtml);
        this.filters.push({ id: filterId });
        this.setupSmartFilter(filterId);
        this.updateStatusBar();
        this.scheduleAutoSave();
    },

    removeFilter(filterId) {
        document.getElementById(`filter-${filterId}`)?.remove();
        this.filters = this.filters.filter(f => f.id !== filterId);

        const container = document.getElementById('filters');
        if (container.children.length === 0) {
            container.innerHTML = `
                <div class="rb-empty-state">
                    <div class="rb-empty-state-text">No filters applied</div>
                </div>
            `;
        }
        this.updateStatusBar();
        this.scheduleAutoSave();
    },

    setupSmartFilter(filterId) {
        const fieldSelect = document.querySelector(`.rb-filter-field[data-filter-id="${filterId}"]`);
        const operatorSelect = document.getElementById(`operator-${filterId}`);
        const valueInput = document.getElementById(`value-${filterId}`);

        if (!fieldSelect || !operatorSelect || !valueInput) {
            console.warn(`[setupSmartFilter] Elements not found for filter ${filterId}`, { 
                fieldSelect: !!fieldSelect, 
                operatorSelect: !!operatorSelect, 
                valueInput: !!valueInput 
            });
            return;
        }

        console.log(`[setupSmartFilter] Setting up filter ${filterId}`);

        fieldSelect.addEventListener('change', (e) => {
            const selectedOption = e.target.options[e.target.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            console.log('[MainFilter] Field changed to', e.target.value, 'DataType:', dataType, 'Option dataset:', selectedOption.dataset);

            this.updateOperators(operatorSelect, dataType);
            this.updateValueInput(valueInput, dataType, operatorSelect.value);
        });

        operatorSelect.addEventListener('change', (e) => {
            const selectedOption = fieldSelect.options[fieldSelect.selectedIndex];
            const dataType = selectedOption.dataset.type || 'String';

            console.log('[MainFilter] Operator changed to', e.target.value, 'DataType:', dataType);

            this.updateValueInput(valueInput, dataType, e.target.value);
        });
    },

    updateOperators(selectElement, dataType, hideOperatorForDates = true) {
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
            'DateTime': this.getDateOperators(),
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

        console.log(`[updateOperators] DataType: ${dataType}, Normalized: ${normalizedType}, Operators: ${operators.length}, hideOperatorForDates: ${hideOperatorForDates}`);
    },

    getDateOperators() {
        return [
            { value: 'Equals', label: 'is' },
            { value: 'NotEquals', label: 'is not' }
        ];
    },

    updateValueInput(inputElement, dataType, operator) {
        // Detect context: check if it's a sub-filter or main filter
        const isSubFilter = inputElement.closest('.rb-subfilter-row') !== null;

        // For sub-filters, use the container wrapper; for main filters, replace the input inline
        let valueContainer;
        if (isSubFilter) {
            valueContainer = inputElement.closest('.rb-subfilter-value-container');
        } else {
            // For main filters in the new inline structure, we'll replace the input element itself
            // But we need to find the parent to support complex UI like date pickers
            const filterRow = inputElement.closest('.rb-filter-row');
            if (filterRow) {
                // For inline main filters, we need a wrapper approach too
                // Check if we already have a container, otherwise create one
                valueContainer = filterRow.querySelector('.rb-filter-value-container');
                if (!valueContainer) {
                    // Wrap the existing input in a container
                    const wrapper = document.createElement('div');
                    wrapper.className = 'rb-filter-value-container';
                    inputElement.parentNode.insertBefore(wrapper, inputElement);
                    wrapper.appendChild(inputElement);
                    valueContainer = wrapper;
                }
            }
        }

        if (!valueContainer) {
            console.warn('[updateValueInput] No value container found', { isSubFilter, inputElement });
            return;
        }

        const uniqueId = inputElement.id || `filter-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

        console.log(`[updateValueInput] DataType: ${dataType}, Operator: ${operator}, isSubFilter: ${isSubFilter}`);

        // Find operator select in the same row for visibility control
        const filterRow = valueContainer.closest('.rb-filter-row, .rb-subfilter-row');
        const operatorSelect = filterRow?.querySelector('.rb-filter-operator, .rb-subfilter-operator');

        if (operator === 'Between') {
            const inputType = dataType.includes('Date') ? 'date' : 'number';
            const cssClass = isSubFilter ? 'form-control form-control-sm rb-subfilter-value' : 'rb-filter-value';
            valueContainer.innerHTML = `
                <input type="${inputType}" class="${cssClass} mb-1" 
                       placeholder="From" style="width: 100%;">
                <input type="${inputType}" class="${cssClass.replace('rb-subfilter-value', 'rb-subfilter-value-end').replace('rb-filter-value', 'rb-filter-value-end')}" 
                       placeholder="To" style="width: 100%;">
            `;
            return;
        }

        if (operator === 'InLast' || operator === 'InNext') {
            const cssClass = isSubFilter ? 'form-control form-control-sm rb-subfilter-value' : 'rb-filter-value';
            valueContainer.innerHTML = `
                <input type="number" class="${cssClass}" 
                       placeholder="Number of days" min="1" style="width: 100%;">
            `;
            return;
        }

        if (operator === 'IsNull' || operator === 'IsNotNull' || operator === 'IsEmpty' || operator === 'IsNotEmpty') {
            const cssClass = isSubFilter ? 'form-control form-control-sm rb-subfilter-value' : 'rb-filter-value';
            valueContainer.innerHTML = `
                <input type="text" class="${cssClass}" 
                       placeholder="(no value needed)" disabled style="width: 100%;">
            `;
            return;
        }

        const normalizedType = dataType.includes('Date') ? 'date' :
                               dataType.includes('Int') || dataType.includes('Number') ? 'number' :
                               dataType.includes('Bool') ? 'checkbox' : 'text';

        if (normalizedType === 'checkbox') {
            // Show operator for both main and sub filters
            if (operatorSelect) operatorSelect.style.display = '';

            const cssClass = isSubFilter ? 'form-check-input rb-subfilter-value' : 'form-check-input rb-filter-value';
            valueContainer.innerHTML = `
                <div class="form-check mt-2">
                    <input type="checkbox" class="${cssClass}" id="value-${uniqueId}">
                    <label class="form-check-label" for="value-${uniqueId}">True</label>
                </div>
            `;
        } else if (normalizedType === 'date') {
            // For main filters, hide operator; for sub-filters, keep it visible
            if (operatorSelect && !isSubFilter) {
                operatorSelect.style.display = 'none';
            } else if (operatorSelect) {
                operatorSelect.style.display = '';
            }

            valueContainer.innerHTML = this.getDateFilterHTML(uniqueId);

            setTimeout(() => {
                this.setupDateFilterListeners(valueContainer, uniqueId);
            }, 0);
        } else if (normalizedType === 'number') {
            // Show operator for both main and sub filters
            if (operatorSelect) operatorSelect.style.display = '';

            const cssClass = isSubFilter ? 'form-control form-control-sm rb-subfilter-value' : 'rb-filter-value';
            valueContainer.innerHTML = `
                <input type="number" class="${cssClass}" 
                       placeholder="Enter number" step="any" style="width: 100%;">
            `;
        } else {
            // Show operator for both main and sub filters
            if (operatorSelect) operatorSelect.style.display = '';

            const cssClass = isSubFilter ? 'form-control form-control-sm rb-subfilter-value' : 'rb-filter-value';
            valueContainer.innerHTML = `
                <input type="text" class="${cssClass}" 
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
            ReportBuilderNotifications.showToast('Please add fields before creating filters', 'warning');
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

    clearAll() {
        console.log('[clearAll] Clearing all fields, filters, and collection queries');

        // Capture 'this' context for use in callback
        const self = this;

        ReportBuilderNotifications.confirm(
            'This will clear all selected fields, filters, and collection queries.',
            () => {
                // Clear selected fields
                self.selectedFields = [];

                // Clear filters
                self.filters = [];
                const filtersContainer = document.getElementById('filters');
                if (filtersContainer) {
                    filtersContainer.innerHTML = `
                        <div class="rb-empty-state">
                            <div class="rb-empty-state-text">No filters. Use Add Filter button to create filters.</div>
                        </div>
                    `;
                }

                // Clear collection queries
                self.collectionQueries = [];
                const collectionContainer = document.getElementById('collectionQueries');
                if (collectionContainer) {
                    collectionContainer.innerHTML = `
                        <div class="rb-empty-state">
                            <div class="rb-empty-state-text">No collection queries. Use Add Collection Query button to filter related data.</div>
                        </div>
                    `;
                }

                // Render selected fields
                self.renderSelectedFields();
                self.updateStatusBar();
                self.scheduleAutoSave();

                ReportBuilderNotifications.showToast('All fields, filters, and queries cleared', 'success');
            },
            null,
            { 
                title: 'Clear All?',
                danger: true,
                confirmText: 'Clear All'
            }
        );
    },

    // Smart filtering functions for collection sub-filters
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

    getDateFilterHTML(uniqueId) {
        return `
            <select class="form-control form-control-sm filter-date-combined" style="width: 100%;">
                <option value="">Select date condition...</option>
                <optgroup label="Specific Date">
                    <option value="static">Pick a specific date...</option>
                    <option value="Equals|Today">today</option>
                    <option value="Equals|Yesterday">yesterday</option>
                    <option value="Equals|Tomorrow">tomorrow</option>
                </optgroup>
                <optgroup label="Relative Dates">
                    <option value="Equals|StartOfWeek">start of this week</option>
                    <option value="Equals|EndOfWeek">end of this week</option>
                    <option value="Equals|StartOfMonth">start of this month</option>
                    <option value="Equals|EndOfMonth">end of this month</option>
                </optgroup>
                <optgroup label="Within Last...">
                    <option value="InLast|7">within the last 7 days</option>
                    <option value="InLast|30">within the last 30 days</option>
                    <option value="InLast|90">within the last 90 days</option>
                    <option value="InLast|180">within the last 180 days</option>
                    <option value="InLast|365">within the last 365 days</option>
                </optgroup>
                <optgroup label="More Than...Ago">
                    <option value="LessThan|Past7Days">more than 7 days ago</option>
                    <option value="LessThan|Past30Days">more than 30 days ago</option>
                    <option value="LessThan|Past3Months">more than 3 months ago</option>
                    <option value="LessThan|Past6Months">more than 6 months ago</option>
                    <option value="LessThan|Past12Months">more than 12 months ago</option>
                </optgroup>
                <optgroup label="Within Next...">
                    <option value="InNext|7">within the next 7 days</option>
                    <option value="InNext|30">within the next 30 days</option>
                    <option value="InNext|90">within the next 90 days</option>
                </optgroup>
                <optgroup label="More Than...Away">
                    <option value="GreaterThan|Next7Days">more than 7 days away</option>
                    <option value="GreaterThan|Next30Days">more than 30 days away</option>
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
