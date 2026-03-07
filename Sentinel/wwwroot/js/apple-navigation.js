// Apple Design System - Sidebar Navigation

(function() {
    'use strict';
    
    // Initialize sidebar functionality
    function initAppleSidebar() {
        const sidebar = document.getElementById('appleSidebar');
        const overlay = document.getElementById('appleOverlay');
        const toggleButtons = document.querySelectorAll('[data-apple-sidebar-toggle]');
        
        if (!sidebar || !overlay) {            return;
        }
        
        // Toggle sidebar
        function toggleSidebar() {
            const isOpen = sidebar.classList.contains('open');
            if (isOpen) {
                closeSidebar();
            } else {
                openSidebar();
            }
        }
        
        // Open sidebar
        function openSidebar() {
            sidebar.classList.add('open');
            overlay.classList.add('show');
            document.body.style.overflow = 'hidden'; // Prevent scrolling when sidebar open
        }
        
        // Close sidebar
        function closeSidebar() {
            sidebar.classList.remove('open');
            overlay.classList.remove('show');
            document.body.style.overflow = ''; // Re-enable scrolling
        }
        
        // Attach event listeners
        toggleButtons.forEach(btn => {
            btn.addEventListener('click', toggleSidebar);
        });
        
        // Close sidebar when clicking overlay
        overlay.addEventListener('click', closeSidebar);
        
        // Close sidebar on escape key
        document.addEventListener('keydown', function(e) {
            if (e.key === 'Escape' && sidebar.classList.contains('open')) {
                closeSidebar();
            }
        });
        
        // Handle active link highlighting - improved to work with Razor Pages
        const currentPath = window.location.pathname.toLowerCase();
        const navLinks = sidebar.querySelectorAll('.sidebar-link');
        
        // Remove all active states first
        navLinks.forEach(link => link.classList.remove('active'));
        
        // Find the best matching link
        let bestMatch = null;
        let bestMatchLength = 0;
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (!href) return;
            
            const linkPath = href.toLowerCase();
            
            // Exact match or starts with the link path
            if (currentPath === linkPath || currentPath.startsWith(linkPath + '/')) {
                // Prefer longer matches (more specific routes)
                if (linkPath.length > bestMatchLength) {
                    bestMatch = link;
                    bestMatchLength = linkPath.length;
                }
            }
        });
        
        // Apply active class to best match
        if (bestMatch) {
            bestMatch.classList.add('active');
        }
        
        // Close sidebar after navigation (for mobile)
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 768) {
                    setTimeout(closeSidebar, 300);
                }
            });
        });
    }
    
    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initAppleSidebar);
    } else {
        initAppleSidebar();
    }
    
})();
