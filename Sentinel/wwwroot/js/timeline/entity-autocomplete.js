/**
 * Entity Autocomplete - VSCode-style autocomplete for entity suggestions
 */
class EntityAutocomplete {
    constructor(caseId, timelineEntry) {
        this.caseId = caseId;
        this.timelineEntry = timelineEntry;
        this.dropdown = null;
        this.currentEntity = null;
        this.currentTextarea = null;
        this.selectedIndex = 0;
        this.suggestions = [];
        this.entityMemory = null;
        
        this.loadEntityMemory();
    }

    async loadEntityMemory() {
        try {
            const response = await fetch(`/api/timeline/memory/${this.caseId}`);
            if (response.ok) {
                this.entityMemory = await response.json();
                console.log('Entity memory loaded:', this.entityMemory);
            }
        } catch (error) {
            console.error('Error loading entity memory:', error);
        }
    }

    async show(textarea, entity) {
        this.currentTextarea = textarea;
        this.currentEntity = entity;
        this.selectedIndex = 0;

        // Get suggestions based on entity type
        this.suggestions = await this.getSuggestions(entity);

        if (this.suggestions.length === 0) {
            this.hide();
            return;
        }

        // Create or update dropdown
        if (!this.dropdown) {
            this.createDropdown();
        }

        this.updateDropdownContent();
        this.positionDropdown(textarea, entity);
        
        if (this.dropdown) {
            this.dropdown.style.display = 'block';
        }
    }

    hide() {
        if (this.dropdown) {
            this.dropdown.style.display = 'none';
        }
        this.currentEntity = null;
        this.currentTextarea = null;
        this.suggestions = [];
        this.selectedIndex = 0;
    }

    isVisible() {
        return this.dropdown && this.dropdown.style.display === 'block';
    }

    createDropdown() {
        this.dropdown = document.createElement('div');
        this.dropdown.className = 'entity-autocomplete';
        this.dropdown.style.display = 'none';
        document.body.appendChild(this.dropdown);

        // Click outside to close
        document.addEventListener('click', (e) => {
            if (this.dropdown && !this.dropdown.contains(e.target) && e.target !== this.currentTextarea) {
                this.hide();
            }
        });
    }

    updateDropdownContent() {
        if (!this.dropdown) return;

        let html = '';

        this.suggestions.forEach((suggestion, index) => {
            const isActive = index === this.selectedIndex;
            html += `
                <div class="autocomplete-item ${isActive ? 'active' : ''}" data-index="${index}">
                    <div class="autocomplete-item-title">
                        <span class="autocomplete-item-icon">${this.getIconForType(suggestion.recordType || this.currentEntity.entityType)}</span>
                        ${this.escapeHtml(suggestion.displayText)}
                    </div>
                    ${suggestion.description ? `
                        <div class="autocomplete-item-description">${this.escapeHtml(suggestion.description)}</div>
                    ` : ''}
                    ${suggestion.address ? `
                        <div class="autocomplete-item-description">${this.escapeHtml(suggestion.address)}</div>
                    ` : ''}
                </div>
            `;
        });

        // Add "Add new" option
        html += `
            <div class="autocomplete-item autocomplete-add-new" data-index="${this.suggestions.length}">
                <div class="autocomplete-item-title">
                    <i class="bi bi-plus-circle autocomplete-item-icon"></i>
                    Add new "${this.currentEntity.rawText}"
                </div>
            </div>
        `;

        this.dropdown.innerHTML = html;

        // Attach click handlers
        this.dropdown.querySelectorAll('.autocomplete-item').forEach((item, index) => {
            item.addEventListener('click', () => {
                this.selectedIndex = index;
                this.acceptSuggestion();
            });
            
            item.addEventListener('mouseenter', () => {
                this.selectedIndex = index;
                this.updateSelection();
            });
        });
    }

    positionDropdown(textarea, entity) {
        if (!this.dropdown) return;

        const textareaRect = textarea.getBoundingClientRect();
        
        // Try to position below the entity text
        // For simplicity, position below textarea for now
        const top = textareaRect.bottom + window.scrollY + 5;
        const left = textareaRect.left + window.scrollX;

        this.dropdown.style.position = 'absolute';
        this.dropdown.style.top = `${top}px`;
        this.dropdown.style.left = `${left}px`;
        this.dropdown.style.minWidth = `${textareaRect.width}px`;
    }

