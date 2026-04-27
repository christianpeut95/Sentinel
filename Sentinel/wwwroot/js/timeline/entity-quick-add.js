/**
 * EntityQuickAdd v3.0 - ".." trigger for guided entity creation
 * Uses Tribute.js for trigger detection, Tippy.js for forms, Fuse.js for search
 * Updated: Cache bust for relationship menu backspace fix
 */
class EntityQuickAdd {
    constructor(timelineEntry) {
        this.timelineEntry = timelineEntry;
        this.tribute = null;
        this.activeTippy = null;
        this.currentState = {};
        this.recentEntities = { people: [], locations: [] };
        
        // Entity type definitions
        this.entityTypes = [
            { label: '👤 Person', value: 'Person', color: '#4A90E2', context: ['with', 'and'] },
            { label: '📍 Location', value: 'Location', color: '#50C878', context: ['to', 'at', 'in'] },
            { label: '🚌 Transport', value: 'Transport', color: '#FF8C42', context: ['by', 'via', 'on', 'took'] },
            { label: '🕐 Time', value: 'DateTime', color: '#FF69B4', context: ['at'] },
            { label: '⏱️ Duration', value: 'Duration', color: '#808080', context: ['for'] },
            { label: '🎉 Event', value: 'Event', color: '#9B59B6', context: [] }
        ];
        
        this.init();
    }

    init() {
        // Configure Tribute.js for ".." trigger with keyboard-optimized recent entities
        this.tribute = new Tribute({
            trigger: '..',
            lookup: 'label',
            fillAttr: 'label',
            selectTemplate: (item) => {
                // Capture textarea reference before any DOM changes
                const textarea = document.activeElement;
                if (textarea && textarea.tagName === 'TEXTAREA') {
                    this.currentState.textarea = textarea;
                    this.currentState.cursorPosition = textarea.selectionStart;

                    // Detect relationship operator before .. trigger (@>)
                    const textBefore = textarea.value.substring(0, textarea.selectionStart);
                    const operatorMatch = textBefore.match(/([@>])\s*\.\.$/);
                    this.currentState.relationshipOperator = operatorMatch ? operatorMatch[1] : null;
                }

                // Handle group selection
                if (item.original.isGroup) {
                    const groupReference = `@#${item.original.groupData.name}`;

                    // After Tribute finishes, trigger group expansion
                    setTimeout(() => {
                        if (this.currentState.textarea) {
                            const ta = this.currentState.textarea;
                            const entryId = ta.closest('.timeline-day-block')?.dataset.entryId;

                            // Trigger input event to parse and expand group reference
                            const event = new Event('input', { bubbles: true });
                            ta.dispatchEvent(event);

                            console.log(`[EntityQuickAdd] Inserted group reference: ${groupReference}`);
                        }
                    }, 100);

                    return groupReference; // Tribute inserts "+#GroupName"
                }

                if (item.original.isRecent) {
                    // Return the entity text directly so Tribute handles insertion natively.
                    // This avoids all cursor-position timing problems with the '' hack.
                    const entityText = item.original.entityData.rawText || '';
                    this.currentState.pendingRecentEntity = item.original.entityData;
                    this.currentState.pendingEntityText = entityText;
                    this.currentState.queryLength = item.original.queryLength || 0; // Capture query length for cleanup

                    // After Tribute finishes, verify insertion and compute positions
                    setTimeout(() => {
                        if (this.currentState.textarea && this.currentState.pendingRecentEntity) {
                            const ta = this.currentState.textarea;
                            const cursorPos = ta.selectionStart;
                            const insertedText = this.currentState.pendingEntityText;

                            // Verify Tribute actually inserted what we expected
                            const textBefore = ta.value.substring(0, cursorPos);

                            if (textBefore.endsWith(insertedText)) {
                                const startPos = cursorPos - insertedText.length;
                                const endPos = cursorPos;
                                console.log('[EntityQuickAdd] Recent entity inserted correctly:', insertedText, 'at', startPos, '-', endPos);
                                this.finishRecentEntityInsertion(
                                    this.currentState.pendingRecentEntity, ta, startPos, endPos
                                );
                            } else {
                                console.error('[EntityQuickAdd] Tribute insertion mismatch!');
                                console.error('Expected text:', insertedText);
                                console.error('Found before cursor:', textBefore.substring(Math.max(0, textBefore.length - 30)));
                                console.error('Cursor at:', cursorPos);
                                console.error('Full textarea:', ta.value);
                            }
                        }
                    }, 0);

                    return entityText; // Tribute inserts this in place of ..query
                }

                // Entity-type selection: return '' so Tribute removes ..query without
                // inserting anything. Re-read cursor position once Tribute is done (T),
                // then show the entity-creation form.
                setTimeout(() => {
                    if (this.currentState.textarea && this.currentState.textarea.tagName === 'TEXTAREA') {
                        // Tribute has now placed cursor at T (trigger-start position)
                        this.currentState.cursorPosition = this.currentState.textarea.selectionStart;

                        // Clean up any remaining query text after the trigger
                        // When user types "..per" and selects "Person", Tribute removes ".." but leaves "per"
                        // We need to remove the query characters that remain
                        if (item.original.isEntityType || item.original.value) {
                            const textarea = this.currentState.textarea;
                            const cursorPos = textarea.selectionStart;
                            const textBefore = textarea.value.substring(0, cursorPos);
                            const textAfter = textarea.value.substring(cursorPos);

                            // Check if there are orphaned query characters right before cursor
                            // These would be lowercase letters that remain from the search query
                            const orphanMatch = textBefore.match(/([a-z]+)$/);
                            if (orphanMatch) {
                                const orphanedText = orphanMatch[1];
                                // Remove the orphaned query text
                                const cleanBefore = textBefore.substring(0, textBefore.length - orphanedText.length);
                                textarea.value = cleanBefore + textAfter;
                                const newCursorPos = cleanBefore.length;
                                textarea.setSelectionRange(newCursorPos, newCursorPos);
                                this.currentState.cursorPosition = newCursorPos;
                                console.log(`[EntityQuickAdd] Cleaned up orphaned query text: "${orphanedText}"`);
                            }
                        }
                    }
                    if (item.original.isEntityType) {
                        this.showEntityForm(item.original.value, item.original.searchTerm);
                    } else if (item.original.value && !item.original.disabled) {
                        this.showEntityForm(item.original.value);
                    }
                }, 0);
                return ''; // Suppress Tribute insertion; form will handle it
            },
            values: async (text, cb) => {
                // Smart filtering: if user types after "..", filter the menu
                const searchTerm = text.toLowerCase().trim();
                const items = [];

                // Get recent entities from current session (all entries in browser)
                const sessionEntities = this.getRecentEntitiesFromSession();

                // Get entity groups
                const groups = await this.loadEntityGroups();

                // Filter recent entities if search term exists
                let filteredRecents = sessionEntities;
                if (searchTerm.length > 0) {
                    filteredRecents = sessionEntities.filter(entity => 
                        entity.rawText.toLowerCase().includes(searchTerm) ||
                        (entity.normalizedValue && entity.normalizedValue.toLowerCase().includes(searchTerm))
                    );
                }

                // Filter groups if search term exists
                let filteredGroups = groups;
                if (searchTerm.length > 0) {
                    filteredGroups = groups.filter(group =>
                        group.name.toLowerCase().includes(searchTerm)
                    );
                }

                // Add filtered groups first (if any)
                if (filteredGroups.length > 0) {
                    filteredGroups.forEach(group => {
                        items.push({
                            key: `group_${group.id}`,
                            label: `<span class="entity-quick-group">👥 ${group.name} <small>(${group.entityCount} entities)</small></span>`,
                            value: 'EntityGroup',
                            groupData: group,
                            isGroup: true
                        });
                    });
                }

                // Add recent entities
                if (filteredRecents.length > 0) {
                    filteredRecents.forEach(entity => {
                        const icon = this.getIconForRecordType(entity.recordType || entity.entityTypeName);
                        items.push({
                            key: `recent_${entity.id}`,
                            label: `<span class="entity-quick-recent">${icon} ${entity.rawText}</span>`,
                            value: 'RecentEntity',
                            entityData: entity,
                            isRecent: true,
                            queryLength: text.length // Store query length for cleanup
                        });
                    });

                    // Add separator
                    items.push({
                        key: 'separator',
                        label: '<span class="tribute-separator">or choose type</span>',
                        value: 'separator',
                        disabled: true
                    });
                } else if (sessionEntities.length > 0 && searchTerm.length > 0 && filteredGroups.length === 0) {
                    // No matches - offer smart search
                    items.push({
                        key: 'smart-search-location',
                        label: `<span class="entity-quick-choice">🔍 Search locations for "${searchTerm}"</span>`,
                        value: 'Location',
                        isEntityType: true,
                        searchTerm: searchTerm
                    });
                    items.push({
                        key: 'smart-search-person',
                        label: `<span class="entity-quick-choice">🔍 Search people for "${searchTerm}"</span>`,
                        value: 'Person',
                        isEntityType: true,
                        searchTerm: searchTerm
                    });
                    items.push({
                        key: 'separator2',
                        label: '<span class="tribute-separator">or choose type</span>',
                        value: 'separator',
                        disabled: true
                    });
                } else if (sessionEntities.length === 0 && groups.length === 0) {
                    // Show helpful message when no recent entities or groups
                    items.push({
                        key: 'no-recent',
                        label: '<span class="tribute-separator">💡 Add entities to see recent suggestions</span>',
                        value: 'no-recent-hint',
                        disabled: true
                    });
                }

                // Always add all entity types as options
                this.entityTypes.forEach(type => {
                    items.push({
                        key: `type_${type.value}`,
                        label: type.label,
                        value: type.value,
                        isEntityType: true
                    });
                });

                cb(items);
            },
            menuItemTemplate: (item) => {
                // Force dark background on container after render
                setTimeout(() => {
                    const container = document.querySelector('.tribute-container');
                    if (container) {
                        container.style.setProperty('background', '#252526', 'important');
                        container.style.setProperty('background-color', '#252526', 'important');
                        console.log('[Tribute] Forced dark background on container');
                    }
                }, 0);

                if (item.original.disabled) {
                    return item.original.label; // Label already contains HTML
                }
                if (item.original.isGroup) {
                    return item.original.label; // Label already contains HTML with styling
                }
                if (item.original.isRecent) {
                    return item.original.label; // Label already contains HTML with styling
                }
                if (item.original.isEntityType) {
                    return `<span class="entity-quick-type">${item.original.label}</span>`;
                }
                return `<span class="entity-quick-type">${item.original.label}</span>`;
            },
            noMatchTemplate: () => '<span class="no-match">Type to search...</span>',
            menuContainer: document.body,
            replaceTextSuffix: '',
            requireLeadingSpace: false,
            allowSpaces: false,
            menuShowMinLength: 0,
            // Add keyboard handlers
            onKeyDown: (e, el) => {
                // Handle Escape key to close menu
                if (e.key === 'Escape') {
                    this.tribute.hideMenu();
                    return false;
                }
                return true;
            }
        });

        console.log('EntityQuickAdd initialized with keyboard-optimized recent entities');
    }

    attach(textarea) {
        if (this.tribute && textarea) {
            this.tribute.attach(textarea);
            console.log('Tribute attached to textarea');
        }
    }

    detach(textarea) {
        if (this.tribute && textarea) {
            this.tribute.detach(textarea);
        }
    }

    async loadRecentEntities() {
        try {
            console.log('[EntityQuickAdd] Loading recent entities for case:', this.timelineEntry.caseId);
            const response = await fetch(`/api/timeline/memory/${this.timelineEntry.caseId}`);
            console.log('[EntityQuickAdd] Recent entities response status:', response.status);
            if (response.ok) {
                const memory = await response.json();
                console.log('[EntityQuickAdd] Loaded memory:', memory);
                this.recentEntities.people = memory.people || [];
                this.recentEntities.locations = memory.locations || [];
                this.recentEntities.transports = memory.transports || [];
                this.recentEntities.events = memory.events || [];
                this.recentEntities.datetimes = memory.datetimes || [];
                console.log('[EntityQuickAdd] Recent people count:', this.recentEntities.people.length);
            } else {
                console.warn('[EntityQuickAdd] Failed to load recent entities, status:', response.status);
            }
        } catch (error) {
            console.error('[EntityQuickAdd] Error loading recent entities:', error);
        }
    }

    /**
     * Load entity groups for the current case
     * Groups can be referenced with +#GroupName syntax
     */
    async loadEntityGroups() {
        try {
            // Access groups directly from TimelineEntry (already loaded)
            const groups = this.timelineEntry.entityGroups || {};

            // Convert to array and return
            return Object.values(groups).map(group => ({
                id: group.id,
                name: group.name,
                entityIds: group.entityIds || [],
                entityCount: (group.entityIds || []).length,
                description: group.description || '',
                createdDate: group.createdDate
            }));
        } catch (error) {
            console.warn('Could not load entity groups:', error);
            return [];
        }
    }

    /**
     * Get icon emoji for entity record type
     */
    getIconForRecordType(recordType) {
        const icons = {
            'Person': '👤',
            'Location': '📍',
            'Transport': '🚌',
            'Event': '📅',
            'DateTime': '🕐',
            'Duration': '⏱️'
        };
        return icons[recordType] || '•';
    }

