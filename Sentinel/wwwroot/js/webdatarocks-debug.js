/**
 * WebDataRocks Debug Helper
 * Add this script to diagnose modal positioning issues
 */
(function() {
    console.log('[WDR Debug] Starting WebDataRocks element monitor');

    // Log all WebDataRocks elements and their computed styles
    function debugWebDataRocksElements() {
        const wdrElements = document.querySelectorAll('[class*="wdr"]');

        if (wdrElements.length > 0) {
            console.group('[WDR Debug] Found ' + wdrElements.length + ' WebDataRocks elements');

            wdrElements.forEach((element, index) => {
                const computed = window.getComputedStyle(element);
                const className = element.className || '';

                // Only log interesting elements (dialogs, popups, overlays, etc.)
                if (className.includes('dialog') || className.includes('popup') || 
                    className.includes('overlay') || className.includes('window') ||
                    className.includes('modal') || className.includes('configurator')) {

                    console.group(`Element ${index}: ${className}`);
                    console.log('Position:', computed.position);
                    console.log('Top:', computed.top);
                    console.log('Left:', computed.left);
                    console.log('Width:', computed.width);
                    console.log('Height:', computed.height);
                    console.log('Z-Index:', computed.zIndex);
                    console.log('Transform:', computed.transform);
                    console.log('Display:', computed.display);
                    console.log('Visibility:', computed.visibility);
                    console.log('Inline Style:', element.getAttribute('style') || 'none');
                    console.log('Bounding Rect:', element.getBoundingClientRect());
                    console.groupEnd();
                }
            });

            console.groupEnd();
        }
    }

    // Monitor for new elements
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            mutation.addedNodes.forEach((node) => {
                if (node.nodeType === 1 && node.className && 
                    typeof node.className === 'string' && node.className.includes('wdr')) {
                    console.log('[WDR Debug] New WebDataRocks element added:', node.className);

                    setTimeout(() => {
                        const computed = window.getComputedStyle(node);
                        console.log('[WDR Debug] Element styles:', {
                            className: node.className,
                            position: computed.position,
                            top: computed.top,
                            left: computed.left,
                            width: computed.width,
                            height: computed.height,
                            zIndex: computed.zIndex,
                            transform: computed.transform,
                            inlineStyle: node.getAttribute('style')
                        });
                    }, 10);
                }
            });
        });
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });

    // Log on demand
    window.debugWDR = debugWebDataRocksElements;

    console.log('[WDR Debug] Monitor active. Call window.debugWDR() to log all elements.');
})();
