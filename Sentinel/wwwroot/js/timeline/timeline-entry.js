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

        // Load patient location for biasing place searches
        await this.loadPatientLocation();

        // Load existing timeline data
        await this.loadTimeline();

        // Initialize components
        this.initializeEventListeners();
        this.initializeMap();

        // Initialize relationship syntax parser
        this.syntaxParser = new RelationshipSyntaxParser();

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
                this.renderExistingEntries();
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

    renderExistingEntries() {
        const container = document.getElementById('timelineContainer');
        container.innerHTML = '';

        this.timelineData.entries
            .sort((a, b) => new Date(a.entryDate) - new Date(b.entryDate))
            .forEach(entry => {
                this.addDayBlock(new Date(entry.entryDate), entry);
            });

        this.updateEntitySummary();
        this.updateGroupsList();
        this.updateMapPins();
    }

    addDayBlock(date = null, existingEntry = null) {
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
        textarea.addEventListener('input', (e) => this.handleTextInput(e, entryId));
        textarea.addEventListener('keydown', (e) => this.handleKeyDown(e, entryId));

        const dateInput = dayBlock.querySelector('input[type="date"]');
        dateInput.addEventListener('change', (e) => this.handleDateChange(e, entryId));

        // If there's existing entry data with entities, highlight them
        if (existingEntry && existingEntry.narrativeText) {
            if (existingEntry.entities && existingEntry.entities.length > 0) {
                this.entryEntities[entryId] = existingEntry.entities;
                this.highlightEntities(entryId, existingEntry.narrativeText, existingEntry.entities);
            }
            if (existingEntry.relationships && existingEntry.relationships.length > 0) {
                this.entryRelationships[entryId] = existingEntry.relationships;
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
        this.parseAndCreateRelationships(entryId, text);

        // Update UI panels
        this.updateEntitySummary();
        this.updateGroupsList();
        this.updateRelationshipTimeline();
        this.updateMapPins();
    }

    /**
     * Parse relationship syntax in text and automatically create relationships
     * Syntax: @person @location >transport @time.
     * Example: "went to ..sushi train @john @mary @1PM."
     * Groups: @#Siblings expands to all members
     * Inline creation: #Family(..John ..Mary) creates group and expands
     */
    async parseAndCreateRelationships(entryId, text) {
        if (!this.syntaxParser || !text) return;

        // Process inline group creation first: #GroupName(...) -> API + expansion
        text = await this.processInlineGroupCreation(text, entryId);

        // NOTE: We do NOT expand @#GroupName in the text itself
        // Groups are expanded only during relationship parsing below
        // This keeps the text clean and avoids position tracking issues

        // Get existing entities for this entry
        const entities = this.entryEntities[entryId] || [];
        if (entities.length < 2) return; // Need at least 2 entities to create relationships

        // Parse text for relationship syntax
        const parsed = this.syntaxParser.parse(text);
        if (!parsed || !Array.isArray(parsed) || parsed.length === 0) {
            console.log('[TimelineEntry] No relationship syntax found');
            return;
        }

        console.log('[TimelineEntry] Parsed relationship groups:', parsed);

        // Create relationships from each parsed group
        const allRelationships = [];
        parsed.forEach((group, groupIndex) => {
            // Resolve syntax markers to actual entities (expand groups here)
            const resolvedEntities = this.resolveGroupEntities(group, entities);

            if (resolvedEntities.length < 2) {
                console.log('[TimelineEntry] Not enough resolved entities for group', groupIndex);
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

        console.log('[TimelineEntry] Created relationships:', allRelationships);
    }

    /**
     * Process inline group creation syntax: #GroupName(...entities...)
     * Creates groups automatically and expands them inline
     * @param {string} text - Text with potential inline group creation
     * @param {string} entryId - Entry ID for entity resolution
     * @returns {Promise<string>} Text with inline groups expanded
     */
    async processInlineGroupCreation(text, entryId) {
        if (!text) return text;

        // Pattern: #GroupName(..entity1 +entity2 ..entity3)
        const inlineGroupPattern = /#(\w+)\(([^)]+)\)/g;
        const matches = [...text.matchAll(inlineGroupPattern)];

        if (matches.length === 0) return text;

        let processedText = text;
        const entities = this.entryEntities[entryId] || [];

        for (let match of matches) {
            const fullMatch = match[0]; // e.g., "#Siblings(..john ..mary)" or "#Siblings( John Cathy)"
            const groupName = match[1]; // e.g., "Siblings"
            const entitiesText = match[2]; // e.g., "..john ..mary" or " John Cathy"

            console.log(`[TimelineEntry] Processing inline group creation: ${fullMatch}`);

            // Calculate position range of the group's parentheses in the text
            const groupStartIndex = match.index;
            const groupEndIndex = groupStartIndex + fullMatch.length;
            const parenStartIndex = text.indexOf('(', groupStartIndex);
            const parenEndIndex = text.indexOf(')', parenStartIndex);

            console.log(`[TimelineEntry] Group range: ${groupStartIndex}-${groupEndIndex}, Paren range: ${parenStartIndex}-${parenEndIndex}`);

            // Method 1: Find entities by position (for Quick-Add entities inserted as plain text)
            const entitiesInRange = entities.filter(e => {
                if (e.startPosition !== undefined && e.endPosition !== undefined) {
                    // Entity is within the parentheses if its position overlaps with paren range
                    const entityInParens = e.startPosition >= parenStartIndex && e.endPosition <= parenEndIndex;
                    if (entityInParens) {
                        console.log(`[TimelineEntry] Found entity by position: ${e.rawText} at ${e.startPosition}-${e.endPosition}`);
                    }
                    return entityInParens;
                }
                return false;
            });

            // Method 2: Parse entities from markers (..entity, +entity, @entity, etc.) - for manual syntax
            const entityPattern = /(\.\.\w+|[+@>]\s*\w+)/g;
            const entityMatches = [...entitiesText.matchAll(entityPattern)];
            const entityNames = entityMatches.map(m => m[0].replace(/^(\.\.|\.\.|[+@>])\s*/, '').trim());

            console.log(`[TimelineEntry] Found ${entitiesInRange.length} entities by position, ${entityNames.length} by marker pattern`);

            // Combine both methods: prioritize position-based entities, then look up marker-based entities
            const entityIds = [];

            // Add position-based entities first
            for (let entity of entitiesInRange) {
                const entityId = entity.sourceEntityId || entity.id;
                if (!entityIds.includes(entityId)) {
                    entityIds.push(entityId);
                }
            }

            // Add marker-based entities (if any)
            for (let name of entityNames) {
                const matchingEntity = entities.find(e => {
                    const displayText = (e.linkedRecordDisplayName || e.normalizedValue || e.rawText || '').trim();
                    return displayText.toLowerCase().includes(name.toLowerCase()) ||
                           name.toLowerCase().includes(displayText.toLowerCase());
                });
                if (matchingEntity) {
                    const entityId = matchingEntity.sourceEntityId || matchingEntity.id;
                    if (!entityIds.includes(entityId)) {
                        entityIds.push(entityId);
                    }
                }
            }

            if (entityIds.length === 0) {
                console.warn(`[TimelineEntry] No entities found in group definition: ${fullMatch}`);
                continue;
            }

            // Build expansion names from actual entities
            const expansionNames = entityIds.map(id => {
                const entity = entities.find(e => (e.sourceEntityId || e.id) === id);
                return entity ? (entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText || 'Unknown') : 'Unknown';
            });

            // Create group via API
            try {
                const response = await fetch('/api/timeline/groups', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        caseId: this.caseId,
                        name: groupName,
                        entityIds: entityIds
                    })
                });

                if (response.ok) {
                    const createdGroup = await response.json();
                    console.log(`[TimelineEntry] Created group "${groupName}" with ${entityIds.length} entities`);

                    // Add to local cache
                    this.entityGroups[createdGroup.id] = createdGroup;

                    // Expand inline: #Siblings( John Cathy) -> @John @Mary
                    const expansion = expansionNames.map(name => `@${name}`).join(' ');
                    processedText = processedText.replace(fullMatch, expansion);
                    console.log(`[TimelineEntry] Expanded ${fullMatch} to: ${expansion}`);
                } else {
                    console.error(`[TimelineEntry] Failed to create group: ${response.statusText}`);
                }
            } catch (error) {
                console.error(`[TimelineEntry] Error creating group:`, error);
            }
        }

        return processedText;
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
        console.log('[TimelineEntry] Resolving group:', group);
        console.log('[TimelineEntry] Available entities:', entities.map(e => ({ text: e.rawText, type: e.entityType })));

        // Filter entities to only those within this sentence's boundaries
        // This prevents group expansion from crossing sentence boundaries (marked by periods)
        const sentenceEntities = group.startPosition !== undefined && group.endPosition !== undefined
            ? entities.filter(e => {
                // Entity must be fully within the sentence boundaries
                const entityStart = e.startPosition || 0;
                const entityEnd = e.endPosition || 0;
                return entityStart >= group.startPosition && entityEnd <= group.endPosition;
              })
            : entities; // Fallback to all entities if no position info (backward compatibility)

        if (sentenceEntities.length < entities.length) {
            console.log(`[TimelineEntry] Filtered to sentence-only entities: ${entities.length} → ${sentenceEntities.length}`);
        }

        const resolved = [];

        for (let syntaxEntity of group.entities) {
            console.log('[TimelineEntry] Trying to resolve syntax entity:', syntaxEntity);

            // Check if this is a group reference (@#GroupName)
            if (syntaxEntity.marker === '@' && syntaxEntity.text.startsWith('#')) {
                const groupName = syntaxEntity.text.substring(1); // Remove # prefix
                const entityGroup = Object.values(this.entityGroups).find(g => 
                    g.name.toLowerCase() === groupName.toLowerCase()
                );

                if (entityGroup) {
                    console.log(`[TimelineEntry] Expanding group #${groupName} with ${entityGroup.entityIds.length} members`);

                    // Find all entities that are members of this group
                    // IMPORTANT: Only search within sentence-scoped entities to prevent
                    // group expansion from crossing sentence boundaries
                    entityGroup.entityIds.forEach(entityId => {
                        const memberEntity = sentenceEntities.find(e => e.id === entityId);
                        if (memberEntity) {
                            resolved.push({
                                ...memberEntity,
                                isPrimary: syntaxEntity.role === 'primary',
                                relationshipType: syntaxEntity.relationshipType
                            });
                        }
                    });

                    continue; // Skip normal entity matching
                }
            }

            // Normal entity matching (not a group reference)
            // Also use sentence-scoped entities for consistency
            const matchingEntity = sentenceEntities.find(e => {
                const displayText = (e.linkedRecordDisplayName || e.normalizedValue || e.rawText || '').trim();
                const syntaxText = syntaxEntity.text.trim();
                const matches = displayText.toLowerCase().includes(syntaxText.toLowerCase()) ||
                       syntaxText.toLowerCase().includes(displayText.toLowerCase());
                console.log(`[TimelineEntry]   Comparing "${displayText}" with "${syntaxText}": ${matches}`);
                return matches;
            });

            if (matchingEntity) {
                console.log('[TimelineEntry]   ✓ Matched:', matchingEntity.rawText);
                resolved.push({
                    ...matchingEntity,
                    isPrimary: syntaxEntity.role === 'primary',
                    relationshipType: syntaxEntity.relationshipType
                });
            } else {
                console.warn(`[TimelineEntry] Could not resolve entity: "${syntaxEntity.text}" (${syntaxEntity.marker})`);
            }
        }

        console.log('[TimelineEntry] Resolved entities:', resolved);
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

        console.log(`[TimelineEntry] Refreshing highlights for ${entities.length} manual entities`);

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

        // Get relationship groups for this entry
        const relationships = this.entryRelationships[entryId] || [];
        const entityGroupMap = this.buildEntityGroupMap(entities, relationships);

        // First, find all group references (@#GroupName) and their positions
        const groupReferences = [];
        const groupRefPattern = /@#([A-Za-z0-9_-]+)/g;
        let match;
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
            } else if (entity.sourceEntityId) {
                // This is a reused entity - group under the original entity's ID
                groupKey = `source_${entity.sourceEntityId}`;
            } else {
                // Original entity OR unlinked entity - group by ID for browser-session linking
                // This allows future sourceEntityId references to find it
                groupKey = `source_${entity.id}`;
            }

            if (!entityMap.has(groupKey)) {
                // First occurrence - store it
                entityMap.set(groupKey, { ...entity, mentions: 1 });
            } else {
                // Additional mention - increment counter
                const existing = entityMap.get(groupKey);
                existing.mentions = (existing.mentions || 1) + 1;
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
                                <th>Type</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                entities.forEach(entity => {
                    const displayText = entity.linkedRecordDisplayName || entity.normalizedValue || entity.rawText;
                    const mentionCount = (entity.mentions && entity.mentions > 1) ? ` (×${entity.mentions})` : '';

                    // Extract metadata fields
                    const address = entity.metadata?.address || '—';

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

                    // Get time from related DateTime entity (entityType 5)
                    let time = '—';
                    if (entity.entryId) {
                        const relationships = this.entryRelationships[entity.entryId] || [];
                        const entities = this.entryEntities[entity.entryId] || [];
                        const locationRelationships = relationships.filter(r => 
                            r.primaryEntityId === entity.id || r.relatedEntityId === entity.id
                        );

                        // Check for timeEntityId in relationships
                        for (const rel of locationRelationships) {
                            if (rel.timeEntityId) {
                                const timeEntity = entities.find(e => e.id === rel.timeEntityId);
                                if (timeEntity) {
                                    time = timeEntity.rawText;
                                    break;
                                }
                            }
                        }

                        // Also check if any related entity IS a DateTime entity
                        if (time === '—') {
                            for (const rel of locationRelationships) {
                                const relatedId = rel.primaryEntityId === entity.id ? rel.relatedEntityId : rel.primaryEntityId;
                                const relatedEntity = entities.find(e => e.id === relatedId);
                                if (relatedEntity && relatedEntity.entityType === 5) {
                                    time = relatedEntity.rawText;
                                    break;
                                }
                            }
                        }

                        // Fallback to metadata if no relationship time found
                        if (time === '—' && entity.metadata?.time) {
                            time = entity.metadata.time;
                        }
                    }

                    // Get duration from related Duration entity (entityType 6)
                    let duration = '—';
                    if (entity.entryId) {
                        const relationships = this.entryRelationships[entity.entryId] || [];
                        const entities = this.entryEntities[entity.entryId] || [];

                        // Look for Duration entity related to this location
                        for (const rel of relationships) {
                            if (rel.primaryEntityId === entity.id || rel.relatedEntityId === entity.id) {
                                // Check if related entity is Duration (entityType 6)
                                const relatedId = rel.primaryEntityId === entity.id ? rel.relatedEntityId : rel.primaryEntityId;
                                const relatedEntity = entities.find(e => e.id === relatedId);
                                if (relatedEntity && relatedEntity.entityType === 6) {
                                    duration = relatedEntity.rawText;
                                    break;
                                }
                            }
                        }

                        // Check metadata in relationships for durationEntityId
                        if (duration === '—') {
                            for (const rel of relationships) {
                                if ((rel.primaryEntityId === entity.id || rel.relatedEntityId === entity.id) && rel.metadata?.durationEntityId) {
                                    const durationEntity = entities.find(e => e.id === rel.metadata.durationEntityId);
                                    if (durationEntity) {
                                        duration = durationEntity.rawText;
                                        break;
                                    }
                                }
                            }
                        }

                        // Fallback to metadata if no relationship duration found
                        if (duration === '—' && entity.metadata?.duration) {
                            duration = entity.metadata.duration;
                        }
                    }

                    html += `
                        <tr>
                            <td><strong>${this.escapeHtml(displayText)}</strong>${mentionCount}</td>
                            <td>${this.escapeHtml(address)}</td>
                            <td>${this.escapeHtml(entryDate)}</td>
                            <td>${this.escapeHtml(time)}</td>
                            <td>${this.escapeHtml(duration)}</td>
                            <td>${entity.linkedRecordType || '—'}</td>
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
        const timelineDiv = document.getElementById('relationshipTimeline');
        if (!timelineDiv) return;

        // Collect all relationships across all entries
        const allRelationships = [];
        Object.entries(this.entryRelationships).forEach(([entryId, relationships]) => {
            const entryEntities = this.entryEntities[entryId] || [];
            const entryBlock = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
            const dateInput = entryBlock?.querySelector('input[type="date"]');
            const entryDate = dateInput ? new Date(dateInput.value) : null;

            relationships.forEach(rel => {
                const primaryEntity = entryEntities.find(e => e.id === rel.primaryEntityId);
                const relatedEntity = entryEntities.find(e => e.id === rel.relatedEntityId);
                const timeEntity = rel.timeEntityId ? entryEntities.find(e => e.id === rel.timeEntityId) : null;

                if (primaryEntity && relatedEntity) {
                    allRelationships.push({
                        ...rel,
                        primaryEntity,
                        relatedEntity,
                        timeEntity,
                        entryDate,
                        entryId
                    });
                }
            });
        });

        if (allRelationships.length === 0) {
            timelineDiv.innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="bi bi-clock-history fs-3 d-block mb-2"></i>
                    No relationships detected yet
                </div>
            `;
            return;
        }

        // Sort by date and sequence
        allRelationships.sort((a, b) => {
            if (a.entryDate && b.entryDate) {
                const dateDiff = a.entryDate - b.entryDate;
                if (dateDiff !== 0) return dateDiff;
            }
            return (a.sequenceOrder || 0) - (b.sequenceOrder || 0);
        });

        // Build timeline HTML
        const relationshipTypeMap = this.getRelationshipTypeMap();
        let html = '<div class="relationship-timeline-content">';

        // Group by person (key entities)
        const peopleRelationships = {};
        allRelationships.forEach(rel => {
            const personId = rel.primaryEntity.entityType === 1 ? rel.primaryEntityId : 
                           (rel.relatedEntity.entityType === 1 ? rel.relatedEntityId : null);

            if (personId) {
                if (!peopleRelationships[personId]) {
                    const person = rel.primaryEntity.entityType === 1 ? rel.primaryEntity : rel.relatedEntity;
                    peopleRelationships[personId] = {
                        person,
                        relationships: []
                    };
                }
                peopleRelationships[personId].relationships.push(rel);
            }
        });

        // Display person-centric relationship chains
        Object.entries(peopleRelationships).forEach(([personId, data]) => {
            const personName = data.person.linkedRecordDisplayName || data.person.normalizedValue || data.person.rawText;

            html += `
                <div class="person-relationship-block mb-3">
                    <div class="person-header">
                        <i class="bi bi-person-circle text-primary"></i>
                        <strong>${this.escapeHtml(personName)}</strong>
                    </div>
                    <div class="relationship-chain">
            `;

            data.relationships.forEach((rel, index) => {
                const relType = relationshipTypeMap[rel.relationType] || { name: 'related to', icon: 'bi-link' };
                const otherEntity = rel.primaryEntityId === personId ? rel.relatedEntity : rel.primaryEntity;
                const entityTypeName = this.entityTypeMap[otherEntity.entityType] || 'unknown';
                const entityDisplayName = otherEntity.linkedRecordDisplayName || otherEntity.normalizedValue || otherEntity.rawText;
                const timeText = rel.timeEntity ? ` <span class="time-badge">${rel.timeEntity.rawText}</span>` : '';
                const dateText = rel.entryDate ? rel.entryDate.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }) : '';

                html += `
                    <div class="relationship-item">
                        ${dateText ? `<span class="date-badge">${dateText}</span>` : ''}
                        <i class="bi ${relType.icon} text-muted"></i>
                        <span class="relationship-description">${relType.description}</span>
                        <span class="entity-badge entity-${entityTypeName.toLowerCase()}">${this.escapeHtml(entityDisplayName)}</span>
                        ${timeText}
                    </div>
                `;

                // Show sequence arrow if this is a sequence relationship
                if (rel.relationType === 8 && index < data.relationships.length - 1) {
                    html += `<div class="sequence-arrow"><i class="bi bi-arrow-down"></i></div>`;
                }
            });

            html += `
                    </div>
                </div>
            `;
        });

        // Display standalone location/event relationships (no person)
        const standalonRels = allRelationships.filter(rel => 
            rel.primaryEntity.entityType !== 1 && rel.relatedEntity.entityType !== 1
        );

        if (standalonRels.length > 0) {
            html += `
                <div class="standalone-relationships mb-3">
                    <div class="section-header">
                        <i class="bi bi-diagram-3"></i>
                        <strong>Other Connections</strong>
                    </div>
            `;

            standalonRels.forEach(rel => {
                const relType = relationshipTypeMap[rel.relationType] || { name: 'related to', icon: 'bi-link' };
                const primaryTypeName = this.entityTypeMap[rel.primaryEntity.entityType] || 'unknown';
                const relatedTypeName = this.entityTypeMap[rel.relatedEntity.entityType] || 'unknown';

                html += `
                    <div class="relationship-item">
                        <span class="entity-badge entity-${primaryTypeName.toLowerCase()}">${this.escapeHtml(rel.primaryEntity.rawText)}</span>
                        <i class="bi ${relType.icon} text-muted"></i>
                        <span class="relationship-description">${relType.description}</span>
                        <span class="entity-badge entity-${relatedTypeName.toLowerCase()}">${this.escapeHtml(rel.relatedEntity.rawText)}</span>
                    </div>
                `;
            });

            html += `</div>`;
        }

        html += '</div>';
        timelineDiv.innerHTML = html;
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
                // First check the dedicated timeEntity property (from timeEntityId)
                if (rel.timeEntity?.entityType === 5) {
                    timeEntity = rel.timeEntity;
                } else if (rel.primaryEntity?.entityType === 5) { // DateTime
                    timeEntity = rel.primaryEntity;
                } else if (rel.relatedEntity?.entityType === 5) {
                    timeEntity = rel.relatedEntity;
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

                // Add time only if not already in array (deduplicate by ID)
                if (timeEntity) {
                    const timeId = timeEntity.sourceEntityId || timeEntity.id;
                    if (!group.times.some(t => (t.sourceEntityId || t.id) === timeId)) {
                        group.times.push(timeEntity);
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
                    const timeTexts = group.times.map(t => t.rawText).join(', ');
                    if (group.durations.length > 0) {
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
                    } else {
                        timeDuration = timeTexts;
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
     * Calculate duration between two time strings
     * @param {string} startTime - Start time (e.g., "9AM", "9:00AM")
     * @param {string} endTime - End time (e.g., "11AM", "11:00AM")
     * @returns {string} Duration string (e.g., "2 hours")
     */
    calculateDuration(startTime, endTime) {
        try {
            // Parse time strings (simple parser for common formats)
            const parseTime = (timeStr) => {
                const match = timeStr.match(/(\d+):?(\d{2})?\s*(AM|PM)/i);
                if (!match) return null;

                let hours = parseInt(match[1]);
                const minutes = match[2] ? parseInt(match[2]) : 0;
                const period = match[3].toUpperCase();

                if (period === 'PM' && hours !== 12) hours += 12;
                if (period === 'AM' && hours === 12) hours = 0;

                return hours * 60 + minutes; // Return total minutes
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
}

