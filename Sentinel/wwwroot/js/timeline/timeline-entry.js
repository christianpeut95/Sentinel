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
        
        this.init();
    }

    async init() {
        console.log('Initializing Timeline Entry for case:', this.caseId);
        
        // Load existing timeline data
        await this.loadTimeline();
        
        // Initialize components
        this.initializeEventListeners();
        this.initializeMap();
        
        // Initialize autocomplete (will be created on demand)
        this.autocomplete = new EntityAutocomplete(this.caseId, this);
        
        // Warn before leaving if there are unsaved changes
        window.addEventListener('beforeunload', (e) => {
            if (this.unsavedChanges) {
                e.preventDefault();
                e.returnValue = '';
            }
        });
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

    renderExistingEntries() {
        const container = document.getElementById('timelineContainer');
        container.innerHTML = '';

        this.timelineData.entries
            .sort((a, b) => new Date(a.entryDate) - new Date(b.entryDate))
            .forEach(entry => {
                this.addDayBlock(new Date(entry.entryDate), entry);
            });

        this.updateEntitySummary();
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

        // If there's existing entry data, parse and highlight it
        if (existingEntry && existingEntry.narrativeText) {
            this.parseAndHighlight(entryId, existingEntry.narrativeText);
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
        
        // Debounce parsing (wait 300ms after user stops typing)
        clearTimeout(this[`parseTimeout_${entryId}`]);
        this[`parseTimeout_${entryId}`] = setTimeout(async () => {
            await this.parseAndHighlight(entryId, text);
        }, 300);
    }

    handleKeyDown(event, entryId) {
        // Handle Tab key for autocomplete acceptance
        if (event.key === 'Tab' && this.autocomplete && this.autocomplete.isVisible()) {
            event.preventDefault();
            this.autocomplete.acceptSuggestion();
        }
        
        // Handle Arrow keys for autocomplete navigation
        if ((event.key === 'ArrowDown' || event.key === 'ArrowUp') && 
            this.autocomplete && this.autocomplete.isVisible()) {
            event.preventDefault();
            this.autocomplete.navigate(event.key === 'ArrowDown' ? 1 : -1);
        }
        
        // Handle Enter key for autocomplete acceptance
        if (event.key === 'Enter' && this.autocomplete && this.autocomplete.isVisible()) {
            event.preventDefault();
            this.autocomplete.acceptSuggestion();
        }
        
        // Handle Escape key to close autocomplete
        if (event.key === 'Escape' && this.autocomplete && this.autocomplete.isVisible()) {
            this.autocomplete.hide();
        }
    }

    handleDateChange(event, entryId) {
        const block = document.querySelector(`.timeline-day-block[data-entry-id="${entryId}"]`);
        if (block) {
            const newDate = new Date(event.target.value);
            block.dataset.entryDate = newDate.toISOString();
            this.unsavedChanges = true;
        }
    }

    async parseAndHighlight(entryId, text) {
        if (!text || text.trim() === '') {
            return;
        }

        try {
            const response = await fetch('/api/timeline/parse', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ text })
            });

            if (!response.ok) {
                throw new Error('Parsing failed');
            }

            const result = await response.json();
            console.log('Parse result:', result);

            // Update status display
            this.updateEntryStatus(entryId, result);

            // Highlight entities in the text
            this.highlightEntities(entryId, text, result.entities);

            // Update summary and map
            this.updateEntitySummary();
            this.updateMapPins();

            // Show autocomplete if appropriate
            const textarea = document.querySelector(`textarea[data-entry-id="${entryId}"]`);
            if (textarea && result.entities.length > 0) {
                const lastEntity = result.entities[result.entities.length - 1];
                this.autocomplete.show(textarea, lastEntity);
            }

        } catch (error) {
            console.error('Error parsing text:', error);
        }
    }

    highlightEntities(entryId, text, entities) {
        const highlightLayer = document.querySelector(`.narrative-highlight-layer[data-entry-id="${entryId}"]`);
        if (!highlightLayer) return;

        // Create highlighted version of the text
        let highlightedHtml = '';
        let lastIndex = 0;

        // Sort entities by position
        const sortedEntities = [...entities].sort((a, b) => a.startPosition - b.startPosition);

        sortedEntities.forEach(entity => {
            // Add text before entity
            highlightedHtml += this.escapeHtml(text.substring(lastIndex, entity.startPosition));

            // Add highlighted entity
            const entityClass = `entity-highlight entity-${entity.entityType.toLowerCase()}`;
            const entityText = text.substring(entity.startPosition, entity.endPosition);
            highlightedHtml += `<span class="${entityClass}" data-entity-id="${entity.id}" title="${entity.entityType}">${this.escapeHtml(entityText)}</span>`;

            lastIndex = entity.endPosition;
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
    }

    updateEntryStatus(entryId, parseResult) {
        const statusDiv = document.getElementById(`status-${entryId}`);
        if (!statusDiv) return;

        let statusHtml = '';

        // Show uncertainty markers
        if (parseResult.uncertaintyMarkers && parseResult.uncertaintyMarkers.length > 0) {
            statusHtml += parseResult.uncertaintyMarkers.map(marker => 
                `<span class="uncertainty-marker"><i class="bi bi-question-circle"></i> ${marker}</span>`
            ).join(' ');
        }

        // Show protective measures
        if (parseResult.protectiveMeasures && parseResult.protectiveMeasures.length > 0) {
            statusHtml += parseResult.protectiveMeasures.map(measure => 
                `<span class="protective-measure-tag"><i class="bi bi-shield-check"></i> ${measure}</span>`
            ).join(' ');
        }

        // Show memory gap
        if (parseResult.isMemoryGap) {
            statusHtml += `
                <div class="memory-gap-indicator">
                    <i class="bi bi-exclamation-triangle"></i>
                    <strong>Memory Gap:</strong> Patient unable to recall details for this date.
                </div>
            `;
        }

        // Show correction
        if (parseResult.correction) {
            statusHtml += `
                <div class="alert alert-info alert-sm mt-2">
                    <i class="bi bi-pencil-square"></i> Correction detected
                </div>
            `;
        }

        statusDiv.innerHTML = statusHtml;
    }

    showEntityDetails(entity, entryId) {
        // TODO: Show modal or popover with entity details for editing/confirmation
        console.log('Show details for entity:', entity);
        alert(`Entity: ${entity.rawText}\nType: ${entity.entityType}\nConfidence: ${entity.confidence}`);
    }

    updateEntitySummary() {
        const summaryDiv = document.getElementById('entitySummary');
        if (!summaryDiv) return;

        const allEntries = this.getAllCurrentEntries();
        
        if (allEntries.length === 0) {
            summaryDiv.innerHTML = `
                <div class="text-muted text-center py-3">
                    <i class="bi bi-inbox fs-3 d-block mb-2"></i>
                    No entities detected yet
                </div>
            `;
            return;
        }

        // TODO: Aggregate entities from all entries and display summary
        // For now, show count
        let totalEntities = 0;
        allEntries.forEach(entry => {
            if (entry.entities) {
                totalEntities += entry.entities.length;
            }
        });

        summaryDiv.innerHTML = `
            <div class="text-center">
                <p class="mb-0"><strong>${totalEntities}</strong> entities detected</p>
                <small class="text-muted">Parse results will appear here</small>
            </div>
        `;
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
                    narrativeText: textarea.value
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
        
        // Trigger parsing
        const entryId = newBlock.dataset.entryId;
        this.parseAndHighlight(entryId, newTextarea.value);
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
}
