/**
 * Timeline Entry - Main orchestrator for natural language timeline entry interface
 */
class TimelineEntry {
    constructor(caseId) {
        this.caseId = caseId;
        this.timelineData = null;
        this.currentEntries = [];
        this.unsavedChanges = false;
        this.autocomplete = null;
        this.map = null;
        this.entryEntities = {}; // Store entities by entryId
        this.entryRelationships = {}; // Store relationships by entry ID
        this.entityGroups = {}; // Store entity groups (e.g., #Siblings, #WorkTeam)
        this.patientLocation = null; // Patient address for location biasing
        this.quickAdd = null; // Entity quick-add with .. trigger
        this.syntaxParser = null; // Relationship syntax parser
        this.autoSaveTimer = null; // Auto-save debounce timer (deprecated - using periodic saves)
        this.periodicSaveInterval = null; // Periodic auto-save interval
        this.isSaving = false; // Prevent concurrent saves
        this.relationshipGroups = {}; // Store relationship groups by entryId

        // Enum mapping for EntityType (matches C# enum values)
        this.entityTypeMap = {
            1: 'Person',
            2: 'Location',
            3: 'Event',
            4: 'Transport',
            5: 'DateTime',
            6: 'Duration',
            7: 'Activity'
        };
    }

    async init() {
        console.log('Initializing Timeline Entry for case:', this.caseId);

        // Initialize relationship syntax parser FIRST (before loading timeline)
        // This is needed for inline group expansion during page load
        this.syntaxParser = new RelationshipSyntaxParser();

        // Load patient location for biasing place searches
        await this.loadPatientLocation();

        // Load existing timeline data (uses syntaxParser for inline group expansion)
        await this.loadTimeline();

        // Initialize components
        this.initializeEventListeners();
        this.initializeMap();

        // Initialize entity quick-add with .. trigger (manual-only entity creation)
        this.quickAdd = new EntityQuickAdd(this);
        await this.quickAdd.init();

        // Attach quickAdd to all existing textareas (loaded from database)
        const existingTextareas = document.querySelectorAll('.narrative-textarea');
        existingTextareas.forEach(textarea => {
            this.quickAdd.attach(textarea);
        });

        // Start periodic auto-save (every 30 seconds)
        this.startPeriodicAutoSave();

        // Load groups from storage
        this.loadGroups();

        // Warn before leaving if there are unsaved changes
        window.addEventListener('beforeunload', async (e) => {
            if (this.unsavedChanges) {
                // Save one last time before leaving
                await this.saveDraft();
                e.preventDefault();
                e.returnValue = '';
            }
        });
    }

    /**
     * Load saved groups from sessionStorage
     */
    loadGroups() {
        try {
            const stored = sessionStorage.getItem(`groups_${this.caseId}`);
            this.entityGroups = stored ? JSON.parse(stored) : {};
            console.log('[TimelineEntry] Loaded groups:', this.entityGroups);
        } catch (error) {
            console.warn('[TimelineEntry] Failed to load groups:', error);
            this.entityGroups = {};
        }
    }

    /**
     * Save groups to sessionStorage
     */
    saveGroups() {
        try {
            sessionStorage.setItem(`groups_${this.caseId}`, JSON.stringify(this.entityGroups));
            console.log('[TimelineEntry] Saved groups:', this.entityGroups);
        } catch (error) {
            console.error('[TimelineEntry] Failed to save groups:', error);
        }
    }

    /**
     * Create a new entity group
     * @param {string} name - Group name (e.g., "Siblings", "Co-workers")
     * @param {Array<string>} entityIds - Array of entity IDs
     */
    createGroup(name, entityIds) {
        if (!name || entityIds.length === 0) return null;

        const groupId = `group_${Date.now()}`;
        this.entityGroups[groupId] = {
            id: groupId,
            name: name,
            entityIds: entityIds,
            created: new Date().toISOString()
        };

        this.saveGroups();
        this.updateGroupsList();

        console.log('[TimelineEntry] Created group:', this.entityGroups[groupId]);
        return groupId;
    }

    /**
     * Delete a group
     * @param {string} groupId - Group ID to delete
     */
    deleteGroup(groupId) {
        if (this.entityGroups[groupId]) {
            delete this.entityGroups[groupId];
            this.saveGroups();
            this.updateGroupsList();
        }
    }

    /**
     * Get entities from a group by name
     * @param {string} groupName - Group name (e.g., "Siblings")
     * @returns {Array} Array of entity objects
     */
    getGroupEntities(groupName) {
        const group = Object.values(this.entityGroups).find(g => 
            g.name.toLowerCase() === groupName.toLowerCase()
        );

        if (!group) return [];

        // Collect entities from all entries
        const entities = [];
        Object.values(this.entryEntities).forEach(entryEnts => {
            entryEnts.forEach(ent => {
                if (group.entityIds.includes(ent.id)) {
                    entities.push(ent);
                }
            });
        });

        return entities;
    }

