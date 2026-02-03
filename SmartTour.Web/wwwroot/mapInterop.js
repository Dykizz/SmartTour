// ============================================
// MAPBOX MAP INTEROP
// Simplified version - C# handles abstraction
// ============================================

var maps = {};
var markers = {};
var sessionToken = null;

function getSessionToken() {
    if (!sessionToken) {
        sessionToken = Math.random().toString(36).substring(2, 15) + 
                       Math.random().toString(36).substring(2, 15);
    }
    return sessionToken;
}

function cleanAddress(fullAddress, name) {
    if (!fullAddress) return name || "";
    let clean = fullAddress.replace(/, \d{5,6}/g, "");
    if (name && clean.startsWith(name)) return clean;
    return name ? `${name} - ${clean}` : clean;
}

function waitForMapbox(callback, retries = 0) {
    if (typeof mapboxgl !== 'undefined') {
        callback();
    } else if (retries < 20) {
        console.log("Waiting for Mapbox SDK...");
        setTimeout(() => waitForMapbox(callback, retries + 1), 200);
    } else {
        console.error("Mapbox GL JS SDK failed to load.");
    }
}

window.mapInterop = {
    initMap: function (lat, lng, elementId, dotNetHelper, accessToken) {
        waitForMapbox(() => {
            mapboxgl.accessToken = accessToken;
            sessionToken = null; // Reset session token

            const container = document.getElementById(elementId);
            if (!container) return;

            if (maps[elementId]) {
                maps[elementId].remove();
            }

            const map = new mapboxgl.Map({
                container: elementId,
                style: 'mapbox://styles/mapbox/standard',
                projection: 'globe',
                center: [lng, lat],
                zoom: 15
            });

            maps[elementId] = map;

            map.on('style.load', () => {
                map.setFog({});
            });

            const marker = new mapboxgl.Marker({ draggable: true })
                .setLngLat([lng, lat])
                .addTo(map);

            markers[elementId] = marker;

            marker.on('dragend', () => {
                const p = marker.getLngLat();
                dotNetHelper.invokeMethodAsync('UpdateCoordinates', p.lat, p.lng);
            });

            map.on('click', e => {
                marker.setLngLat(e.lngLat);
                dotNetHelper.invokeMethodAsync('UpdateCoordinates', e.lngLat.lat, e.lngLat.lng);
            });

            map.addControl(new mapboxgl.NavigationControl());
            setTimeout(() => map.resize(), 300);
        });
    },

    setMarker: function (lat, lng, elementId) {
        const map = maps[elementId];
        const marker = markers[elementId];
        if (map && marker) {
            marker.setLngLat([lng, lat]);
            map.flyTo({ center: [lng, lat], zoom: 17 });
        }
    },

    invalidateSize: function (elementId) {
        const map = maps[elementId];
        if (map) map.resize();
    },

    getPredictions: async function (input, accessToken) {
        if (!input) return [];
        
        let proximity = "";
        for (let id in maps) {
            const center = maps[id].getCenter();
            proximity = `&proximity=${center.lng},${center.lat}`;
            break;
        }

        try {
            const url = `https://api.mapbox.com/search/searchbox/v1/suggest?q=${encodeURIComponent(input)}&access_token=${accessToken}&session_token=${getSessionToken()}&language=vi&limit=8&country=vn&types=poi,address,street${proximity}`;
            const response = await fetch(url);
            const data = await response.json();
            
            return data.suggestions.map(s => ({
                description: cleanAddress(s.full_address, s.name),
                place_id: s.mapbox_id
            }));
        } catch (e) {
            console.error("Mapbox Suggest Error:", e);
            return [];
        }
    },

    getPlaceDetails: async function (placeId, accessToken) {
        try {
            const url = `https://api.mapbox.com/search/searchbox/v1/retrieve/${placeId}?access_token=${accessToken}&session_token=${getSessionToken()}`;
            const response = await fetch(url);
            const data = await response.json();
            const feature = data.features[0];
            
            return {
                lat: feature.geometry.coordinates[1],
                lng: feature.geometry.coordinates[0],
                name: feature.properties.name,
                address: feature.properties.full_address
            };
        } catch (e) {
            console.error("Mapbox Retrieve Error:", e);
            return null;
        }
    }
};
