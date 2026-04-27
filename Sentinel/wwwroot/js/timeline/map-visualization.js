/**
 * Map Visualization - Displays location pins on a map using Leaflet
 */
class MapVisualization {
    constructor(containerId) {
        this.containerId = containerId;
        this.map = null;
        this.markers = [];
        this.markerLayer = null;
        
        this.init();
    }

    init() {
        const container = document.getElementById(this.containerId);
        if (!container) {
            console.error('Map container not found:', this.containerId);
            return;
        }

        // Initialize Leaflet map
        // Default center: Adelaide, Australia (can be customized)
        this.map = L.map(this.containerId).setView([-34.9285, 138.6007], 12);

        // Add OpenStreetMap tiles (free, no API key required)
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
            maxZoom: 19
        }).addTo(this.map);

        // Create a layer group for markers
        this.markerLayer = L.layerGroup().addTo(this.map);

        console.log('Map initialized');
    }

    addMarker(latitude, longitude, popupContent, iconColor = 'blue') {
        if (!this.map) {
            console.error('Map not initialized');
            return;
        }

        // Create custom icon (optional)
        const icon = L.divIcon({
            className: 'custom-marker',
            html: `<div style="background-color: ${iconColor}; width: 25px; height: 25px; border-radius: 50%; border: 3px solid white; box-shadow: 0 2px 6px rgba(0,0,0,0.3);"></div>`,
            iconSize: [25, 25],
            iconAnchor: [12, 12]
        });

        // Create marker
        const marker = L.marker([latitude, longitude], { icon })
            .bindPopup(popupContent);

        marker.addTo(this.markerLayer);
        this.markers.push(marker);

        // Auto-fit bounds if we have multiple markers
        if (this.markers.length > 1) {
            const group = L.featureGroup(this.markers);
            this.map.fitBounds(group.getBounds().pad(0.1));
        } else {
            // Single marker - center on it
            this.map.setView([latitude, longitude], 14);
        }

        return marker;
    }

    clearMarkers() {
        if (this.markerLayer) {
            this.markerLayer.clearLayers();
        }
        this.markers = [];
    }

    addMarkersFromEntities(entries) {
        this.clearMarkers();

        entries.forEach(entry => {
            if (!entry.entities) return;

            entry.entities
                .filter(entity => entity.entityType === 'Location' && entity.latitude && entity.longitude)
                .forEach(entity => {
                    const popupContent = `
                        <div style="font-family: var(--f-sans, sans-serif);">
                            <strong>${entity.linkedRecordDisplayName || entity.rawText}</strong><br>
                            <small class="text-muted">${new Date(entry.entryDate).toLocaleDateString()}</small>
                            ${entity.address ? `<br><small>${entity.address}</small>` : ''}
                        </div>
                    `;

                    this.addMarker(
                        entity.latitude,
                        entity.longitude,
                        popupContent,
                        this.getColorForConfidence(entity.confidence)
                    );
                });
        });
    }

    getColorForConfidence(confidence) {
        switch (confidence?.toLowerCase()) {
            case 'high':
                return '#28a745'; // Green
            case 'medium':
                return '#ffc107'; // Yellow
            case 'low':
                return '#dc3545'; // Red
            default:
                return '#6c757d'; // Gray
        }
    }

    centerOnLocation(latitude, longitude, zoom = 15) {
        if (this.map) {
            this.map.setView([latitude, longitude], zoom);
        }
    }

    fitAllMarkers() {
        if (this.markers.length === 0) return;

        const group = L.featureGroup(this.markers);
        this.map.fitBounds(group.getBounds().pad(0.1));
    }
}
