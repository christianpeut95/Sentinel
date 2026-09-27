/**
 * Sentinel geocoding configuration and Google Places (New) integration.
 *
 * The browser key is deliberately restricted to Maps JavaScript API and
 * Places API (New). Server-side geocoding uses the server-only API key.
 */
window.SentinelGeocoding = (function () {
    'use strict';

    let config = null;
    let googleMapsPromise = null;
    let placesLibrary = null;
    const predictionCache = new Map();
    const placesStatus = Object.freeze({ OK: 'OK', ERROR: 'ERROR' });

    function init(geocodingConfig) {
        config = {
            provider: (geocodingConfig.provider || 'nominatim').toLowerCase(),
            browserApiKey: geocodingConfig.browserApiKey || '',
            defaultCountry: geocodingConfig.defaultCountry || 'AU'
        };
    }

    function shouldUseGoogleMaps() {
        if (!config) {
            console.warn('Geocoding is not available.');
            return false;
        }

        return config.provider === 'google' && !!config.browserApiKey;
    }

    function reportGoogleMapsLoadFailure(error) {
        console.error('Google Places could not be loaded. Address entry remains available.', error);
    }

    function completeGoogleMapsLoad(resolve, reject) {
        Promise.resolve()
            .then(async function () {
                if (!window.google || !google.maps || !google.maps.importLibrary) {
                    throw new Error('Google Maps JavaScript API did not initialise.');
                }

                placesLibrary = await google.maps.importLibrary('places');
                resolve(true);
            })
            .catch(function (error) {
                googleMapsPromise = null;
                reject(error);
            });
    }

    function waitForExistingGoogleMapsScript(resolve, reject, attemptsRemaining) {
        if (window.google && google.maps && google.maps.importLibrary) {
            completeGoogleMapsLoad(resolve, reject);
            return;
        }

        if (attemptsRemaining <= 0) {
            googleMapsPromise = null;
            reject(new Error('The existing Google Maps script did not initialise.'));
            return;
        }

        window.setTimeout(function () {
            waitForExistingGoogleMapsScript(resolve, reject, attemptsRemaining - 1);
        }, 100);
    }

    /**
     * Loads the Maps JavaScript API and Places library once. Callbacks run only
     * after the supported Places (New) library is ready.
     */
    function loadGoogleMaps(callback) {
        if (!shouldUseGoogleMaps()) {
            return Promise.resolve(false);
        }

        if (!googleMapsPromise) {
            googleMapsPromise = new Promise(function (resolve, reject) {
                if (window.google && google.maps && google.maps.importLibrary) {
                    completeGoogleMapsLoad(resolve, reject);
                    return;
                }

                if (document.querySelector('script[src*="maps.googleapis.com/maps/api/js"]')) {
                    waitForExistingGoogleMapsScript(resolve, reject, 100);
                    return;
                }

                const callbackName = 'initSentinelGoogleMaps';
                window[callbackName] = function () {
                    delete window[callbackName];
                    completeGoogleMapsLoad(resolve, reject);
                };

                const script = document.createElement('script');
                script.src = 'https://maps.googleapis.com/maps/api/js?key=' +
                    encodeURIComponent(config.browserApiKey) +
                    '&v=weekly&loading=async&callback=' + callbackName;
                script.async = true;
                script.defer = true;
                script.onerror = function () {
                    delete window[callbackName];
                    googleMapsPromise = null;
                    reject(new Error('Google Maps JavaScript API could not be downloaded.'));
                };
                document.head.appendChild(script);
            });
        }

        if (callback) {
            googleMapsPromise.then(callback).catch(reportGoogleMapsLoadFailure);
        }

        return googleMapsPromise;
    }

    function requirePlacesLibrary() {
        if (!placesLibrary) {
            throw new Error('Google Places has not been loaded.');
        }

        return placesLibrary;
    }

    function createAutocompleteSession() {
        const library = requirePlacesLibrary();
        return new library.AutocompleteSessionToken();
    }

    /**
     * A small adapter used by Sentinel's custom dropdowns. It is intentionally
     * named for the current Autocomplete Data API rather than exposing any of
     * Google’s deprecated AutocompleteService surface.
     */
    function createAutocompleteDataClient() {
        const library = requirePlacesLibrary();

        return {
            getPredictions: function (request, callback) {
                const query = {
                    input: request.input,
                    sessionToken: request.sessionToken
                };

                const country = request.country || request.componentRestrictions?.country;
                if (country) {
                    query.includedRegionCodes = [String(country).toUpperCase()];
                }

                library.AutocompleteSuggestion.fetchAutocompleteSuggestions(query)
                    .then(function (response) {
                        const predictions = (response.suggestions || [])
                            .map(function (suggestion) { return suggestion.placePrediction; })
                            .filter(Boolean)
                            .map(function (prediction) {
                                const placeId = prediction.placeId;
                                if (placeId) {
                                    predictionCache.set(placeId, prediction);
                                }

                                return {
                                    placeId: placeId,
                                    description: prediction.text ? prediction.text.toString() : '',
                                    prediction: prediction
                                };
                            });

                        callback(predictions, placesStatus.OK);
                    })
                    .catch(function (error) {
                        console.error('Google Places suggestions could not be loaded.', error);
                        callback([], placesStatus.ERROR);
                    });
            }
        };
    }

    function normalisePlaceResult(place) {
        const location = place.location;
        const latitude = location ? (typeof location.lat === 'function' ? location.lat() : location.lat) : null;
        const longitude = location ? (typeof location.lng === 'function' ? location.lng() : location.lng) : null;

        return {
            name: place.displayName || '',
            formattedAddress: place.formattedAddress || '',
            addressComponents: (place.addressComponents || []).map(function (component) {
                return {
                    longName: component.longText || '',
                    shortName: component.shortText || '',
                    types: component.types || []
                };
            }),
            geometry: latitude === null || longitude === null ? null : {
                location: {
                    lat: function () { return latitude; },
                    lng: function () { return longitude; }
                }
            },
            types: place.types || [],
            businessStatus: place.businessStatus || null
        };
    }

    function createPlaceDetailsClient() {
        const library = requirePlacesLibrary();

        return {
            getDetails: function (request, callback) {
                const prediction = request.prediction || predictionCache.get(request.placeId);
                const place = prediction ? prediction.toPlace() : new library.Place({ id: request.placeId });

                place.fetchFields({
                    fields: ['displayName', 'formattedAddress', 'addressComponents', 'location', 'types', 'businessStatus']
                })
                    .then(function () {
                        callback(normalisePlaceResult(place), placesStatus.OK);
                    })
                    .catch(function (error) {
                        console.error('Google Place details could not be loaded.', error);
                        callback(null, placesStatus.ERROR);
                    });
            }
        };
    }

    function debounce(fn, delay) {
        let timeout;
        return function () {
            const args = arguments;
            window.clearTimeout(timeout);
            timeout = window.setTimeout(function () { fn.apply(null, args); }, delay);
        };
    }

    /**
     * Adds a keyboard-accessible Place Autocomplete (New) dropdown to an
     * existing Sentinel input and returns current Place data to the callback.
     */
    function attachPlaceAutocomplete(input, options, onPlaceSelected) {
        if (!input || !shouldUseGoogleMaps() || input.dataset.sentinelPlacesBound === 'true') {
            return Promise.resolve(false);
        }

        input.dataset.sentinelPlacesBound = 'true';

        return loadGoogleMaps().then(function (loaded) {
            if (!loaded) {
                return false;
            }

            const container = input.parentElement;
            if (!container) {
                return false;
            }

            const dropdown = document.createElement('div');
            dropdown.className = 'dropdown-menu w-100';
            dropdown.setAttribute('role', 'listbox');
            dropdown.setAttribute('aria-label', 'Place suggestions');
            if (window.getComputedStyle(container).position === 'static') {
                container.style.position = 'relative';
            }
            dropdown.style.position = 'absolute';
            dropdown.style.top = '100%';
            dropdown.style.left = '0';
            dropdown.style.zIndex = '1050';
            dropdown.style.maxHeight = '300px';
            dropdown.style.overflow = 'auto';
            container.appendChild(dropdown);

            input.setAttribute('autocomplete', 'off');
            input.setAttribute('aria-haspopup', 'listbox');
            input.setAttribute('aria-expanded', 'false');

            const suggestions = createAutocompleteDataClient();
            const details = createPlaceDetailsClient();
            let sessionToken = createAutocompleteSession();
            let currentPredictions = [];
            let optionElements = [];
            let focusedIndex = -1;

            function clearDropdown() {
                dropdown.replaceChildren();
                dropdown.classList.remove('show');
                input.setAttribute('aria-expanded', 'false');
                currentPredictions = [];
                optionElements = [];
                focusedIndex = -1;
            }

            function focusOption(index) {
                if (focusedIndex >= 0 && optionElements[focusedIndex]) {
                    optionElements[focusedIndex].classList.remove('active');
                }

                focusedIndex = index;
                if (focusedIndex >= 0 && optionElements[focusedIndex]) {
                    optionElements[focusedIndex].classList.add('active');
                    optionElements[focusedIndex].scrollIntoView({ block: 'nearest' });
                }
            }

            function selectPrediction(index) {
                const prediction = currentPredictions[index];
                if (!prediction) {
                    return;
                }

                details.getDetails({ placeId: prediction.placeId, prediction: prediction.prediction }, function (place, status) {
                    if (status !== placesStatus.OK || !place) {
                        clearDropdown();
                        return;
                    }

                    clearDropdown();
                    sessionToken = createAutocompleteSession();
                    onPlaceSelected?.(place, prediction);
                });
            }

            function renderPredictions(predictions) {
                dropdown.replaceChildren();
                currentPredictions = predictions;
                optionElements = [];
                focusedIndex = -1;

                predictions.slice(0, 8).forEach(function (prediction, index) {
                    const option = document.createElement('button');
                    option.type = 'button';
                    option.className = 'dropdown-item';
                    option.setAttribute('role', 'option');
                    option.textContent = prediction.description;
                    option.addEventListener('click', function () { selectPrediction(index); });
                    dropdown.appendChild(option);
                    optionElements.push(option);
                });

                if (optionElements.length) {
                    dropdown.classList.add('show');
                    input.setAttribute('aria-expanded', 'true');
                } else {
                    clearDropdown();
                }
            }

            input.addEventListener('input', debounce(function () {
                const query = input.value.trim();
                if (!query) {
                    clearDropdown();
                    return;
                }

                suggestions.getPredictions({
                    input: query,
                    country: options?.country || config.defaultCountry,
                    sessionToken: sessionToken
                }, function (predictions, status) {
                    if (status !== placesStatus.OK) {
                        clearDropdown();
                        return;
                    }

                    renderPredictions(predictions);
                });
            }, 300));

            input.addEventListener('keydown', function (event) {
                if (!dropdown.classList.contains('show')) {
                    return;
                }

                if (event.key === 'ArrowDown') {
                    event.preventDefault();
                    focusOption(Math.min(optionElements.length - 1, focusedIndex + 1));
                } else if (event.key === 'ArrowUp') {
                    event.preventDefault();
                    focusOption(Math.max(0, focusedIndex - 1));
                } else if (event.key === 'Enter' && focusedIndex >= 0) {
                    event.preventDefault();
                    selectPrediction(focusedIndex);
                } else if (event.key === 'Escape') {
                    clearDropdown();
                }
            });

            document.addEventListener('click', function (event) {
                if (event.target !== input && !dropdown.contains(event.target)) {
                    clearDropdown();
                }
            });

            input.addEventListener('blur', function () {
                window.setTimeout(function () {
                    if (!dropdown.contains(document.activeElement)) {
                        clearDropdown();
                    }
                }, 150);
            });

            return true;
        }).catch(function (error) {
            input.dataset.sentinelPlacesBound = 'false';
            reportGoogleMapsLoadFailure(error);
            return false;
        });
    }

    function initAddressAutocomplete(inputId, onPlaceSelected) {
        const input = document.getElementById(inputId);
        if (!input) {
            console.error('Geocoding input was not found.');
            return;
        }

        if (!shouldUseGoogleMaps()) {
            input.placeholder = 'Enter address manually (autocomplete requires Google Maps)';
            input.disabled = false;
            return;
        }

        void attachPlaceAutocomplete(input, { country: config.defaultCountry }, onPlaceSelected);
    }

    function initPlaceSearch(inputId, onPlaceSelected) {
        const input = document.getElementById(inputId);
        if (!input) {
            console.error('Geocoding input was not found.');
            return;
        }

        if (!shouldUseGoogleMaps()) {
            input.placeholder = 'Place search requires Google Maps (currently using ' + (config?.provider || 'Nominatim') + ')';
            input.disabled = true;
            return;
        }

        void attachPlaceAutocomplete(input, { country: config.defaultCountry }, onPlaceSelected);
    }

    return {
        init: init,
        loadGoogleMaps: loadGoogleMaps,
        initAddressAutocomplete: initAddressAutocomplete,
        initPlaceSearch: initPlaceSearch,
        attachPlaceAutocomplete: attachPlaceAutocomplete,
        createAutocompleteDataClient: createAutocompleteDataClient,
        createPlaceDetailsClient: createPlaceDetailsClient,
        createAutocompleteSession: createAutocompleteSession,
        getConfig: function () { return config; },
        placesStatus: placesStatus,
        shouldUseGoogleMaps: shouldUseGoogleMaps
    };
})();
