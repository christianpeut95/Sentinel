/**
 * HL7 Review Queue JavaScript
 * Handles keyboard shortcuts, card expansion, and interactive features
 */

(function() {
    'use strict';

    const ReviewQueue = {
        currentCardIndex: 0,
        cards: [],
        shortcuts: {
            'n': 'Next message',
            'p': 'Previous message',
            'e': 'Expand/collapse current message',
            '1': 'Mark as Fixed - Config Updated',
            '2': 'Mark as Fixed - Manual Case',
            '3': 'Mark as Fixed - Mapping Added',
            '4': 'Mark as Ignored - Duplicate',
            '5': 'Mark as Ignored - Not Notifiable',
            '6': 'Mark as Escalated - Task Created',
            '7': 'Mark as Reprocessed',
            '?': 'Toggle shortcuts help'
        },

        init() {
            this.cards = Array.from(document.querySelectorAll('.review-message-card'));
            if (this.cards.length === 0) return;

            this.setupKeyboardShortcuts();
            this.setupCardExpansion();
            this.setupBulkActions();
            this.highlightCurrentCard();
            this.createShortcutsHint();
        },

        setupKeyboardShortcuts() {
            document.addEventListener('keydown', (e) => {
                // Ignore if user is typing in an input field
                if (e.target.matches('input, textarea, select')) {
                    return;
                }

                switch(e.key.toLowerCase()) {
                    case 'n':
                        e.preventDefault();
                        this.nextCard();
                        break;
                    case 'p':
                        e.preventDefault();
                        this.previousCard();
                        break;
                    case 'e':
                        e.preventDefault();
                        this.toggleCurrentCard();
                        break;
                    case '1':
                    case '2':
                    case '3':
                    case '4':
                    case '5':
                    case '6':
                    case '7':
                        e.preventDefault();
                        this.quickReview(e.key);
                        break;
                    case '?':
                        e.preventDefault();
                        this.toggleShortcutsHelp();
                        break;
                    case 'a':
                        if (e.ctrlKey || e.metaKey) {
                            e.preventDefault();
                            this.toggleSelectAll();
                        }
                        break;
                }
            });
        },

        setupCardExpansion() {
            this.cards.forEach(card => {
                const toggleBtn = card.querySelector('.review-toggle-content');
                if (toggleBtn) {
                    toggleBtn.addEventListener('click', () => {
                        this.toggleCard(card);
                    });
                }

                // Click on card header to expand
                const header = card.querySelector('.review-card-header');
                if (header) {
                    header.addEventListener('click', (e) => {
                        // Don't expand if clicking on action buttons or links
                        if (!e.target.closest('.review-card-actions') && 
                            !e.target.closest('a') && 
                            !e.target.closest('button')) {
                            this.toggleCard(card);
                        }
                    });
                }
            });
        },

        setupBulkActions() {
            const selectAllCheckbox = document.querySelector('#selectAll');
            if (selectAllCheckbox) {
                selectAllCheckbox.addEventListener('change', (e) => {
                    this.selectAll(e.target.checked);
                });
            }

            // Individual checkboxes
            const checkboxes = document.querySelectorAll('.review-message-checkbox');
            checkboxes.forEach(checkbox => {
                checkbox.addEventListener('change', () => {
                    this.updateBulkActionsState();
                });
            });
        },

        nextCard() {
            if (this.currentCardIndex < this.cards.length - 1) {
                this.currentCardIndex++;
                this.highlightCurrentCard();
                this.scrollToCurrentCard();
            }
        },

        previousCard() {
            if (this.currentCardIndex > 0) {
                this.currentCardIndex--;
                this.highlightCurrentCard();
                this.scrollToCurrentCard();
            }
        },

        toggleCurrentCard() {
            const currentCard = this.cards[this.currentCardIndex];
            if (currentCard) {
                this.toggleCard(currentCard);
            }
        },

        toggleCard(card) {
            card.classList.toggle('expanded');
            const toggleBtn = card.querySelector('.review-toggle-content');
            if (toggleBtn) {
                const isExpanded = card.classList.contains('expanded');
                toggleBtn.textContent = isExpanded ? '▴ Collapse' : '▾ Show HL7 Content';
            }
        },

        highlightCurrentCard() {
            this.cards.forEach((card, index) => {
                if (index === this.currentCardIndex) {
                    card.style.outline = '2px solid var(--signal)';
                    card.style.outlineOffset = '2px';
                } else {
                    card.style.outline = 'none';
                }
            });
        },

        scrollToCurrentCard() {
            const currentCard = this.cards[this.currentCardIndex];
            if (currentCard) {
                currentCard.scrollIntoView({ behavior: 'smooth', block: 'center' });
            }
        },

        quickReview(key) {
            const outcomeMap = {
                '1': 1,  // FixedConfigurationUpdated
                '2': 2,  // FixedManualCaseCreated
                '3': 3,  // FixedMappingAdded
                '4': 4,  // IgnoredDuplicateTest
                '5': 5,  // IgnoredNotNotifiable
                '6': 9,  // EscalatedTaskCreated
                '7': 10  // ReprocessedSuccessfully
            };

            const outcome = outcomeMap[key];
            if (!outcome) return;

            const currentCard = this.cards[this.currentCardIndex];
            if (!currentCard) return;

            const messageId = currentCard.dataset.messageId;
            if (!messageId) return;

            if (confirm(`Complete review with outcome ${this.getOutcomeName(outcome)}?`)) {
                this.completeReview(messageId, outcome);
            }
        },

        getOutcomeName(outcome) {
            const names = {
                1: 'Fixed - Configuration Updated',
                2: 'Fixed - Manual Case Created',
                3: 'Fixed - Mapping Added',
                4: 'Ignored - Duplicate Test',
                5: 'Ignored - Not Notifiable',
                9: 'Escalated - Task Created',
                10: 'Reprocessed Successfully'
            };
            return names[outcome] || 'Unknown';
        },

        async completeReview(messageId, outcome, notes = '') {
            try {
                const formData = new FormData();
                formData.append('messageId', messageId);
                formData.append('outcome', outcome);
                if (notes) formData.append('notes', notes);

                const response = await fetch(window.location.pathname + '?handler=CompleteReview', {
                    method: 'POST',
                    headers: {
                        'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]').value
                    },
                    body: formData
                });

                if (response.ok) {
                    window.location.reload();
                } else {
                    alert('Failed to complete review. Please try again.');
                }
            } catch (error) {
                console.error('Error completing review:', error);
                alert('An error occurred. Please try again.');
            }
        },

        createShortcutsHint() {
            const hint = document.createElement('div');
            hint.className = 'review-shortcuts-hint';
            hint.id = 'shortcutsHint';
            hint.style.display = 'none';
            hint.innerHTML = `
                <div style="margin-bottom: 0.5rem; font-weight: 600;">Keyboard Shortcuts</div>
                ${Object.entries(this.shortcuts).map(([key, desc]) =>
                    `<div><kbd>${key}</kbd> ${desc}</div>`
                ).join('')}
            `;
            document.body.appendChild(hint);
        },

        toggleShortcutsHelp() {
            const hint = document.getElementById('shortcutsHint');
            if (hint) {
                hint.style.display = hint.style.display === 'none' ? 'block' : 'none';
            }
        },

        toggleSelectAll() {
            const selectAllCheckbox = document.querySelector('#selectAll');
            if (selectAllCheckbox) {
                selectAllCheckbox.checked = !selectAllCheckbox.checked;
                this.selectAll(selectAllCheckbox.checked);
            }
        },

        selectAll(checked) {
            const checkboxes = document.querySelectorAll('.review-message-checkbox');
            checkboxes.forEach(checkbox => {
                checkbox.checked = checked;
            });
            this.updateBulkActionsState();
        },

        updateBulkActionsState() {
            const checkboxes = document.querySelectorAll('.review-message-checkbox');
            const checkedCount = Array.from(checkboxes).filter(cb => cb.checked).length;
            const bulkActions = document.querySelector('.review-bulk-action-btns');

            if (bulkActions) {
                bulkActions.style.display = checkedCount > 0 ? 'flex' : 'none';
            }

            const selectAllCheckbox = document.querySelector('#selectAll');
            if (selectAllCheckbox) {
                selectAllCheckbox.checked = checkedCount === checkboxes.length && checkedCount > 0;
                selectAllCheckbox.indeterminate = checkedCount > 0 && checkedCount < checkboxes.length;
            }
        },

        getSelectedMessageIds() {
            const checkboxes = document.querySelectorAll('.review-message-checkbox:checked');
            return Array.from(checkboxes).map(cb => cb.value);
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => ReviewQueue.init());
    } else {
        ReviewQueue.init();
    }

    // Expose to window for external access if needed
    window.ReviewQueue = ReviewQueue;
})();
