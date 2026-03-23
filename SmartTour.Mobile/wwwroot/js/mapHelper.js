window.mapHelper = {
    map: null,
    markers: {},
    userMarker: null,
    dotNetRef: null,
    _pois: [],
    _lastClosestId: 0,
    _lastSelectedId: 0,
    clusterer: null,

    _loadGoogleMapsScript: function (apiKey, callback) {
        let scriptsLoaded = 0;
        const checkDone = () => {
            scriptsLoaded++;
            if (scriptsLoaded === 2) callback();
        };

        // Load MarkerClusterer
        if (!document.getElementById('markerclusterer-script')) {
            const ms = document.createElement('script');
            ms.id = 'markerclusterer-script';
            ms.src = 'https://unpkg.com/@googlemaps/markerclusterer/dist/index.min.js';
            ms.onload = checkDone;
            document.head.appendChild(ms);
        } else {
            checkDone();
        }

        // Load Google Maps API
        if (window.google && window.google.maps) {
            checkDone();
        } else if (!document.getElementById('google-maps-script')) {
            const script = document.createElement('script');
            script.id = 'google-maps-script';
            script.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places,marker&callback=gmCallback`;
            script.async = true;
            window.gmCallback = checkDone;
            document.head.appendChild(script);
        }
    },

    _createClusterRenderer: function () {
        return {
            render: function (cluster, stats) {
                var count = cluster.count;
                if (count === 1) {
                    return cluster.markers[0];
                }
                var position = cluster.position;
                var color, size;

                if (count < 10)       { color = '#51bbd6'; size = 28; }
                else if (count < 50)  { color = '#f1f075'; size = 36; }
                else                  { color = '#f28cb1'; size = 44; }

                var half = size / 2;
                var svg = window.btoa(
                    '<svg xmlns="http://www.w3.org/2000/svg" width="' + size + '" height="' + size + '" viewBox="0 0 ' + size + ' ' + size + '">' +
                    '<circle cx="' + half + '" cy="' + half + '" r="' + (half - 1.5) + '" fill="' + color + '" stroke="#fff" stroke-width="3"/>' +
                    '<text x="50%" y="' + (half + 1) + '" text-anchor="middle" dominant-baseline="central" ' +
                    'font-family="Arial,sans-serif" font-weight="bold" font-size="13px" fill="#000">' +
                    count + '</text></svg>'
                );

                var clusterMarker = new google.maps.Marker({
                    position: position,
                    icon: {
                        url: 'data:image/svg+xml;base64,' + svg,
                        scaledSize: new google.maps.Size(size, size),
                        anchor: new google.maps.Point(half, half)
                    },
                    zIndex: 1000 + count
                });

                clusterMarker.addListener('click', function () {
                    var map = clusterMarker.getMap();
                    if (map) { 
                        if (cluster.bounds) map.fitBounds(cluster.bounds, { padding: 50 });
                        else { map.setZoom(map.getZoom() + 2); map.panTo(position); }
                    }
                });

                return clusterMarker;
            }
        };
    },

    initMap: function (token, lat, lng, zoom, dotNetRef) {
        this.dotNetRef = dotNetRef;

        return new Promise((resolve) => {
            this._loadGoogleMapsScript(token, () => {
                const container = document.getElementById('map');
                if (!container) { resolve(false); return; }

                if (this.map) {
                    if (this.clusterer) this.clusterer.clearMarkers();
                    google.maps.event.clearInstanceListeners(this.map);
                }

                this.map = new google.maps.Map(container, {
                    center: { lat: lat, lng: lng },
                    zoom: zoom,
                    mapTypeControl: false,
                    streetViewControl: false,
                    fullscreenControl: false,
                    gestureHandling: 'greedy',
                    zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_TOP },
                    styles: [
                        { featureType: "poi", elementType: "labels", stylers: [{ visibility: "off" }] }
                    ]
                });

                if (this.userMarker) { this.userMarker.setMap(null); this.userMarker = null; }
                Object.values(this.markers).forEach(m => m.setMap(null));
                this.markers = {};
                this._pois = [];
                this.clusterer = null;

                this.map.addListener('zoom_changed', () => {
                    const z = this.map.getZoom();
                    if (this._pois && this._pois.length > 0) {
                        this._renderPOIs(z);
                    }
                    if (this.userMarker) {
                        this._renderUserLocationSize(z);
                    }
                });

                console.log('[mapHelper] Google Map loaded with Clusterer');
                resolve(true);
            });
        });
    },

    updateUserLocation: function (lat, lng) {
        if (!this.map) return;

        if (this.userMarker) {
            this.userMarker.setPosition({ lat, lng });
            return;
        }

        this.userMarker = new google.maps.Marker({
            position: { lat, lng },
            map: this.map,
            zIndex: 999
        });
        
        this._renderUserLocationSize(this.map.getZoom());
    },

    _renderUserLocationSize: function(zoomLevel) {
        if (!this.userMarker) return;

        let baseScale = 1.0;
        if (zoomLevel <= 11) baseScale = 0.5;
        else if (zoomLevel <= 13) baseScale = 0.7;
        else if (zoomLevel <= 15) baseScale = 0.9;
        else if (zoomLevel >= 18) baseScale = 1.2;

        let size = Math.round(80 * baseScale);
        if (size < 40) size = 40; 

        const svgData = window.btoa(
            '<svg xmlns="http://www.w3.org/2000/svg" width="' + size + '" height="' + size + '" viewBox="0 0 100 100">' +
            '<circle cx="50" cy="50" r="10" fill="#00C853" opacity="0">' +
            '<animate attributeName="r" values="10;45" keyTimes="0;1" dur="2s" begin="0s" repeatCount="indefinite" />' +
            '<animate attributeName="opacity" values="0;0.5;0" keyTimes="0;0.1;1" dur="2s" begin="0s" repeatCount="indefinite" />' +
            '</circle>' +
            '<circle cx="50" cy="50" r="12" fill="#fff" />' +
            '<circle cx="50" cy="50" r="9" fill="#00C853" />' +
            '</svg>'
        );

        this.userMarker.setIcon({
            url: 'data:image/svg+xml;base64,' + svgData,
            scaledSize: new google.maps.Size(size, size),
            anchor: new google.maps.Point(size / 2, size / 2)
        });
    },

    fitBoundsToPOIs: function () {
        if (!this.map || !this._pois || this._pois.length === 0) return;
        const bounds = new google.maps.LatLngBounds();
        this._pois.forEach(p => {
            bounds.extend(new google.maps.LatLng(p.latitude, p.longitude));
        });
        this.map.fitBounds(bounds, { padding: 50 });
    },

    updateMarkers: function (pois, closestId, selectedId) {
        if (!this.map) return;
        this._pois = pois || [];
        this._lastClosestId = closestId;
        this._lastSelectedId = selectedId;

        const newIds = new Set(this._pois.map(p => p.id));

        // Remove stale markers from MAP and CLUSTERER
        Object.keys(this.markers).forEach(id => {
            const numId = parseInt(id);
            if (!newIds.has(numId)) {
                if (this.clusterer) this.clusterer.removeMarker(this.markers[id]);
                this.markers[id].setMap(null);
                delete this.markers[id];
            }
        });

        this._renderPOIs(this.map.getZoom());
    },

    _renderPOIs: function(zoomLevel) {
        if (!this._pois || this._pois.length === 0) return;

        // Base Scale (nhỏ hơn, hợp với màn mobile)
        let baseScale = 1.0;
        if (zoomLevel <= 11) baseScale = 0.5;
        else if (zoomLevel <= 13) baseScale = 0.7;
        else if (zoomLevel <= 15) baseScale = 0.9;
        else if (zoomLevel >= 18) baseScale = 1.2;

        const markersArray = [];

        this._pois.forEach(poi => {
            const isClosest = poi.id === this._lastClosestId;
            const isSelected = poi.id === this._lastSelectedId;

            // SIZE ĐÃ ĐƯỢC THU NHỎ đáng kể so với trước đó (từ 28/40/48 xuống 20/30/36)
            let baseSize = isClosest ? 36 : isSelected ? 30 : 20;
            let size = Math.round(baseSize * baseScale);
            if (size < 12) size = 12;

            const color = isClosest ? '#FF5722' : '#0e6eb8';
            const stroke = isSelected ? '#ffffff' : '#ffffff';
            let strokeWidth = isSelected ? 2.5 : 1.5;
            
            if (size <= 16) strokeWidth = 1;

            const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}"><circle cx="${size/2}" cy="${size/2}" r="${(size/2) - strokeWidth}" fill="${color}" stroke="${stroke}" stroke-width="${strokeWidth}"/></svg>`;
            const iconUrl = 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(svg);

            let marker = this.markers[poi.id];
            if (marker) {
                marker.setPosition({ lat: poi.latitude, lng: poi.longitude });
                marker.setIcon({ url: iconUrl, anchor: new google.maps.Point(size / 2, size / 2) });
                marker.setZIndex(isClosest || isSelected ? 99 : 10);
            } else {
                marker = new google.maps.Marker({
                    position: { lat: poi.latitude, lng: poi.longitude },
                    // map: this.map, Khong the gan map neu dung Clusterer! Clusterer se lo viec nay.
                    icon: { url: iconUrl, anchor: new google.maps.Point(size / 2, size / 2) },
                    zIndex: isClosest || isSelected ? 99 : 10
                });

                marker.addListener('click', () => {
                    if (this.dotNetRef) {
                        this.dotNetRef.invokeMethodAsync('OnMarkerClick', poi.id);
                    }
                });

                this.markers[poi.id] = marker;
            }

            markersArray.push(marker);
        });

        // Áp dụng thuật toán Gom cụm
        if (window.markerClusterer && window.markerClusterer.MarkerClusterer) {
            if (this.clusterer) {
                this.clusterer.clearMarkers();
                this.clusterer.addMarkers(markersArray);
            } else {
                const algorithm = new window.markerClusterer.SuperClusterAlgorithm({ maxZoom: 15, radius: 60 });
                this.clusterer = new window.markerClusterer.MarkerClusterer({
                    map: this.map,
                    markers: markersArray,
                    renderer: this._createClusterRenderer(),
                    algorithm: algorithm
                });
            }
        } else {
            // Fallback neu cluster chua load dc
            markersArray.forEach(m => m.setMap(this.map));
        }
    },

    flyTo: function (lat, lng, zoom) {
        if (!this.map) return;
        this.map.panTo({ lat, lng });
        if (zoom) this.map.setZoom(zoom);
    },

    onDetailClick: function(id) {
        window.location.href = '/poi-details/' + id;
    },

    onPlayClick: function(id) {
        if (this.dotNetRef) {
            this.dotNetRef.invokeMethodAsync('OnPlayClick', id);
        }
    }
};
