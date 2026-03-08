/**
 * Sentinel Geocoding Configuration
 * Centralized Google Maps API loader - checks provider before loading
 * Usage: Call SentinelGeocoding.initAddressAutocomplete() in your page
 */

window.SentinelGeocoding = (function() {
    'use strict';
    
    let config = null;
    let isGoogleMapsLoaded = false;
    let googleMapsCallbacks = [];
    
    /**
     * Initialize geocoding configuration
     * Call this once at app startup (in _Layout.cshtml)
     */
    function init(geocodingConfig) {
        config = {
            provider: (geocodingConfig.provider || 'nominatim').toLowerCase(),
            apiKey: geocodingConfig.apiKey || '',
            email: geocodingConfig.email || '',
            defaultCountry: geocodingConfig.defaultCountry || 'AU'
        };
        
        console.log('Sentinel Geocoding initialized:', {
            provider: config.provider,
            hasApiKey: !!config.apiKey,
            hasEmail: !!config.email
        });
    }
    
    /**
     * Check if Google Maps should be used
     */
    function shouldUseGoogleMaps() {
        if (!config) {
            console.warn('Geocoding not initialized. Call SentinelGeocoding.init() first.');
            return false;
        }
        
        if (config.provider !== 'google') {
            console.log('Geocoding provider is', config.provider, '- Google Maps not needed');
            return false;
        }
        
        if (!config.apiKey) {
            console.warn('Google provider selected but no API key configured');
            return false;
        }
        
        return true;
    }
    
    /**
     * Load Google Maps API if needed
     */
    function loadGoogleMaps(callback) {
        if (!shouldUseGoogleMaps()) {
            console.log('Skipping Google Maps API load - using', config?.provider || 'nominatim');
            return;
        }
        
        // Already loaded
        if (isGoogleMapsLoaded || (typeof google !== 'undefined' && google.maps && google.maps.places)) {
            if (callback) callback();
            return;
        }
        
        // Add callback to queue
        if (callback) {
            googleMapsCallbacks.push(callback);
        }
        
        // Already loading
        if (document.querySelector('script[src*="maps.googleapis.com"]')) {
            return;
        }
        
        // Load script
        console.log('Loading Google Maps API...');
        const script = document.createElement('script');
        const callbackName = 'initSentinelGoogleMaps';
        
        window[callbackName] = function() {
            isGoogleMapsLoaded = true;
            console.log('Google Maps API loaded successfully');
            
            // Execute all queued callbacks
            googleMapsCallbacks.forEach(cb => cb());
            googleMapsCallbacks = [];
            
            delete window[callbackName];
        };
        
        script.src = `https://maps.googleapis.com/maps/api/js?key=${encodeURIComponent(config.apiKey)}&libraries=places&loading=async&callback=${callbackName}`;
        script.async = true;
        script.defer = true;
        script.onerror = function() {
            console.error('Failed to load Google Maps API');
            isGoogleMapsLoaded = false;
        };
        
        document.head.appendChild(script);
    }
    
    /**
     * Initialize address autocomplete on an input field
     * With Nominatim: Allows manual entry (no autocomplete dropdown)
     * With Google: Enables autocomplete dropdown + manual entry
     */
    function initAddressAutocomplete(inputId, onPlaceSelected) {
        const input = document.getElementById(inputId);
        if (!input) {
            console.error('Input element not found:', inputId);
            return;
        }
        
        if (!shouldUseGoogleMaps()) {
            // Nominatim mode: Allow manual entry, no autocomplete
            console.log('Address field enabled for manual entry (Nominatim provider - no autocomplete)');
            input.placeholder = 'Enter address manually (autocomplete requires Google Maps)';
            input.disabled = false;
            
            // You can add a helper text or icon to indicate manual entry
            const helpText = document.createElement('small');
            helpText.className = 'form-text text-muted';
            helpText.innerHTML = '<i class="bi bi-info-circle"></i> Address autocomplete disabled (using Nominatim). Enter address manually.';
            
            // Insert after input if not already present
            if (!input.nextElementSibling || !input.nextElementSibling.classList.contains('form-text')) {
                input.parentNode.insertBefore(helpText, input.nextSibling);
            }
            return;
        }
        
        // Google Maps mode: Enable autocomplete
        loadGoogleMaps(function() {
            const autocomplete = new google.maps.places.Autocomplete(input, {
                types: ['address'],
                componentRestrictions: { country: config.defaultCountry.toLowerCase() }
            });
            
            if (onPlaceSelected) {
                autocomplete.addListener('place_changed', function() {
                    const place = autocomplete.getPlace();
                    onPlaceSelected(place);
                });
            }
            
            console.log('Address autocomplete (Google) initialized on', inputId);
        });
    }
    
    /**
     * Initialize place search (for businesses/locations)
     * This REQUIRES Google Maps - Nominatim doesn't support place/business search
     */
    function initPlaceSearch(inputId, onPlaceSelected) {
        const input = document.getElementById(inputId);
        if (!input) {
            console.error('Input element not found:', inputId);
            return;
        }
        
        if (!shouldUseGoogleMaps()) {
            console.log('Place search disabled - requires Google Maps provider (currently using', config?.provider || 'nominatim', ')');
            input.placeholder = 'Place search requires Google Maps (currently using ' + (config?.provider || 'Nominatim') + ')';
            input.disabled = true;
            
            // Add warning badge
            const badge = document.createElement('span');
            badge.className = 'badge bg-warning text-dark ms-2';
            badge.innerHTML = '<i class="bi bi-exclamation-triangle"></i> Requires Google Maps';
            const label = input.parentElement.querySelector('label');
            if (label && !label.querySelector('.badge')) {
                label.appendChild(badge);
            }
            return;
        }
        
        // Google Maps available - enable place search
        loadGoogleMaps(function() {
            const autocomplete = new google.maps.places.Autocomplete(input, {
                types: ['establishment'],
                componentRestrictions: { country: config.defaultCountry.toLowerCase() }
            });
            
            if (onPlaceSelected) {
                autocomplete.addListener('place_changed', function() {
                    const place = autocomplete.getPlace();
                    onPlaceSelected(place);
                });
            }
            
            console.log('Place search (Google) initialized on', inputId);
        });
    }
    
    /**
     * Get current configuration
     */
    function getConfig() {
        return config;
    }
    
    // Public API
    return {
        init: init,
        loadGoogleMaps: loadGoogleMaps,
        initAddressAutocomplete: initAddressAutocomplete,
        initPlaceSearch: initPlaceSearch,
        shouldUseGoogleMaps: shouldUseGoogleMaps,
        getConfig: getConfig
    };
})();