    /**
     * Show entity type selection menu (fallback when no suitable recent entity)
     */
    showEntityTypeMenu() {
        const textarea = this.currentState.textarea || document.activeElement;
        if (!textarea || textarea.tagName !== 'TEXTAREA') return;

        // Store cursor position
        this.currentState.cursorPosition = textarea.selectionStart;

        // Calculate cursor coordinates for proper positioning
        const coords = this.getCursorCoordinates(textarea, this.currentState.cursorPosition);

        // Build entity type selection menu
        const typeMenuHtml = `
            <div class="entity-form entity-type-menu">
                <div class="form-header">
                    <h4>Choose Entity Type</h4>
                </div>
                <div class="form-body">
                    <div class="entity-type-list">
                        ${this.entityTypes.map(type => `
                            <button class="entity-type-btn" data-type="${type.value}">
                                <span class="type-icon">${type.label.split(' ')[0]}</span>
                                <span class="type-name">${type.label.split(' ').slice(1).join(' ')}</span>
                            </button>
                        `).join('')}
                    </div>
                </div>
                <div class="form-footer">
                    <div class="keyboard-hints">
                        <span><kbd>↑</kbd><kbd>↓</kbd> Navigate</span>
                        <span><kbd>Enter</kbd> Select</span>
                        <span><kbd>Esc</kbd> Close</span>
                    </div>
                </div>
            </div>
        `;

        // Show menu at cursor position
        this.showTippyForm(textarea, typeMenuHtml, coords);

        // Attach click handlers and keyboard navigation
        setTimeout(() => {
            const form = this.activeTippy?.popper;
            if (!form) return;

            const buttons = form.querySelectorAll('.entity-type-btn');
            let selectedIndex = 0;

            // Focus first button
            if (buttons.length > 0) {
                buttons[0].focus();
                buttons[0].classList.add('keyboard-selected');
            }

            // Type selection button clicks
            buttons.forEach((btn, index) => {
                btn.addEventListener('click', () => {
                    const entityType = btn.dataset.type;
                    console.log('[EntityTypeMenu] Selected type:', entityType);

                    // Preserve relationship operator before closing Tippy
                    const preservedOperator = this.currentState.relationshipOperator;
                    const preservedTextarea = this.currentState.textarea;
                    const preservedCursorPos = this.currentState.cursorPosition;

                    this.closeTippy();

                    // Restore preserved state
                    if (preservedOperator) {
                        this.currentState.relationshipOperator = preservedOperator;
                        console.log('[EntityTypeMenu] Preserved relationship operator:', preservedOperator);
                    }
                    if (preservedTextarea) {
                        this.currentState.textarea = preservedTextarea;
                        this.currentState.cursorPosition = preservedCursorPos;
                    }

                    setTimeout(() => {
                        this.showEntityForm(entityType);
                    }, 50);
                });

                // Handle focus for visual feedback
                btn.addEventListener('focus', () => {
                    buttons.forEach(b => b.classList.remove('keyboard-selected'));
                    btn.classList.add('keyboard-selected');
                    selectedIndex = index;
                });
            });

            // Keyboard navigation handler
            const handleKeydown = (e) => {
                switch(e.key) {
                    case 'ArrowDown':
                    case 'ArrowUp':
                        e.preventDefault();
                        const direction = e.key === 'ArrowDown' ? 1 : -1;
                        selectedIndex = (selectedIndex + direction + buttons.length) % buttons.length;
                        buttons[selectedIndex].focus();
                        break;

                    case 'Enter':
                        e.preventDefault();
                        buttons[selectedIndex]?.click();
                        break;

                    case 'Escape':
                        e.preventDefault();
                        this.closeTippy();
                        this.removeDoubleDot();
                        textarea.focus();
                        break;
                }
            };

            // Attach keyboard handler to form
            form.addEventListener('keydown', handleKeydown);

            // Also attach to each button for redundancy
            buttons.forEach(btn => {
                btn.addEventListener('keydown', handleKeydown);
            });
        }, 100);
    }

    showEntityForm(entityType, searchTerm = null) {
        // Get the textarea - use currentState.textarea if available (from editEntity),
        // otherwise use document.activeElement (from Tribute trigger)
        const textarea = this.currentState.textarea || document.activeElement;
        if (!textarea || textarea.tagName !== 'TEXTAREA') {
            console.error('[EntityQuickAdd] No valid textarea found for showEntityForm');
            return;
        }

        // Determine if we're editing or creating new
        // If editingEntity exists in currentState, preserve it; otherwise, this is a NEW entity
        const isEditing = !!this.currentState.editingEntity;

        // Preserve textarea and cursor position from Tribute callback, update entity type
        this.currentState = {
            textarea: this.currentState.textarea || textarea,
            cursorPosition: this.currentState.cursorPosition,
            entityType,
            entryId: textarea.closest('.timeline-day-block')?.dataset.entryId,
            smartSearchTerm: searchTerm, // Store search term for pre-filling
            // Only preserve editing state if we were already editing
            ...(isEditing && {
                editingEntity: this.currentState.editingEntity,
                originalEntity: this.currentState.originalEntity
            })
        };

        // Load recent entities
        this.loadRecentEntities();

        // Create form content based on entity type
        let formHtml = '';
        switch(entityType) {
            case 'Person':
                formHtml = this.renderPersonForm();
                break;
            case 'Location':
                formHtml = this.renderLocationForm();
                break;
            case 'Transport':
                formHtml = this.renderTransportForm();
                break;
            case 'DateTime':
                formHtml = this.renderDateTimeForm();
                break;
            case 'Duration':
                formHtml = this.renderDurationForm();
                break;
            case 'Event':
                formHtml = this.renderEventForm();
                break;
        }

        // Show form in Tippy popover
        this.showTippyForm(textarea, formHtml);

        // Pre-fill search if smart search term exists
        if (searchTerm && (entityType === 'Location' || entityType === 'Person')) {
            setTimeout(() => {
                if (this.activeTippy) {
                    const searchInput = this.activeTippy.popper.querySelector('#locationSearch, #personName');
                    if (searchInput) {
                        searchInput.value = searchTerm;
                        searchInput.focus();
                        // Trigger input event to start search (for locations)
                        if (entityType === 'Location') {
                            searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                        }
                    }
                }
            }, 150);
        }
    }

    /**
     * Show location form at specific cursor position (for auto-trigger)
     * @param {HTMLTextAreaElement} textarea - The textarea element
     * @param {number} cursorPos - Character position where location was detected
     * @param {string} entityText - The detected location text (e.g., "cinema", "home")
     */
    showLocationFormAt(textarea, cursorPos, entityText = '') {
        // Store state for location entity
        this.currentState = {
            entityType: 'Location',
            textarea,
            cursorPosition: cursorPos,
            entryId: textarea.closest('.timeline-day-block')?.dataset.entryId,
            detectedText: entityText // Store the detected text for pre-filling
        };

        // Load recent entities
        this.loadRecentEntities();

        // Calculate cursor coordinates
        const coords = this.getCursorCoordinates(textarea, cursorPos);

        // Create location form
        const formHtml = this.renderLocationForm();

        // Show form at cursor position
        this.showTippyForm(textarea, formHtml, coords);

        // Pre-fill search box if text was detected
        setTimeout(() => {
            if (entityText && this.activeTippy) {
                const searchInput = this.activeTippy.popper.querySelector('#locationSearch');
                if (searchInput) {
                    searchInput.value = entityText;
                    searchInput.focus();
                    // Trigger input event to start search
                    searchInput.dispatchEvent(new Event('input', { bubbles: true }));
                }
            }
        }, 150);
    }

    /**
     * Calculate pixel coordinates of cursor position within textarea
     * @param {HTMLTextAreaElement} textarea - The textarea element
     * @param {number} position - Character position (selectionStart)
     * @returns {{top: number, left: number, height: number}} Coordinates relative to viewport
     */
    getCursorCoordinates(textarea, position) {
        const div = document.createElement('div');
        const span = document.createElement('span');

        // Copy textarea styles to div for accurate measurement
        const computed = window.getComputedStyle(textarea);
        const properties = [
            'fontFamily', 'fontSize', 'fontWeight', 'fontStyle',
            'letterSpacing', 'lineHeight', 'padding', 'border',
            'whiteSpace', 'wordWrap', 'overflowWrap'
        ];

        properties.forEach(prop => {
            div.style[prop] = computed[prop];
        });

        div.style.position = 'absolute';
        div.style.visibility = 'hidden';
        div.style.width = textarea.offsetWidth + 'px';
        div.style.height = 'auto';
        div.style.overflow = 'hidden';
        div.style.whiteSpace = 'pre-wrap';
        div.style.wordWrap = 'break-word';

        // Insert text up to cursor position
        const textBeforeCursor = textarea.value.substring(0, position);
        div.textContent = textBeforeCursor;

        // Add span at cursor position for measurement
        span.textContent = '|';
        div.appendChild(span);

        document.body.appendChild(div);

        // Get coordinates relative to textarea
        const textareaRect = textarea.getBoundingClientRect();
        const spanRect = span.getBoundingClientRect();

        const coordinates = {
            top: spanRect.top,
            left: spanRect.left,
            height: spanRect.height || parseInt(computed.lineHeight) || 20
        };

        document.body.removeChild(div);

        return coordinates;
    }

    showTippyForm(textarea, content, cursorCoordinates = null) {
        // Destroy existing tippy
        if (this.activeTippy) {
            this.activeTippy.destroy();
        }

        let referenceElement;

        if (cursorCoordinates) {
            // Create virtual reference element at cursor position
            referenceElement = {
                getBoundingClientRect: () => ({
                    top: cursorCoordinates.top,
                    left: cursorCoordinates.left,
                    bottom: cursorCoordinates.top + cursorCoordinates.height,
                    right: cursorCoordinates.left + 1,
                    width: 1,
                    height: cursorCoordinates.height,
                    x: cursorCoordinates.left,
                    y: cursorCoordinates.top
                })
            };
        } else {
            // Use textarea as reference (original behavior)
            referenceElement = textarea;
        }

        // Create new tippy instance
        this.activeTippy = tippy(referenceElement, {
            content: content,
            trigger: 'manual',
            interactive: true,
            placement: 'bottom-start',
            appendTo: document.body,
            allowHTML: true,
            theme: 'entity-form',
            arrow: false,
            offset: [0, 10],
            popperOptions: {
                strategy: 'fixed',
                modifiers: [
                    {
                        name: 'flip',
                        enabled: true,
                        options: {
                            fallbackPlacements: ['top-start', 'right-start', 'left-start'],
                        },
                    },
                    {
                        name: 'preventOverflow',
                        enabled: true,
                        options: {
                            boundary: 'viewport',
                            padding: 8,
                        },
                    },
                ],
            },
            getReferenceClientRect: cursorCoordinates ? referenceElement.getBoundingClientRect : null,
            onShow: (instance) => {
                // Focus first input after render
                setTimeout(() => {
                    const firstInput = instance.popper.querySelector('input, select');
                    if (firstInput) {
                        firstInput.focus();
                    } else {
                        // No input - focus first menu item (for relationship selector, etc.)
                        const firstItem = instance.popper.querySelector('.menu-item[tabindex="0"]');
                        if (firstItem) firstItem.focus();
                    }

                    // Add keyboard event listener to form container
                    this.setupFormKeyboardNavigation(instance.popper);
                }, 100);
            }
        });

        this.activeTippy.show();

        // Attach event handlers
        setTimeout(() => {
            // Check if it's a menu or form
            const isMenu = this.activeTippy.popper.querySelector('.entity-menu');
            if (isMenu) {
                this.attachMenuHandlers();
            } else {
                this.attachFormHandlers();
            }
        }, 100);
    }