    /**
     * Update the groups list UI
     */
    updateGroupsList() {
        const container = document.getElementById('groupsList');
        if (!container) return;

        const groups = Object.values(this.entityGroups);

        if (groups.length === 0) {
            container.innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="bi bi-people fs-3 d-block mb-2"></i>
                    No groups created yet
                </div>
            `;
            return;
        }

        // Build group cards
        let html = '<div class="groups-list-content">';
        groups.forEach(group => {
            html += `
                <div class="group-card mb-2" data-group-id="${group.id}">
                    <div class="d-flex justify-content-between align-items-center">
                        <div>
                            <strong style="color: var(--forest);">#${this.escapeHtml(group.name)}</strong>
                            <br>
                            <small class="text-muted">
                                <i class="bi bi-people-fill"></i> ${group.entityIds.length} member${group.entityIds.length !== 1 ? 's' : ''}
                            </small>
                        </div>
                        <button type="button" class="btn btn-sm btn-outline-danger" 
                                onclick="window.timelineEntry.deleteGroup('${group.id}')"
                                title="Delete group">
                            <i class="bi bi-trash"></i>
                        </button>
                    </div>
                </div>
            `;
        });
        html += '</div>';

        container.innerHTML = html;
    }

    /**
     * Show dialog to create a new group
     */
    showCreateGroupDialog() {
        // Collect all entities from all entries
        const allEntities = [];
        Object.entries(this.entryEntities).forEach(([entryId, entities]) => {
            entities.forEach(entity => {
                if (!allEntities.some(e => e.id === entity.id)) {
                    allEntities.push({ ...entity, entryId });
                }
            });
        });

        if (allEntities.length === 0) {
            alert('No entities available. Please add entities to your timeline first.');
            return;
        }

        // Create dialog HTML
        const dialogHtml = `
            <div class="modal fade" id="createGroupModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content" style="border: 2px solid var(--hairline); border-radius: 12px;">
                        <div class="modal-header" style="background: var(--bone); border-bottom: 2px solid var(--hairline);">
                            <h5 class="modal-title" style="font-weight: 600; color: var(--forest);">
                                <i class="bi bi-people-fill me-2"></i>Create Entity Group
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body" style="font-family: var(--f-sans);">
                            <div class="mb-3">
                                <label for="groupName" class="form-label fw-bold">Group Name</label>
                                <div class="input-group">
                                    <span class="input-group-text">#</span>
                                    <input type="text" class="form-control" id="groupName" 
                                           placeholder="e.g., Siblings, Coworkers, Family">
                                </div>
                                <small class="text-muted">Use this name to reference the group (e.g., @#Siblings)</small>
                            </div>
                            <div class="mb-3">
                                <label class="form-label fw-bold">Select Members</label>
                                <div id="groupMembersList" style="max-height: 300px; overflow-y: auto; border: 1px solid var(--hairline); border-radius: 6px; padding: 0.75rem;">
                                    ${this.renderGroupMembersCheckboxes(allEntities)}
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer" style="background: var(--bone); border-top: 1px solid var(--hairline);">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Cancel</button>
                            <button type="button" class="btn btn-primary" onclick="window.timelineEntry.confirmCreateGroup()">
                                <i class="bi bi-check2-circle"></i> Create Group
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;

        // Remove old modal if exists
        const oldModal = document.getElementById('createGroupModal');
        if (oldModal) oldModal.remove();

        // Add to document
        document.body.insertAdjacentHTML('beforeend', dialogHtml);

        // Show modal
        const modal = new bootstrap.Modal(document.getElementById('createGroupModal'));
        modal.show();

        // Focus on name input
        setTimeout(() => {
            document.getElementById('groupName')?.focus();
        }, 300);
    }

    /**
     * Render checkboxes for group member selection
     */
    renderGroupMembersCheckboxes(entities) {
        let html = '';

        // Group by type
        const groupedByType = {};
        entities.forEach(entity => {
            const typeName = this.entityTypeMap[entity.entityType] || 'Unknown';
            if (!groupedByType[typeName]) {
                groupedByType[typeName] = [];
            }
            groupedByType[typeName].push(entity);
        });

        // Render each type group
        const typeOrder = ['Person', 'Location', 'Event', 'Transport'];
        typeOrder.forEach(typeName => {
            if (!groupedByType[typeName] || groupedByType[typeName].length === 0) return;

            const icon = this.getIconForType(typeName);
            html += `
                <div class="mb-3">
                    <h6 class="mb-2" style="color: var(--forest); font-size: 14px;">
                        ${icon} ${typeName}s
                    </h6>
            `;

            groupedByType[typeName].forEach(entity => {
                const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                html += `
                    <div class="form-check">
                        <input class="form-check-input group-member-checkbox" type="checkbox" 
                               value="${entity.id}" id="member_${entity.id}">
                        <label class="form-check-label" for="member_${entity.id}">
                            ${this.escapeHtml(displayText)}
                        </label>
                    </div>
                `;
            });

            html += `</div>`;
        });

        return html;
    }

    /**
     * Confirm and create the group
     */
    confirmCreateGroup() {
        const nameInput = document.getElementById('groupName');
        const name = nameInput?.value.trim();

        if (!name) {
            alert('Please enter a group name.');
            nameInput?.focus();
            return;
        }

        // Get selected entity IDs
        const checkboxes = document.querySelectorAll('.group-member-checkbox:checked');
        const entityIds = Array.from(checkboxes).map(cb => cb.value);

        if (entityIds.length === 0) {
            alert('Please select at least one member for the group.');
            return;
        }

        // Create the group
        const groupId = this.createGroup(name, entityIds);

        if (groupId) {
            // Close modal
            const modal = bootstrap.Modal.getInstance(document.getElementById('createGroupModal'));
            modal?.hide();

            // Show success message
            this.showSuccess(`Group #${name} created with ${entityIds.length} member${entityIds.length !== 1 ? 's' : ''}`);
        }
    }

    initializeEventListeners() {
        // Add day button
        document.getElementById('addDayBtn').addEventListener('click', () => {
            this.addDayBlock();
        });

        // Save draft button
        document.getElementById('saveDraftBtn').addEventListener('click', async () => {
            await this.saveDraft();
        });

        // Copy previous day button
        document.getElementById('copyPreviousDayBtn').addEventListener('click', () => {
            this.copyPreviousDay();
        });

        // Save and review button
        document.getElementById('saveAndReviewBtn').addEventListener('click', async () => {
            await this.saveAndReview();
        });
    }

    initializeMap() {
        // Initialize map visualization
        if (window.MapVisualization) {
            this.map = new MapVisualization('mapContainer');
        }
    }

    async loadTimeline() {
        try {
            const response = await fetch(`/api/timeline/${this.caseId}`);
            if (!response.ok) {
                throw new Error('Failed to load timeline');
            }

            this.timelineData = await response.json();
            console.log('Loaded timeline:', this.timelineData);

            // Render existing entries
            if (this.timelineData.entries && this.timelineData.entries.length > 0) {
                await this.renderExistingEntries();
            }

            this.unsavedChanges = false;
        } catch (error) {
            console.error('Error loading timeline:', error);
            this.showError('Failed to load timeline data');
        }
    }

    async loadPatientLocation() {
        try {
            const response = await fetch(`/api/patients/${this.caseId}/address`);
            if (response.ok) {
                this.patientLocation = await response.json();
                console.log('Patient location loaded for bias:', this.patientLocation);
            }
        } catch (error) {
            console.warn('Could not load patient location for bias:', error);
            // Not critical - continue without location bias
        }
    }

    async renderExistingEntries() {
        const container = document.getElementById('timelineContainer');
        container.innerHTML = '';

        const sortedEntries = this.timelineData.entries
            .sort((a, b) => new Date(a.entryDate) - new Date(b.entryDate));

        for (const entry of sortedEntries) {
            await this.addDayBlock(new Date(entry.entryDate), entry);
        }

        this.updateEntitySummary();
        this.updateGroupsList();
        this.updateMapPins();
    }

    async addDayBlock(date = null, existingEntry = null) {
        const container = document.getElementById('timelineContainer');
        
        // Clear empty state message if present
        if (container.querySelector('.text-muted.text-center')) {
            container.innerHTML = '';
        }

        const entryDate = date || new Date();
        const entryId = existingEntry?.id || this.generateId();
        
        const dayBlock = document.createElement('div');
        dayBlock.className = 'timeline-day-block';
        dayBlock.dataset.entryId = entryId;
        dayBlock.dataset.entryDate = entryDate.toISOString();

        dayBlock.innerHTML = `
            <div class="timeline-day-header">
                <div class="timeline-day-date">
                    <i class="bi bi-calendar3"></i>
                    <input type="date" 
                           class="form-control form-control-sm" 
                           style="width: auto; border: none; background: transparent; font-weight: 600;"
                           value="${this.formatDateForInput(entryDate)}"
                           data-entry-id="${entryId}">
                </div>
                <div class="timeline-day-actions">
                    <button type="button" class="btn btn-sm btn-outline-danger" onclick="window.timelineEntry.removeDayBlock('${entryId}')">
                        <i class="bi bi-trash"></i>
                    </button>
                </div>
            </div>
            <div class="timeline-day-body">
                <div class="narrative-highlight-container">
                    <textarea class="narrative-textarea" 
                              data-entry-id="${entryId}" 
                              placeholder="e.g., went to work with mum, then took the 557 bus to Coles Salisbury around 3pm"
                              rows="4">${existingEntry?.narrativeText || ''}</textarea>
                    <div class="narrative-highlight-layer" data-entry-id="${entryId}"></div>
                </div>
                
                <div class="mt-2" id="status-${entryId}"></div>
                
                ${existingEntry?.uncertaintyMarkers && existingEntry.uncertaintyMarkers.length > 0 ? `
                    <div class="mt-2">
                        ${existingEntry.uncertaintyMarkers.map(marker => 
                            `<span class="uncertainty-marker"><i class="bi bi-question-circle"></i> ${marker}</span>`
                        ).join('')}
                    </div>
                ` : ''}
                
                ${existingEntry?.protectiveMeasures && existingEntry.protectiveMeasures.length > 0 ? `
                    <div class="mt-2">
                        ${existingEntry.protectiveMeasures.map(measure => 
                            `<span class="protective-measure-tag"><i class="bi bi-shield-check"></i> ${measure}</span>`
                        ).join('')}
                    </div>
                ` : ''}
                
                ${existingEntry?.isMemoryGap ? `
                    <div class="memory-gap-indicator mt-2">
                        <i class="bi bi-exclamation-triangle"></i>
                        <strong>Memory Gap:</strong> Patient unable to recall details for this date.
                    </div>
                ` : ''}
            </div>
        `;

        container.appendChild(dayBlock);

        // Attach event listeners
        const textarea = dayBlock.querySelector('.narrative-textarea');
        const highlightLayer = dayBlock.querySelector('.narrative-highlight-layer');

        // Helper function to sync highlight layer position with textarea scroll
        const syncHighlightPosition = () => {
            if (highlightLayer && textarea) {
                highlightLayer.style.transform = `translate(-${textarea.scrollLeft}px, -${textarea.scrollTop}px)`;
            }
        };

        textarea.addEventListener('input', (e) => this.handleTextInput(e, entryId));
        textarea.addEventListener('keydown', (e) => this.handleKeyDown(e, entryId));

        // Sync highlight layer scroll with textarea scroll using transform
        textarea.addEventListener('scroll', syncHighlightPosition);

        const dateInput = dayBlock.querySelector('input[type="date"]');
        dateInput.addEventListener('change', (e) => this.handleDateChange(e, entryId));

        // If there's existing entry data with entities, process inline groups FIRST, then highlight
        if (existingEntry && existingEntry.narrativeText) {
            if (existingEntry.entities && existingEntry.entities.length > 0) {
                // Repair entity positions if they don't match the actual text
                // This fixes data corruption from previous inline group expansion bugs
                this.repairEntityPositions(existingEntry.entities, existingEntry.narrativeText);

                // Enrich loaded entities with stable IDs for proper deduplication
                // This ensures entities loaded from JSON have the same stable IDs as newly created ones
                existingEntry.entities.forEach(entity => {
                    this.enrichEntityWithStableIds(entity);
                });

                this.entryEntities[entryId] = existingEntry.entities;
            }
            if (existingEntry.relationships && existingEntry.relationships.length > 0) {
                this.entryRelationships[entryId] = existingEntry.relationships;
            }

            // Re-parse relationships from saved text
            // NOTE: We do NOT expand inline groups on load - they should already be expanded in saved data
            // Inline group expansion only happens when user first types #GroupName(...)
            if (this.syntaxParser) {
                // Skip inline group processing for loaded data - pass a flag
                await this.parseAndCreateRelationships(entryId, existingEntry.narrativeText, true);
            } else {
                console.warn('[addDayBlock] syntaxParser not available, skipping parseAndCreateRelationships');
            }

            // NOW highlight with the expanded text and adjusted positions
            if (existingEntry.entities && existingEntry.entities.length > 0) {
                // Get the potentially-updated text from the textarea (after group expansion)
                const currentText = textarea.value;
                this.highlightEntities(entryId, currentText, this.entryEntities[entryId]);

                // Initialize highlight layer position to match textarea scroll
                // Use requestAnimationFrame to ensure DOM is fully rendered
                requestAnimationFrame(() => {
                    syncHighlightPosition();
                });
            }
        }

        // Attach entity quick-add to textarea (AFTER highlighting so entities are available for context)
        if (this.quickAdd) {
            this.quickAdd.attach(textarea);
        }

        // Enable copy previous day button if we have entries
        const copyBtn = document.getElementById('copyPreviousDayBtn');
        if (container.querySelectorAll('.timeline-day-block').length > 1) {
            copyBtn.disabled = false;
        }

        this.unsavedChanges = true;
    }

    removeDayBlock(entryId) {
        if (confirm('Are you sure you want to remove this day entry?')) {
            const block = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
            if (block) {
                block.remove();
                this.unsavedChanges = true;
                this.updateEntitySummary();
                this.updateMapPins();
                
                // Check if we should show empty state
                const container = document.getElementById('timelineContainer');
                if (container.querySelectorAll('.timeline-day-block').length === 0) {
                    container.innerHTML = `
                        <p class="text-muted text-center py-4">
                            <i class="bi bi-calendar-plus fs-2 d-block mb-2"></i>
                            Click "Add Day" to start entering timeline data
                        </p>
                    `;
                    document.getElementById('copyPreviousDayBtn').disabled = true;
                }
            }
        }
    }

    async handleTextInput(event, entryId) {
        const text = event.target.value;
        this.unsavedChanges = true;

        // If text is empty or only whitespace, clear entities and highlights
        if (!text || text.trim() === '') {
            this.entryEntities[entryId] = [];
            this.entryRelationships[entryId] = [];

            const highlightLayer = document.querySelector(`.narrative-highlight-layer[data-entry-id="${entryId}"]`);
            if (highlightLayer) {
                highlightLayer.innerHTML = '';
            }

            // Update UI panels
            this.updateEntitySummary();
            this.updateGroupsList();
            this.updateRelationshipTimeline();
            this.updateMapPins();
            return;
        }

        // Adjust entity positions dynamically by searching for entity text in current text
        // This handles insertions/deletions before entities
        let entities = this.entryEntities[entryId] || [];
        const adjustedEntities = [];

        entities.forEach(entity => {
            const entityText = entity.rawText;

            // Skip position adjustment for freshly added entities (their position is already correct)
            if (entity.freshlyAdded) {
                adjustedEntities.push(entity);
                delete entity.freshlyAdded; // Clear flag after first use
                return;
            }

            // Try to find entity text in current text, preferring positions close to original
            let foundIndex = -1;
            let searchStart = Math.max(0, entity.startPosition - 50); // Search around original position

            // First try: exact position (no change)
            if (entity.endPosition <= text.length) {
                const currentText = text.substring(entity.startPosition, entity.endPosition);
                if (currentText === entityText) {
                    foundIndex = entity.startPosition;
                }
            }

            // Second try: search nearby (text was inserted/deleted before this entity)
            if (foundIndex === -1) {
                foundIndex = text.indexOf(entityText, searchStart);
            }

            // Third try: search from beginning (major text changes)
            if (foundIndex === -1) {
                foundIndex = text.indexOf(entityText);
            }

            if (foundIndex !== -1) {
                // Entity text still exists - update positions
                const adjustedEntity = {
                    ...entity,
                    startPosition: foundIndex,
                    endPosition: foundIndex + entityText.length
                };
                adjustedEntities.push(adjustedEntity);

                if (foundIndex !== entity.startPosition) {
                    console.log(`[TimelineEntry] Adjusted entity "${entityText}" position from ${entity.startPosition}-${entity.endPosition} to ${foundIndex}-${adjustedEntity.endPosition}`);
                }
            } else {
                // Entity text was deleted or heavily modified - remove it
                console.log(`[TimelineEntry] Removing entity "${entityText}" - no longer found in text`);
            }
        });

        // Update entities array
        if (adjustedEntities.length !== entities.length) {
            console.log(`[TimelineEntry] Entity adjustment: ${entities.length} -> ${adjustedEntities.length}`);
        }
        this.entryEntities[entryId] = adjustedEntities;

        // Refresh highlighting for confirmed manual entities only
        if (adjustedEntities.length > 0) {
            this.highlightEntities(entryId, text, adjustedEntities);
        } else {
            // Clear highlight layer if no valid entities
            const highlightLayer = document.querySelector(`.narrative-highlight-layer[data-entry-id="${entryId}"]`);
            if (highlightLayer) {
                highlightLayer.innerHTML = '';
            }
        }

        // Parse relationship syntax and auto-create relationships
        await this.parseAndCreateRelationships(entryId, text);

        // Update UI panels
        this.updateEntitySummary();
        this.updateGroupsList();
        this.updateRelationshipTimeline();
        this.updateMapPins();
    }

    /**
     * Repair entity positions by finding each entity's text in the actual narrative
     * This fixes data corruption where entity positions don't match the saved text
     * (e.g., positions calculated for expanded inline groups but text is unexpanded)
     * @param {Array} entities - Array of entity objects with positions
     * @param {string} text - The actual narrative text
     */
    repairEntityPositions(entities, text) {
        if (!entities || entities.length === 0 || !text) return;

        let repairedCount = 0;

        entities.forEach(entity => {
            // Skip entities without positions
            if (entity.startPosition === undefined || entity.endPosition === undefined) return;

            // Get what the entity text should be
            const entityText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText || '';
            if (!entityText) return;

            // Check if current position matches
            const textAtPosition = text.substring(entity.startPosition, entity.endPosition);
            const matches = textAtPosition === entityText;

            if (!matches) {
                // Position is wrong - search for the entity text in the narrative
                // Try to find the entity text (case-insensitive)
                const searchText = entityText.toLowerCase();
                let foundIndex = -1;
                let searchStart = 0;

                // Search through the text looking for this entity
                // We want to find it near where it should be, so start from the recorded position
                const searchRadius = 100; // Search 100 characters before and after
                const minSearch = Math.max(0, entity.startPosition - searchRadius);
                const maxSearch = Math.min(text.length, entity.endPosition + searchRadius);

                // First try: search nearby
                for (let i = minSearch; i < maxSearch; i++) {
                    if (text.substring(i, i + entityText.length).toLowerCase() === searchText) {
                        foundIndex = i;
                        break;
                    }
                }

                // Second try: search from beginning if not found nearby
                if (foundIndex === -1) {
                    foundIndex = text.toLowerCase().indexOf(searchText);
                }

                if (foundIndex !== -1) {
                    // Found it - update positions
                    entity.startPosition = foundIndex;
                    entity.endPosition = foundIndex + entityText.length;
                    repairedCount++;
                } else {
                    console.warn(`[EntityRepair] Could not find "${entityText}" in text - entity may have been deleted`);
                }
            }
        });

        if (repairedCount > 0) {
            console.log(`[EntityRepair] Repaired ${repairedCount} entity position(s)`);
            // Mark that we need to save the repaired data
            this.unsavedChanges = true;
        }
    }

    /**
     * Parse relationship syntax in text and automatically create relationships
     * Syntax: @person @location >transport @time.
     * Example: "went to ..sushi train @john @mary @1PM."
     * Groups: @#Siblings expands to all members
     * Inline creation: #Family(..John ..Mary) creates group and expands
     * @param {string} entryId - The timeline entry ID
     * @param {string} text - The text to parse
     * @param {boolean} skipInlineGroupExpansion - If true, skip inline group processing (for loaded data)
     */
    async parseAndCreateRelationships(entryId, text, skipInlineGroupExpansion = false) {
        if (!this.syntaxParser || !text) return;

        // NOTE: Inline group creation syntax (+#GroupName(...)) has been removed for simplicity
        // Groups should be created via UI, then referenced with @#GroupName

        // Get existing entities for this entry
        const entities = this.entryEntities[entryId] || [];
        if (entities.length < 2) return; // Need at least 2 entities to create relationships

        // Parse text for relationship syntax
        const parsed = this.syntaxParser.parse(text);
        if (!parsed || !Array.isArray(parsed) || parsed.length === 0) {
            return;
        }

        console.log(`[Relationships] Found ${parsed.length} relationship group(s)`);

        // Create relationships from each parsed group
        const allRelationships = [];
        parsed.forEach((group, groupIndex) => {
            // Resolve syntax markers to actual entities (expand groups here)
            const resolvedEntities = this.resolveGroupEntities(group, entities);

            if (resolvedEntities.length < 2) {
                return;
            }

            // Create relationships between entities
            const relationships = this.syntaxParser.createRelationships(group, resolvedEntities);
            relationships.forEach(rel => {
                rel.sequenceOrder = groupIndex; // Track which group this came from
            });
            allRelationships.push(...relationships);
        });

        // Store relationships for this entry
        if (!this.entryRelationships[entryId]) {
            this.entryRelationships[entryId] = [];
        }

        // Remove old syntax-generated relationships (keep manual ones)
        this.entryRelationships[entryId] = this.entryRelationships[entryId].filter(r => !r.isFromSyntax);

        // Add new syntax relationships
        allRelationships.forEach(r => {
            r.isFromSyntax = true; // Mark as auto-generated
        });

        this.entryRelationships[entryId].push(...allRelationships);

        console.log(`[Relationships] Created ${allRelationships.length} relationship(s)`);
    }

    /**
     * DISABLED: Inline group creation syntax was too complex and buggy
     * Groups should be created via UI instead
     * @param {string} text - Text with potential inline group creation
     * @param {string} entryId - Entry ID for entity resolution
     * @returns {Promise<string>} Text unchanged
     */
    async processInlineGroupCreation(text, entryId) {
        // FEATURE DISABLED - inline group creation was too complex
        // Users should create groups via UI, then reference with @#GroupName
        return text;
    }

    /**
     * Expand group references (@#GroupName) to individual entity markers
     * @param {string} text - Original text with potential group references
     * @returns {string} Text with groups expanded to individual markers
     */
    expandGroupReferences(text) {
        if (!text || Object.keys(this.entityGroups).length === 0) return text;

        let expandedText = text;

        // Find all @#GroupName patterns
        const groupPattern = /@#(\w+)/g;
        const matches = [...text.matchAll(groupPattern)];

        matches.forEach(match => {
            const groupName = match[1];
            const group = Object.values(this.entityGroups).find(g => 
                g.name.toLowerCase() === groupName.toLowerCase()
            );

            if (group && group.entityIds.length > 0) {
                // Get entity names from the group
                const entityNames = [];
                Object.values(this.entryEntities).forEach(entryEnts => {
                    entryEnts.forEach(ent => {
                        if (group.entityIds.includes(ent.id)) {
                            const name = ent.linkedRecordDisplayName || ent.normalizedValue || ent.rawText;
                            if (!entityNames.includes(name)) {
                                entityNames.push(name);
                            }
                        }
                    });
                });

                // Replace @#GroupName with individual @name markers
                const replacement = entityNames.map(name => `@${name}`).join(' ');
                expandedText = expandedText.replace(match[0], replacement);

                console.log(`[TimelineEntry] Expanded ${match[0]} to: ${replacement}`);
            }
        });

        return expandedText;
    }

     /**
      * Resolve syntax markers (..entity, @person, @location, @#GroupName) to actual entity objects
      * Expands group references on-the-fly for relationship parsing
      * Groups are only expanded if their members are within the same sentence boundary
      * @param {Object} group - Relationship group from parser
      * @param {Array} entities - Available entities in this entry
      * @returns {Array} Resolved entities with IDs and types
      */
    resolveGroupEntities(group, entities) {
       // Filter entities to only those within this sentence's boundaries
       // This prevents group expansion from crossing sentence boundaries (marked by periods)
       const sentenceEntities = group.startPosition !== undefined && group.endPosition !== undefined
           ? entities.filter(e => {
               // Entity must overlap with the sentence boundaries (not necessarily fully within)
               // This allows entities added via menu (which may extend past original parse boundary)
               const entityStart = e.startPosition || 0;
               const entityEnd = e.endPosition || 0;
               // Check for overlap: entity starts at or before sentence ends AND entity ends at or after sentence starts
               // Use <= to include entities that start exactly at the boundary (e.g., last item before period)
               return entityStart <= group.endPosition && entityEnd >= group.startPosition;
             })
           : entities; // Fallback to all entities if no position info (backward compatibility)

       const resolved = [];

       for (let syntaxEntity of group.entities) {

            // Check if this is a group reference (@#GroupName)
            if (syntaxEntity.marker === '@' && syntaxEntity.text.startsWith('#')) {
                const groupName = syntaxEntity.text.substring(1); // Remove # prefix
                console.log(`[GroupExpansion] Detected group reference: @#${groupName}`);
                console.log(`[GroupExpansion] Available groups:`, Object.keys(this.entityGroups).map(id => this.entityGroups[id].name));

                const entityGroup = Object.values(this.entityGroups).find(g => 
                    g.name.toLowerCase() === groupName.toLowerCase()
                );

                if (entityGroup) {
                    console.log(`[GroupExpansion] ✓ Found group "${groupName}" with ${entityGroup.entityIds.length} members`);

                    // Find all entities that are members of this group
                    // NOTE: Search in ALL entities (not just sentence-scoped) because groups can reference
                    // entities from anywhere in the entry. The group reference itself is within the sentence,
                    // but its members may have been defined elsewhere.
                    entityGroup.entityIds.forEach(entityId => {
                        const memberEntity = entities.find(e => (e.sourceEntityId || e.id) === entityId);
                        if (memberEntity) {
                            resolved.push({
                                ...memberEntity,
                                isPrimary: syntaxEntity.role === 'primary',
                                relationshipType: syntaxEntity.relationshipType
                            });
                            console.log(`[TimelineEntry]   ✓ Expanded group member: ${memberEntity.rawText}`);
                        } else {
                            console.warn(`[TimelineEntry]   ✗ Group member entity ${entityId} not found in entry`);
                        }
                    });

                    continue; // Skip normal entity matching
                } else {
                    console.warn(`[GroupExpansion] ✗ Group "${groupName}" not found in entityGroups`);
                }
            }

            // Normal entity matching (not a group reference)
            // Also use sentence-scoped entities for consistency
            const matchingEntity = sentenceEntities.find(e => {
                const displayText = (e.linkedRecordDisplayName || e.normalizedValue || e.rawText || '').trim();
                const syntaxText = syntaxEntity.text.trim();
                return displayText.toLowerCase().includes(syntaxText.toLowerCase()) ||
                       syntaxText.toLowerCase().includes(displayText.toLowerCase());
            });

            if (matchingEntity) {
                resolved.push({
                    ...matchingEntity,
                    isPrimary: syntaxEntity.role === 'primary',
                    relationshipType: syntaxEntity.relationshipType
                });
            } else {
                console.warn(`[TimelineEntry] Could not resolve entity: "${syntaxEntity.text}" (${syntaxEntity.marker})`);
            }
        }

        return resolved;
    }

    handleKeyDown(event, entryId) {
        // Keyboard handlers removed - using manual .. menu only
        // All entity creation happens through EntityQuickAdd (.. trigger)
    }

    handleDateChange(event, entryId) {
        const block = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
        if (block) {
            const newDate = new Date(event.target.value);
            block.dataset.entryDate = newDate.toISOString();
            this.unsavedChanges = true;
        }
    }

    /**
     * Refresh highlighting for manually-added entities only
     * No parser - all entities created via .. menu
     */
    refreshHighlighting(entryId, text) {
        if (!text || text.trim() === '') {
            const highlightLayer = document.querySelector(`.narrative-highlight-layer[data-entry-id="${entryId}"]`);
            if (highlightLayer) highlightLayer.innerHTML = '';
            return;
        }

        // Get manually-added confirmed entities
        const entities = this.entryEntities[entryId] || [];

        // Refresh highlights for manual entities

        // Highlight confirmed entities in the text
        this.highlightEntities(entryId, text, entities);

        // Update summary, timeline visualization, and map
        this.updateEntitySummary();
        this.updateRelationshipTimeline();
        this.updateMapPins();
    }

    highlightEntities(entryId, text, entities) {
        const highlightLayer = document.querySelector(`.narrative-highlight-layer[data-entry-id="${entryId}"]`);
        if (!highlightLayer) return;

        console.log(`[Highlighting] ${entities.length} entities to highlight`);
        console.log(`[Highlighting] Entities:`, entities.map(e => `${e.rawText} (${e.startPosition}-${e.endPosition})`));

        // Get relationship groups for this entry
        const relationships = this.entryRelationships[entryId] || [];
        const entityGroupMap = this.buildEntityGroupMap(entities, relationships);

        // Find all group references (@#GroupName)
        const groupReferences = [];
        const groupRefPattern = /@#([A-Za-z0-9_-]+)/g;

        let match;

        // Find @#GroupName references only (inline group creation disabled)
        while ((match = groupRefPattern.exec(text)) !== null) {
            const groupName = match[1];
            const entityGroup = Object.values(this.entityGroups).find(g => 
                g.name.toLowerCase() === groupName.toLowerCase()
            );

            if (entityGroup) {
                groupReferences.push({
                    start: match.index,
                    end: match.index + match[0].length,
                    name: entityGroup.name,
                    memberCount: entityGroup.entityIds.length,
                    groupId: entityGroup.id
                });
                console.log(`[Highlighting] Found group reference: @#${groupName} at position ${match.index}`);
            }
        }

        // Create highlighted version of the text
        let highlightedHtml = '';
        let lastIndex = 0;

        // Combine entities and group references, sorted by position
        const sortedEntities = [...entities].sort((a, b) => a.startPosition - b.startPosition);
        const sortedGroups = [...groupReferences].sort((a, b) => a.start - b.start);

        // Merge and process in order
        const allItems = [
            ...sortedEntities.map(e => ({ type: 'entity', pos: e.startPosition, data: e })),
            ...sortedGroups.map(g => ({ type: 'group', pos: g.start, data: g }))
        ].sort((a, b) => a.pos - b.pos);

        allItems.forEach(item => {
            if (item.type === 'entity') {
                const entity = item.data;

                // Add text before entity
                highlightedHtml += this.escapeHtml(text.substring(lastIndex, entity.startPosition));

                // Add highlighted entity
                const entityTypeName = this.entityTypeMap[entity.entityType] || 'unknown';
                const entityClass = `entity-highlight entity-${entityTypeName.toLowerCase()}`;
                const entityText = text.substring(entity.startPosition, entity.endPosition);

                // Add group data attributes if entity is in a relationship group
                const groupInfo = entityGroupMap.get(entity.id);
                let groupAttrs = '';
                if (groupInfo) {
                    groupAttrs = `data-group-id="${groupInfo.groupId}" data-group-label="Group ${groupInfo.groupId + 1}: ${groupInfo.members.length} related"`;
                }

                highlightedHtml += `<span class="${entityClass}" data-entity-id="${entity.id}" ${groupAttrs} title="${entityTypeName}">${this.escapeHtml(entityText)}</span>`;

                lastIndex = entity.endPosition;
            } else if (item.type === 'group') {
                const group = item.data;

                // Add text before group reference
                highlightedHtml += this.escapeHtml(text.substring(lastIndex, group.start));

                // Add group reference chip
                const groupLabel = `👥 ${group.name} (${group.memberCount})`;
                highlightedHtml += `<span class="entity-group-ref" data-group-id="${group.groupId}" data-group-name="${this.escapeHtml(group.name)}" title="Group: ${this.escapeHtml(group.name)} - ${group.memberCount} members">${this.escapeHtml(groupLabel)}</span>`;

                lastIndex = group.end;
            }
        });

        // Add remaining text
        highlightedHtml += this.escapeHtml(text.substring(lastIndex));

        highlightLayer.innerHTML = highlightedHtml;

        // Attach click handlers to highlighted entities
        highlightLayer.querySelectorAll('.entity-highlight').forEach(span => {
            span.addEventListener('click', (e) => {
                const entityId = e.target.dataset.entityId;
                const entity = entities.find(ent => ent.id === entityId);
                if (entity) {
                    this.showEntityDetails(entity, entryId);
                }
            });
        });

        // Attach click handlers to group chips
        highlightLayer.querySelectorAll('.entity-group-ref').forEach(span => {
            span.addEventListener('click', (e) => {
                const groupId = e.target.dataset.groupId;
                const groupName = e.target.dataset.groupName;
                this.showGroupDetails(groupId, groupName, entryId);
            });
        });
    }

    /**
     * Show details popup for a group reference
     * @param {string} groupId - Group ID
     * @param {string} groupName - Group name
     * @param {string} entryId - Entry ID
     */
    showGroupDetails(groupId, groupName, entryId) {
        const entityGroup = this.entityGroups[groupId];
        if (!entityGroup) return;

        // Get all entities for this entry
        const allEntities = this.entryEntities[entryId] || [];

        // Find member entities
        const memberEntities = allEntities.filter(e => entityGroup.entityIds.includes(e.id));

        const details = `
            <div class="entity-details-popup">
                <h4>👥 Group: ${this.escapeHtml(groupName)}</h4>
                <p><strong>Members:</strong> ${memberEntities.length}</p>
                <ul>
                    ${memberEntities.map(e => `<li>${this.escapeHtml(e.linkedRecordDisplayName || e.normalizedValue || e.rawText)} (${this.entityTypeMap[e.entityType]})</li>`).join('')}
                </ul>
            </div>
        `;

        // Show in a simple alert for now (can be upgraded to a proper modal)
        alert(details.replace(/<[^>]*>/g, '\n').trim());
    }

    /**
     * Build a map of entity IDs to their relationship groups
     * @param {Array} entities - All entities in entry
     * @param {Array} relationships - All relationships in entry
     * @returns {Map} Map of entityId -> {groupId, members}
     */
    buildEntityGroupMap(entities, relationships) {
        const groupMap = new Map();
        const groups = [];

        // Build groups from relationships
        relationships.forEach(rel => {
            // Find or create group
            let group = groups.find(g => 
                g.entities.has(rel.primaryEntityId) || 
                g.entities.has(rel.relatedEntityId)
            );

            if (!group) {
                group = { entities: new Set() };
                groups.push(group);
            }

            group.entities.add(rel.primaryEntityId);
            group.entities.add(rel.relatedEntityId);
            if (rel.timeEntityId) {
                group.entities.add(rel.timeEntityId);
            }
        });

        // Convert sets to arrays and assign group IDs
        groups.forEach((group, index) => {
            const entityIds = Array.from(group.entities);
            entityIds.forEach(entityId => {
                groupMap.set(entityId, {
                    groupId: index,
                    members: entityIds
                });
            });
        });

        return groupMap;
    }

    // updateEntryStatus() - REMOVED: No longer needed without parser

    showEntityDetails(entity, entryId) {
        console.log('Show details for entity:', entity);
        // Use the same inline Tippy.js popup for editing
        if (this.quickAdd) {
            this.quickAdd.editEntity(entity, entryId);
        }
    }

    updateEntitySummary() {
        const summaryDiv = document.getElementById('entitySummary');
        if (!summaryDiv) return;

        // Collect all entities from all entries
        const allEntities = [];
        Object.entries(this.entryEntities).forEach(([entryId, entities]) => {
            entities.forEach(entity => {
                allEntities.push({ ...entity, entryId });
            });
        });

        // Also scan for group references in text and add their members to counts
        document.querySelectorAll('.narrative-textarea').forEach(textarea => {
            const text = textarea.value;
            const entryId = textarea.dataset.entryId;

            // Find all group references (@#GroupName)
            const groupRefPattern = /@#([A-Za-z0-9_-]+)/g;
            let match;
            while ((match = groupRefPattern.exec(text)) !== null) {
                const groupName = match[1];
                const entityGroup = Object.values(this.entityGroups).find(g => 
                    g.name.toLowerCase() === groupName.toLowerCase()
                );

                if (entityGroup) {
                    // Add all member entities to the count
                    const entryEntities = this.entryEntities[entryId] || [];
                    entityGroup.entityIds.forEach(entityId => {
                        const memberEntity = entryEntities.find(e => e.id === entityId);
                        if (memberEntity) {
                            // Mark as coming from a group reference
                            allEntities.push({ 
                                ...memberEntity, 
                                entryId,
                                fromGroupReference: true,
                                groupName: entityGroup.name
                            });
                        }
                    });
                }
            }
        });

        if (allEntities.length === 0) {
            summaryDiv.innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="bi bi-inbox fs-3 d-block mb-2"></i>
                    No entities detected yet
                </div>
            `;
            return;
        }

        // Deduplicate entities in real-time using sourceEntityId (browser-session grouping)
        // This allows the sidebar to show "John" once even if typed multiple times via ..john
        const entityMap = new Map();
        allEntities.forEach(entity => {
            // Grouping key hierarchy (PRIORITY ORDER):
            // 1. Database IDs (personId, locationId, etc.) - strongest post-save grouping
            // 2. sourceEntityId (if this is a linked/reused entity) - groups all mentions back to original
            // 3. id (if this is an original entity) - becomes the root for future mentions
            // 4. Text matching (entityTypeName + normalizedValue) - fallback for unlinked entities
            let groupKey;

            if (entity.personId || entity.locationId || entity.transportId || entity.eventId) {
                // Post-save: use database ID as grouping key (HIGHEST PRIORITY)
                const dbId = entity.personId || entity.locationId || entity.transportId || entity.eventId;
                const entityTypeName = this.entityTypeMap[entity.entityType] || 'unknown';
                groupKey = `db_${entityTypeName}_${dbId}`;
                console.log(`[Dedup] Entity "${entity.rawText}" using db ID: ${groupKey}`);
            } else if (entity.sourceEntityId) {
                // This is a reused entity - group under the original entity's ID
                groupKey = `source_${entity.sourceEntityId}`;
                console.log(`[Dedup] Entity "${entity.rawText}" using sourceEntityId: ${groupKey}`);
            } else {
                // Original entity OR unlinked entity - group by ID for browser-session linking
                // This allows future sourceEntityId references to find it
                groupKey = `source_${entity.id}`;
                console.log(`[Dedup] Entity "${entity.rawText}" using entity ID: ${groupKey}`);
            }

            if (!entityMap.has(groupKey)) {
                // First occurrence - store it
                entityMap.set(groupKey, { ...entity, mentions: 1 });
            } else {
                // Additional mention - increment counter
                const existing = entityMap.get(groupKey);
                existing.mentions = (existing.mentions || 1) + 1;
                console.log(`[Dedup] Incrementing "${entity.rawText}" to ${existing.mentions} mentions`);
            }
        });

        // Convert deduplicated entities back to array
        const deduplicatedEntities = Array.from(entityMap.values());
        console.log(`[TimelineEntry] Entity deduplication: ${allEntities.length} total → ${deduplicatedEntities.length} unique`);

        // Group entities by type (exclude DateTime and Duration - they're shown in relationship context only)
        const groupedEntities = {};
        deduplicatedEntities.forEach(entity => {
            // Skip DateTime (5) and Duration (6) - they're useful in relationships but not as standalone items
            if (entity.entityType === 5 || entity.entityType === 6) {
                return;
            }
            const typeName = this.entityTypeMap[entity.entityType] || 'Unknown';
            if (!groupedEntities[typeName]) {
                groupedEntities[typeName] = [];
            }
            groupedEntities[typeName].push(entity);
        });

        // Build HTML - use simple, clean tables
        let html = '';

        // Add export button at the top
        html += `
            <div style="display: flex; justify-content: flex-end; padding: 0.75rem 1rem; background: #f8f9fa; border-bottom: 1px solid #e0e0e0;">
                <button type="button" class="btn btn-sm btn-outline-primary" 
                        onclick="window.timelineEntry.exportComprehensiveReport()" 
                        title="Print Comprehensive Report">
                    <i class="bi bi-printer"></i> Print Report
                </button>
            </div>
        `;

        // Order: Person, Location, Event, Transport, DateTime, Duration, Activity
        const typeOrder = ['Person', 'Location', 'Event', 'Transport', 'DateTime', 'Duration', 'Activity'];

        typeOrder.forEach(typeName => {
            if (!groupedEntities[typeName] || groupedEntities[typeName].length === 0) return;

            const entities = groupedEntities[typeName];
            const icon = this.getIconForType(typeName);

            html += `
                <div class="table-container">
                    <div class="table-header">
                        ${icon} ${typeName}s (${entities.length})
                    </div>
            `;

            // Generate table based on entity type
            if (typeName === 'Person') {
                html += `
                    <table>
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Relationship</th>
                                <th>Phone</th>
                                <th>Age/DOB</th>
                                <th>Notes</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                entities.forEach((entity, idx) => {
                    const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const mentionCount = (entity.mentions && entity.mentions > 1) ? ` (×${entity.mentions})` : '';

                    // Extract metadata fields
                    const relationship = entity.metadata?.relationship || '—';
                    const phone = entity.metadata?.phone || '—';
                    const ageDob = entity.metadata?.ageDob || '—';
                    const notes = entity.metadata?.notes || '—';

                    // Get relationship history for this person
                    const relationshipHistory = this.getPersonRelationshipHistory(entity);
                    const hasRelationships = relationshipHistory.length > 0;

                    html += `
                        <tr class="person-row" data-person-id="${entity.id}">
                            <td>
                                ${hasRelationships ? `<span class="expand-icon" onclick="window.timelineEntry.togglePersonDetails('${entity.id}')">▶</span>` : ''}
                                <strong>${this.escapeHtml(displayText)}</strong>${mentionCount}
                            </td>
                            <td>${this.escapeHtml(relationship)}</td>
                            <td>${this.escapeHtml(phone)}</td>
                            <td>${this.escapeHtml(ageDob)}</td>
                            <td>${this.escapeHtml(notes)}</td>
                            <td>
                                <button type="button" class="btn btn-sm btn-outline-primary" 
                                        onclick="window.timelineEntry.editEntityFromSummary('${entity.id}', '${entity.entryId}')">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            </td>
                        </tr>
                        ${hasRelationships ? `
                        <tr class="person-detail-row" id="person-details-${entity.id}" style="display: none;">
                            <td colspan="6" class="detail-cell">
                                <table class="relationship-table">
                                    <thead>
                                        <tr>
                                            <th>Date</th>
                                            <th>Location</th>
                                            <th>Time/Duration</th>
                                            <th>Entry</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${relationshipHistory.map(rel => `
                                            <tr>
                                                <td>${rel.date}</td>
                                                <td>
                                                    ${this.escapeHtml(rel.location)}
                                                    ${rel.transport ? `<br><small class="text-muted">via ${this.escapeHtml(rel.transport)}</small>` : ''}
                                                </td>
                                                <td>${this.escapeHtml(rel.timeDuration)}</td>
                                                <td><small>${this.escapeHtml(rel.entryText)}</small></td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                        ` : ''}
                    `;
                });

                html += `
                        </tbody>
                    </table>
                `;

            } else if (typeName === 'Location') {
                html += `
                    <table>
                        <thead>
                            <tr>
                                <th>Name</th>
                                <th>Address</th>
                                <th>Date</th>
                                <th>Time</th>
                                <th>Duration</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                entities.forEach(entity => {
                    const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const address = entity.metadata?.address || '—';
                    const visitCount = entity.mentions || 1;

                    // Collect all occurrences of this location across entries
                    const locationOccurrences = [];
                    const groupKey = this.getEntityGroupKey(entity);

                    Object.entries(this.entryEntities).forEach(([entryId, entryEntityList]) => {
                        entryEntityList.forEach(e => {
                            const eGroupKey = this.getEntityGroupKey(e);
                            if (eGroupKey === groupKey && e.entityType === entity.entityType) {
                                // Get entry date
                                let entryDate = '—';
                                if (this.timelineData?.entries) {
                                    const entry = this.timelineData.entries.find(entry => entry.id === entryId);
                                    if (entry?.entryDate) {
                                        const date = new Date(entry.entryDate);
                                        entryDate = date.toLocaleDateString('en-AU', { 
                                            day: 'numeric', 
                                            month: 'short', 
                                            year: 'numeric',
                                            weekday: 'short'
                                        });
                                    }
                                }

                                // Get time and duration
                                const timeData = this.getLocationTimeData(e, entryId);

                                locationOccurrences.push({
                                    entryId,
                                    date: entryDate,
                                    time: timeData.time,
                                    duration: timeData.duration,
                                    entityId: e.id
                                });
                            }
                        });
                    });

                    const hasMultipleVisits = visitCount > 1;
                    const expandIconHtml = hasMultipleVisits 
                        ? `<span class="expand-icon" onclick="window.timelineEntry.toggleLocationDetails('${entity.id}')">▶</span>`
                        : '';

                    // For single visits, show the details inline
                    const singleVisit = locationOccurrences.length === 1 ? locationOccurrences[0] : null;

                    html += `
                        <tr class="location-row">
                            <td>
                                ${expandIconHtml}
                                <strong>${this.escapeHtml(displayText)}</strong>
                                ${hasMultipleVisits ? `<span class="visit-count-badge ms-2">${visitCount} visits</span>` : ''}
                            </td>
                            <td>${this.escapeHtml(address)}</td>
                            <td>${singleVisit ? this.escapeHtml(singleVisit.date) : '—'}</td>
                            <td>${singleVisit ? this.escapeHtml(singleVisit.time) : '—'}</td>
                            <td>${singleVisit ? this.escapeHtml(singleVisit.duration) : '—'}</td>
                            <td>
                                <button type="button" class="btn btn-sm btn-outline-primary" 
                                        onclick="window.timelineEntry.editEntityFromSummary('${entity.id}', '${entity.entryId}')">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            </td>
                        </tr>
                        ${hasMultipleVisits ? `
                        <tr class="location-detail-row" id="location-details-${entity.id}" style="display: none;">
                            <td colspan="6" class="detail-cell">
                                <table class="visit-detail-table">
                                    <thead>
                                        <tr>
                                            <th>Date</th>
                                            <th>Time</th>
                                            <th>Duration</th>
                                            <th>Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${locationOccurrences.map(occ => `
                                            <tr>
                                                <td>${this.escapeHtml(occ.date)}</td>
                                                <td>${this.escapeHtml(occ.time)}</td>
                                                <td>${this.escapeHtml(occ.duration)}</td>
                                                <td>
                                                    <button type="button" class="btn btn-sm btn-outline-primary" 
                                                            onclick="window.timelineEntry.editEntityFromSummary('${occ.entityId}', '${occ.entryId}')">
                                                        <i class="bi bi-pencil"></i>
                                                    </button>
                                                </td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </td>
                        </tr>
                        ` : ''}
                    `;
                });

                html += `
                        </tbody>
                    </table>
                `;

            } else if (typeName === 'Transport') {
                html += `
                    <table>
                        <thead>
                            <tr>
                                <th>Transport</th>
                                <th>Date</th>
                                <th>⏱️ Departure</th>
                                <th>🏁 Arrival</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                entities.forEach(entity => {
                    const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const mentionCount = (entity.mentions && entity.mentions > 1) ? ` (×${entity.mentions})` : '';

                    // Extract transport details
                    const transportType = entity.metadata?.transportType || displayText;
                    const transportDetails = entity.metadata?.details || entity.metadata?.routeNumber || '';
                    const transportDisplay = transportDetails 
                        ? `${this.escapeHtml(transportType)}<br><small class="text-muted">${this.escapeHtml(transportDetails)}</small>`
                        : `${this.escapeHtml(transportType)}`;

                    // Get entry date from timeline data
                    let entryDate = '—';
                    if (entity.entryId && this.timelineData?.entries) {
                        const entry = this.timelineData.entries.find(e => e.id === entity.entryId);
                        if (entry?.entryDate) {
                            const date = new Date(entry.entryDate);
                            entryDate = date.toLocaleDateString('en-AU', { 
                                day: 'numeric', 
                                month: 'short', 
                                year: 'numeric' 
                            });
                        }
                    }

                    // Extract transport metadata
                    const departedFrom = entity.metadata?.departedFrom || '—';
                    const departedAt = entity.metadata?.departedAt || '—';
                    const arrivedTo = entity.metadata?.arrivedTo || '—';
                    const arrivedAt = entity.metadata?.arrivedAt || '—';

                    const departureText = `${this.escapeHtml(departedFrom)}<br><small class="text-muted">${this.escapeHtml(departedAt)}</small>`;
                    const arrivalText = `${this.escapeHtml(arrivedTo)}<br><small class="text-muted">${this.escapeHtml(arrivedAt)}</small>`;

                    html += `
                        <tr>
                            <td><strong>${transportDisplay}</strong>${mentionCount}</td>
                            <td>${this.escapeHtml(entryDate)}</td>
                            <td>${departureText}</td>
                            <td>${arrivalText}</td>
                            <td>
                                <button type="button" class="btn btn-sm btn-outline-primary" 
                                        onclick="window.timelineEntry.editEntityFromSummary('${entity.id}', '${entity.entryId}')">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            </td>
                        </tr>
                    `;
                });

                html += `
                        </tbody>
                    </table>
                `;

            } else {
                // Other entity types (Event, Activity, etc.) - simple 2-column table
                html += `
                    <table>
                        <thead>
                            <tr>
                                <th>${typeName}</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                entities.forEach(entity => {
                    const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const mentionCount = (entity.mentions && entity.mentions > 1) ? ` (×${entity.mentions})` : '';

                    html += `
                        <tr>
                            <td><strong>${this.escapeHtml(displayText)}</strong>${mentionCount}</td>
                            <td>
                                <button type="button" class="btn btn-sm btn-outline-primary" 
                                        onclick="window.timelineEntry.editEntityFromSummary('${entity.id}', '${entity.entryId}')">
                                    <i class="bi bi-pencil"></i>
                                </button>
                            </td>
                        </tr>
                    `;
                });

                html += `
                        </tbody>
                    </table>
                `;
            }

            html += `
                </div>
            `;
        });

        summaryDiv.innerHTML = html || '<div class="empty-state"><i class="bi bi-inbox"></i><p>No entities detected yet</p></div>';
    }

    updateRelationshipTimeline() {
        // REMOVED: Relationship timeline panel has been removed from the UI
        // Relationships are still created and stored, just not visualized in a separate panel
    }

    updateMapPins() {
        if (!this.map) return;

        const allEntries = this.getAllCurrentEntries();
        const locations = [];

        // TODO: Extract location entities with coordinates and add to map
        // For now, just clear the map
        this.map.clearMarkers();
    }

    getAllCurrentEntries() {
        const entries = [];
        document.querySelectorAll('.timeline-day-block').forEach(block => {
            const entryId = block.dataset.entryId;
            const textarea = block.querySelector('.narrative-textarea');
            const dateInput = block.querySelector('input[type="date"]');

            if (textarea && dateInput) {
                entries.push({
                    id: entryId,
                    entryDate: new Date(dateInput.value),
                    narrativeText: textarea.value,
                    entities: this.entryEntities[entryId] || [],
                    relationships: this.entryRelationships[entryId] || []
                });
            }
        });
        return entries;
    }

    async saveDraft() {
        try {
            this.showLoading(true);

            const entries = this.getAllCurrentEntries();
            const timelineData = {
                caseId: this.caseId,
                entries: entries,
                conventions: this.timelineData?.conventions || {},
                isReviewed: false
            };

            const response = await fetch('/api/timeline/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(timelineData)
            });

            if (!response.ok) {
                throw new Error('Save failed');
            }

            this.unsavedChanges = false;
            this.showSuccess('Draft saved successfully');

        } catch (error) {
            console.error('Error saving draft:', error);
            this.showError('Failed to save draft');
        } finally {
            this.showLoading(false);
        }
    }

    /**
     * Auto-save with debouncing - saves timeline after entity changes
     * This enables immediate availability of entities in the recent menu
     */
    async autoSave() {
        // Clear any existing timer
        if (this.autoSaveTimer) {
            clearTimeout(this.autoSaveTimer);
        }

        // Debounce: wait 2 seconds after last change before saving
        this.autoSaveTimer = setTimeout(async () => {
            if (this.isSaving) {
                console.log('[TimelineEntry] Already saving, skipping auto-save');
                return;
            }

            try {
                this.isSaving = true;
                console.log('[TimelineEntry] Auto-saving timeline...');

                const entries = this.getAllCurrentEntries();
                const timelineData = {
                    caseId: this.caseId,
                    entries: entries,
                    conventions: this.timelineData?.conventions || {},
                    isReviewed: false
                };

                const response = await fetch('/api/timeline/save', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(timelineData)
                });

                if (!response.ok) {
                    throw new Error('Auto-save failed');
                }

                this.unsavedChanges = false;
                console.log('[TimelineEntry] Auto-save successful');

                // Reload timeline to get updated entity database IDs
                // This ensures proper entity grouping after save
                await this.reloadTimelineAfterSave();

                // Show subtle indicator (no intrusive alert)
                this.showAutoSaveIndicator();

            } catch (error) {
                console.error('Error during auto-save:', error);
                // Don't show error toast for auto-save failures
                // User can still manually save
            } finally {
                this.isSaving = false;
            }
        }, 2000); // 2 second debounce
    }

    /**
     * Show a subtle auto-save indicator without interrupting workflow
     */
    showAutoSaveIndicator() {
        const indicator = document.getElementById('saveDraftBtn');
        if (indicator) {
            const originalText = indicator.innerHTML;
            indicator.innerHTML = '<i class="bi bi-check-circle text-success"></i> Saved';
            indicator.classList.add('btn-success');
            indicator.classList.remove('btn-outline-secondary');

            setTimeout(() => {
                indicator.innerHTML = originalText;
                indicator.classList.remove('btn-success');
                indicator.classList.add('btn-outline-secondary');
            }, 3000);
        }
    }

    /**
     * Start periodic background auto-save every 30 seconds
     * This runs in background without interrupting the interview workflow
     */
    startPeriodicAutoSave() {
        // Save every 30 seconds if there are unsaved changes
        this.periodicSaveInterval = setInterval(async () => {
            if (this.unsavedChanges && !this.isSaving) {
                console.log('[TimelineEntry] Periodic auto-save triggered');
                await this.performQuietSave();
            }
        }, 30000); // 30 seconds

        console.log('[TimelineEntry] Periodic auto-save started (every 30 seconds)');
    }

    /**
     * Stop periodic auto-save (cleanup)
     */
    stopPeriodicAutoSave() {
        if (this.periodicSaveInterval) {
            clearInterval(this.periodicSaveInterval);
            this.periodicSaveInterval = null;
            console.log('[TimelineEntry] Periodic auto-save stopped');
        }
    }

    /**
     * Perform a quiet background save without showing loading indicators
     * Used by periodic auto-save to avoid interrupting user workflow
     */
    async performQuietSave() {
        if (this.isSaving) {
            console.log('[TimelineEntry] Already saving, skipping quiet save');
            return;
        }

        try {
            this.isSaving = true;

            const entries = this.getAllCurrentEntries();
            const timelineData = {
                caseId: this.caseId,
                entries: entries,
                conventions: this.timelineData?.conventions || {},
                isReviewed: false
            };

            const response = await fetch('/api/timeline/save', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(timelineData)
            });

            if (response.ok) {
                this.unsavedChanges = false;
                console.log('[TimelineEntry] Quiet save successful');
                this.showAutoSaveIndicator();
            } else {
                console.warn('[TimelineEntry] Quiet save failed:', response.status);
            }

        } catch (error) {
            console.error('Error during quiet save:', error);
            // Silently fail - user can still manually save
        } finally {
            this.isSaving = false;
        }
    }

    async saveAndReview() {
        await this.saveDraft();
        // TODO: Navigate to review page
        window.location.href = `/Cases/Exposures/Review?caseId=${this.caseId}`;
    }

    copyPreviousDay() {
        const blocks = Array.from(document.querySelectorAll('.timeline-day-block'));
        if (blocks.length < 1) return;

        const lastBlock = blocks[blocks.length - 1];
        const lastTextarea = lastBlock.querySelector('.narrative-textarea');
        const lastDate = new Date(lastBlock.dataset.entryDate);
        
        const newDate = new Date(lastDate);
        newDate.setDate(newDate.getDate() + 1);

        this.addDayBlock(newDate);

        // Set the text from previous day
        const newBlock = Array.from(document.querySelectorAll('.timeline-day-block')).pop();
        const newTextarea = newBlock.querySelector('.narrative-textarea');
        newTextarea.value = lastTextarea.value;

        // Manual entities only - no auto-parsing
        // User can tag entities via ".." trigger
    }

    // Utility methods
    generateId() {
        return 'entry_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
    }

    formatDateForInput(date) {
        const d = new Date(date);
        const year = d.getFullYear();
        const month = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    getIconForType(typeName) {
        const icons = {
            'Person': '<i class="bi bi-person-fill text-primary"></i>',
            'Location': '<i class="bi bi-geo-alt-fill text-success"></i>',
            'Event': '<i class="bi bi-calendar-event-fill" style="color: #9b59b6;"></i>',
            'Transport': '<i class="bi bi-bus-front-fill text-warning"></i>',
            'DateTime': '<i class="bi bi-clock-fill text-danger"></i>',
            'Duration': '<i class="bi bi-hourglass-split text-secondary"></i>',
            'Activity': '<i class="bi bi-activity text-info"></i>'
        };
        return icons[typeName] || '<i class="bi bi-dot"></i>';
    }

    /**
     * Get entity type name from entity type number
     * @param {number} entityType - The entity type number
     * @returns {string} The entity type name
     */
    getEntityTypeName(entityType) {
        return this.entityTypeMap[entityType] || 'Unknown';
    }

    /**
     * Get relationship history for a person entity
     * @param {Object} personEntity - The person entity
     * @returns {Array} Array of relationship records with date, location, time/duration
     */
    getPersonRelationshipHistory(personEntity) {
        const history = [];
        const personId = personEntity.sourceEntityId || personEntity.id;

        // Scan all entries to find ACTUAL RELATIONSHIPS involving this person
        Object.entries(this.entryRelationships || {}).forEach(([entryId, relationships]) => {
            // Filter relationships where this person is involved
            const personRelationships = relationships.filter(rel => 
                rel.primaryEntityId === personId || rel.relatedEntityId === personId
            );

            if (personRelationships.length === 0) return;

            // Get the primary entity IDs from person relationships
            // (to find other entities like transport that share the same primary entity)
            const primaryEntityIds = new Set();
            personRelationships.forEach(rel => {
                if (rel.primaryEntityId === personId) {
                    // Person is primary - this shouldn't happen typically
                    primaryEntityIds.add(rel.relatedEntityId);
                } else {
                    // Person is related - add the primary entity
                    primaryEntityIds.add(rel.primaryEntityId);
                }
            });

            // Now get ALL relationships that involve these primary entities
            // (this will include transport, time, etc. that are related to the same location/event)
            const allRelatedRelationships = relationships.filter(rel => 
                primaryEntityIds.has(rel.primaryEntityId) ||
                personRelationships.some(pr => pr.primaryEntityId === rel.primaryEntityId || pr.relatedEntityId === rel.primaryEntityId)
            );

            // Get entities for this entry to enrich relationships
            const entryEntities = this.entryEntities[entryId] || [];

            // Enrich relationships with entity data
            const enrichedRelationships = allRelatedRelationships.map(rel => {
                const primaryEntity = entryEntities.find(e => 
                    (e.sourceEntityId || e.id) === rel.primaryEntityId
                );
                const relatedEntity = entryEntities.find(e => 
                    (e.sourceEntityId || e.id) === rel.relatedEntityId
                );
                const timeEntity = rel.timeEntityId ? entryEntities.find(e => 
                    (e.sourceEntityId || e.id) === rel.timeEntityId
                ) : null;

                return {
                    ...rel,
                    primaryEntity,
                    relatedEntity,
                    timeEntity
                };
            });

            // Get the date from the day block
            const dayBlock = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
            const entryDate = dayBlock?.dataset.entryDate 
                ? new Date(dayBlock.dataset.entryDate).toLocaleDateString() 
                : '—';

            // Get entry text for context
            const entry = this.currentEntries.find(e => e.id === entryId);
            const entryText = entry?.entryText || '';

            // Group relationships by location OR standalone transport
            // (for cases like "+John +Mary >bus" without a specific location)
            const locationGroups = new Map();

            enrichedRelationships.forEach(rel => {
                // Find the location, transport, and time entities in this relationship
                let location = null;
                let transport = null;
                let timeEntity = null;

                // Check if either primary or related entity is a location
                if (rel.primaryEntity?.entityType === 2) { // Location
                    location = rel.primaryEntity;
                } else if (rel.relatedEntity?.entityType === 2) {
                    location = rel.relatedEntity;
                }

                // Check if either primary or related entity is a transport
                if (rel.primaryEntity?.entityType === 4) { // Transport
                    transport = rel.primaryEntity;
                } else if (rel.relatedEntity?.entityType === 4) {
                    transport = rel.relatedEntity;
                }

                // Check for time entity in this relationship
                // Collect ALL time entities associated with this relationship
                const timeEntitiesToAdd = [];

                // First check if there's an allTimeEntityIds array (from updated relationship parser)
                if (rel.allTimeEntityIds && rel.allTimeEntityIds.length > 0) {
                    // Use all time entity IDs from the relationship
                    rel.allTimeEntityIds.forEach(timeId => {
                        const timeEnt = entryEntities.find(e => e.id === timeId);
                        if (timeEnt && timeEnt.entityType === 5) {
                            timeEntitiesToAdd.push(timeEnt);
                        }
                    });
                } else {
                    // Fallback to old behavior - single time entity
                    if (rel.timeEntity?.entityType === 5) {
                        timeEntitiesToAdd.push(rel.timeEntity);
                    } else if (rel.primaryEntity?.entityType === 5) { // DateTime
                        timeEntitiesToAdd.push(rel.primaryEntity);
                    } else if (rel.relatedEntity?.entityType === 5) {
                        timeEntitiesToAdd.push(rel.relatedEntity);
                    }
                }

                // Create grouping key:
                // - If location exists, group by location
                // - If no location but transport is PRIMARY entity, group by transport (standalone transport)
                // - Otherwise, group as 'no-location'
                let locationKey;
                let isStandaloneTransport = false;

                if (location) {
                    locationKey = 'loc_' + (location.id || location.rawText);
                } else if (transport && rel.primaryEntity?.entityType === 4) {
                    // Transport is the primary entity (not just a modifier) - this is standalone transport
                    locationKey = 'transport_' + (transport.id || transport.rawText);
                    isStandaloneTransport = true;
                } else {
                    locationKey = 'no-location';
                }

                if (!locationGroups.has(locationKey)) {
                    locationGroups.set(locationKey, {
                        location: location,
                        transports: [],
                        times: [],
                        durations: [],
                        isStandaloneTransport: isStandaloneTransport
                    });
                }

                const group = locationGroups.get(locationKey);

                // Add transport only if not already in array (deduplicate by ID)
                // For standalone transport, don't add to transports array since it's the main entity
                if (transport && !isStandaloneTransport) {
                    const transportId = transport.sourceEntityId || transport.id;
                    if (!group.transports.some(t => (t.sourceEntityId || t.id) === transportId)) {
                        group.transports.push(transport);
                    }
                }

                // Add ALL time entities found (not just one)
                timeEntitiesToAdd.forEach(timeEntity => {
                    const timeId = timeEntity.sourceEntityId || timeEntity.id;
                    if (!group.times.some(t => (t.sourceEntityId || t.id) === timeId)) {
                        group.times.push(timeEntity);
                    }
                });

                // Check for duration entity in this relationship
                if (rel.durationEntityId) {
                    const durationEntity = entryEntities.find(e => e.id === rel.durationEntityId);
                    if (durationEntity && durationEntity.entityType === 6) {
                        const durationId = durationEntity.sourceEntityId || durationEntity.id;
                        if (!group.durations.some(d => (d.sourceEntityId || d.id) === durationId)) {
                            group.durations.push(durationEntity);
                        }
                    }
                } else if (rel.primaryEntity?.entityType === 6) {
                    // Duration is primary entity
                    const durationId = rel.primaryEntity.sourceEntityId || rel.primaryEntity.id;
                    if (!group.durations.some(d => (d.sourceEntityId || d.id) === durationId)) {
                        group.durations.push(rel.primaryEntity);
                    }
                } else if (rel.relatedEntity?.entityType === 6) {
                    // Duration is related entity
                    const durationId = rel.relatedEntity.sourceEntityId || rel.relatedEntity.id;
                    if (!group.durations.some(d => (d.sourceEntityId || d.id) === durationId)) {
                        group.durations.push(rel.relatedEntity);
                    }
                }
            });

            // Create history entries for each location group
            locationGroups.forEach((group, locationKey) => {
                let locationText = '';
                let transportText = '';

                // Handle standalone transport (e.g., "+John >bus" without location)
                if (group.isStandaloneTransport && locationKey.startsWith('transport_')) {
                    // Extract the transport entity from the locationKey or first relationship
                    const standaloneTransport = enrichedRelationships.find(rel => 
                        rel.primaryEntity?.entityType === 4 && 
                        'transport_' + (rel.primaryEntity.id || rel.primaryEntity.rawText) === locationKey
                    )?.primaryEntity;

                    locationText = standaloneTransport ? (standaloneTransport.rawText || standaloneTransport.normalizedValue) : '—';
                    // For standalone transport, location IS the transport, so don't show it separately
                } else {
                    // Regular location-based entry
                    locationText = group.location 
                        ? (group.location.rawText || group.location.normalizedValue)
                        : '—';

                    // Build transport string (separate, not concatenated with location)
                    if (group.transports.length > 0) {
                        transportText = group.transports.map(t => t.rawText).join(', ');
                    }
                }

                // Build time/duration string
                let timeDuration = '—';
                if (group.times.length > 0) {
                    if (group.durations.length > 0) {
                        const timeTexts = group.times.map(t => t.rawText).join(', ');
                        const durationText = group.durations.map(d => d.rawText).join(', ');
                        timeDuration = `${timeTexts} (${durationText})`;
                    } else if (group.times.length === 2) {
                        // If two times, treat as start and end
                        const startTime = group.times[0].rawText;
                        const endTime = group.times[1].rawText;

                        // Only show duration format if times are different
                        if (startTime.toLowerCase() === endTime.toLowerCase()) {
                            timeDuration = startTime;
                        } else {
                            const duration = this.calculateDuration(startTime, endTime);
                            timeDuration = `${startTime} - ${endTime}${duration ? ` (${duration})` : ''}`;
                        }
                    } else if (group.times.length > 2) {
                        // Multiple times - show earliest to latest
                        const timeTexts = group.times.map(t => t.rawText);
                        const sortedTimes = this.sortTimes(timeTexts);
                        const earliest = sortedTimes[0];
                        const latest = sortedTimes[sortedTimes.length - 1];
                        const duration = this.calculateDuration(earliest, latest);
                        timeDuration = `${earliest} - ${latest}${duration ? ` (${duration})` : ''}`;
                    } else {
                        timeDuration = group.times[0].rawText;
                    }
                }

                history.push({
                    date: entryDate,
                    location: locationText,
                    transport: transportText, // Separate transport field
                    timeDuration: timeDuration,
                    entryText: entryText.length > 50 ? entryText.substring(0, 50) + '...' : entryText
                });
            });
        });

        return history;
    }

    /**
     * Sort time strings chronologically
     * @param {Array<string>} times - Array of time strings (e.g., ["9AM", "11:30AM", "2PM", "14:00"])
     * @returns {Array<string>} Sorted array of time strings
     */
    sortTimes(times) {
        const parseTime = (timeStr) => {
            timeStr = timeStr.trim();

            // Try AM/PM format
            let match = timeStr.match(/(\d+):?(\d{2})?\s*(AM|PM)/i);
            if (match) {
                let hours = parseInt(match[1]);
                const minutes = match[2] ? parseInt(match[2]) : 0;
                const period = match[3].toUpperCase();

                if (period === 'PM' && hours !== 12) hours += 12;
                if (period === 'AM' && hours === 12) hours = 0;

                return hours * 60 + minutes;
            }

            // Try 24-hour format
            match = timeStr.match(/^(\d{1,2}):?(\d{2})$/);
            if (match) {
                const hours = parseInt(match[1]);
                const minutes = parseInt(match[2]);

                if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60) {
                    return hours * 60 + minutes;
                }
            }

            // Try hour only
            match = timeStr.match(/^(\d{1,2})$/);
            if (match) {
                const hours = parseInt(match[1]);
                if (hours >= 0 && hours < 24) {
                    return hours * 60;
                }
            }

            return 999999; // Invalid times sort to end
        };

        return times.slice().sort((a, b) => parseTime(a) - parseTime(b));
    }

    /**
     * Calculate duration between two time strings
     * @param {string} startTime - Start time (e.g., "9AM", "9:00AM", "9:00 AM", "09:00")
     * @param {string} endTime - End time (e.g., "11AM", "11:00AM", "11:00 AM", "11:00")
     * @returns {string} Duration string (e.g., "2h", "2h 30m")
     */
    calculateDuration(startTime, endTime) {
        try {
            // Enhanced time parser supporting multiple formats
            const parseTime = (timeStr) => {
                // Remove extra spaces and normalize
                timeStr = timeStr.trim();

                // Try AM/PM format first (e.g., "9AM", "9:00AM", "9:00 AM")
                let match = timeStr.match(/(\d+):?(\d{2})?\s*(AM|PM)/i);
                if (match) {
                    let hours = parseInt(match[1]);
                    const minutes = match[2] ? parseInt(match[2]) : 0;
                    const period = match[3].toUpperCase();

                    if (period === 'PM' && hours !== 12) hours += 12;
                    if (period === 'AM' && hours === 12) hours = 0;

                    return hours * 60 + minutes;
                }

                // Try 24-hour format (e.g., "09:00", "14:30", "1430")
                match = timeStr.match(/^(\d{1,2}):?(\d{2})$/);
                if (match) {
                    const hours = parseInt(match[1]);
                    const minutes = parseInt(match[2]);

                    if (hours >= 0 && hours < 24 && minutes >= 0 && minutes < 60) {
                        return hours * 60 + minutes;
                    }
                }

                // Try hour only (e.g., "9", "14") - assume on the hour
                match = timeStr.match(/^(\d{1,2})$/);
                if (match) {
                    const hours = parseInt(match[1]);
                    if (hours >= 0 && hours < 24) {
                        return hours * 60;
                    }
                }

                return null;
            };

            const startMinutes = parseTime(startTime);
            const endMinutes = parseTime(endTime);

            if (startMinutes === null || endMinutes === null) return null;

            let diffMinutes = endMinutes - startMinutes;
            if (diffMinutes < 0) diffMinutes += 24 * 60; // Handle overnight

            const hours = Math.floor(diffMinutes / 60);
            const minutes = diffMinutes % 60;

            if (hours > 0 && minutes > 0) {
                return `${hours}h ${minutes}m`;
            } else if (hours > 0) {
                return `${hours}h`;
            } else {
                return `${minutes}m`;
            }
        } catch (error) {
            console.warn('Could not calculate duration:', error);
            return null;
        }
    }

    /**
     * Toggle person detail row visibility
     * @param {string} personId - The person entity ID
     */
    togglePersonDetails(personId) {
        const detailRow = document.getElementById(`person-details-${personId}`);
        const expandIcon = document.querySelector(`[data-person-id="${personId}"] .expand-icon`);

        if (detailRow && expandIcon) {
            const isVisible = detailRow.style.display !== 'none';
            detailRow.style.display = isVisible ? 'none' : 'table-row';
            expandIcon.textContent = isVisible ? '▶' : '▼';
        }
    }

    // getConfidenceClass() - REMOVED: No longer needed without parser

    editEntityFromSummary(entityId, entryId) {
        const entities = this.entryEntities[entryId] || [];
        const entity = entities.find(e => e.id === entityId);
        if (entity) {
            this.openEntityEditModal(entity, entryId);
        }
    }

    openEntityEditModal(entity, entryId) {
        const modal = document.getElementById('entityEditModal');
        if (!modal) return;

        const typeName = this.entityTypeMap[entity.entityType] || 'Unknown';

        // Populate modal fields
        document.getElementById('editEntityId').value = entity.id;
        document.getElementById('editEntryId').value = entryId;
        document.getElementById('editEntityType').textContent = typeName;
        document.getElementById('editEntityOriginal').textContent = entity.rawText;
        document.getElementById('editEntityDisplayName').value = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
        document.getElementById('editEntityConfirmed').checked = entity.isConfirmed || false;

        // Populate relationships
        this.populateEntityRelationships(entity.id, entryId);

        // Show modal (custom overlay, not Bootstrap)
        modal.style.display = 'flex';

        // Focus first input for keyboard navigation
        setTimeout(() => {
            document.getElementById('editEntityDisplayName')?.focus();
        }, 100);

        // ESC key to close
        const closeHandler = (e) => {
            if (e.key === 'Escape') {
                modal.style.display = 'none';
                document.removeEventListener('keydown', closeHandler);
            }
        };
        document.addEventListener('keydown', closeHandler);
    }

    populateEntityRelationships(entityId, entryId) {
        const listDiv = document.getElementById('entityRelationshipsList');
        if (!listDiv) return;

        const relationships = this.getEntityRelationships(entityId);
        const relationshipTypeMap = this.getRelationshipTypeMap();

        if (relationships.length === 0) {
            listDiv.innerHTML = '<div style="color: #858585; font-size: 13px; padding: 0.5rem;">No relationships detected</div>';
            return;
        }

        let html = '';
        relationships.forEach((rel, index) => {
            const relType = relationshipTypeMap[rel.relationType] || { name: 'Related', icon: 'bi-link' };
            const relatedEntityTypeName = this.entityTypeMap[rel.relatedEntity.entityType] || 'unknown';
            const relatedDisplayName = rel.relatedEntity.linkedRecordDisplayName || rel.relatedEntity.rawText;
            const timeText = rel.timeEntity ? ` at ${rel.timeEntity.rawText}` : '';

            html += `
                <div style="display: flex; align-items: center; justify-content: space-between; padding: 0.5rem; background: #2d2d30; border: 1px solid #3e3e42; border-radius: 4px; margin-bottom: 0.5rem;">
                    <div style="display: flex; align-items: center; gap: 0.5rem;">
                        <i class="bi ${relType.icon}" style="color: #858585;"></i>
                        <span style="color: #cccccc; font-size: 13px;">${relType.description}</span>
                        <span style="color: #4ec9b0; font-size: 13px;">${this.escapeHtml(relatedDisplayName)}</span>
                        ${timeText ? `<span style="color: #858585; font-size: 12px;">${timeText}</span>` : ''}
                    </div>
                    <button type="button" style="background: none; border: none; color: #c72e2e; cursor: pointer; padding: 0.25rem 0.5rem; border-radius: 3px;" 
                            onclick="window.timelineEntry.removeRelationship('${rel.id}', '${rel.entryId}')" title="Remove relationship"
                            onmouseover="this.style.background='#3e3e42'" onmouseout="this.style.background='none'">
                        <i class="bi bi-x"></i>
                    </button>
                </div>
            `;
        });
        html += '</div>';
        listDiv.innerHTML = html;
    }

    removeRelationship(relationshipId, entryId) {
        if (!confirm('Remove this relationship?')) return;

        const relationships = this.entryRelationships[entryId] || [];
        const index = relationships.findIndex(r => r.id === relationshipId);

        if (index !== -1) {
            relationships.splice(index, 1);
            this.entryRelationships[entryId] = relationships;
            this.unsavedChanges = true;

            // Refresh modal and UI
            const entityId = document.getElementById('editEntityId').value;
            this.populateEntityRelationships(entityId, entryId);
            this.updateRelationshipTimeline();
        }
    }

    showAddRelationshipForm() {
        alert('Manual relationship creation coming soon! For now, relationships are detected automatically from your narrative text.');
        // TODO: Implement form to manually create relationships
    }

    saveEntityEdit() {
        const entityId = document.getElementById('editEntityId').value;
        const entryId = document.getElementById('editEntryId').value;
        const displayName = document.getElementById('editEntityDisplayName').value;
        const isConfirmed = document.getElementById('editEntityConfirmed').checked;

        // Update entity in storage
        const entities = this.entryEntities[entryId] || [];
        const entity = entities.find(e => e.id === entityId);

        if (entity) {
            entity.linkedRecordDisplayName = displayName;
            entity.isConfirmed = isConfirmed;
            entity.confidence = isConfirmed ? 3 : entity.confidence; // High confidence if confirmed

            // Update UI
            this.updateEntitySummary();

            // Re-highlight entities in the textarea
            const block = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
            if (block) {
                const textarea = block.querySelector('.narrative-textarea');
                if (textarea) {
                    this.highlightEntities(entryId, textarea.value, entities);
                }
            }

            this.unsavedChanges = true;

            // Close modal (custom overlay)
            document.getElementById('entityEditModal').style.display = 'none';

            this.showSuccess('Entity updated successfully');
        }
    }

    deleteEntity() {
        const entityId = document.getElementById('editEntityId').value;
        const entryId = document.getElementById('editEntryId').value;

        if (!confirm('Are you sure you want to remove this entity?')) {
            return;
        }

        // Remove entity from storage
        const entities = this.entryEntities[entryId] || [];
        const index = entities.findIndex(e => e.id === entityId);

        if (index !== -1) {
            entities.splice(index, 1);
            this.entryEntities[entryId] = entities;

            // Update UI
            this.updateEntitySummary();

            // Re-highlight entities in the textarea
            const block = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
            if (block) {
                const textarea = block.querySelector('.narrative-textarea');
                if (textarea) {
                    this.highlightEntities(entryId, textarea.value, entities);
                }
            }

            this.unsavedChanges = true;

            // Close modal (custom overlay)
            document.getElementById('entityEditModal').style.display = 'none';
        }
    }

    showLoading(show) {
        // TODO: Implement loading overlay
        console.log('Loading:', show);
    }

    showSuccess(message) {
        // TODO: Implement toast notification
        alert(message);
    }

    showError(message) {
        // TODO: Implement error notification
        alert('Error: ' + message);
    }

    // Relationship mapping
    getRelationshipTypeMap() {
        return {
            1: { name: 'With', icon: 'bi-person-plus', description: 'accompanied' },
            2: { name: 'At Location', icon: 'bi-geo-alt', description: 'at' },
            3: { name: 'Via Transport', icon: 'bi-bus-front', description: 'via' },
            4: { name: 'At Event', icon: 'bi-calendar-event', description: 'during' },
            5: { name: 'At Time', icon: 'bi-clock', description: 'at' },
            6: { name: 'For Duration', icon: 'bi-hourglass-split', description: 'for' },
            7: { name: 'Co-occurred', icon: 'bi-link-45deg', description: 'with' },
            8: { name: 'Sequence', icon: 'bi-arrow-right', description: 'then' },
            9: { name: 'Met', icon: 'bi-people', description: 'met' },
            10: { name: 'Activity', icon: 'bi-activity', description: 'doing' }
        };
    }

    // Get all relationships for an entity
    getEntityRelationships(entityId) {
        const relationships = [];

        // Search through all entries
        Object.entries(this.entryRelationships).forEach(([entryId, entryRels]) => {
            const entryEntities = this.entryEntities[entryId] || [];

            entryRels.forEach(rel => {
                // Check if this relationship involves our entity
                if (rel.primaryEntityId === entityId || rel.relatedEntityId === entityId) {
                    // Find the related entity details
                    const relatedEntityId = rel.primaryEntityId === entityId ? 
                        rel.relatedEntityId : rel.primaryEntityId;
                    const relatedEntity = entryEntities.find(e => e.id === relatedEntityId);

                    // Find time entity if specified
                    const timeEntity = rel.timeEntityId ? 
                        entryEntities.find(e => e.id === rel.timeEntityId) : null;

                    if (relatedEntity) {
                        relationships.push({
                            ...rel,
                            relatedEntity,
                            timeEntity,
                            entryId
                        });
                    }
                }
            });
        });

        return relationships;
    }

    // Get relationship HTML for entity card
    getRelationshipBadges(entityId) {
        const relationships = this.getEntityRelationships(entityId);
        const relationshipTypeMap = this.getRelationshipTypeMap();

        if (relationships.length === 0) {
            return '';
        }

        let html = '<div class="entity-relationships mt-2">';
        html += '<small class="text-muted d-block mb-1">Related to:</small>';
        html += '<div class="d-flex flex-wrap gap-1">';

        relationships.forEach(rel => {
            const relType = relationshipTypeMap[rel.relationType] || { name: 'Unknown', icon: 'bi-question' };
            const relatedTypeName = this.entityTypeMap[rel.relatedEntity.entityType] || 'unknown';
            const timeText = rel.timeEntity ? ` ${rel.timeEntity.rawText}` : '';

            html += `<span class="badge bg-light text-dark border" title="${relType.description} ${rel.relatedEntity.rawText}${timeText}">
                        <i class="bi ${relType.icon}"></i> 
                        <span class="entity-${relatedTypeName.toLowerCase()}">${rel.relatedEntity.rawText}</span>
                        ${timeText}
                     </span>`;
        });

        html += '</div>';
        html += '</div>';

        return html;
    }

    /**
     * Get group badge HTML for entity card (shows which groups this entity belongs to)
     */
    getGroupBadges(entityId) {
        const groups = Object.values(this.entityGroups).filter(g => 
            g.entityIds.includes(entityId)
        );

        if (groups.length === 0) {
            return '';
        }

        let html = '<div class="entity-groups mt-1">';
        groups.forEach(group => {
            html += `<span class="group-badge" title="Member of group #${group.name}">
                        <i class="bi bi-people-fill"></i> #${this.escapeHtml(group.name)}
                     </span>`;
        });
        html += '</div>';

        return html;
    }

    // autoPromptLocationConfirmation() - REMOVED: No longer needed without parser
    // promptLocationConfirmation() - REMOVED: No longer needed without parser
    // Location confirmation now handled entirely through manual entity creation via ".." trigger

    setupPlacesAutocomplete(input, entity, entryId) {
        let debounceTimeout;

        input.addEventListener('input', async (e) => {
            clearTimeout(debounceTimeout);
            const searchText = e.target.value.trim();

            if (searchText.length < 2) {
                return;
            }

            debounceTimeout = setTimeout(async () => {
                try {
                    // Build URL with location bias if available
                    let url = `/api/places-suggest?q=${encodeURIComponent(searchText)}`;
                    if (this.patientLocation?.latitude && this.patientLocation?.longitude) {
                        url += `&lat=${this.patientLocation.latitude}&lon=${this.patientLocation.longitude}`;
                    }

                    const response = await fetch(url);
                    if (!response.ok) return;

                    const places = await response.json();
                    const resultsDiv = input.closest('.prompt-body').querySelector('.location-search-results');

                    resultsDiv.innerHTML = places.map(place => `
                        <div class="location-result" data-place='${JSON.stringify(place)}'>
                            <i class="bi bi-pin-map"></i>
                            <span>${place.displayName || place.description}</span>
                        </div>
                    `).join('');

                    // Add click handlers
                    resultsDiv.querySelectorAll('.location-result').forEach(result => {
                        result.addEventListener('click', () => {
                            const placeData = JSON.parse(result.dataset.place);
                            this.selectPlace(entity, entryId, placeData);
                        });
                    });

                } catch (error) {
                    console.error('Error fetching places:', error);
                }
            }, 300);
        });
    }

    selectPlace(entity, entryId, placeData) {
        console.log('Selected place:', placeData, 'for entity:', entity);

        // Update entity with place details
        const entities = this.entryEntities[entryId] || [];
        const entityIndex = entities.findIndex(e => e.id === entity.id);

        if (entityIndex !== -1) {
            entities[entityIndex].metadata = {
                ...entities[entityIndex].metadata,
                placeId: placeData.placeId,
                displayName: placeData.displayName || placeData.description,
                address: placeData.formattedAddress,
                coordinates: placeData.coordinates
            };
            entities[entityIndex].isConfirmed = true;
            entities[entityIndex].confidence = 3; // High confidence

            this.entryEntities[entryId] = entities;
            this.unsavedChanges = true;

            // Update UI
            this.updateEntitySummary();
            this.updateMapPins();

            // Remove prompt
            document.querySelector(`.location-confirm-prompt[data-entity-id="${entity.id}"]`)?.remove();

            this.showSuccess(`Location confirmed: ${placeData.displayName || placeData.description}`);
        }
    }

    confirmLocationSelection(entityId, entryId) {
        // User manually confirmed without selecting a place
        const entities = this.entryEntities[entryId] || [];
        const entity = entities.find(e => e.id === entityId);

        if (entity) {
            entity.isConfirmed = true;
            this.unsavedChanges = true;
            this.updateEntitySummary();

            document.querySelector(`.location-confirm-prompt[data-entity-id="${entityId}"]`)?.remove();
        }
    }

    getEntityGroupKey(entity) {
        // Generate the same grouping key used in deduplication
        if (entity.personId || entity.locationId || entity.transportId || entity.eventId) {
            const dbId = entity.personId || entity.locationId || entity.transportId || entity.eventId;
            const entityTypeName = this.entityTypeMap[entity.entityType] || 'unknown';
            return `db_${entityTypeName}_${dbId}`;
        } else if (entity.sourceEntityId) {
            return `source_${entity.sourceEntityId}`;
        } else {
            return `source_${entity.id}`;
        }
    }

    getLocationTimeData(entity, entryId) {
        // Get time and duration data for a location entity
        let time = '—';
        let duration = '—';

        if (!entryId) {
            return { time, duration };
        }

        const relationships = this.entryRelationships[entryId] || [];
        const entities = this.entryEntities[entryId] || [];
        const locationRelationships = relationships.filter(r => 
            r.primaryEntityId === entity.id || r.relatedEntityId === entity.id
        );

        const timeTexts = [];

        // Collect all time entities related to this location
        for (const rel of locationRelationships) {
            if (rel.allTimeEntityIds && rel.allTimeEntityIds.length > 0) {
                rel.allTimeEntityIds.forEach(timeId => {
                    const timeEntity = entities.find(e => e.id === timeId);
                    if (timeEntity && !timeTexts.includes(timeEntity.rawText)) {
                        timeTexts.push(timeEntity.rawText);
                    }
                });
            } else if (rel.timeEntityId) {
                const timeEntity = entities.find(e => e.id === rel.timeEntityId);
                if (timeEntity && !timeTexts.includes(timeEntity.rawText)) {
                    timeTexts.push(timeEntity.rawText);
                }
            }
        }

        // Also check if any related entity IS a DateTime entity
        for (const rel of locationRelationships) {
            const relatedId = rel.primaryEntityId === entity.id ? rel.relatedEntityId : rel.primaryEntityId;
            const relatedEntity = entities.find(e => e.id === relatedId);
            if (relatedEntity && relatedEntity.entityType === 5 && !timeTexts.includes(relatedEntity.rawText)) {
                timeTexts.push(relatedEntity.rawText);
            }
        }

        // Fallback to metadata if no relationship time found
        if (timeTexts.length === 0 && entity.metadata?.time) {
            timeTexts.push(entity.metadata.time);
        }

        // Format time display
        if (timeTexts.length === 2) {
            const [time1, time2] = timeTexts;
            const dur = this.calculateDuration(time1, time2);
            time = `${time1} - ${time2}${dur ? ` (${dur})` : ''}`;
        } else if (timeTexts.length > 2) {
            const sortedTimes = this.sortTimes(timeTexts);
            const earliest = sortedTimes[0];
            const latest = sortedTimes[sortedTimes.length - 1];
            const dur = this.calculateDuration(earliest, latest);
            time = `${earliest} - ${latest}${dur ? ` (${dur})` : ''}`;
        } else if (timeTexts.length === 1) {
            time = timeTexts[0];
        }

        // Get duration from related Duration entity
        for (const rel of locationRelationships) {
            if (rel.primaryEntityId === entity.id || rel.relatedEntityId === entity.id) {
                const relatedId = rel.primaryEntityId === entity.id ? rel.relatedEntityId : rel.primaryEntityId;
                const relatedEntity = entities.find(e => e.id === relatedId);
                if (relatedEntity && relatedEntity.entityType === 6) {
                    duration = relatedEntity.rawText;
                    break;
                }
            }
        }

        // Check relationship durationEntityId (stored directly on relationship by parser)
        if (duration === '—') {
            for (const rel of locationRelationships) {
                if ((rel.primaryEntityId === entity.id || rel.relatedEntityId === entity.id) && rel.durationEntityId) {
                    const durationEntity = entities.find(e => e.id === rel.durationEntityId);
                    if (durationEntity) {
                        duration = durationEntity.rawText;
                        break;
                    }
                }
            }
        }

        // Check metadata in relationships for durationEntityId (legacy/fallback)
        if (duration === '—') {
            for (const rel of locationRelationships) {
                if ((rel.primaryEntityId === entity.id || rel.relatedEntityId === entity.id) && rel.metadata?.durationEntityId) {
                    const durationEntity = entities.find(e => e.id === rel.metadata.durationEntityId);
                    if (durationEntity) {
                        duration = durationEntity.rawText;
                        break;
                    }
                }
            }
        }

        // Fallback to metadata
        if (duration === '—' && entity.metadata?.duration) {
            duration = entity.metadata.duration;
        }

        return { time, duration };
    }

    toggleLocationDetails(entityId) {
        const detailRow = document.getElementById(`location-details-${entityId}`);
        const expandIcon = event.target;

        if (detailRow) {
            const isVisible = detailRow.style.display !== 'none';
            detailRow.style.display = isVisible ? 'none' : 'table-row';
            expandIcon.textContent = isVisible ? '▶' : '▼';
        }
    }

    async reloadTimelineAfterSave() {
        try {
            console.log('[TimelineEntry] Reloading timeline after save to update entity IDs');

            // Reload timeline data from server
            const response = await fetch(`/api/timeline/${this.caseId}`);
            if (!response.ok) {
                throw new Error('Failed to reload timeline');
            }

            const reloadedData = await response.json();

            // Update only the entity and relationship data, preserving UI state
            if (reloadedData.entries) {
                reloadedData.entries.forEach(entry => {
                    if (entry.entities) {
                        // Enrich reloaded entities with stable IDs for proper deduplication
                        entry.entities.forEach(entity => {
                            this.enrichEntityWithStableIds(entity);
                        });
                        this.entryEntities[entry.id] = entry.entities;
                    }
                    if (entry.relationships) {
                        this.entryRelationships[entry.id] = entry.relationships;
                    }
                });
            }

            // Update entity summary to reflect proper grouping
            this.updateEntitySummary();

            console.log('[TimelineEntry] Timeline reloaded successfully');
        } catch (error) {
            console.error('[TimelineEntry] Error reloading timeline after save:', error);
            // Non-critical error - grouping will be correct after page refresh
        }
    }

    /**
     * Generate a stable hash-based ID from text
     * This ensures the same entity text always gets the same ID for proper grouping
     * @param {string} text - The text to hash
     * @returns {string} Stable numeric ID
     */
    generateStableId(text) {
        if (!text) return null;

        // Normalize text: lowercase, trim whitespace
        const normalized = text.toLowerCase().trim();

        // Simple hash function (djb2)
        let hash = 5381;
        for (let i = 0; i < normalized.length; i++) {
            hash = ((hash << 5) + hash) + normalized.charCodeAt(i); // hash * 33 + c
        }

        // Convert to positive integer and return as string
        return Math.abs(hash).toString();
    }

    /**
     * Add stable database-like IDs to an entity for proper grouping
     * @param {Object} entity - Entity object to enrich
     * @returns {Object} Entity with stable IDs added
     */
    enrichEntityWithStableIds(entity) {
        if (!entity) return entity;

        const normalizedValue = entity.normalizedValue || entity.rawText;
        if (!normalizedValue) return entity;

        // Generate stable IDs based on entity type and normalized value
        switch(entity.entityType) {
            case 1: // Person
                if (!entity.personId) {
                    entity.personId = this.generateStableId(normalizedValue);
                }
                break;
            case 2: // Location
                if (!entity.locationId) {
                    entity.locationId = this.generateStableId(normalizedValue);
                }
                break;
            case 3: // Event
                if (!entity.eventId) {
                    entity.eventId = this.generateStableId(normalizedValue);
                }
                break;
            case 4: // Transport
                if (!entity.transportId) {
                    entity.transportId = this.generateStableId(normalizedValue);
                }
                break;
        }

        return entity;
    }

    /**
     * Export comprehensive report with all entities in clean card-based layout
     */
    exportComprehensiveReport() {
        // Get case details from page
        const caseId = document.getElementById('caseId')?.value || 'Unknown';

        // Extract patient name and disease from the header section
        let patientName = 'Unknown Patient';
        let diseaseName = 'Unknown Disease';
        let onsetDate = 'Unknown';

        // Look for patient name and disease in the header paragraph
        const headerParagraph = document.querySelector('.case-details-container h1 + p');
        if (headerParagraph) {
            const strongElement = headerParagraph.querySelector('strong');
            if (strongElement) {
                patientName = strongElement.textContent.trim();
            }
            // Disease is in a span after the bullet separator
            const spans = headerParagraph.querySelectorAll('span');
            spans.forEach(span => {
                const text = span.textContent.trim();
                // Skip if it's a chip (has class) or empty
                if (!span.className && text && text !== '•' && !text.includes('Confirmed')) {
                    diseaseName = text;
                }
            });
        }

        // Look for Date of Onset in the case info card
        const infoRows = document.querySelectorAll('.info-row');
        infoRows.forEach(row => {
            const label = row.querySelector('.info-label')?.textContent?.trim();
            const value = row.querySelector('.info-value')?.textContent?.trim();
            if (label === 'Date of Onset' && value) {
                onsetDate = value;
            }
        });

        // Get all entities and deduplicate (same as updateEntitySummary)
        const allEntities = [];
        Object.values(this.entryEntities).forEach(entities => {
            allEntities.push(...entities);
        });

        if (allEntities.length === 0) {
            alert('No data to export');
            return;
        }

        // Deduplicate entities using the same logic as updateEntitySummary
        const entityMap = new Map();
        allEntities.forEach(entity => {
            const groupKey = this.getEntityGroupKey(entity);

            if (!entityMap.has(groupKey)) {
                entityMap.set(groupKey, { 
                    ...entity, 
                    mentions: 1,
                    allInstances: [entity]
                });
            } else {
                const existing = entityMap.get(groupKey);
                existing.mentions++;
                existing.allInstances.push(entity);
            }
        });

        // Group by type (exclude DateTime and Duration)
        const groupedByType = {};
        entityMap.forEach(entity => {
            if (entity.entityType === 5 || entity.entityType === 6) return;

            const typeName = this.getEntityTypeName(entity.entityType);
            if (!groupedByType[typeName]) {
                groupedByType[typeName] = [];
            }
            groupedByType[typeName].push(entity);
        });

        // Generate print HTML
        const printWindow = window.open('', '_blank');
        const currentDate = new Date().toLocaleDateString('en-AU', { 
            day: 'numeric', 
            month: 'long', 
            year: 'numeric' 
        });

        // Avatar color classes
        const avatarColors = ['avatar-blue', 'avatar-green', 'avatar-amber', 'avatar-pink', 'avatar-purple'];

        // Helper to get initials
        const getInitials = (name) => {
            if (!name) return '??';
            const parts = name.trim().split(/\s+/);
            if (parts.length === 1) return parts[0].substring(0, 2).toUpperCase();
            return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
        };

        // Build entity cards HTML
        let entitiesHTML = '';
        const typeOrder = ['Person', 'Location', 'Transport', 'Event'];

        typeOrder.forEach(typeName => {
            const entities = groupedByType[typeName];
            if (!entities || entities.length === 0) return;

            entitiesHTML += `<div class="section-label">${typeName}s (${entities.length})</div>`;

            if (typeName === 'Person') {
                entities.forEach((entity, idx) => {
                    const displayName = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const relationship = entity.metadata?.relationship || '';
                    const initials = getInitials(displayName);
                    const avatarClass = avatarColors[idx % avatarColors.length];

                    // Get relationship history for this person (exposures)
                    const relationshipHistory = this.getPersonRelationshipHistory(entity);

                    entitiesHTML += `
                    <div class="person-card">
                        <div class="person-header">
                            <div class="avatar ${avatarClass}">${this.escapeHtml(initials)}</div>
                            <div>
                                <div class="person-name">${this.escapeHtml(displayName)}</div>
                                ${relationship ? `<div class="person-rel">${this.escapeHtml(relationship)}</div>` : ''}
                            </div>
                        </div>`;

                    if (relationshipHistory.length > 0) {
                        entitiesHTML += `
                        <table class="data-table">
                            <thead>
                                <tr>
                                    <th>Date</th>
                                    <th>Location</th>
                                    <th>Time / Duration</th>
                                </tr>
                            </thead>
                            <tbody>`;

                        relationshipHistory.forEach(rel => {
                            entitiesHTML += `
                                <tr>
                                    <td><span class="date-badge">${this.escapeHtml(rel.date)}</span></td>
                                    <td><span class="location-name">${this.escapeHtml(rel.location)}</span>${rel.transport ? `<br><span style="font-size: 11px; color: #888;">via ${this.escapeHtml(rel.transport)}</span>` : ''}</td>
                                    <td>${this.escapeHtml(rel.timeDuration)}</td>
                                </tr>`;
                        });

                        entitiesHTML += `
                            </tbody>
                        </table>`;
                    } else {
                        entitiesHTML += `<div style="font-size: 12px; color: #888; font-style: italic;">No exposures recorded</div>`;
                    }

                    entitiesHTML += `</div>`;
                });

            } else if (typeName === 'Location') {
                entities.forEach(entity => {
                    const displayName = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const address = entity.metadata?.address || '';
                    const visitCount = entity.mentions || 1;

                    // Collect all occurrences of this location
                    const locationOccurrences = [];
                    const groupKey = this.getEntityGroupKey(entity);

                    Object.entries(this.entryEntities).forEach(([entryId, entryEntityList]) => {
                        entryEntityList.forEach(e => {
                            const eGroupKey = this.getEntityGroupKey(e);
                            if (eGroupKey === groupKey && e.entityType === entity.entityType) {
                                let entryDate = '—';
                                if (this.timelineData?.entries) {
                                    const entry = this.timelineData.entries.find(entry => entry.id === entryId);
                                    if (entry?.entryDate) {
                                        const date = new Date(entry.entryDate);
                                        entryDate = date.toLocaleDateString('en-AU', { 
                                            day: 'numeric', 
                                            month: 'short', 
                                            year: 'numeric',
                                            weekday: 'short'
                                        });
                                    }
                                }
                                const timeData = this.getLocationTimeData(e, entryId);
                                locationOccurrences.push({
                                    date: entryDate,
                                    time: timeData.time,
                                    duration: timeData.duration
                                });
                            }
                        });
                    });

                    entitiesHTML += `
                    <div class="location-card">
                        <div class="loc-header">
                            <div>
                                <div class="loc-name">${this.escapeHtml(displayName)}</div>
                                ${address ? `<div class="loc-address">${this.escapeHtml(address)}</div>` : ''}
                            </div>
                            <span class="visit-count">${visitCount} visit${visitCount !== 1 ? 's' : ''}</span>
                        </div>`;

                    locationOccurrences.forEach(occ => {
                        entitiesHTML += `
                        <div class="visit-row">
                            <span class="visit-date">${this.escapeHtml(occ.date)}</span>
                            <span class="visit-time">${this.escapeHtml(occ.time)}</span>
                            <span class="visit-dur">${this.escapeHtml(occ.duration)}</span>
                        </div>`;
                    });

                    entitiesHTML += `</div>`;
                });

            } else if (typeName === 'Transport') {
                entities.forEach(entity => {
                    const displayName = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const details = entity.metadata?.details || '';
                    const mentionCount = entity.mentions || 1;

                    entitiesHTML += `
                    <div class="person-card">
                        <div class="person-header">
                            <div class="avatar avatar-blue">🚗</div>
                            <div>
                                <div class="person-name">${this.escapeHtml(displayName)}</div>
                                ${details ? `<div class="person-rel">${this.escapeHtml(details)}</div>` : ''}
                            </div>
                        </div>
                        <div style="font-size: 12px; color: #666;">Mentioned ${mentionCount} time${mentionCount !== 1 ? 's' : ''}</div>
                    </div>`;
                });

            } else if (typeName === 'Event') {
                entities.forEach(entity => {
                    const displayName = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const description = entity.metadata?.description || '';
                    const mentionCount = entity.mentions || 1;

                    entitiesHTML += `
                    <div class="person-card">
                        <div class="person-header">
                            <div class="avatar avatar-purple">📅</div>
                            <div>
                                <div class="person-name">${this.escapeHtml(displayName)}</div>
                                ${description ? `<div class="person-rel">${this.escapeHtml(description)}</div>` : ''}
                            </div>
                        </div>
                        <div style="font-size: 12px; color: #666;">Mentioned ${mentionCount} time${mentionCount !== 1 ? 's' : ''}</div>
                    </div>`;
                });
            }

            if (typeName !== typeOrder[typeOrder.length - 1] && groupedByType[typeOrder[typeOrder.indexOf(typeName) + 1]]?.length > 0) {
                entitiesHTML += `<hr class="divider">`;
            }
        });

        const printHTML = `
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Exposure Timeline Report</title>
    <style>
        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }

        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, sans-serif;
            font-size: 13px;
            line-height: 1.5;
            color: #1a1a1a;
            background: #f5f4f0;
            padding: 1.5rem 1rem;
        }

        .report {
            max-width: 900px;
            margin: 0 auto;
        }

        .report-header {
            margin-bottom: 1.25rem;
        }

        .report-title {
            font-size: 20px;
            font-weight: 600;
            color: #1a1a1a;
            margin-bottom: 0.4rem;
        }

        .meta-row {
            display: flex;
            gap: 1.5rem;
            flex-wrap: wrap;
            margin-top: 0.4rem;
        }

        .meta-item {
            font-size: 10px;
            color: #888;
            text-transform: uppercase;
            letter-spacing: 0.06em;
        }

        .meta-item span {
            color: #1a1a1a;
            font-weight: 500;
            text-transform: none;
            letter-spacing: 0;
        }

        .section-label {
            font-size: 10px;
            font-weight: 600;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            color: #aaa;
            margin-bottom: 0.5rem;
            margin-top: 0.2rem;
        }

        .divider {
            border: none;
            border-top: 1px solid #e5e3dc;
            margin: 1rem 0;
        }

        .person-card,
        .location-card {
            background: #fff;
            border: 1px solid #e5e3dc;
            border-radius: 8px;
            padding: 0.65rem 0.85rem;
            margin-bottom: 0.5rem;
            page-break-inside: avoid;
        }

        .person-header {
            display: flex;
            align-items: center;
            gap: 8px;
            margin-bottom: 0.55rem;
        }

        .avatar {
            width: 28px;
            height: 28px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 11px;
            font-weight: 600;
            flex-shrink: 0;
        }

        .avatar-blue   { background: #ddeeff; color: #1a4e8a; }
        .avatar-green  { background: #d9f0e5; color: #1a5c40; }
        .avatar-amber  { background: #fdefd4; color: #7a4b0a; }
        .avatar-pink   { background: #fce4ef; color: #8a1a48; }
        .avatar-purple { background: #ece8fc; color: #4a2eb0; }

        .person-name {
            font-size: 13px;
            font-weight: 600;
            color: #1a1a1a;
        }

        .person-rel {
            font-size: 11px;
            color: #888;
            margin-top: 0px;
        }

        .data-table {
            width: 100%;
            border-collapse: collapse;
            font-size: 11px;
        }

        .data-table th {
            font-size: 10px;
            font-weight: 600;
            color: #bbb;
            text-transform: uppercase;
            letter-spacing: 0.06em;
            padding: 3px 6px 4px 0;
            border-bottom: 1px solid #e5e3dc;
            text-align: left;
            white-space: nowrap;
        }

        .data-table td {
            padding: 5px 6px 5px 0;
            border-bottom: 1px solid #e5e3dc;
            color: #1a1a1a;
            vertical-align: middle;
        }

        .data-table tr:last-child td {
            border-bottom: none;
        }

        .date-badge {
            display: inline-block;
            background: #f5f4f0;
            border-radius: 3px;
            padding: 1px 6px;
            font-size: 10px;
            color: #666;
            white-space: nowrap;
        }

        .location-name {
            font-weight: 500;
        }

        .loc-header {
            display: flex;
            align-items: flex-start;
            justify-content: space-between;
            gap: 10px;
            margin-bottom: 0.5rem;
        }

        .loc-name {
            font-size: 13px;
            font-weight: 600;
            color: #1a1a1a;
        }

        .loc-address {
            font-size: 11px;
            color: #888;
            margin-top: 2px;
            line-height: 1.4;
        }

        .visit-count {
            font-size: 10px;
            color: #666;
            background: #f5f4f0;
            border-radius: 20px;
            padding: 2px 8px;
            white-space: nowrap;
            flex-shrink: 0;
        }

        .visit-row {
            display: flex;
            gap: 1rem;
            padding: 5px 0;
            border-bottom: 1px solid #e5e3dc;
            font-size: 11px;
            align-items: center;
        }

        .visit-row:last-child {
            border-bottom: none;
        }

        .visit-date { color: #1a1a1a; min-width: 100px; }
        .visit-time { color: #666; }
        .visit-dur  { color: #888; margin-left: auto; }

        .report-footer {
            font-size: 10px;
            color: #bbb;
            margin-top: 1.5rem;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 0.5rem;
        }

        @media print {
            body {
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
                padding: 1rem 0.5rem;
            }
            .report {
                max-width: 100%;
            }
        }
    </style>
</head>
<body>
<div class="report">
    <div class="report-header">
        <div class="report-title">Exposure Timeline Report</div>
        <div class="meta-row">
            <div class="meta-item">Case <span>${this.escapeHtml(caseId)}</span></div>
            <div class="meta-item">Patient <span>${this.escapeHtml(patientName)}</span></div>
            <div class="meta-item">Disease <span>${this.escapeHtml(diseaseName)}</span></div>
            <div class="meta-item">Onset <span>${this.escapeHtml(onsetDate)}</span></div>
            <div class="meta-item">Generated <span>${currentDate}</span></div>
        </div>
    </div>

    ${entitiesHTML}

    <div class="report-footer">
        <span>Sentinel · Exposure Timeline System</span>
        <span>Case ${this.escapeHtml(caseId)}</span>
    </div>
</div>

<script>
    window.onload = function() {
        window.print();
    };
</script>
</body>
</html>
        `;

        printWindow.document.write(printHTML);
        printWindow.document.close();
    }

    /**
     * Get contacts for a person entity
     */
    getPersonContacts(personEntity, allEntities, allRelationships) {
        const contacts = new Set();

        allRelationships.forEach(rel => {
            if (rel.primaryEntityId === personEntity.id || rel.relatedEntityId === personEntity.id) {
                const otherId = rel.primaryEntityId === personEntity.id ? rel.relatedEntityId : rel.primaryEntityId;
                const otherEntity = allEntities.find(e => e.id === otherId);

                if (otherEntity && otherEntity.entityType === 1 && otherEntity.id !== personEntity.id) {
                    const name = otherEntity.linkedRecordDisplayName || otherEntity.normalizedValue || otherEntity.rawText;
                    contacts.add(name);
                }
            }
        });

        return Array.from(contacts);
    }

    /**
     * Get relationship type name from relationship type number
     */
    getRelationshipTypeName(relType) {
        const types = {
            1: 'WITH',
            2: 'AT_LOCATION',
            3: 'VIA',
            4: 'AT_EVENT',
            5: 'AT_TIME',
            6: 'FOR_DURATION',
            7: 'CO_OCCURRED',
            8: 'SEQUENCE',
            9: 'MET',
            10: 'ACTIVITY'
        };
        return types[relType] || 'Unknown';
    }
}

