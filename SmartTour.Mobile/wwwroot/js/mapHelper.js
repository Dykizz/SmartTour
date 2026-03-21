window.mapHelper = {
    map: null,
    markers: {},
    userMarker: null,
    dotNetRef: null,

    initMap: function (token, lat, lng, zoom, dotNetRef) {
        if (this.map) {
            Object.values(this.markers).forEach(m => m.remove());
            this.markers = {};
            if (this.userMarker) { this.userMarker.remove(); this.userMarker = null; }
            this.map.remove();
            this.map = null;
        }

        this.dotNetRef = dotNetRef;
        mapboxgl.accessToken = token;

        this.map = new mapboxgl.Map({
            container: 'map',
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [lng, lat],
            zoom: zoom,
            maxZoom: 20,
            minZoom: 4
        });

        // Place zoom control lower so the (+) button is not hidden by AppBar
        this.map.addControl(
            new mapboxgl.NavigationControl({ showCompass: false }),
            'top-right'
        );

        // Inject shared keyframe styles once
        if (!document.getElementById('map-anim-style')) {
            const s = document.createElement('style');
            s.id = 'map-anim-style';
            s.textContent = `
                @keyframes userPulse {
                    0%   { transform: translate(-50%,-50%) scale(0.5); opacity: 0.8; }
                    100% { transform: translate(-50%,-50%) scale(2.2); opacity: 0; }
                }
                @keyframes geofencePulse {
                    0%, 100% { transform: scale(1);    box-shadow: 0 0 0 0   rgba(255,87,34,0.5); }
                    50%      { transform: scale(1.18); box-shadow: 0 0 0 6px rgba(255,87,34,0);   }
                }
                /* zoom control offset – push it below the AppBar */
                .mapboxgl-ctrl-top-right {
                    top: 14px !important;
                }
            `;
            document.head.appendChild(s);
        }

        this.map.on('load', () => console.log('[mapHelper] Map loaded'));
        return true;
    },

    updateUserLocation: function (lat, lng) {
        if (!this.map) return;

        if (this.userMarker) {
            this.userMarker.setLngLat([lng, lat]);
            return;
        }

        const el = document.createElement('div');
        el.style.cssText = `
            width: 16px; height: 16px;
            background: #4285F4;
            border: 2.5px solid white;
            border-radius: 50%;
            box-shadow: 0 2px 8px rgba(66,133,244,0.5);
            position: relative;
        `;

        const pulse = document.createElement('div');
        pulse.style.cssText = `
            position: absolute;
            top: 50%; left: 50%;
            width: 34px; height: 34px;
            border-radius: 50%;
            background: rgba(66,133,244,0.25);
            animation: userPulse 2s ease-out infinite;
        `;
        el.appendChild(pulse);

        this.userMarker = new mapboxgl.Marker({ element: el, anchor: 'center' })
            .setLngLat([lng, lat])
            .addTo(this.map);
    },

    updateMarkers: function (pois, closestId, selectedId) {
        if (!this.map) return;

        const newIds = new Set(pois ? pois.map(p => p.id) : []);

        // Remove stale markers
        Object.keys(this.markers).forEach(id => {
            const numId = parseInt(id);
            if (!newIds.has(numId)) {
                this.markers[id].remove();
                delete this.markers[id];
            }
        });

        if (!pois || pois.length === 0) return;

        pois.forEach(poi => {
            const isClosest = poi.id === closestId;
            const isSelected = poi.id === selectedId;

            if (this.markers[poi.id]) {
                // Update position (stays with map automatically)
                this.markers[poi.id].setLngLat([poi.longitude, poi.latitude]);
                // Re-apply style
                this._styleMarker(this.markers[poi.id].getElement(), isClosest, isSelected);
                return;
            }

            // Create element
            const el = document.createElement('div');
            this._styleMarker(el, isClosest, isSelected);

            const marker = new mapboxgl.Marker({ element: el, anchor: 'center' })
                .setLngLat([poi.longitude, poi.latitude])
                .addTo(this.map);

            el.addEventListener('click', (e) => {
                e.stopPropagation();
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnMarkerClick', poi.id);
                }
            });

            this.markers[poi.id] = marker;
        });
    },

    _styleMarker: function (el, isClosest, isSelected) {
        // Base size
        const size = isClosest ? '22px' : isSelected ? '20px' : '16px';
        const bg = isClosest
            ? 'linear-gradient(135deg,#FF5722,#FF8A65)'
            : 'linear-gradient(135deg,#0e6eb8,#1a9de8)';
        const border = isSelected
            ? '3px solid white'
            : '2px solid white';
        const shadow = isSelected
            ? '0 0 0 3px #0e6eb8, 0 3px 8px rgba(0,0,0,0.35)'   // ring = extra box-shadow layer
            : isClosest
                ? '0 2px 6px rgba(0,0,0,0.3)'
                : '0 1px 4px rgba(0,0,0,0.25)';
        const anim = isClosest ? 'geofencePulse 1.6s ease-in-out infinite' : 'none';
        const zIndex = (isClosest || isSelected) ? '2' : '1';

        el.style.cssText = `
            width: ${size};
            height: ${size};
            background: ${bg};
            border: ${border};
            border-radius: 50%;
            cursor: pointer;
            box-shadow: ${shadow};
            animation: ${anim};
            position: relative;
            z-index: ${zIndex};
            transition: width 0.25s, height 0.25s, box-shadow 0.25s;
        `;
    },

    flyTo: function (lat, lng, zoom) {
        if (!this.map) return;
        this.map.flyTo({ center: [lng, lat], zoom, speed: 1.5, curve: 1.2, essential: true });
    }
};
