/**
 * Sentinel Feedback Widget
 * Privacy-safe feedback submission with optional diagnostics
 */

(function() {
    'use strict';

    const FeedbackWidget = {
        isOpen: false,

        init() {
            this.createFloatingButton();
            this.createModal();
            this.attachEventListeners();
        },

        createFloatingButton() {
            const button = document.createElement('button');
            button.id = 'sentinel-feedback-button';
            button.className = 'sentinel-feedback-btn';
            button.innerHTML = '<i class="bi bi-chat-dots"></i>';
            button.title = 'Send Feedback';
            button.setAttribute('aria-label', 'Send Feedback');
            document.body.appendChild(button);
        },

        createModal() {
            const modal = document.createElement('div');
            modal.id = 'sentinel-feedback-modal';
            modal.className = 'sentinel-feedback-modal';
            modal.innerHTML = `
                <div class="sentinel-feedback-backdrop"></div>
                <div class="sentinel-feedback-dialog">
                    <div class="sentinel-feedback-header">
                        <h3>Send Feedback</h3>
                        <button class="sentinel-feedback-close" aria-label="Close">&times;</button>
                    </div>
                    <div class="sentinel-feedback-body">
                        <form id="sentinel-feedback-form">
                            <div class="form-group">
                                <label for="feedback-type">Feedback Type <span class="text-danger">*</span></label>
                                <select id="feedback-type" class="form-control" required>
                                    <option value="">Select type...</option>
                                    <option value="Bug">🐛 Bug - Something isn't working</option>
                                    <option value="FeatureRequest">✨ Feature Request - Suggest new functionality</option>
                                    <option value="Confusing">🤔 Confusing - UI/UX is unclear</option>
                                    <option value="General">💬 General - Other feedback</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label for="feedback-summary">
                                    Summary <span class="text-danger">*</span>
                                    <span class="char-count" id="summary-count">0/200</span>
                                </label>
                                <input 
                                    type="text" 
                                    id="feedback-summary" 
                                    class="form-control" 
                                    placeholder="Brief description of the issue or suggestion"
                                    minlength="3"
                                    maxlength="200"
                                    required
                                />
                            </div>

                            <div class="form-group">
                                <label for="feedback-description">
                                    Description <span class="text-danger">*</span>
                                    <span class="char-count" id="description-count">0/5000</span>
                                </label>
                                <textarea 
                                    id="feedback-description" 
                                    class="form-control" 
                                    rows="5"
                                    placeholder="Provide detailed information about your feedback"
                                    minlength="3"
                                    maxlength="5000"
                                    required
                                ></textarea>
                            </div>

                            <div class="form-group">
                                <label for="feedback-expected">
                                    What did you expect to happen? <span class="text-muted">(optional)</span>
                                </label>
                                <textarea 
                                    id="feedback-expected" 
                                    class="form-control" 
                                    rows="2"
                                    placeholder="Describe the expected behavior"
                                    maxlength="5000"
                                ></textarea>
                            </div>

                            <div class="form-group">
                                <label for="feedback-reproducibility">
                                    How often does this happen? <span class="text-muted">(optional)</span>
                                </label>
                                <select id="feedback-reproducibility" class="form-control">
                                    <option value="">Select frequency...</option>
                                    <option value="Every time">Every time</option>
                                    <option value="Often">Often</option>
                                    <option value="Sometimes">Sometimes</option>
                                    <option value="Rarely">Rarely</option>
                                    <option value="Once">Just once</option>
                                </select>
                            </div>

                            <div class="form-group">
                                <label for="feedback-email">
                                    Your email <span class="text-muted">(optional, for follow-up)</span>
                                </label>
                                <input 
                                    type="email" 
                                    id="feedback-email" 
                                    class="form-control" 
                                    placeholder="you@example.com"
                                    maxlength="320"
                                />
                            </div>

                            <div class="form-group">
                                <div class="custom-control custom-checkbox">
                                    <input 
                                        type="checkbox" 
                                        class="custom-control-input" 
                                        id="feedback-diagnostics" 
                                        checked
                                    />
                                    <label class="custom-control-label" for="feedback-diagnostics">
                                        Include technical diagnostics
                                        <button type="button" class="btn btn-link btn-sm p-0 ml-1" id="diagnostics-info">
                                            <i class="bi bi-info-circle"></i>
                                        </button>
                                    </label>
                                </div>
                                <small class="form-text text-muted privacy-note">
                                    <i class="bi bi-shield-check text-success"></i>
                                    Diagnostic data never includes patient information
                                </small>
                            </div>

                            <div id="diagnostics-details" class="alert alert-info" style="display:none;">
                                <strong>What's included in diagnostics?</strong>
                                <ul class="mb-0 mt-2">
                                    <li>Sentinel version and installation ID</li>
                                    <li>Operating system and browser info</li>
                                    <li>Current page route</li>
                                    <li>Recent error counts (not messages)</li>
                                    <li>Your role (not name or email)</li>
                                </ul>
                                <strong class="text-danger">Never included:</strong>
                                <ul class="mb-0">
                                    <li>Patient names, IDs, or clinical data</li>
                                    <li>Credentials or sensitive information</li>
                                </ul>
                            </div>

                            <div id="feedback-alert" class="alert" style="display:none;"></div>

                            <div class="sentinel-feedback-actions">
                                <button type="button" class="btn btn-secondary" id="feedback-cancel">Cancel</button>
                                <button type="submit" class="btn btn-primary" id="feedback-submit">
                                    <span class="submit-text">Submit Feedback</span>
                                    <span class="submit-spinner" style="display:none;">
                                        <span class="spinner-border spinner-border-sm mr-1"></span>
                                        Submitting...
                                    </span>
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            `;
            document.body.appendChild(modal);
        },

        attachEventListeners() {
            // Open modal
            document.getElementById('sentinel-feedback-button')?.addEventListener('click', () => {
                this.openModal();
            });

            // Close modal
            document.querySelector('.sentinel-feedback-close')?.addEventListener('click', () => {
                this.closeModal();
            });
            document.getElementById('feedback-cancel')?.addEventListener('click', () => {
                this.closeModal();
            });
            document.querySelector('.sentinel-feedback-backdrop')?.addEventListener('click', () => {
                this.closeModal();
            });

            // Escape key to close
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && this.isOpen) {
                    this.closeModal();
                }
            });

            // Character counters
            document.getElementById('feedback-summary')?.addEventListener('input', (e) => {
                const count = e.target.value.length;
                document.getElementById('summary-count').textContent = `${count}/200`;
            });

            document.getElementById('feedback-description')?.addEventListener('input', (e) => {
                const count = e.target.value.length;
                document.getElementById('description-count').textContent = `${count}/5000`;
            });

            // Diagnostics info toggle
            document.getElementById('diagnostics-info')?.addEventListener('click', (e) => {
                e.preventDefault();
                const details = document.getElementById('diagnostics-details');
                details.style.display = details.style.display === 'none' ? 'block' : 'none';
            });

            // Form submission
            document.getElementById('sentinel-feedback-form')?.addEventListener('submit', (e) => {
                e.preventDefault();
                this.submitFeedback();
            });
        },

        openModal() {
            const modal = document.getElementById('sentinel-feedback-modal');
            modal.classList.add('show');
            this.isOpen = true;
            document.body.style.overflow = 'hidden';

            // Focus first input
            setTimeout(() => {
                document.getElementById('feedback-type')?.focus();
            }, 100);
        },

        closeModal() {
            const modal = document.getElementById('sentinel-feedback-modal');
            modal.classList.remove('show');
            this.isOpen = false;
            document.body.style.overflow = '';
            this.resetForm();
        },

        resetForm() {
            document.getElementById('sentinel-feedback-form')?.reset();
            document.getElementById('summary-count').textContent = '0/200';
            document.getElementById('description-count').textContent = '0/5000';
            document.getElementById('diagnostics-details').style.display = 'none';
            this.hideAlert();
        },

        showAlert(message, type = 'danger') {
            const alert = document.getElementById('feedback-alert');
            alert.className = `alert alert-${type}`;
            alert.textContent = message;
            alert.style.display = 'block';
        },

        hideAlert() {
            const alert = document.getElementById('feedback-alert');
            alert.style.display = 'none';
        },

        async submitFeedback() {
            this.hideAlert();

            const submitBtn = document.getElementById('feedback-submit');
            submitBtn.disabled = true;
            submitBtn.querySelector('.submit-text').style.display = 'none';
            submitBtn.querySelector('.submit-spinner').style.display = 'inline';

            try {
                // Gather form data
                const payload = {
                    type: document.getElementById('feedback-type').value,
                    summary: document.getElementById('feedback-summary').value.trim(),
                    description: document.getElementById('feedback-description').value.trim(),
                    expectedBehaviour: document.getElementById('feedback-expected').value.trim() || null,
                    reporterEmail: document.getElementById('feedback-email').value.trim() || null,
                    reproducibility: document.getElementById('feedback-reproducibility').value || null,
                    includeDiagnostics: document.getElementById('feedback-diagnostics').checked,
                    pageUrl: window.location.pathname,
                    clientInfo: {
                        browserLanguage: navigator.language,
                        timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                        viewportWidth: window.innerWidth,
                        viewportHeight: window.innerHeight,
                        devicePixelRatio: window.devicePixelRatio
                    }
                };

                // Submit to API
                const response = await fetch('/api/feedback/submit', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(payload)
                });

                const result = await response.json();

                if (result.success) {
                    this.showAlert('Thank you for your feedback! Your submission has been received.', 'success');
                    setTimeout(() => {
                        this.closeModal();
                    }, 2000);
                } else {
                    this.showAlert(result.message || 'Failed to submit feedback. Please try again.', 'danger');
                }
            } catch (error) {
                console.error('Feedback submission error:', error);
                this.showAlert('An error occurred. Please check your connection and try again.', 'danger');
            } finally {
                submitBtn.disabled = false;
                submitBtn.querySelector('.submit-text').style.display = 'inline';
                submitBtn.querySelector('.submit-spinner').style.display = 'none';
            }
        }
    };

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => FeedbackWidget.init());
    } else {
        FeedbackWidget.init();
    }
})();
