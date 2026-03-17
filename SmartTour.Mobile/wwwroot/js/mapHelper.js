window.mapHelper = {
    map: null,
    markers: {},
    userMarker: null,
    dotNetRef: null,

    initMap: function (token, lat, lng, zoom, dotNetRef) {
        // Nếu map đã tồn tại thì remove trước
        if (this.map) {
            this.map.remove();
            this.map = null;
        }

        this.dotNetRef = dotNetRef;
        mapboxgl.accessToken = token;

        this.map = new mapboxgl.Map({
            container: 'map',         // ID của div trong Blazor
            style: 'mapbox://styles/mapbox/streets-v12',
            center: [lng, lat],       // Mapbox dùng [lng, lat]
            zoom: zoom
        });

        // Thêm navigation control
        this.map.addControl(new mapboxgl.NavigationControl(), 'top-right');

        // Báo cho Blazor biết map đã load xong (tuỳ chọn)
        this.map.on('load', () => {
            console.log('[mapHelper] Map loaded successfully');
        });

        return true;
    },

    updateUserLocation: function (lat, lng) {
        if (!this.map) return;

        if (this.userMarker) {
            this.userMarker.setLngLat([lng, lat]);
        } else {
            // Tạo marker cho vị trí người dùng
            const el = document.createElement('div');
            el.style.cssText = `
                width: 20px; height: 20px;
                background: #4285F4;
                border: 3px solid white;
                border-radius: 50%;
                box-shadow: 0 2px 6px rgba(0,0,0,0.3);
            `;
            this.userMarker = new mapboxgl.Marker(el)
                .setLngLat([lng, lat])
                .addTo(this.map);
        }
    },

    updateMarkers: function (pois, closestId) {
        if (!this.map) return;

        // Xóa marker cũ
        Object.values(this.markers).forEach(m => m.remove());
        this.markers = {};

        if (!pois || pois.length === 0) return;

        pois.forEach(poi => {
            const isClosest = poi.id === closestId;

            const el = document.createElement('div');
            el.style.cssText = `
                width: ${isClosest ? '36px' : '28px'};
                height: ${isClosest ? '36px' : '28px'};
                background: ${isClosest ? '#FF5722' : '#1976D2'};
                border: 3px solid white;
                border-radius: 50%;
                cursor: pointer;
                box-shadow: 0 2px 8px rgba(0,0,0,0.4);
                transition: all 0.2s;
            `;

            const marker = new mapboxgl.Marker(el)
                .setLngLat([poi.longitude, poi.latitude])
                .addTo(this.map);

            // Gọi Blazor khi click marker
            el.addEventListener('click', () => {
                if (this.dotNetRef) {
                    this.dotNetRef.invokeMethodAsync('OnMarkerClick', poi.id);
                }
            });

            this.markers[poi.id] = marker;
        });
    },

    flyTo: function (lat, lng, zoom) {
        if (!this.map) return;
        this.map.flyTo({
            center: [lng, lat],
            zoom: zoom,
            speed: 1.5,
            curve: 1.2
        });
    }
};