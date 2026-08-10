// Report Builder Notifications System
// Provides toast notifications and modal confirmations without blocking alert/confirm dialogs

console.log('[report-builder-notifications.js] Loading...');

window.ReportBuilderNotifications = {
    // Show a toast notification
    showToast: function(message, type = 'info', duration = 3000) {
        const icons = {
            'success': '✓',
            'error': '✕',
            'warning': '⚠',
            'info': 'ℹ'
        };

        const colors = {
            'success': '#10b981',
            'error': '#ef4444',
            'warning': '#f59e0b',
            'info': '#3b82f6'
        };

        const toast = document.createElement('div');
        toast.className = 'rb-toast';
        toast.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${colors[type] || colors.info};
            color: white;
            padding: 1rem 1.5rem;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
            z-index: 10000;
            display: flex;
            align-items: center;
            gap: 0.75rem;
            min-width: 250px;
            max-width: 400px;
            animation: slideInRight 0.3s ease-out;
            font-size: 14px;
        `;

        toast.innerHTML = `
            <span style="font-size: 18px; font-weight: bold;">${icons[type] || icons.info}</span>
            <span style="flex: 1;">${message}</span>
        `;

        document.body.appendChild(toast);

        // Auto-remove after duration
        setTimeout(() => {
            toast.style.animation = 'slideOutRight 0.3s ease-in';
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        }, duration);
    },

    // Show a confirmation modal (non-blocking)
    confirm: function(message, onConfirm, onCancel = null, options = {}) {
        const title = options.title || 'Confirm Action';
        const confirmText = options.confirmText || 'Continue';
        const cancelText = options.cancelText || 'Cancel';
        const dangerMode = options.danger || false;

        // Create overlay
        const overlay = document.createElement('div');
        overlay.className = 'rb-modal-overlay';
        overlay.style.cssText = `
            position: fixed;
            top: 0;
            left: 0;
            right: 0;
            bottom: 0;
            background: rgba(0, 0, 0, 0.5);
            z-index: 10001;
            display: flex;
            align-items: center;
            justify-content: center;
            animation: fadeIn 0.2s ease-out;
        `;

        // Create modal
        const modal = document.createElement('div');
        modal.className = 'rb-confirm-modal';
        modal.style.cssText = `
            background: white;
            border-radius: 12px;
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.2);
            max-width: 450px;
            width: 90%;
            animation: scaleIn 0.2s ease-out;
        `;

        const confirmButtonColor = dangerMode ? '#ef4444' : '#3b82f6';

        modal.innerHTML = `
            <div style="padding: 1.5rem;">
                <h3 style="margin: 0 0 1rem 0; font-size: 18px; font-weight: 600; color: #1f2937;">
                    ${title}
                </h3>
                <p style="margin: 0; color: #6b7280; font-size: 14px; line-height: 1.5;">
                    ${message}
                </p>
            </div>
            <div style="padding: 1rem 1.5rem; background: #f9fafb; border-top: 1px solid #e5e7eb; display: flex; gap: 0.75rem; justify-content: flex-end; border-radius: 0 0 12px 12px;">
                <button class="rb-modal-btn rb-modal-cancel" style="
                    padding: 0.5rem 1rem;
                    border: 1px solid #d1d5db;
                    background: white;
                    color: #374151;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 14px;
                    font-weight: 500;
                    transition: all 0.15s;
                ">${cancelText}</button>
                <button class="rb-modal-btn rb-modal-confirm" style="
                    padding: 0.5rem 1rem;
                    border: none;
                    background: ${confirmButtonColor};
                    color: white;
                    border-radius: 6px;
                    cursor: pointer;
                    font-size: 14px;
                    font-weight: 500;
                    transition: all 0.15s;
                ">${confirmText}</button>
            </div>
        `;

        overlay.appendChild(modal);
        document.body.appendChild(overlay);

        // Button hover effects
        const buttons = modal.querySelectorAll('.rb-modal-btn');
        buttons.forEach(btn => {
            btn.addEventListener('mouseenter', () => {
                btn.style.transform = 'translateY(-1px)';
                btn.style.boxShadow = '0 2px 8px rgba(0, 0, 0, 0.15)';
            });
            btn.addEventListener('mouseleave', () => {
                btn.style.transform = 'translateY(0)';
                btn.style.boxShadow = 'none';
            });
        });

        // Handle confirm
        const confirmBtn = modal.querySelector('.rb-modal-confirm');
        confirmBtn.addEventListener('click', () => {
            overlay.style.animation = 'fadeOut 0.2s ease-in';
            setTimeout(() => {
                if (overlay.parentNode) {
                    overlay.parentNode.removeChild(overlay);
                }
            }, 200);
            if (onConfirm) onConfirm();
        });

        // Handle cancel
        const cancelBtn = modal.querySelector('.rb-modal-cancel');
        const handleCancel = () => {
            overlay.style.animation = 'fadeOut 0.2s ease-in';
            setTimeout(() => {
                if (overlay.parentNode) {
                    overlay.parentNode.removeChild(overlay);
                }
            }, 200);
            if (onCancel) onCancel();
        };

        cancelBtn.addEventListener('click', handleCancel);
        overlay.addEventListener('click', (e) => {
            if (e.target === overlay) {
                handleCancel();
            }
        });

        // ESC key to cancel
        const escHandler = (e) => {
            if (e.key === 'Escape') {
                handleCancel();
                document.removeEventListener('keydown', escHandler);
            }
        };
        document.addEventListener('keydown', escHandler);
    }
};

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }

    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }

    @keyframes fadeIn {
        from { opacity: 0; }
        to { opacity: 1; }
    }

    @keyframes fadeOut {
        from { opacity: 1; }
        to { opacity: 0; }
    }

    @keyframes scaleIn {
        from {
            transform: scale(0.9);
            opacity: 0;
        }
        to {
            transform: scale(1);
            opacity: 1;
        }
    }
`;
document.head.appendChild(style);

console.log('[report-builder-notifications.js] Loaded successfully');