    async getSuggestions(entity) {
        const suggestions = [];

        if (!this.entityMemory) {
            await this.loadEntityMemory();
        }

        if (!this.entityMemory) return suggestions;

        const entityType = entity.entityType.toLowerCase();
        const searchText = entity.rawText.toLowerCase();

        // Get suggestions based on entity type
        if (entityType === 'person') {
            // Search known people
            const matches = this.entityMemory.people.filter(p => 
                p.displayText.toLowerCase().includes(searchText)
            );
            suggestions.push(...matches);
        } else if (entityType === 'location') {
            // Check if it's a convention name first
            const conventionName = entity.metadata?.IsConvention ? entity.normalizedValue : null;
            
            if (conventionName && this.entityMemory.conventions[conventionName]) {
                const convention = this.entityMemory.conventions[conventionName];
                suggestions.push({
                    displayText: convention.locationName || conventionName,
                    description: convention.freeTextAddress || 'User-defined location',
                    address: convention.freeTextAddress,
                    recordType: 'Convention',
                    recordId: convention.locationId,
                    score: 1.0
                });
            } else {
                // Search known locations
                const matches = this.entityMemory.locations.filter(loc => 
                    loc.displayText.toLowerCase().includes(searchText)
                );
                suggestions.push(...matches);
            }

            // TODO: Add Google Places API suggestions
            // For now, just use entity memory
        }

        return suggestions.sort((a, b) => (b.score || 0) - (a.score || 0));
    }

    navigate(direction) {
        const maxIndex = this.suggestions.length; // includes "Add new"
        this.selectedIndex += direction;

        if (this.selectedIndex < 0) {
            this.selectedIndex = maxIndex;
        } else if (this.selectedIndex > maxIndex) {
            this.selectedIndex = 0;
        }

        this.updateSelection();
    }

    updateSelection() {
        if (!this.dropdown) return;

        this.dropdown.querySelectorAll('.autocomplete-item').forEach((item, index) => {
            if (index === this.selectedIndex) {
                item.classList.add('active');
                item.scrollIntoView({ block: 'nearest' });
            } else {
                item.classList.remove('active');
            }
        });
    }

    acceptSuggestion() {
        if (this.selectedIndex === this.suggestions.length) {
            // "Add new" selected - keep the original text
            console.log('Add new entity:', this.currentEntity.rawText);
            this.hide();
            return;
        }

        const suggestion = this.suggestions[this.selectedIndex];
        if (!suggestion) {
            this.hide();
            return;
        }

        // Update the entity with the selected suggestion
        this.currentEntity.linkedRecordDisplayName = suggestion.displayText;
        this.currentEntity.linkedRecordId = suggestion.recordId;
        this.currentEntity.linkedRecordType = suggestion.recordType;
        this.currentEntity.isConfirmed = true;
        this.currentEntity.confidence = 'High';

        console.log('Accepted suggestion:', suggestion);

        // TODO: Update the highlighted entity in the UI
        // For now, just hide the autocomplete
        this.hide();

        // Mark as changed
        this.timelineEntry.unsavedChanges = true;

        // Update summary
        this.timelineEntry.updateEntitySummary();
    }

    getIconForType(type) {
        const icons = {
            'Person': '<i class="bi bi-person-fill text-primary"></i>',
            'Location': '<i class="bi bi-geo-alt-fill text-success"></i>',
            'Event': '<i class="bi bi-calendar-event-fill" style="color: #9b59b6;"></i>',
            'Transport': '<i class="bi bi-bus-front-fill text-warning"></i>',
            'Convention': '<i class="bi bi-star-fill text-info"></i>',
            'DateTime': '<i class="bi bi-clock-fill text-danger"></i>',
            'Duration': '<i class="bi bi-hourglass-split text-secondary"></i>'
        };
        return icons[type] || '<i class="bi bi-dot"></i>';
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}
