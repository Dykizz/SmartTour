
var vietmapMaps = {};
var vietmapMarkers = {};

function waitForVietmap(callback, retries = 0) {
    if (typeof vietmapgl !== 'undefined') {
        callback();
    } else if (retries < 20) {
        console.log("Waiting for Vietmap SDK...");
        setTimeout(() => waitForVietmap(callback, retries + 1), 200);
    } else {
        console.error("Vietmap GL JS SDK failed to load.");
    }
}

window.vietmapInterop = {
    initMap: function (lat, lng, elementId, dotNetHelper, apiKey) {
        waitForVietmap(() => {
            const container = document.getElementById(elementId);
            if (!container) return;

            if (vietmapMaps[elementId]) {
                vietmapMaps[elementId].remove();
            }

            const map = new vietmapgl.Map({
                container: elementId,
                style: `https://maps.vietmap.vn/api/maps/light/style.json?apikey=${apiKey}`,
                center: [lng, lat],
                zoom: 15
            });

            vietmapMaps[elementId] = map;

            const marker = new vietmapgl.Marker({ draggable: true })
                .setLngLat([lng, lat])
                .addTo(map);

            vietmapMarkers[elementId] = marker;

            marker.on('dragend', () => {
                const p = marker.getLngLat();
                dotNetHelper.invokeMethodAsync('UpdateCoordinates', p.lat, p.lng);
            });

            map.on('click', e => {
                marker.setLngLat(e.lngLat);
                dotNetHelper.invokeMethodAsync('UpdateCoordinates', e.lngLat.lat, e.lngLat.lng);
            });

            map.addControl(new vietmapgl.NavigationControl());
            setTimeout(() => map.resize(), 300);
        });
    },

    setMarker: function (lat, lng, elementId) {
        const map = vietmapMaps[elementId];
        const marker = vietmapMarkers[elementId];
        if (map && marker) {
            marker.setLngLat([lng, lat]);
            map.flyTo({ center: [lng, lat], zoom: 17 });
        }
    },

    invalidateSize: function (elementId) {
        const map = vietmapMaps[elementId];
        if (map) map.resize();
    },

    getPredictions: async function (input, apiKey) {
        if (!input) return [];

        try {
            // Sử dụng Autocomplete v4
            const url = `https://maps.vietmap.vn/api/autocomplete/v4?apikey=${apiKey}&text=${encodeURIComponent(input)}`;
            const response = await fetch(url);
            const data = await response.json();
            
            return data.map(item => ({
                description: item.display_name,
                place_id: item.ref_id
            }));
        } catch (e) {
            console.error("Vietmap Autocomplete v4 Error:", e);
            return [];
        }
    },

    getPlaceDetails: async function (placeId, apiKey) {
        try {
            // Sử dụng Place v4 (đồng bộ với Autocomplete v4)
            const url = `https://maps.vietmap.vn/api/place/v4?apikey=${apiKey}&refid=${placeId}`;
            const response = await fetch(url);
            const data = await response.json();
            
            return {
                lat: data.lat,
                lng: data.lng,
                name: data.display || data.name || "",
                address: data.address || data.display || ""
            };
        } catch (e) {
            console.error("Vietmap Place v4 Error:", e);
            return null;
        }
    }
};