    /**
     * Show relationship selector for new person (searchable input with suggestions)
     * @param {string} personName - Name of person being created
     */
    showPersonDetailsForm(personName) {
        console.log('[Person Details] Showing form for:', personName);

        const isEditing = !!this.currentState.editingEntity;
        const headerText = isEditing ? 'Edit person details' : 'Complete person details';

        const relationshipSuggestions = [
            'Contact',
            'Family Member',
            'Friend',
            'Colleague',
            'Healthcare Worker',
            'Neighbor',
            'Classmate',
            'Coworker'
        ];

        const menuHtml = `
            <div class="entity-menu" style="min-width: 400px; max-width: 500px;" data-menu-type="person-details">
                <div style="padding: 12px; background: var(--slate-dk); border-bottom: 1px solid var(--slate);">
                    <div style="font-weight: 600; margin-bottom: 4px;">${this.escapeHtml(personName)}</div>
                    <div style="font-size: 11px; color: var(--graphite);">${headerText}</div>
                </div>

                <div style="padding: 16px; display: flex; flex-direction: column; gap: 12px;">
                    <!-- Relationship Field -->
                    <div style="position: relative;">
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Relationship
                        </label>
                        <input 
                            type="text" 
                            id="personRelationship"
                            class="details-input"
                            placeholder="e.g., Contact, Friend, Colleague..."
                            autocomplete="off"
                            list="relationshipSuggestions"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                        <datalist id="relationshipSuggestions">
                            ${relationshipSuggestions.map(rel => `<option value="${rel}">`).join('')}
                        </datalist>
                    </div>

                    <!-- Phone Field -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Phone
                        </label>
                        <input 
                            type="tel" 
                            id="personPhone"
                            class="details-input"
                            placeholder="e.g., (555) 123-4567"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                    </div>

                    <!-- Age/DOB Field -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Age or Date of Birth
                        </label>
                        <input 
                            type="text" 
                            id="personAgeDob"
                            class="details-input"
                            placeholder="e.g., 30 or 1994-05-15"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                    </div>

                    <!-- Notes Field -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Notes
                        </label>
                        <textarea 
                            id="personNotes"
                            class="details-input"
                            placeholder="Additional information..."
                            rows="3"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; resize: vertical; background: white;"></textarea>
                    </div>
                </div>

                <div style="display: flex; gap: 8px; padding: 12px; border-top: 1px solid var(--slate); background: var(--slate-dk);">
                    <button id="savePersonDetails" style="flex: 1; padding: 8px 16px; background: var(--primary); color: white; border: none; border-radius: 4px; font-weight: 500; cursor: pointer;">
                        Save Person
                    </button>
                    <button id="cancelPersonDetails" style="padding: 8px 16px; background: transparent; color: var(--graphite); border: 1px solid var(--slate); border-radius: 4px; cursor: pointer;">
                        Cancel
                    </button>
                </div>
            </div>
        `;

        // Show in Tippy
        const textarea = this.currentState.textarea;
        if (textarea) {
            this.showTippyForm(textarea, menuHtml);

            // Attach handlers after render
            setTimeout(() => {
                const relationshipInput = this.activeTippy?.popper.querySelector('#personRelationship');
                const phoneInput = this.activeTippy?.popper.querySelector('#personPhone');
                const ageDobInput = this.activeTippy?.popper.querySelector('#personAgeDob');
                const notesInput = this.activeTippy?.popper.querySelector('#personNotes');
                const saveButton = this.activeTippy?.popper.querySelector('#savePersonDetails');
                const cancelButton = this.activeTippy?.popper.querySelector('#cancelPersonDetails');

                if (!relationshipInput || !saveButton) {
                    console.error('[Person Details] Failed to find required form elements');
                    return;
                }

                // Focus first field
                relationshipInput.focus();

                // Pre-fill form if editing
                const isEditing = !!this.currentState.editingEntity;
                if (isEditing) {
                    const metadata = this.currentState.editingEntity.metadata || {};
                    relationshipInput.value = metadata.relationship || '';
                    phoneInput.value = metadata.phone || '';
                    ageDobInput.value = metadata.ageDob || '';
                    notesInput.value = metadata.notes || '';
                    console.log('[Person Details] Pre-filled form for editing:', metadata);
                }

                // Save handler
                const savePerson = () => {
                    const relationship = relationshipInput.value.trim();
                    const phone = phoneInput.value.trim();
                    const ageDob = ageDobInput.value.trim();
                    const notes = notesInput.value.trim();

                    console.log('[Person Details] Saving:', { relationship, phone, ageDob, notes });

                    const entity = {
                        id: isEditing ? this.currentState.editingEntity.id : `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                        entityType: 1, // Person
                        entityTypeName: 'Person',
                        rawText: personName,
                        normalizedValue: personName,
                        confidence: 2,
                        isConfirmed: true, // Mark as confirmed so it appears in recent entities
                        metadata: {
                            relationship: relationship || null,
                            phone: phone || null,
                            ageDob: ageDob || null,
                            notes: notes || null
                        }
                    };

                    if (isEditing) {
                        this.updateEntityInText(entity);
                    } else {
                        this.insertEntityIntoText(entity);
                    }
                };

                // Cancel/Backspace handler
                const cancelForm = () => {
                    if (isEditing) {
                        // When editing, just close the form
                        console.log('[Person Details] Canceling edit');
                        this.closeTippy();
                    } else {
                        // When creating, return to name entry
                        console.log('[Person Details] Returning to name entry');

                        const textareaToRestore = this.currentState.textarea;
                        const entryId = this.currentState.entryId;

                        this.closeTippy();

                        setTimeout(() => {
                            if (textareaToRestore && textareaToRestore.isConnected) {
                                textareaToRestore.focus();
                            }

                            this.currentState = {
                                textarea: textareaToRestore,
                                entryId: entryId
                            };

                            this.showEntityForm('Person');

                            setTimeout(() => {
                                const personInput = this.activeTippy?.popper.querySelector('.menu-search[data-menu-type="person"]');
                                if (personInput) {
                                                    personInput.value = personName;
                                                    personInput.focus();
                                                    const inputEvent = new Event('input', { bubbles: true });
                                                    personInput.dispatchEvent(inputEvent);
                                                }
                                            }, 150);
                                        }, 50);
                                    }
                                };

                                // Button click handlers
                                saveButton.addEventListener('click', savePerson);
                                cancelButton.addEventListener('click', cancelForm);

                                // Enter key on any input saves (except textarea)
                                [relationshipInput, phoneInput, ageDobInput].forEach(input => {
                                    input.addEventListener('keydown', (e) => {
                                        if (e.key === 'Enter') {
                                            e.preventDefault();
                                            savePerson();
                                        }
                                    });
                                });

                                // Backspace on empty first field - cancel form (returns to name entry if creating)
                                relationshipInput.addEventListener('keydown', (e) => {
                                    if (e.key === 'Backspace' && relationshipInput.value === '') {
                                        e.preventDefault();
                                        cancelForm();
                                    }
                                });

                                // Escape key cancels
                                const allInputs = [relationshipInput, phoneInput, ageDobInput, notesInput];
                                allInputs.forEach(input => {
                                    input.addEventListener('keydown', (e) => {
                                        if (e.key === 'Escape') {
                                            e.preventDefault();
                                            cancelForm();
                                        }
                                    });
                                });

                console.log('[Person Details] Form initialized');
            }, 100);
        }
    }

    /**
     * Show location details form to capture time and duration
     * @param {string} locationName - Name of the location
     * @param {object} locationData - Optional place data (placeId, address, lat, lng)
     */
    showLocationDetailsForm(locationName, locationData = {}) {
        console.log('[Location Details] Showing form for:', locationName);

        const isEditing = !!this.currentState.editingEntity;
        const headerText = isEditing ? 'Edit location details' : 'Complete location details';

        const menuHtml = `
            <div class="entity-menu" style="min-width: 400px; max-width: 500px;" data-menu-type="location-details">
                <div style="padding: 12px; background: var(--slate-dk); border-bottom: 1px solid var(--slate);">
                    <div style="font-weight: 600; margin-bottom: 4px;">${this.escapeHtml(locationName)}</div>
                    <div style="font-size: 11px; color: var(--graphite);">${headerText}</div>
                </div>

                <div style="padding: 16px; display: flex; flex-direction: column; gap: 12px;">
                    <!-- Time Field -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Time
                        </label>
                        <input 
                            type="text" 
                            id="locationTime"
                            class="details-input"
                            placeholder="e.g., 2:00 PM, 14:00"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                    </div>

                    <!-- Duration Field -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Duration
                        </label>
                        <input 
                            type="text" 
                            id="locationDuration"
                            class="details-input"
                            placeholder="e.g., 2 hours, 30 minutes"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                    </div>
                </div>

                <div style="display: flex; gap: 8px; padding: 12px; border-top: 1px solid var(--slate); background: var(--slate-dk);">
                    <button id="saveLocationDetails" style="flex: 1; padding: 8px 16px; background: var(--primary); color: white; border: none; border-radius: 4px; font-weight: 500; cursor: pointer;">
                        Save Location
                    </button>
                    <button id="cancelLocationDetails" style="padding: 8px 16px; background: transparent; color: var(--graphite); border: 1px solid var(--slate); border-radius: 4px; cursor: pointer;">
                        Cancel
                    </button>
                </div>
            </div>
        `;

        // Show in Tippy
        const textarea = this.currentState.textarea;
        if (textarea) {
            this.showTippyForm(textarea, menuHtml);

            // Attach handlers after render
            setTimeout(() => {
                const timeInput = this.activeTippy?.popper.querySelector('#locationTime');
                const durationInput = this.activeTippy?.popper.querySelector('#locationDuration');
                const saveButton = this.activeTippy?.popper.querySelector('#saveLocationDetails');
                const cancelButton = this.activeTippy?.popper.querySelector('#cancelLocationDetails');

                if (!timeInput || !saveButton) {
                    console.error('[Location Details] Failed to find required form elements');
                    return;
                }

                // Focus first field
                timeInput.focus();

                // Pre-fill form if editing
                if (isEditing) {
                    const metadata = this.currentState.editingEntity.metadata || {};
                    timeInput.value = metadata.time || '';
                    durationInput.value = metadata.duration || '';
                    console.log('[Location Details] Pre-filled form for editing:', metadata);
                }

                // Save handler
                const saveLocation = () => {
                    const time = timeInput.value.trim();
                    const duration = durationInput.value.trim();

                    console.log('[Location Details] Saving:', { time, duration });

                    const entity = {
                        id: isEditing ? this.currentState.editingEntity.id : `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                        entityType: 2, // Location
                        entityTypeName: 'Location',
                        rawText: locationName,
                        normalizedValue: locationName,
                        confidence: locationData.placeId ? 3 : 2,
                        isConfirmed: true,
                        metadata: {
                            ...locationData, // Include placeId, address, lat, lng if from Google Places
                            time: time || null,
                            duration: duration || null
                        }
                    };

                    if (isEditing) {
                        this.updateEntityInText(entity);
                    } else {
                        this.insertEntityIntoText(entity);
                    }
                };

                // Cancel handler
                const cancelForm = () => {
                    if (isEditing) {
                        console.log('[Location Details] Canceling edit');
                        this.closeTippy();
                    } else {
                        console.log('[Location Details] Canceling creation');
                        this.closeTippy();
                        this.removeDoubleDot();
                        textarea.focus();
                    }
                };

                // Button click handlers
                saveButton.addEventListener('click', saveLocation);
                cancelButton.addEventListener('click', cancelForm);

                // Enter key on any input saves
                [timeInput, durationInput].forEach(input => {
                    input.addEventListener('keydown', (e) => {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            saveLocation();
                        }
                    });
                });

                // Backspace on empty first field - cancel form
                timeInput.addEventListener('keydown', (e) => {
                    if (e.key === 'Backspace' && timeInput.value === '') {
                        e.preventDefault();
                        cancelForm();
                    }
                });

                // Escape key cancels
                [timeInput, durationInput].forEach(input => {
                    input.addEventListener('keydown', (e) => {
                        if (e.key === 'Escape') {
                            e.preventDefault();
                            cancelForm();
                        }
                    });
                });

                console.log('[Location Details] Form initialized');
            }, 100);
        }
    }

    /**
     * Show transport details form with optional fields
     * @param {string} transportType - Type of transport (bus, train, car, etc.)
     */
    showTransportDetailsForm(transportType) {
        console.log('[Transport Details] Showing form for:', transportType);

        const isEditing = !!this.currentState.editingEntity;
        const headerText = isEditing ? 'Edit transport details' : 'Complete transport details';

        const menuHtml = `
            <div class="entity-menu" style="min-width: 400px; max-width: 500px;" data-menu-type="transport-details">
                <div style="padding: 12px; background: var(--slate-dk); border-bottom: 1px solid var(--slate);">
                    <div style="font-weight: 600; margin-bottom: 4px;">${this.escapeHtml(transportType.charAt(0).toUpperCase() + transportType.slice(1))}</div>
                    <div style="font-size: 11px; color: var(--graphite);">${headerText}</div>
                </div>

                <div style="padding: 16px; display: flex; flex-direction: column; gap: 12px;">
                    <!-- Details Field (route, flight number, etc.) -->
                    <div>
                        <label style="display: block; font-size: 12px; font-weight: 500; margin-bottom: 4px; color: var(--graphite);">
                            Details <span style="color: var(--slate); font-weight: 400;">(optional)</span>
                        </label>
                        <input 
                            type="text" 
                            id="transportDetails"
                            class="details-input"
                            placeholder="e.g., Route 123, Flight UA456..."
                            autocomplete="off"
                            style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                    </div>

                    <!-- Travel Times Table -->
                    <div style="border: 1px solid var(--slate); border-radius: 4px; overflow: hidden;">
                        <!-- Table Header -->
                        <div style="display: grid; grid-template-columns: 1fr 1fr; background: var(--slate-dk); border-bottom: 1px solid var(--slate);">
                            <div style="padding: 8px 12px; font-size: 11px; font-weight: 600; color: var(--graphite); text-transform: uppercase; border-right: 1px solid var(--slate);">
                                ⏱️ Departure
                            </div>
                            <div style="padding: 8px 12px; font-size: 11px; font-weight: 600; color: var(--graphite); text-transform: uppercase;">
                                🏁 Arrival
                            </div>
                        </div>

                        <!-- Time Row -->
                        <div style="display: grid; grid-template-columns: 1fr 1fr; border-bottom: 1px solid var(--slate);">
                            <div style="padding: 4px; border-right: 1px solid var(--slate);">
                                <input 
                                    type="text" 
                                    id="transportDepartedAt"
                                    class="details-input"
                                    placeholder="Time (e.g., 9:00 AM)"
                                    style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                            </div>
                            <div style="padding: 4px;">
                                <input 
                                    type="text" 
                                    id="transportArrivedAt"
                                    class="details-input"
                                    placeholder="Time (e.g., 10:00 AM)"
                                    style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                            </div>
                        </div>

                        <!-- Location Row -->
                        <div style="display: grid; grid-template-columns: 1fr 1fr;">
                            <div style="padding: 4px; border-right: 1px solid var(--slate);">
                                <input 
                                    type="text" 
                                    id="transportDepartedFrom"
                                    class="details-input"
                                    placeholder="From (e.g., Central Station)"
                                    style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                            </div>
                            <div style="padding: 4px;">
                                <input 
                                    type="text" 
                                    id="transportArrivedTo"
                                    class="details-input"
                                    placeholder="To (e.g., Airport Terminal 2)"
                                    style="width: 100%; padding: 8px 10px; border: 1px solid var(--slate); border-radius: 4px; font-size: 13px; background: white;">
                            </div>
                        </div>
                    </div>
                </div>

                <div style="display: flex; gap: 8px; padding: 12px; border-top: 1px solid var(--slate); background: var(--slate-dk);">
                    <button id="saveTransportDetails" style="flex: 1; padding: 8px 16px; background: var(--primary); color: white; border: none; border-radius: 4px; font-weight: 500; cursor: pointer;">
                        Save Transport
                    </button>
                    <button id="cancelTransportDetails" style="padding: 8px 16px; background: transparent; color: var(--graphite); border: 1px solid var(--slate); border-radius: 4px; cursor: pointer;">
                        Cancel
                    </button>
                </div>
            </div>
        `;

        // Show in Tippy
        const textarea = this.currentState.textarea;
        if (textarea) {
            this.showTippyForm(textarea, menuHtml);

            // Attach handlers after render
            setTimeout(() => {
                const detailsInput = this.activeTippy?.popper.querySelector('#transportDetails');
                const departedFromInput = this.activeTippy?.popper.querySelector('#transportDepartedFrom');
                const departedAtInput = this.activeTippy?.popper.querySelector('#transportDepartedAt');
                const arrivedAtInput = this.activeTippy?.popper.querySelector('#transportArrivedAt');
                const arrivedToInput = this.activeTippy?.popper.querySelector('#transportArrivedTo');
                const saveButton = this.activeTippy?.popper.querySelector('#saveTransportDetails');
                const cancelButton = this.activeTippy?.popper.querySelector('#cancelTransportDetails');

                if (!detailsInput || !saveButton) {
                    console.error('[Transport Details] Failed to find required form elements');
                    return;
                }

                // Focus first field
                detailsInput.focus();

                // Pre-fill form if editing
                if (isEditing) {
                    const metadata = this.currentState.editingEntity.metadata || {};
                    detailsInput.value = metadata.details || '';
                    departedFromInput.value = metadata.departedFrom || '';
                    departedAtInput.value = metadata.departedAt || '';
                    arrivedAtInput.value = metadata.arrivedAt || '';
                    arrivedToInput.value = metadata.arrivedTo || '';
                    console.log('[Transport Details] Pre-filled form for editing:', metadata);
                }

                // Save handler
                const saveTransport = () => {
                    const details = detailsInput.value.trim();
                    const departedFrom = departedFromInput.value.trim();
                    const departedAt = departedAtInput.value.trim();
                    const arrivedAt = arrivedAtInput.value.trim();
                    const arrivedTo = arrivedToInput.value.trim();

                    console.log('[Transport Details] Saving:', { details, departedFrom, departedAt, arrivedAt, arrivedTo });

                    // Build transport name
                    const transportName = details ? `${transportType} ${details}` : transportType;

                    const entity = {
                        id: isEditing ? this.currentState.editingEntity.id : `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                        entityType: 4, // Transport (corrected from 3)
                        entityTypeName: 'Transport',
                        rawText: transportName,
                        normalizedValue: transportName,
                        confidence: 2,
                        isConfirmed: true,
                        metadata: {
                            transportType: transportType,
                            details: details || null,
                            departedFrom: departedFrom || null,
                            departedAt: departedAt || null,
                            arrivedAt: arrivedAt || null,
                            arrivedTo: arrivedTo || null
                        }
                    };

                    if (isEditing) {
                        this.updateEntityInText(entity);
                    } else {
                        this.insertEntityIntoText(entity);
                    }
                };

                // Cancel/Backspace handler
                const cancelForm = () => {
                    if (isEditing) {
                        // When editing, just close the form
                        console.log('[Transport Details] Canceling edit');
                        this.closeTippy();
                    } else {
                        // When creating, return to transport type menu
                        console.log('[Transport Details] Returning to transport type menu');

                        const textareaToRestore = this.currentState.textarea;
                        const entryId = this.currentState.entryId;

                        this.closeTippy();

                        setTimeout(() => {
                            if (textareaToRestore && textareaToRestore.isConnected) {
                                textareaToRestore.focus();
                            }

                            this.currentState = {
                                textarea: textareaToRestore,
                                entryId: entryId
                            };

                            this.showEntityForm('Transport');
                        }, 50);
                    }
                };

                // Button click handlers
                saveButton.addEventListener('click', saveTransport);
                cancelButton.addEventListener('click', cancelForm);

                // Enter key on any input saves
                [detailsInput, departedFromInput, departedAtInput, arrivedAtInput].forEach(input => {
                    input.addEventListener('keydown', (e) => {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            saveTransport();
                        }
                    });
                });

                // Backspace on empty first field - cancel form (returns to transport type menu if creating)
                detailsInput.addEventListener('keydown', (e) => {
                    if (e.key === 'Backspace' && detailsInput.value === '') {
                        e.preventDefault();
                        cancelForm();
                    }
                });

                // Escape key cancels
                const allInputs = [detailsInput, departedFromInput, departedAtInput, arrivedAtInput];
                allInputs.forEach(input => {
                    input.addEventListener('keydown', (e) => {
                        if (e.key === 'Escape') {
                            e.preventDefault();
                            cancelForm();
                        }
                    });
                });

                console.log('[Transport Details] Form initialized');
            }, 100);
        }
    }

    renderPersonForm() {
        // Get people from browser memory (current session) - these are instant
        const sessionEntities = this.getRecentEntitiesFromSession();
        const sessionPeople = sessionEntities
            .filter(e => e.entityTypeName === 'Person' || e.entityType === 1)
            .slice(0, 10); // Show up to 10 recent people

        console.log('[Person Menu] Rendering with', sessionPeople.length, 'session people:', sessionPeople);

        return `
            <div class="entity-menu">
                <input 
                    type="text" 
                    class="menu-search" 
                    placeholder="Type to search or create person..."
                    data-menu-type="person"
                    autocomplete="off">
                <div class="menu-items" role="listbox">
                    <div class="menu-item menu-create-hint" 
                         role="option"
                         data-action="create-person"
                         tabindex="0"
                         style="display: none;">
                        <span class="menu-icon">✨</span>
                        <span class="menu-text">
                            <span class="menu-name">Create "<span class="create-name-preview"></span>"</span>
                        </span>
                    </div>
                    ${sessionPeople.length > 0 ? sessionPeople.map((p, idx) => `
                        <div class="menu-item" 
                             role="option"
                             data-action="select-recent"
                             data-entity-id="${p.id}"
                             data-entity-value="${this.escapeHtml(p.rawText || p.normalizedValue)}"
                             data-entity-metadata='${JSON.stringify(p.metadata || {})}'
                             data-index="${idx}"
                             tabindex="-1">
                            <span class="menu-icon">👤</span>
                            <span class="menu-text">
                                <span class="menu-name">${this.escapeHtml(p.rawText || p.normalizedValue)}</span>
                                <small class="menu-detail">${this.escapeHtml(p.metadata?.relationship || 'Unknown')}</small>
                            </span>
                        </div>
                    `).join('') : ''}
                    ${sessionPeople.length === 0 ? '<div class="menu-empty">Type a name to create a new person</div>' : ''}
                </div>
            </div>
        `;
    }

    renderLocationForm() {

        const recent = this.recentEntities.locations.slice(0, 5);

        

        return `

            <div class="entity-menu">

                <input 

                    type="text" 

                    class="menu-search" 

                    placeholder="Type to search places..."

                    data-menu-type="location"

                    autocomplete="off">

                <div class="menu-items" role="listbox">

                    <div class="menu-item" 

                         role="option"

                         data-action="select-convention"

                         data-convention="home"

                         tabindex="-1">

                        <span class="menu-icon">🏠</span>

                        <span class="menu-text">

                            <span class="menu-name">Home</span>

                        </span>

                    </div>

                    <div class="menu-item" 

                         role="option"

                         data-action="select-convention"

                         data-convention="work"

                         tabindex="-1">

                        <span class="menu-icon">💼</span>

                        <span class="menu-text">

                            <span class="menu-name">Work</span>

                        </span>

                    </div>

                    <div class="menu-item" 

                         role="option"

                         data-action="select-convention"

                         data-convention="school"

                         tabindex="-1">

                        <span class="menu-icon">🏫</span>

                        <span class="menu-text">

                            <span class="menu-name">School</span>

                        </span>

                    </div>

                    ${recent.length > 0 ? '<div class="menu-divider"></div>' : ''}

                    ${recent.length > 0 ? recent.map((l, idx) => `

                        <div class="menu-item" 

                             role="option"

                             data-action="select-recent"

                             data-entity-id="${l.id}"

                             data-entity-value="${this.escapeHtml(l.normalizedValue)}"

                             data-index="${idx + 3}"

                             tabindex="-1">

                            <span class="menu-icon">📍</span>

                            <span class="menu-text">

                                <span class="menu-name">${this.escapeHtml(l.normalizedValue)}</span>

                            </span>

                        </div>

                    `).join('') : ''}

                    <div class="menu-divider" id="location-google-divider" style="display: none;"></div>

                    <div id="location-google-results"></div>

                    <div id="location-google-loading" style="display: none; padding: 12px; text-align: center; color: var(--graphite);">
                        <span>Searching nearby places...</span>
                    </div>

                    <div class="menu-item menu-create-hint"

                         role="option"

                         tabindex="-1"

                         style="display: none;">

                        <span class="menu-icon">🆕</span>

                        <span class="menu-text">

                            <span class="menu-name">Create "<span class="create-name-preview"></span>"</span>

                            <small class="menu-detail">Manual location (needs address)</small>

                        </span>

                    </div>

                </div>

            </div>

        `;

    }



    renderTransportForm() {

        const transportTypes = [

            { icon: '🚌', name: 'Bus', type: 'bus' },

            { icon: '🚆', name: 'Train', type: 'train' },

            { icon: '🚗', name: 'Car', type: 'car' },

            { icon: '🚕', name: 'Taxi', type: 'taxi' },

            { icon: '✈️', name: 'Flight', type: 'flight' },

            { icon: '🚶', name: 'Walk', type: 'walk' }

        ];



        return `

            <div class="entity-menu">

                <input 

                    type="text" 

                    class="menu-search" 

                    placeholder="Type to search transport type..."

                    data-menu-type="transport"

                    autocomplete="off">

                <div class="menu-items" role="listbox">

                    ${transportTypes.map((t, idx) => `

                        <div class="menu-item" 

                             role="option"

                             data-action="select-transport"

                             data-transport-type="${t.type}"

                             data-index="${idx}"

                             tabindex="-1">

                            <span class="menu-icon">${t.icon}</span>

                            <span class="menu-text">

                                <span class="menu-name">${t.name}</span>

                            </span>

                        </div>

                    `).join('')}

                </div>

            </div>

        `;

    }



    renderDateTimeForm() {

        const timePresets = [

            { icon: '🌅', name: 'Morning (9:00 AM)', value: '9:00 AM' },

            { icon: '☀️', name: 'Noon (12:00 PM)', value: '12:00 PM' },

            { icon: '🌆', name: 'Afternoon (3:00 PM)', value: '3:00 PM' },

            { icon: '🌃', name: 'Evening (6:00 PM)', value: '6:00 PM' },

            { icon: '🌙', name: 'Night (9:00 PM)', value: '9:00 PM' }

        ];



        return `

            <div class="entity-menu">

                <input 

                    type="text" 

                    class="menu-search" 

                    placeholder="Type time (e.g., 3pm, 14:30)..."

                    data-menu-type="datetime"

                    autocomplete="off"

                    aria-label="Enter time">

                <div class="menu-items" role="listbox" aria-label="Time suggestions">

                    <div class="menu-section-label">Common times</div>

                    ${timePresets.map((t, idx) => `

                        <div class="menu-item" 

                             role="option"

                             data-action="select-time"

                             data-time-value="${t.value}"

                             data-index="${idx}"

                             tabindex="-1">

                            <span class="menu-icon">${t.icon}</span>

                            <span class="menu-text">

                                <span class="menu-name">${t.name}</span>

                            </span>

                        </div>

                    `).join('')}

                </div>

            </div>

        `;

    }



    renderDurationForm() {

        const durationPresets = [

            { icon: '⏱️', name: '30 minutes', value: '30 minutes' },

            { icon: '⏱️', name: '1 hour', value: '1 hour' },

            { icon: '⏱️', name: '2 hours', value: '2 hours' },

            { icon: '⏱️', name: 'Half day', value: 'half day' },

            { icon: '⏱️', name: 'All day', value: 'all day' }

        ];



        return `

            <div class="entity-menu">

                <input 

                    type="text" 

                    class="menu-search" 

                    placeholder="Type duration (e.g., 2 hours, 30min)..."

                    data-menu-type="duration"

                    autocomplete="off"

                    aria-label="Enter duration">

                <div class="menu-items" role="listbox" aria-label="Duration suggestions">

                    <div class="menu-section-label">Common durations</div>

                    ${durationPresets.map((d, idx) => `

                        <div class="menu-item" 

                             role="option"

                             data-action="select-duration"

                             data-duration-value="${d.value}"

                             data-index="${idx}"

                             tabindex="-1">

                            <span class="menu-icon">${d.icon}</span>

                            <span class="menu-text">

                                <span class="menu-name">${d.name}</span>

                            </span>

                        </div>

                    `).join('')}

                </div>

            </div>

        `;

    }



    renderEventForm() {

        return `

            <div class="entity-menu">

                <input 

                    type="text" 

                    class="menu-search" 

                    placeholder="Type event name and press Enter..."

                    data-menu-type="event"

                    autocomplete="off">

                <div class="menu-items" role="listbox">

                    <div class="menu-empty">

                        Type an event name above and press Enter

                    </div>

                </div>

            </div>

        `;

    }
    renderEventForm() {
        return `
            <div class="entity-form event-form">
                <div class="form-header">
                    <button class="back-btn" data-action="back">←</button>
                    <h4>🎉 Add Event</h4>
                </div>
                <div class="form-body">
                    <div class="form-group">
                        <label>Type:</label>
                        <select id="eventType">
                            <option value="">Select...</option>
                            <option value="social">🎉 Social gathering</option>
                            <option value="medical">🏥 Medical appointment</option>
                            <option value="shopping">🏪 Shopping</option>
                            <option value="dining">🍽️ Dining</option>
                            <option value="sports">🏋️ Sports/Recreation</option>
                            <option value="education">🎓 Education</option>
                            <option value="work">💼 Work meeting</option>
                            <option value="entertainment">🎭 Entertainment</option>
                            <option value="travel">✈️ Travel</option>
                            <option value="other">Other</option>
                        </select>
                    </div>
                    <div class="form-group">
                        <label>Name:</label>
                        <input type="text" id="eventName" placeholder="e.g., 'Birthday party', 'Gym'" autocomplete="off">
                    </div>
                </div>
                <div class="form-footer">
                    <button class="btn-cancel" data-action="cancel">Cancel</button>
                    <button class="btn-primary" data-action="submit">✓ Add</button>
                </div>
            </div>
        `;
    }

    /**
     * Attach handlers for IntelliSense-style menu
     */
    attachMenuHandlers() {
        const menu = this.activeTippy?.popper.querySelector('.entity-menu');
        if (!menu) return;

        const searchInput = menu.querySelector('.menu-search');
        const menuItems = menu.querySelector('.menu-items');
        const createHint = menu.querySelector('.menu-create-hint');
        const createPreview = createHint?.querySelector('.create-name-preview');

        // Get menu type from search input data attribute
        const menuType = searchInput?.dataset.menuType || 'person';

        // Search input - filter items and show "Create new" option
        if (searchInput) {
            searchInput.focus();

            searchInput.addEventListener('input', (e) => {
                const query = e.target.value.toLowerCase().trim();
                const items = menuItems.querySelectorAll('.menu-item:not(.menu-create-hint)');

                // Filter existing items
                let hasVisibleItems = false;
                items.forEach(item => {
                    const name = item.querySelector('.menu-name')?.textContent.toLowerCase();
                    const matches = !query || name.includes(query);
                    item.style.display = matches ? 'flex' : 'none';
                    if (matches) hasVisibleItems = true;
                });

                // Show/hide "Create new" hint
                if (query && createHint && createPreview) {
                    createHint.style.display = 'flex';
                    createPreview.textContent = query;
                    createHint.dataset.createValue = query;
                } else if (createHint) {
                    createHint.style.display = 'none';
                }
            });

            // Arrow keys navigate menu items
            searchInput.addEventListener('keydown', (e) => {
                if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    const firstVisible = menuItems.querySelector('.menu-item[style*="display: flex"], .menu-item:not([style*="display: none"])');
                    if (firstVisible) firstVisible.focus();
                } else if (e.key === 'Escape') {
                    this.closeTippy();
                    this.removeDoubleDot();
                } else if (e.key === 'Enter') {
                    e.preventDefault();
                    const query = searchInput.value.trim();

                    // Handle DateTime and Duration direct text entry
                    if (menuType === 'datetime' && query) {
                        // User typed a time (e.g., "3pm", "14:30") and pressed Enter
                        const entity = {
                            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                            entityType: 5, // DateTime
                            rawText: query,
                            normalizedValue: query,
                            confidence: 2,
                            isConfirmed: false,
                            metadata: {}
                        };
                        this.insertEntityIntoText(entity);
                        return;
                    } else if (menuType === 'duration' && query) {
                        // User typed a duration (e.g., "2 hours", "30min") and pressed Enter
                        const entity = {
                            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                            entityType: 6, // Duration
                            rawText: query,
                            normalizedValue: query,
                            confidence: 2,
                            isConfirmed: false,
                            metadata: {}
                        };
                        this.insertEntityIntoText(entity);
                        return;
                    } else if (createHint && createHint.style.display !== 'none') {
                        // Enter on search box with text = create new
                        // (handled by general search filter above)
                    }
                }
            });

            // Location-specific: Google Places search
            if (menuType === 'location') {

                    let searchTimeout;

                    const locationHandler = async (e) => {

                        clearTimeout(searchTimeout);

                        const query = e.target.value.trim();
                        console.log(`[Location Menu] Search query: "${query}"`);



                        if (query.length >= 3) {

                            // Show loading indicator
                            const loadingIndicator = menu.querySelector('#location-google-loading');
                            const googleResults = menu.querySelector('#location-google-results');
                            const divider = menu.querySelector('#location-google-divider');

                            if (loadingIndicator) loadingIndicator.style.display = 'block';
                            if (googleResults) googleResults.innerHTML = '';
                            if (divider) divider.style.display = 'none';

                            searchTimeout = setTimeout(async () => {

                                try {
                                    console.log(`[Location Menu] Fetching Google Places for: "${query}"`);

                                    // Build URL with location bias if available
                                    let url = `/api/location-lookup/search?query=${encodeURIComponent(query)}`;
                                    if (this.timelineEntry.patientLocation) {
                                        const lat = this.timelineEntry.patientLocation.latitude;
                                        const lng = this.timelineEntry.patientLocation.longitude;
                                        if (lat && lng) {
                                            url += `&lat=${lat}&lng=${lng}`;
                                            console.log(`[Location Menu] Using location bias: ${lat}, ${lng}`);
                                        }
                                    }

                                    const response = await fetch(url);
                                    console.log(`[Location Menu] Response status: ${response.status}`);

                                    if (response.ok) {

                                        const places = await response.json();
                                        console.log(`[Location Menu] Found ${places.length} places:`, places);

                                        // Hide loading indicator
                                        if (loadingIndicator) loadingIndicator.style.display = 'none';

                                        const googleResults = menu.querySelector('#location-google-results');

                                        const divider = menu.querySelector('#location-google-divider');

                                        

                                        if (places.length > 0) {

                                            divider.style.display = 'block';

                                            googleResults.innerHTML = places.slice(0, 5).map(place => `

                                                <div class="menu-item" 

                                                     role="option"

                                                     data-action="select-google-place"

                                                     data-place-id="${place.placeId}"

                                                     data-place-name="${this.escapeHtml(place.name)}"

                                                     data-place-address="${this.escapeHtml(place.formattedAddress || '')}"

                                                     data-place-lat="${place.latitude || ''}"

                                                     data-place-lng="${place.longitude || ''}"

                                                     tabindex="-1">

                                                    <span class="menu-icon">📍</span>

                                                    <span class="menu-text">

                                                        <span class="menu-name">${this.escapeHtml(place.name)}}</span>

                                                        <small class="menu-detail">${this.escapeHtml(place.formattedAddress || '')}</small>

                                                    </span>

                                                </div>

                                            `).join('');



                                            // Reattach click AND keyboard handlers for new Google Place items

                                            googleResults.querySelectorAll('.menu-item').forEach((placeItem, index) => {

                                                placeItem.addEventListener('click', () => {

                                                    this.handleMenuItemSelection(placeItem);

                                                });

                                                // Add keyboard navigation
                                                placeItem.addEventListener('keydown', (e) => {
                                                    const allItems = Array.from(menuItems.querySelectorAll('.menu-item:not([style*="display: none"])'));
                                                    const currentIndex = allItems.indexOf(placeItem);

                                                    if (e.key === 'Enter') {
                                                        e.preventDefault();
                                                        this.handleMenuItemSelection(placeItem);
                                                    } else if (e.key === 'ArrowDown') {
                                                        e.preventDefault();
                                                        const nextIndex = (currentIndex + 1) % allItems.length;
                                                        allItems[nextIndex]?.focus();
                                                    } else if (e.key === 'ArrowUp') {
                                                        e.preventDefault();
                                                        if (currentIndex === 0) {
                                                            searchInput?.focus();
                                                        } else {
                                                            const prevIndex = currentIndex - 1;
                                                            allItems[prevIndex]?.focus();
                                                        }
                                                    } else if (e.key === 'Escape') {
                                                        this.closeTippy();
                                                        this.removeDoubleDot();
                                                    }
                                                });

                                                // Set tabindex for keyboard access
                                                placeItem.setAttribute('tabindex', '-1');

                                            });

                                        } else {

                                            divider.style.display = 'none';

                                            googleResults.innerHTML = '';

                                        }

                                    }

                                } catch (error) {

                                    console.warn('[Location Menu] Google Places error:', error);

                                    // Hide loading indicator on error
                                    const loadingIndicator = menu.querySelector('#location-google-loading');
                                    if (loadingIndicator) loadingIndicator.style.display = 'none';

                                }

                            }, 300);

                        } else {

                            const googleResults = menu.querySelector('#location-google-results');

                            const divider = menu.querySelector('#location-google-divider');

                            const loadingIndicator = menu.querySelector('#location-google-loading');

                            if (googleResults) googleResults.innerHTML = '';

                            if (divider) divider.style.display = 'none';

                            if (loadingIndicator) loadingIndicator.style.display = 'none';

                        }

                    };

                    searchInput.addEventListener('input', locationHandler);
                    console.log('[Location Menu] Google Places search handler attached');

                }



                // Person-specific: Search filtering and backspace navigation
                if (menuType === 'person') {
                    const personHandler = (e) => {
                        const query = e.target.value.trim().toLowerCase();
                        console.log('[Person Menu] Search query:', query);

                        const createHint = menu.querySelector('.menu-create-hint');
                        const menuItems = menu.querySelectorAll('.menu-item[data-action="select-recent"]');
                        const emptyState = menu.querySelector('.menu-empty');

                        if (query.length > 0) {
                            // Show create hint at top with query
                            if (createHint) {
                                createHint.style.display = 'flex';
                                createHint.dataset.createValue = e.target.value.trim();
                                createHint.setAttribute('tabindex', '0');
                                const preview = createHint.querySelector('.create-name-preview');
                                if (preview) preview.textContent = e.target.value.trim();
                            }

                            // Hide empty state
                            if (emptyState) emptyState.style.display = 'none';

                            // Filter recent people
                            let visibleCount = 1; // Create hint is first
                            menuItems.forEach((item, idx) => {
                                const name = item.dataset.entityValue.toLowerCase();
                                if (name.includes(query)) {
                                    item.style.display = 'flex';
                                    item.dataset.index = visibleCount;
                                    item.setAttribute('tabindex', '-1');
                                    visibleCount++;
                                } else {
                                    item.style.display = 'none';
                                }
                            });
                        } else {
                            // Hide create hint when empty
                            if (createHint) {
                                createHint.style.display = 'none';
                                createHint.setAttribute('tabindex', '-1');
                            }

                            // Show all recent people
                            menuItems.forEach((item, idx) => {
                                item.style.display = 'flex';
                                item.dataset.index = idx;
                                if (idx === 0) {
                                    item.setAttribute('tabindex', '0');
                                } else {
                                    item.setAttribute('tabindex', '-1');
                                }
                            });

                            // Show empty state if no recent people
                            if (emptyState && menuItems.length === 0) {
                                emptyState.style.display = 'block';
                            }
                        }
                    };

                    searchInput.addEventListener('input', personHandler);

                    // Enter key handler: select first visible item (Create New hint or recent person)
                    searchInput.addEventListener('keydown', (e) => {
                        if (e.key === 'Enter') {
                            e.preventDefault();
                            const createHint = menu.querySelector('.menu-create-hint');
                            const menuItems = menu.querySelectorAll('.menu-item[data-action="select-recent"]');

                            // If create hint is visible, click it
                            if (createHint && createHint.style.display !== 'none' && createHint.dataset.createValue) {
                                console.log('[Person Menu] Enter pressed - clicking create hint');
                                createHint.click();
                            } else {
                                // Otherwise, find first visible recent person
                                const firstVisible = Array.from(menuItems).find(item => item.style.display !== 'none');
                                if (firstVisible) {
                                    console.log('[Person Menu] Enter pressed - clicking first visible recent person');
                                    firstVisible.click();
                                }
                            }
                        } else if (e.key === 'ArrowDown') {
                            // Arrow down focuses first visible menu item
                            e.preventDefault();
                            const createHint = menu.querySelector('.menu-create-hint');
                            const menuItems = menu.querySelectorAll('.menu-item[data-action="select-recent"]');

                            if (createHint && createHint.style.display !== 'none') {
                                createHint.focus();
                            } else {
                                const firstVisible = Array.from(menuItems).find(item => item.style.display !== 'none');
                                if (firstVisible) firstVisible.focus();
                            }
                        } else if (e.key === 'Backspace' && e.target.value === '') {
                            console.log('[Person Menu] Backspace on empty input - closing menu');
                            e.preventDefault();
                            this.closeTippy();
                            // Re-trigger .. menu
                            const textarea = this.currentState.textarea;
                            if (textarea) {
                                textarea.focus();
                                // Simulate typing .. to reopen top menu
                                setTimeout(() => {
                                    const cursorPos = textarea.selectionStart;
                                    textarea.value = textarea.value.substring(0, cursorPos - 2) + '..' + textarea.value.substring(cursorPos);
                                    textarea.setSelectionRange(cursorPos, cursorPos);
                                    const inputEvent = new Event('input', { bubbles: true });
                                    textarea.dispatchEvent(inputEvent);
                                }, 50);
                            }
                        }
                    });

                    console.log('[Person Menu] Search handler attached');
                }

                // Event-specific: Enter key creates event

                if (menuType === 'event') {

                    const eventHandler = (e) => {

                        if (e.key === 'Enter') {

                            e.preventDefault();

                            const eventName = searchInput.value.trim();

                            if (eventName) {

                                const entity = {

                                    id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                                    entityType: 6,

                                    rawText: eventName,

                                    normalizedValue: eventName,

                                    confidence: 2,

                                    isConfirmed: false,

                                    metadata: {}

                                };

                                this.insertEntityIntoText(entity);

                            }

                        }

                    };

                    searchInput.addEventListener('keydown', eventHandler);

                }

        }

        // Menu item selection
        if (menuItems) {
            menuItems.querySelectorAll('.menu-item').forEach((item, index) => {
                item.addEventListener('click', () => {
                    this.handleMenuItemSelection(item);
                });

                item.addEventListener('keydown', (e) => {
                    const allItems = Array.from(menuItems.querySelectorAll('.menu-item')).filter(i => 
                        i.style.display !== 'none'
                );
                const currentIndex = allItems.indexOf(item);

                if (e.key === 'Enter') {
                    e.preventDefault();
                    this.handleMenuItemSelection(item);
                } else if (e.key === 'ArrowDown') {
                    e.preventDefault();
                    const nextIndex = (currentIndex + 1) % allItems.length;
                    allItems[nextIndex].focus();
                } else if (e.key === 'ArrowUp') {
                    e.preventDefault();
                    if (currentIndex === 0) {
                        searchInput?.focus();
                    } else {
                        const prevIndex = currentIndex - 1;
                        allItems[prevIndex].focus();
                    }
                } else if (e.key === 'Escape') {
                    this.closeTippy();
                    this.removeDoubleDot();
                }
            });

            // Hover to highlight
            item.addEventListener('mouseenter', () => {
                menuItems.querySelectorAll('.menu-item').forEach(i => i.classList.remove('selected'));
                item.classList.add('selected');
            });
        });
        }
    }

    /**
     * Handle menu item selection (recent entity or create new)
     */
    /**

     * Handle menu item selection (recent entity or create new)

     */

    handleMenuItemSelection(item) {

        const action = item.dataset.action;

        const menuType = this.activeTippy?.popper.querySelector('.menu-search')?.dataset.menuType;



        // Recent entity selection

        if (action === 'select-recent') {

            const menuType = this.activeTippy?.popper.querySelector('.menu-search')?.dataset.menuType;
            const sourceEntityId = item.dataset.entityId; // Get the original entity ID
            console.log('[Menu Selection] Recent entity selected:', item.dataset, 'menuType:', menuType, 'sourceEntityId:', sourceEntityId);

            // Parse metadata if it exists
            let metadata = {};
            if (item.dataset.entityMetadata) {
                try {
                    metadata = JSON.parse(item.dataset.entityMetadata);
                } catch (e) {
                    console.error('[Menu Selection] Failed to parse metadata:', e);
                }
            }

            const entity = {
                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                entityType: menuType === 'person' ? 1 : (menuType === 'location' ? 2 : 3),
                entityTypeName: menuType === 'person' ? 'Person' : (menuType === 'location' ? 'Location' : 'Event'),
                rawText: item.dataset.entityValue,
                normalizedValue: item.dataset.entityValue,
                confidence: 3,
                isConfirmed: true,
                metadata: metadata, // Use the full parsed metadata
                // CRITICAL: Link back to the original entity for sidebar deduplication
                // This lets the sidebar group all mentions even though each insertion has a unique id
                sourceEntityId: sourceEntityId
            };

            this.insertEntityIntoText(entity);

            return;

        }



        // Relationship selection for new person

        if (action === 'select-relationship') {

            const relationship = item.dataset.relationship;

            const personName = item.dataset.personName;



            const entity = {

                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                entityType: 1, // Person

                rawText: personName,

                normalizedValue: personName,

                confidence: 2,

                isConfirmed: false,

                metadata: { relationship: relationship }

            };



            this.insertEntityIntoText(entity);

            return;

        }



        // Skip relationship for new person

        if (action === 'skip-relationship') {

            const personName = item.dataset.personName;



            const entity = {

                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                entityType: 1, // Person

                rawText: personName,

                normalizedValue: personName,

                confidence: 2,

                isConfirmed: false,

                metadata: {}

            };



            this.insertEntityIntoText(entity);

            return;

        }



        // Location convention (Home/Work/School) - Insert location entity directly

        if (action === 'select-convention') {

            const convention = item.dataset.convention;

            const entity = {

                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                entityType: 2, // Location

                rawText: convention,

                normalizedValue: convention,

                confidence: 2,

                isConfirmed: false,

                metadata: {}

            };

            this.insertEntityIntoText(entity);

            return;

        }



        // Transport type selection

        if (action === 'select-transport') {

            const transportType = item.dataset.transportType;

            this.showTransportDetailsForm(transportType);

            return;

        }



        // Time selection

        if (action === 'select-time') {

            let timeValue = item.dataset.timeValue;

            

            if (timeValue === 'custom') {

                timeValue = prompt('Enter time (e.g., 10:30 AM, 14:00):', '');

                if (!timeValue) {

                    this.closeTippy();

                    this.removeDoubleDot();

                    return;

                }

            }



            const entity = {

                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                entityType: 5, // DateTime

                rawText: timeValue,

                normalizedValue: timeValue,

                confidence: 2,

                isConfirmed: false,

                metadata: {}

            };



            this.insertEntityIntoText(entity);

            return;

        }



        // Duration selection

        if (action === 'select-duration') {

            let durationValue = item.dataset.durationValue;

            

            if (durationValue === 'custom') {

                durationValue = prompt('Enter duration (e.g., 45 minutes, 3 hours):', '');

                if (!durationValue) {

                    this.closeTippy();

                    this.removeDoubleDot();

                    return;

                }

            }



            const entity = {

                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                entityType: 6, // Duration

                rawText: durationValue,

                normalizedValue: durationValue,

                confidence: 2,

                isConfirmed: false,

                metadata: {}

            };



            this.insertEntityIntoText(entity);

            return;

        }



        // Google Place selection (Location) - Insert location entity directly
        if (action === 'select-google-place') {
            const locationName = item.dataset.placeName;

            const entity = {
                id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                entityType: 2, // Location
                rawText: locationName,
                normalizedValue: locationName,
                confidence: 3,
                isConfirmed: true,
                metadata: {
                    placeId: item.dataset.placeId,
                    address: item.dataset.placeAddress,
                    latitude: item.dataset.placeLat,
                    longitude: item.dataset.placeLng
                }
            };

            this.insertEntityIntoText(entity);
            return;
        }



        // Create new person (from create hint or data-action)
        if (action === 'create-person' || (item.classList.contains('menu-create-hint') && menuType === 'person')) {
            const createValue = item.dataset.createValue;
            if (createValue) {
                console.log('[Person Menu] Creating new person:', createValue);
                // Show person details form in Tippy (non-blocking)
                this.showPersonDetailsForm(createValue);
            }
            return;
        }

        // Create new entity

        if (item.classList.contains('menu-create-hint')) {

            const createValue = item.dataset.createValue;



            if (menuType === 'location') {
                // Insert location entity directly
                const entity = {
                    id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
                    entityType: 2, // Location
                    rawText: createValue,
                    normalizedValue: createValue,
                    confidence: 2,
                    isConfirmed: false,
                    metadata: {}
                };
                this.insertEntityIntoText(entity);
                return;
            } else if (menuType === 'event') {

                const entity = {

                    id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,

                    entityType: 6, // Event

                    rawText: createValue,

                    normalizedValue: createValue,

                    confidence: 2,

                    isConfirmed: false,

                    metadata: {}

                };



                this.insertEntityIntoText(entity);

            }

        }

    }

    attachFormHandlers() {
        const form = this.activeTippy?.popper;
        if (!form) return;

        // Back button
        form.querySelector('[data-action="back"]')?.addEventListener('click', () => {
            this.closeTippy();
            // Re-trigger tribute menu
            setTimeout(() => {
                const textarea = this.currentState.textarea;
                if (textarea) {
                    textarea.focus();
                    textarea.setSelectionRange(this.currentState.cursorPosition - 2, this.currentState.cursorPosition);
                }
            }, 100);
        });

        // Cancel button
        form.querySelector('[data-action="cancel"]')?.addEventListener('click', () => {
            this.closeTippy();
            this.removeDoubleDot();
        });

        // Submit button
        form.querySelector('[data-action="submit"]')?.addEventListener('click', () => {
            this.submitEntity();
        });

        // Unknown button
        form.querySelector('[data-action="unknown"]')?.addEventListener('click', () => {
            this.submitUnknownEntity();
        });

        // Recent entity clicks
        form.querySelectorAll('.recent-item').forEach(item => {
            item.addEventListener('click', () => {
                this.submitExistingEntity(item.dataset.entityValue);
            });
            // Keyboard support for recent items
            item.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    this.submitExistingEntity(item.dataset.entityValue);
                }
            });
        });

        // Convention buttons with keyboard navigation
        const conventionList = form.querySelector('.convention-list');
        if (conventionList) {
            this.setupListNavigation(conventionList, '.convention-btn', (btn) => {
                this.submitConvention(btn.dataset.convention);
            });
        } else {
            // Fallback for older code without list container
            form.querySelectorAll('.convention-btn').forEach(btn => {
                btn.addEventListener('click', () => {
                    this.submitConvention(btn.dataset.convention);
                });
            });
        }

        // Transport type selection with keyboard list navigation
        const transportList = form.querySelector('.transport-list');
        if (transportList) {
            this.setupListNavigation(transportList, '.transport-btn', (btn) => {
                form.querySelectorAll('.transport-btn').forEach(b => {
                    b.classList.remove('selected');
                    b.setAttribute('aria-checked', 'false');
                });
                btn.classList.add('selected');
                btn.setAttribute('aria-checked', 'true');
                this.currentState.transportType = btn.dataset.type;
                form.querySelector('#transportDetailsGroup').style.display = 'block';
                const detailsInput = form.querySelector('#transportDetails');
                if (detailsInput) detailsInput.focus();
                form.querySelector('[data-action="submit"]').disabled = false;
            });
        }

        // Time period quick picks with full keyboard grid navigation
        const timeGrid = form.querySelector('.time-grid');
        if (timeGrid) {
            this.setupGridNavigation(timeGrid, '.time-btn', (btn) => {
                this.currentState.timePeriod = btn.dataset.period;
                this.submitEntity();
            });
        }

        // Duration quick picks with full keyboard grid navigation
        const durationGrid = form.querySelector('.duration-grid');
        if (durationGrid) {
            this.setupGridNavigation(durationGrid, '.duration-btn', (btn) => {
                this.currentState.duration = btn.dataset.duration;
                this.submitEntity();
            });
        }

        // Google Places search
        const searchInput = form.querySelector('#locationSearch');
        if (searchInput) {
            this.setupPlacesSearch(searchInput, form);
        }
    }

    setupPlacesSearch(input, form) {
        let debounceTimeout;
        let selectedIndex = -1;
        let lastSearchQuery = ''; // Track the user's original search text
        let places = []; // Cache the current places results
        let isNavigating = false; // Flag to prevent input events during navigation
        const resultsDiv = form.querySelector('#placesResults');

        console.log('[PlacesSearch] Setting up keyboard handler for input:', input.id);

        // Attach keyboard handler ONCE, outside of any callbacks
        // Use capture phase to get event FIRST
        input.addEventListener('keydown', (e) => {
            const results = resultsDiv ? resultsDiv.querySelectorAll('.place-result') : [];

            console.log('[PlacesSearch] Keydown:', e.key, 'Results count:', results.length, 'Selected index:', selectedIndex);

            // Handle arrow keys IMMEDIATELY when results exist
            if ((e.key === 'ArrowDown' || e.key === 'ArrowUp') && results.length > 0) {
                console.log('[PlacesSearch] Preventing default for arrow key');
                // CRITICAL: Prevent default cursor movement
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();

                if (e.key === 'ArrowDown') {
                    console.log('[PlacesSearch] ArrowDown - current selectedIndex:', selectedIndex);
                    // Moving down through results
                    if (selectedIndex === -1) {
                        // First time pressing down - save search query
                        lastSearchQuery = input.value;
                        selectedIndex = 0;
                        console.log('[PlacesSearch] First selection, saved query:', lastSearchQuery);
                    } else if (selectedIndex < results.length - 1) {
                        // Remove highlight from current
                        results[selectedIndex].classList.remove('highlight');
                        selectedIndex++;
                        console.log('[PlacesSearch] Moving to index:', selectedIndex);
                    } else {
                        // Already at last item, do nothing
                        console.log('[PlacesSearch] Already at last item');
                        return false;
                    }

                    // Highlight new selection
                    results[selectedIndex].classList.add('highlight');

                    // Update input to show the highlighted place name
                    isNavigating = true;
                    input.value = places[selectedIndex].displayName;
                    console.log('[PlacesSearch] Updated input to:', input.value);

                    // Scroll highlighted item into view
                    results[selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });

                } else if (e.key === 'ArrowUp') {
                    console.log('[PlacesSearch] ArrowUp - current selectedIndex:', selectedIndex);
                    // Moving up through results
                    if (selectedIndex === -1) {
                        // No selection, do nothing
                        console.log('[PlacesSearch] No selection, ignoring');
                        return false;
                    } else if (selectedIndex > 0) {
                        // Remove highlight from current
                        results[selectedIndex].classList.remove('highlight');
                        selectedIndex--;
                        console.log('[PlacesSearch] Moving to index:', selectedIndex);

                        // Highlight new selection
                        results[selectedIndex].classList.add('highlight');

                        // Update input to show the highlighted place name
                        isNavigating = true;
                        input.value = places[selectedIndex].displayName;
                        console.log('[PlacesSearch] Updated input to:', input.value);

                        // Scroll highlighted item into view
                        results[selectedIndex].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
                    } else if (selectedIndex === 0) {
                        // At first item, go back to original search text
                        results[0].classList.remove('highlight');
                        selectedIndex = -1;
                        isNavigating = true;
                        input.value = lastSearchQuery;
                        console.log('[PlacesSearch] Restored search query:', lastSearchQuery);
                    }
                }

                return false; // Stop all propagation
            }

            // Handle Enter key
            if (e.key === 'Enter' && selectedIndex >= 0 && places[selectedIndex]) {
                console.log('[PlacesSearch] Enter pressed with selection:', selectedIndex);
                e.preventDefault();
                e.stopPropagation();
                e.stopImmediatePropagation();

                // Update input field with the place name before submitting
                input.value = places[selectedIndex].displayName;
                this.currentState.selectedPlace = places[selectedIndex];
                console.log('[PlacesSearch] Submitting place:', places[selectedIndex].displayName);
                this.submitEntity();
                return false;
            }

            // Handle Escape key
            if (e.key === 'Escape' && results.length > 0) {
                console.log('[PlacesSearch] Escape pressed, clearing results');
                e.preventDefault();
                // Clear selection and results
                if (selectedIndex >= 0 && results[selectedIndex]) {
                    results[selectedIndex].classList.remove('highlight');
                }
                selectedIndex = -1;
                resultsDiv.innerHTML = '';
                places = [];
                input.value = '';
            }
        }, true); // Use capture phase to get the event first!

        console.log('[PlacesSearch] Keyboard handler attached');

        // Handle input changes for search
        input.addEventListener('input', async (e) => {
            // If we're navigating with arrows, don't treat this as a new search
            if (isNavigating) {
                isNavigating = false;
                return;
            }

            clearTimeout(debounceTimeout);
            const query = e.target.value.trim();
            lastSearchQuery = query; // Remember what the user typed
            selectedIndex = -1; // Reset selection when user types

            // Clear all highlights
            if (resultsDiv) {
                resultsDiv.querySelectorAll('.place-result').forEach(r => r.classList.remove('highlight'));
            }

            if (query.length < 2) {
                resultsDiv.innerHTML = '';
                places = [];
                return;
            }

            debounceTimeout = setTimeout(async () => {
                try {
                    let url = `/api/places-suggest?q=${encodeURIComponent(query)}`;
                    if (this.timelineEntry.patientLocation?.latitude) {
                        url += `&lat=${this.timelineEntry.patientLocation.latitude}&lon=${this.timelineEntry.patientLocation.longitude}`;
                    }

                    const response = await fetch(url);
                    places = await response.json(); // Cache the results

                    resultsDiv.innerHTML = places.map((place, index) => `
                        <div class="place-result" 
                             data-place='${JSON.stringify(place)}' 
                             data-index="${index}"
                             tabindex="-1"
                             role="option"
                             aria-label="Select ${place.displayName}">
                            <span class="icon">📍</span>
                            <div>
                                <div class="place-name">${place.displayName}</div>
                                <div class="place-address">${place.formattedAddress || ''}</div>
                            </div>
                        </div>
                    `).join('');

                    const handlePlaceSelect = (placeData) => {
                        // Update input field to show selected place name
                        isNavigating = true;
                        input.value = placeData.displayName;
                        this.currentState.selectedPlace = placeData;
                        this.submitEntity();
                    };

                    // Only click handlers needed - keyboard nav happens from input field
                    resultsDiv.querySelectorAll('.place-result').forEach((result) => {
                        result.addEventListener('click', () => {
                            const placeData = JSON.parse(result.dataset.place);
                            handlePlaceSelect(placeData);
                        });
                    });

                    selectedIndex = -1; // Reset selection
                } catch (error) {
                    console.error('Places search error:', error);
                }
            }, 300);
        });
    }

    submitEntity() {
        console.log('[EntityQuickAdd] submitEntity() called, currentState:', this.currentState);
        const entity = this.buildEntityFromState();
        console.log('[EntityQuickAdd] Built entity:', entity);
        if (!entity) {
            console.warn('[EntityQuickAdd] No valid entity to submit');
            return;
        }

        const isEditing = !!this.currentState.editingEntity;
        if (isEditing) {
            // Update existing entity
            this.updateEntityInText(entity);
        } else {
            // Insert new entity
            this.insertEntityIntoText(entity);
        }

        this.closeTippy();
    }

    submitUnknownEntity() {
        const entity = {
            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            entityType: this.getEntityTypeId(this.currentState.entityType),
            entityTypeName: this.currentState.entityType, // Set entityTypeName explicitly
            rawText: `unknown ${this.currentState.entityType.toLowerCase()}`,
            normalizedValue: `unknown ${this.currentState.entityType.toLowerCase()}`,
            confidence: 0,
            isConfirmed: false,
            metadata: { unknown: true }
        };

        this.insertEntityIntoText(entity);
        this.closeTippy();
    }

    submitExistingEntity(value) {
        const entity = {
            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            entityType: this.getEntityTypeId(this.currentState.entityType),
            entityTypeName: this.currentState.entityType, // Set entityTypeName explicitly
            rawText: value,
            normalizedValue: value,
            confidence: 3,
            isConfirmed: true,
            metadata: {}
        };

        this.insertEntityIntoText(entity);
        this.closeTippy();
    }

    submitConvention(convention) {
        // If setting up a convention from the autocomplete prompt
        const conventionName = this.currentState.conventionName || convention;

        const entity = {
            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            entityType: 2, // Location
            rawText: conventionName,
            normalizedValue: conventionName,
            confidence: 3,
            isConfirmed: false,
            metadata: { convention: true, conventionName: conventionName }
        };

        this.insertEntityIntoText(entity);
        this.closeTippy();

        // TODO: Could trigger a backend call here to save the convention
        // For now, it will be prompted for full setup when reviewing/saving
    }

    buildEntityFromState() {
        const form = this.activeTippy?.popper;
        console.log('[EntityQuickAdd] buildEntityFromState() - form:', form, 'currentState:', this.currentState);
        if (!form) {
            console.warn('[EntityQuickAdd] No form found in buildEntityFromState');
            return null;
        }

        const entity = {
            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            entityType: this.getEntityTypeId(this.currentState.entityType),
            rawText: '',
            normalizedValue: '',
            confidence: 3,
            isConfirmed: true,
            metadata: {}
        };

        switch(this.currentState.entityType) {
            case 'Person':
                entity.rawText = form.querySelector('#personName')?.value || 'someone';
                console.log('[EntityQuickAdd] Person entity rawText:', entity.rawText);
                entity.normalizedValue = entity.rawText;
                entity.metadata.relationship = form.querySelector('#personRelationship')?.value;
                break;

            case 'Location':
                if (this.currentState.selectedPlace && this.currentState.selectedPlace.displayName) {
                    // Google Places selection
                    entity.rawText = this.currentState.selectedPlace.displayName;
                    entity.normalizedValue = entity.rawText;
                    entity.metadata = {
                        placeId: this.currentState.selectedPlace.placeId,
                        address: this.currentState.selectedPlace.formattedAddress,
                        coordinates: this.currentState.selectedPlace.coordinates
                    };
                    entity.isConfirmed = true;
                    console.log('[EntityQuickAdd] Location (Google Places) entity:', entity);
                } else {
                    // Manual entry
                    const manualName = form.querySelector('#locationName')?.value?.trim();
                    console.log('[EntityQuickAdd] Location (manual) input value:', manualName);
                    if (!manualName) {
                        // No valid input - don't create entity
                        console.warn('[EntityQuickAdd] No location name provided');
                        return null;
                    }
                    entity.rawText = manualName;
                    entity.normalizedValue = manualName;
                    entity.isConfirmed = false;
                }
                break;
                
            case 'Transport':
                const details = form.querySelector('#transportDetails')?.value;
                entity.rawText = details || this.currentState.transportType;
                entity.normalizedValue = `${this.currentState.transportType}${details ? ' ' + details : ''}`;
                entity.metadata.transportType = this.currentState.transportType;
                break;
                
            case 'DateTime':
                if (this.currentState.timePeriod) {
                    entity.rawText = this.currentState.timePeriod;
                } else if (form.querySelector('#timeVague')?.value) {
                    entity.rawText = form.querySelector('#timeVague').value;
                } else {
                    const hour = form.querySelector('#timeHour')?.value;
                    const minute = form.querySelector('#timeMinute')?.value || '00';
                    const ampm = form.querySelector('#timeAmPm')?.value;
                    entity.rawText = hour ? `${hour}:${minute} ${ampm}` : 'some time';
                }
                entity.normalizedValue = entity.rawText;
                break;
                
            case 'Duration':
                if (this.currentState.duration) {
                    entity.rawText = this.currentState.duration;
                } else {
                    const value = form.querySelector('#durationValue')?.value;
                    const unit = form.querySelector('#durationUnit')?.value;
                    entity.rawText = value ? `${value} ${unit}` : 'a while';
                }
                entity.normalizedValue = entity.rawText;
                break;
                
            case 'Event':
                entity.rawText = form.querySelector('#eventName')?.value || 'an event';
                entity.normalizedValue = entity.rawText;
                entity.metadata.eventType = form.querySelector('#eventType')?.value;
                break;
        }

        return entity;
    }

    insertEntityIntoText(entity) {
        console.log('[EntityQuickAdd] insertEntityIntoText() called with entity:', entity);
        const textarea = this.currentState.textarea;
        if (!textarea) {
            console.warn('[EntityQuickAdd] No textarea reference in currentState for insertEntityIntoText');
            return;
        }

        // Ensure entityTypeName is set (needed for recent entity deduplication)
        if (!entity.entityTypeName && entity.entityType) {
            const typeMap = {1: 'Person', 2: 'Location', 3: 'Event', 4: 'Transport', 5: 'DateTime', 6: 'Duration'};
            entity.entityTypeName = typeMap[entity.entityType] || 'Person';
        }

        // Add stable IDs for proper entity grouping across mentions
        this.enrichEntityWithStableIds(entity);

        const text = textarea.value;
        const pos = this.currentState.cursorPosition;
        console.log('[EntityQuickAdd] Textarea value:', text);
        console.log('[EntityQuickAdd] Cursor position (T):', pos);

        // Insert entity text at T (Tribute has already removed the ..query trigger)
        let before = text.substring(0, pos);
        let after = text.substring(pos);

        // Clean up: if user typed `..john.` the trailing `.` might remain
        if (after.startsWith('.') && !before.endsWith('.')) {
            after = after.substring(1); // Remove the orphaned dot
        }

        // Preserve relationship operator if user typed +.., @.., or >..
        const operator = this.currentState.relationshipOperator;
        if (operator && !before.endsWith(operator)) {
            console.log(`[EntityQuickAdd] Preserving relationship operator "${operator}" before form entity`);
            before = before + operator;
        }

        const needsSpaceBefore = before.length > 0 && !before.endsWith(' ') && !before.endsWith('\n') && !before.endsWith('+') && !before.endsWith('@') && !before.endsWith('>');
        const needsSpaceAfter = after.length > 0 && !after.startsWith(' ') && !after.startsWith('\n') && !after.startsWith('.');

        // Calculate entity position in new text
        const startPosition = before.length + (needsSpaceBefore ? 1 : 0);
        const endPosition = startPosition + entity.rawText.length;

        // Add position information to entity
        entity.startPosition = startPosition;
        entity.endPosition = endPosition;

        const newText = before + 
                        (needsSpaceBefore ? ' ' : '') + 
                        entity.rawText + 
                        (needsSpaceAfter ? ' ' : '') + 
                        after;

        console.log('[EntityQuickAdd] New text value:', newText);
        console.log('[EntityQuickAdd] Entity position:', startPosition, '-', endPosition);
        textarea.value = newText;

        // Position cursor
        const newCursorPos = (before + (needsSpaceBefore ? ' ' : '') + entity.rawText).length;
        textarea.setSelectionRange(newCursorPos, newCursorPos);

        // Store entity BEFORE triggering parse
        if (this.currentState.entryId) {
            if (!this.timelineEntry.entryEntities[this.currentState.entryId]) {
                this.timelineEntry.entryEntities[this.currentState.entryId] = [];
            }
            // Mark entity as freshly added to skip position adjustment in handleTextInput
            entity.freshlyAdded = true;
            this.timelineEntry.entryEntities[this.currentState.entryId].push(entity);
            console.log('[EntityQuickAdd] Stored entity in entryEntities');
        }

        // Trigger parse (with setTimeout for timing)
        setTimeout(() => {
            const event = new Event('input', { bubbles: true });
            textarea.dispatchEvent(event);
            console.log('[EntityQuickAdd] Triggered input event on textarea');
        }, 0);

        // Close the menu and clean up
        this.closeTippy();
        this.removeDoubleDot();

        textarea.focus();
    }

    /**
     * Update an existing entity in the textarea text and entity array
     * @param {Object} entity - The updated entity
     */
    updateEntityInText(entity) {
        console.log('[EntityQuickAdd] updateEntityInText() called with entity:', entity);
        const textarea = this.currentState.textarea;
        const originalEntity = this.currentState.originalEntity;

        if (!textarea || !originalEntity) {
            console.warn('[EntityQuickAdd] Missing textarea or originalEntity for update');
            return;
        }

        const text = textarea.value;
        const entryId = this.currentState.entryId;

        // Replace the old text with the new text
        const before = text.substring(0, originalEntity.startPosition);
        const after = text.substring(originalEntity.endPosition);

        const needsSpaceBefore = before.length > 0 && !before.endsWith(' ');
        const needsSpaceAfter = after.length > 0 && !after.startsWith(' ');

        // Calculate new entity position
        const startPosition = before.length + (needsSpaceBefore ? 1 : 0);
        const endPosition = startPosition + entity.rawText.length;

        // Update entity positions
        entity.startPosition = startPosition;
        entity.endPosition = endPosition;

        const newText = before + 
                        (needsSpaceBefore ? ' ' : '') + 
                        entity.rawText + 
                        (needsSpaceAfter ? ' ' : '') + 
                        after;

        console.log('[EntityQuickAdd] Updating text from:', originalEntity.rawText, 'to:', entity.rawText);
        console.log('[EntityQuickAdd] New text value:', newText);
        textarea.value = newText;

        // Position cursor after the updated entity
        const newCursorPos = endPosition;
        textarea.setSelectionRange(newCursorPos, newCursorPos);

        // Update entity in entryEntities array
        if (entryId && this.timelineEntry.entryEntities[entryId]) {
            const index = this.timelineEntry.entryEntities[entryId].findIndex(e => e.id === originalEntity.id);
            if (index !== -1) {
                // Preserve the original ID so we maintain the same entity reference
                entity.id = originalEntity.id;
                this.timelineEntry.entryEntities[entryId][index] = entity;
                console.log('[EntityQuickAdd] Updated entity in entryEntities array');
            }
        }

        // Trigger parse to update highlights and visualizations
        setTimeout(() => {
            const event = new Event('input', { bubbles: true });
            textarea.dispatchEvent(event);
            console.log('[EntityQuickAdd] Triggered input event after update');
        }, 0);

        textarea.focus();
    }

    /**
     * Get recent entities from current browser session (instant search)
     * @returns {Array} Recent entities sorted by usage
     */
    getRecentEntitiesFromSession() {
        const entityMap = new Map(); // For deduplication by rawText
        const typeMap = {1: 'Person', 2: 'Location', 3: 'Event', 4: 'Transport', 5: 'DateTime', 6: 'Duration'};

        // Scan all entry entities from browser memory
        Object.entries(this.timelineEntry.entryEntities).forEach(([entryId, entities]) => {
            entities.forEach(entity => {
                // Include all entities with rawText (both confirmed and parser-detected)
                if (entity.rawText && entity.isConfirmed) {
                    // Normalize entity type name: try entityTypeName, recordType, or map from entityType
                    const typeName = entity.entityTypeName || entity.recordType || typeMap[entity.entityType] || 'Unknown';

                    // Ensure entity has entityTypeName set for consistent rendering
                    if (!entity.entityTypeName) {
                        entity.entityTypeName = typeName;
                    }

                    const key = `${typeName}:${entity.rawText.toLowerCase()}`;
                    if (!entityMap.has(key)) {
                        entityMap.set(key, {
                            ...entity,
                            entityTypeName: typeName,  // Ensure this is always set
                            recordType: typeName,       // Also set recordType for icon rendering
                            lastUsed: Date.now()
                        });
                    }
                }
            });
        });

        // Convert to array and sort by type priority, then alphabetically
        const typePriority = { 'Person': 1, 'Location': 2, 'Transport': 3, 'DateTime': 4, 'Duration': 5, 'Event': 6 };
        const sorted = Array.from(entityMap.values()).sort((a, b) => {
            const priorityDiff = (typePriority[a.entityTypeName] || 99) - (typePriority[b.entityTypeName] || 99);
            if (priorityDiff !== 0) return priorityDiff;
            return a.rawText.localeCompare(b.rawText);
        });

        console.log(`[EntityQuickAdd] Found ${sorted.length} recent entities (deduplicated)`);

        // Return top 5
        return sorted.slice(0, 5);
    }

    removeDoubleDot() {
        const textarea = this.currentState.textarea;
        if (!textarea) return;

        const text = textarea.value;
        const pos = this.currentState.cursorPosition;

        const newText = text.substring(0, pos - 2) + text.substring(pos);
        textarea.value = newText;
        textarea.setSelectionRange(pos - 2, pos - 2);
        textarea.focus();
    }

    /**
     * Register a recently-inserted entity after Tribute has already placed its text.
     * Tribute natively inserted entityData.rawText in place of ..query, so this method
     * only needs to record the entity at the correct positions and trigger a re-parse.
     * @param {Object} entityData - Recent entity data from session memory
     * @param {HTMLTextAreaElement} textarea
     * @param {number} startPosition - Character index where entity text starts
     * @param {number} endPosition - Character index where entity text ends
     */
    finishRecentEntityInsertion(entityData, textarea, startPosition, endPosition) {
        const entryId = textarea.closest('.timeline-day-block')?.dataset.entryId;
        if (!entryId) {
            console.error('[EntityQuickAdd] Could not find entry ID');
            return;
        }

        console.log('[EntityQuickAdd] finishRecentEntityInsertion - entityData:', entityData);

        // Clean up orphaned query characters (e.g., "..mc" leaves "c" after "McDonald's" insertion)
        // Tribute only replaces the trigger ("..") not the query text ("mc")
        let text = textarea.value;
        const queryLength = this.currentState.queryLength || 0;
        if (queryLength > 0) {
            // Remove the orphaned query characters that come after the inserted entity
            const orphanedText = text.substring(endPosition, endPosition + queryLength);
            console.log(`[EntityQuickAdd] Removing ${queryLength} orphaned query characters: "${orphanedText}"`);
            text = text.substring(0, endPosition) + text.substring(endPosition + queryLength);
            textarea.value = text;
        }

        // Preserve relationship operator if user typed +..entity, @..entity, or >..entity
        const operator = this.currentState.relationshipOperator;
        if (operator) {
            console.log(`[EntityQuickAdd] Preserving relationship operator "${operator}" before entity`);
            // Check if operator is already there (from previous insertion)
            const before = text.substring(0, startPosition);
            if (!before.endsWith(operator)) {
                // Insert operator before entity
                text = text.substring(0, startPosition) + operator + text.substring(startPosition);
                textarea.value = text;
                // Adjust positions to account for the inserted operator
                startPosition += operator.length;
                endPosition += operator.length;
            }
        }

        // Clean up orphaned trailing dot if user typed `..john.`
        // Tribute stops matching at punctuation, leaving the `.` behind
        const after = text.substring(endPosition);
        if (after.startsWith('.') && !text.substring(0, startPosition).endsWith('.')) {
            console.log('[EntityQuickAdd] Removing orphaned trailing dot after recent entity');
            text = text.substring(0, endPosition) + text.substring(endPosition + 1);
            textarea.value = text;
            // Cursor position stays at endPosition (before the removed dot)
        }

        // Ensure entityTypeName is set
        let entityTypeName = entityData.entityTypeName;
        if (!entityTypeName && entityData.entityType) {
            const typeMap = {1: 'Person', 2: 'Location', 3: 'Event', 4: 'Transport', 5: 'DateTime', 6: 'Duration'};
            entityTypeName = typeMap[entityData.entityType] || 'Person';
        }

        const entity = {
            id: `entity_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
            entityType: entityData.entityType,
            entityTypeName: entityTypeName,
            rawText: entityData.rawText,
            normalizedValue: entityData.normalizedValue || entityData.rawText,
            confidence: 3,
            isConfirmed: true,
            startPosition,
            endPosition,
            metadata: entityData.metadata || {},
            // CRITICAL: Link back to the original entity for sidebar deduplication
            // This lets the sidebar group all mentions of "John" even though each has a unique id
            sourceEntityId: entityData.id  // The original entity's browser-session ID
        };

        // Copy database IDs if they exist (after save, these will be populated)
        // Also generate stable location/person IDs based on normalized value for proper grouping
        if (entityData.personId) {
            entity.personId = entityData.personId;
        } else if (entityData.entityType === 1) { // Person
            // Generate stable ID from normalized value
            entity.personId = this.generateStableId(entityData.normalizedValue || entityData.rawText);
        }

        if (entityData.locationId) {
            entity.locationId = entityData.locationId;
        } else if (entityData.entityType === 2) { // Location
            // Generate stable ID from normalized value
            entity.locationId = this.generateStableId(entityData.normalizedValue || entityData.rawText);
        }

        if (entityData.transportId) {
            entity.transportId = entityData.transportId;
        } else if (entityData.entityType === 4) { // Transport
            // Generate stable ID from normalized value
            entity.transportId = this.generateStableId(entityData.normalizedValue || entityData.rawText);
        }

        if (entityData.eventId) {
            entity.eventId = entityData.eventId;
        } else if (entityData.entityType === 3) { // Event
            // Generate stable ID from normalized value
            entity.eventId = this.generateStableId(entityData.normalizedValue || entityData.rawText);
        }

        console.log('[EntityQuickAdd] Created entity for insertion:', entity);
        console.log('[EntityQuickAdd] Entity locationId:', entity.locationId, 'personId:', entity.personId);
        console.log('[EntityQuickAdd] Linked to source entity:', entityData.id);

        if (!this.timelineEntry.entryEntities[entryId]) {
            this.timelineEntry.entryEntities[entryId] = [];
        }
        this.timelineEntry.entryEntities[entryId].push(entity);

        this.currentState.pendingRecentEntity = null;
        this.currentState.pendingEntityText = null;

        setTimeout(() => {
            const event = new Event('input', { bubbles: true });
            textarea.dispatchEvent(event);
        }, 0);

        textarea.focus();
    }

    /**
     * Escape HTML special characters to prevent XSS and rendering issues
     * @param {string} text - The text to escape
     * @returns {string} The escaped text
     */
    escapeHtml(text) {
        if (!text) return '';
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
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
     * Edit an existing entity using the same Tippy.js inline popup
     * @param {Object} entity - The entity to edit
     * @param {string} entryId - The entry ID containing the entity
     */
    editEntity(entity, entryId) {
        console.log('[EntityQuickAdd] editEntity() called for:', entity);

        // Find the textarea for this entry
        const textarea = document.querySelector(`textarea[data-entry-id="${entryId}"]`);
        if (!textarea) {
            console.error('[EntityQuickAdd] Could not find textarea for entry:', entryId);
            return;
        }

        // Get the entity type name from numeric ID
        const entityTypeName = this.timelineEntry.entityTypeMap[entity.entityType] || 'Person';

        // Set up currentState for editing
        this.currentState = {
            textarea,
            cursorPosition: entity.startPosition || 0,
            entityType: entityTypeName,
            entryId,
            editingEntity: entity, // Flag that we're editing
            originalEntity: { ...entity } // Keep original for comparison
        };

        // For Person entities, show the person details form directly (skip the search menu)
        if (entityTypeName === 'Person') {
            const personName = entity.rawText || entity.normalizedValue || '';
            this.showPersonDetailsForm(personName);
        } else {
            // For other entity types, show the standard form
            this.showEntityForm(entityTypeName);
        }
    }

    getEntityTypeId(typeName) {
        const typeMap = {
            'Person': 1,
            'Location': 2,
            'Event': 3,
            'Transport': 4,
            'DateTime': 5,
            'Duration': 6
        };
        return typeMap[typeName] || 1;
    }

    setupFormKeyboardNavigation(formContainer) {
        // Handle Escape key to close form
        const keydownHandler = (e) => {
            if (e.key === 'Escape') {
                e.preventDefault();
                this.closeTippy();
                // Return focus to textarea
                if (this.currentState.textarea) {
                    this.currentState.textarea.focus();
                }
            } else if (e.key === 'Enter' && !e.shiftKey) {
                // Allow Enter in textareas and search inputs
                if (e.target.tagName === 'TEXTAREA' || 
                    e.target.id === 'locationSearch' ||
                    e.target.classList.contains('place-result')) {
                    return; // Let default behavior handle it
                }

                // Check if we're in an input field with a value
                if (e.target.tagName === 'INPUT' && e.target.value.trim()) {
                    e.preventDefault();
                    // Find and click the submit button
                    const submitBtn = formContainer.querySelector('.btn-primary[data-action="submit"]');
                    if (submitBtn) submitBtn.click();
                }
            }
        };

        formContainer.addEventListener('keydown', keydownHandler);

        // Make all buttons keyboard accessible with proper focus styles
        formContainer.querySelectorAll('button').forEach(btn => {
            if (!btn.hasAttribute('tabindex')) {
                btn.setAttribute('tabindex', '0');
            }
        });

        // Handle Tab navigation to keep focus within form
        formContainer.addEventListener('keydown', (e) => {
            if (e.key === 'Tab') {
                const focusableElements = formContainer.querySelectorAll(
                    'input:not([disabled]), select:not([disabled]), button:not([disabled]), [tabindex]:not([tabindex="-1"])'
                );
                const firstElement = focusableElements[0];
                const lastElement = focusableElements[focusableElements.length - 1];

                // If Shift+Tab on first element, go to last
                if (e.shiftKey && document.activeElement === firstElement) {
                    e.preventDefault();
                    lastElement.focus();
                }
                // If Tab on last element, go to first
                else if (!e.shiftKey && document.activeElement === lastElement) {
                    e.preventDefault();
                    firstElement.focus();
                }
            }
        });
    }

    closeTippy() {
        if (this.activeTippy) {
            this.activeTippy.destroy();
            this.activeTippy = null;
        }
        this.currentState = {};
    }

    /**
     * Setup keyboard-accessible grid navigation (roving tabindex pattern)
     * @param {HTMLElement} gridContainer - Container element with role="radiogroup"
     * @param {string} itemSelector - Selector for grid items (e.g., '.transport-btn')
     * @param {Function} onSelect - Callback when item is selected
     */
    setupGridNavigation(gridContainer, itemSelector, onSelect) {
        const items = Array.from(gridContainer.querySelectorAll(itemSelector));
        if (items.length === 0) return;

        let currentIndex = 0;

        // Calculate grid dimensions (assuming 2D grid layout)
        const gridStyles = window.getComputedStyle(gridContainer);
        const columns = gridStyles.gridTemplateColumns ? 
            gridStyles.gridTemplateColumns.split(' ').length : 
            items.length; // Fallback to single row

        console.log(`[GridNav] Grid has ${items.length} items in ${columns} columns`);

        /**
         * Move focus to specific index using roving tabindex pattern
         */
        const focusItem = (index) => {
            if (index < 0 || index >= items.length) return false;

            // Update tabindex (roving tabindex pattern)
            items.forEach((item, i) => {
                item.tabIndex = i === index ? 0 : -1;
            });

            items[index].focus();
            currentIndex = index;
            return true;
        };

        /**
         * Handle arrow key navigation in 2D grid
         */
        const handleArrowKey = (key) => {
            let newIndex = currentIndex;

            switch(key) {
                case 'ArrowRight':
                    newIndex = currentIndex + 1;
                    if (newIndex >= items.length) newIndex = currentIndex; // Stay at end
                    break;
                case 'ArrowLeft':
                    newIndex = currentIndex - 1;
                    if (newIndex < 0) newIndex = 0; // Stay at start
                    break;
                case 'ArrowDown':
                    newIndex = currentIndex + columns;
                    if (newIndex >= items.length) {
                        // Wrap to first column of same row if at bottom
                        newIndex = currentIndex % columns;
                    }
                    break;
                case 'ArrowUp':
                    newIndex = currentIndex - columns;
                    if (newIndex < 0) {
                        // Wrap to last row, same column
                        const column = currentIndex % columns;
                        const lastRow = Math.floor((items.length - 1) / columns);
                        newIndex = Math.min(lastRow * columns + column, items.length - 1);
                    }
                    break;
                case 'Home':
                    newIndex = 0;
                    break;
                case 'End':
                    newIndex = items.length - 1;
                    break;
            }

            return focusItem(newIndex);
        };

        // Attach event listeners to each grid item
        items.forEach((item, index) => {
            // Click handler
            item.addEventListener('click', () => {
                focusItem(index);
                onSelect(item);
            });

            // Keyboard handler
            item.addEventListener('keydown', (e) => {
                // Arrow key navigation
                if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) {
                    e.preventDefault();
                    handleArrowKey(e.key);
                    return;
                }

                // Enter or Space activates selection
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    onSelect(item);
                    return;
                }
            });

            // Update current index on focus
            item.addEventListener('focus', () => {
                currentIndex = index;
            });
        });

        console.log('[GridNav] Keyboard navigation setup complete for', itemSelector);
    }

    /**
     * Setup keyboard-accessible single-column list navigation (simplified for keyboard-first UX)
     * @param {HTMLElement} listContainer - Container element
     * @param {string} itemSelector - Selector for list items (e.g., '.transport-btn')
     * @param {Function} onSelect - Callback when item is selected
     */
    setupListNavigation(listContainer, itemSelector, onSelect) {
        const items = Array.from(listContainer.querySelectorAll(itemSelector));
        if (items.length === 0) return;

        let currentIndex = 0;

        // Focus first item initially
        items[0].focus();

        /**
         * Move focus to specific index
         */
        const focusItem = (index) => {
            if (index < 0 || index >= items.length) return false;
            items[index].focus();
            items[index].classList.add('keyboard-selected');
            items.forEach((item, i) => {
                if (i !== index) item.classList.remove('keyboard-selected');
            });
            currentIndex = index;
            return true;
        };

        // Attach event listeners to each list item
        items.forEach((item, index) => {
            // Click handler
            item.addEventListener('click', () => {
                focusItem(index);
                onSelect(item);
            });

            // Keyboard handler
            item.addEventListener('keydown', (e) => {
                switch(e.key) {
                    case 'ArrowDown':
                        e.preventDefault();
                        focusItem((currentIndex + 1) % items.length);
                        break;
                    case 'ArrowUp':
                        e.preventDefault();
                        focusItem((currentIndex - 1 + items.length) % items.length);
                        break;
                    case 'Home':
                        e.preventDefault();
                        focusItem(0);
                        break;
                    case 'End':
                        e.preventDefault();
                        focusItem(items.length - 1);
                        break;
                    case 'Enter':
                    case ' ':
                        e.preventDefault();
                        onSelect(item);
                        break;
                }
            });

            // Update current index on focus
            item.addEventListener('focus', () => {
                currentIndex = index;
            });
        });

        console.log('[ListNav] Keyboard navigation setup complete for', itemSelector, '(', items.length, 'items )');
    }

    destroy() {
        this.closeTippy();
        if (this.tribute) {
            // Tribute doesn't have a destroy method, just detach from all
            this.tribute = null;
        }
    }
}
