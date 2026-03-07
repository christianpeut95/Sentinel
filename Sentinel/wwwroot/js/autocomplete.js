/**
 * Surveillance MVP - Autocomplete Utilities
 * Provides reusable autocomplete functionality for large datasets
 */

class SurveillanceAutocomplete {
    constructor(inputElement, options) {
        this.input = inputElement;
        this.options = {
            apiUrl: options.apiUrl,
            minLength: options.minLength || 2,
            maxResults: options.maxResults || 20,
            displayField: options.displayField || 'name',
            valueField: options.valueField || 'id',
            onSelect: options.onSelect || (() => {}),
            template: options.template || null,
            placeholder: options.placeholder || 'Type to search...',
            debounceMs: options.debounceMs || 300
        };

        this.hiddenInput = null;
        this.resultsContainer = null;
        this.debounceTimer = null;
        this.currentFocus = -1;

        this.init();
    }

    init() {
        // Set placeholder
        this.input.placeholder = this.options.placeholder;
        this.input.autocomplete = 'off';

        // Create hidden input for actual value
        this.hiddenInput = document.createElement('input');
        this.hiddenInput.type = 'hidden';
        this.hiddenInput.name = this.input.name;
        this.hiddenInput.id = this.input.id + '_value';
        this.input.after(this.hiddenInput);

        // Remove name from visible input
        this.input.removeAttribute('name');
        this.input.id = this.input.id + '_display';

        // Create results container
        this.resultsContainer = document.createElement('div');
        this.resultsContainer.className = 'autocomplete-results';
        this.input.parentElement.style.position = 'relative';
        this.input.after(this.resultsContainer);

        // Bind events
        this.input.addEventListener('input', (e) => this.handleInput(e));
        this.input.addEventListener('keydown', (e) => this.handleKeyDown(e));
        this.input.addEventListener('blur', () => setTimeout(() => this.hideResults(), 200));
        
        // Add CSS if not already present
        this.addStyles();
    }

    handleInput(e) {
        clearTimeout(this.debounceTimer);
        const query = e.target.value.trim();

        if (query.length < this.options.minLength) {
            this.hideResults();
            this.hiddenInput.value = '';
            return;
        }

        this.debounceTimer = setTimeout(() => this.search(query), this.options.debounceMs);
    }

    async search(query) {
        try {
            const url = `${this.options.apiUrl}?term=${encodeURIComponent(query)}`;
            const response = await fetch(url);
            const results = await response.json();

            this.displayResults(results);
        } catch (error) {            this.hideResults();
        }
    }

    displayResults(results) {
        this.resultsContainer.innerHTML = '';
        this.currentFocus = -1;

        if (!results || results.length === 0) {
            this.resultsContainer.innerHTML = '<div class="autocomplete-item no-results">No results found</div>';
            this.resultsContainer.style.display = 'block';
            return;
        }

        results.forEach((item, index) => {
            const div = document.createElement('div');
            div.className = 'autocomplete-item';
            
            if (this.options.template) {
                div.innerHTML = this.options.template(item);
            } else {
                div.textContent = item[this.options.displayField];
            }

            div.addEventListener('click', () => this.selectItem(item));
            div.dataset.index = index;

            this.resultsContainer.appendChild(div);
        });

        this.resultsContainer.style.display = 'block';
    }

    selectItem(item) {
        this.input.value = item[this.options.displayField];
        this.hiddenInput.value = item[this.options.valueField];
        this.hideResults();
        this.options.onSelect(item);
    }

    handleKeyDown(e) {
        const items = this.resultsContainer.querySelectorAll('.autocomplete-item:not(.no-results)');
        
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            this.currentFocus++;
            if (this.currentFocus >= items.length) this.currentFocus = 0;
            this.setActive(items);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            this.currentFocus--;
            if (this.currentFocus < 0) this.currentFocus = items.length - 1;
            this.setActive(items);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (this.currentFocus > -1 && items[this.currentFocus]) {
                items[this.currentFocus].click();
            }
        } else if (e.key === 'Escape') {
            this.hideResults();
        }
    }

    setActive(items) {
        items.forEach((item, index) => {
            if (index === this.currentFocus) {
                item.classList.add('active');
            } else {
                item.classList.remove('active');
            }
        });
    }

    hideResults() {
        this.resultsContainer.style.display = 'none';
        this.currentFocus = -1;
    }

    addStyles() {
        if (document.getElementById('surveillance-autocomplete-styles')) return;

        const style = document.createElement('style');
        style.id = 'surveillance-autocomplete-styles';
        style.textContent = `
            .autocomplete-results {
                position: absolute;
                z-index: 1050;
                background: white;
                border: 1px solid #cbd5e0;
                border-radius: 8px;
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
                max-height: 300px;
                overflow-y: auto;
                width: 100%;
                display: none;
                margin-top: 4px;
            }

            .autocomplete-item {
                padding: 10px 12px;
                cursor: pointer;
                border-bottom: 1px solid #e2e8f0;
                transition: background-color 0.15s;
            }

            .autocomplete-item:last-child {
                border-bottom: none;
            }

            .autocomplete-item:hover,
            .autocomplete-item.active {
                background-color: #f7fafc;
            }

            .autocomplete-item.no-results {
                color: #718096;
                cursor: default;
                text-align: center;
                font-style: italic;
            }

            .autocomplete-item.no-results:hover {
                background-color: white;
            }

            .autocomplete-item-title {
                font-weight: 600;
                color: #2d3748;
                margin-bottom: 2px;
            }

            .autocomplete-item-subtitle {
                font-size: 0.875rem;
                color: #718096;
            }

            .autocomplete-item-badge {
                display: inline-block;
                padding: 2px 6px;
                background: #e2e8f0;
                border-radius: 4px;
                font-size: 0.75rem;
                color: #4a5568;
                margin-left: 8px;
            }
        `;
        document.head.appendChild(style);
    }

    destroy() {
        this.input.removeEventListener('input', this.handleInput);
        this.input.removeEventListener('keydown', this.handleKeyDown);
        this.resultsContainer.remove();
        this.hiddenInput.remove();
    }
}

// Initialize all autocomplete fields with data attributes
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('[data-autocomplete]').forEach(input => {
        const apiUrl = input.dataset.autocompleteUrl;
        const displayField = input.dataset.autocompleteDisplay || 'name';
        const valueField = input.dataset.autocompleteValue || 'id';
        const minLength = parseInt(input.dataset.autocompleteMinlength) || 2;

        new SurveillanceAutocomplete(input, {
            apiUrl,
            displayField,
            valueField,
            minLength
        });
    });
});

// Export for global use
window.SurveillanceAutocomplete = SurveillanceAutocomplete;
