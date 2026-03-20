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
            
            const geolocate = new mapboxgl.GeolocateControl({
                positionOptions: { enableHighAccuracy: true },
                trackUserLocation: true,
                showUserLocation: true
            });
            map.addControl(geolocate);

            geolocate.on('geolocate', (e) => {
                const lat = e.coords.latitude;
                const lng = e.coords.longitude;
                marker.setLngLat([lng, lat]);
                dotNetHelper.invokeMethodAsync('UpdateCoordinates', lat, lng);
            });

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
    },

    showOverviewMap: function(elementId, pois, dotNetHelper, accessToken) {
        waitForMapbox(() => {
            mapboxgl.accessToken = accessToken;
            const container = document.getElementById(elementId);
            if (!container) return;

            if (maps[elementId]) {
                maps[elementId].remove();
            }

            const map = new mapboxgl.Map({
                container: elementId,
                style: 'mapbox://styles/mapbox/standard',
                center: [106.660172, 10.802622],
                zoom: 12
            });

            maps[elementId] = map;
            map.addControl(new mapboxgl.NavigationControl());
            map.addControl(new mapboxgl.GeolocateControl({
                positionOptions: { enableHighAccuracy: true },
                trackUserLocation: true
            }));

            // Hàm toán học vẽ hình tròn tính bằng Mét
            function createGeoJSONCircle(center, radiusInMeters, points = 64) {
                const coords = { latitude: center[1], longitude: center[0] };
                const km = radiusInMeters / 1000;
                const ret = [];
                const distanceX = km / (111.320 * Math.cos(coords.latitude * Math.PI / 180));
                const distanceY = km / 110.574;
            
                for (let i = 0; i < points; i++) {
                    const theta = (i / points) * (2 * Math.PI);
                    const x = distanceX * Math.cos(theta);
                    const y = distanceY * Math.sin(theta);
                    ret.push([coords.longitude + x, coords.latitude + y]);
                }
                ret.push(ret[0]);
                return { type: "Feature", geometry: { type: "Polygon", coordinates: [ret] } };
            }

            if (pois && pois.length > 0) {
                const bounds = new mapboxgl.LngLatBounds();
                const features = pois.map(poi => {
                    bounds.extend([poi.lng, poi.lat]);
                    const statusHtml = poi.isActive ? '<span style="color:#4caf50">Hoạt động</span>' : '<span style="color:#f44336">Tạm dừng</span>';
                    return {
                        type: 'Feature',
                        geometry: { type: 'Point', coordinates: [poi.lng, poi.lat] },
                        properties: { 
                            id: poi.id, name: poi.name, radius: poi.radius, isActive: poi.isActive,
                            tooltipHtml: `<div style="text-align:center">
                                            <strong style="font-size:15px;color:#1976d2">${poi.name}</strong><br/>
                                            <small>Bán kính (Geofence): <b>${poi.radius}m</b></small><br/>
                                            <small>Trạng thái: <b>${statusHtml}</b></small>
                                          </div>`
                        }
                    };
                });

                // Chờ map tải style xong rồi mới nhúng Layer
                map.on('style.load', () => {
                    // 1. DATA SOURCE (Hỗ trợ Gom cụm - Cluster)
                    map.addSource('pois-source', {
                        type: 'geojson',
                        data: { type: 'FeatureCollection', features: features },
                        cluster: true,
                        clusterMaxZoom: 14,
                        clusterRadius: 50
                    });

                    // 2. LỚP VẼ CỤM (Clusters)
                    map.addLayer({
                        id: 'clusters',
                        type: 'circle',
                        source: 'pois-source',
                        filter: ['has', 'point_count'],
                        paint: {
                            'circle-color': ['step', ['get', 'point_count'], '#51bbd6', 10, '#f1f075', 50, '#f28cb1'],
                            'circle-radius': ['step', ['get', 'point_count'], 20, 10, 25, 50, 30]
                        }
                    });

                    // 3. CHỮ SỐ BÊN TRONG CỤM
                    map.addLayer({
                        id: 'cluster-count',
                        type: 'symbol',
                        source: 'pois-source',
                        filter: ['has', 'point_count'],
                        layout: {
                            'text-field': '{point_count_abbreviated}',
                            'text-size': 14
                        }
                    });

                    // 4. CÁC ĐIỂM LẺ (Khi Zoom gần)
                    map.addLayer({
                        id: 'unclustered-point',
                        type: 'circle',
                        source: 'pois-source',
                        filter: ['!', ['has', 'point_count']],
                        paint: {
                            'circle-color': ['case', ['==', ['get', 'isActive'], true], '#4caf50', '#9e9e9e'],
                            'circle-radius': 9,
                            'circle-stroke-width': 2,
                            'circle-stroke-color': '#fff'
                        }
                    });

                    // 5. VẼ BÁN KÍNH (GEOFENCES)
                    const geofenceFeatures = pois.filter(p => p.radius && p.radius > 0).map(p => createGeoJSONCircle([p.lng, p.lat], p.radius));
                    if(geofenceFeatures.length > 0) {
                        map.addSource('geofences', { type: 'geojson', data: { type: 'FeatureCollection', features: geofenceFeatures } });
                        map.addLayer({
                            id: 'geofence-fills', type: 'fill', source: 'geofences',
                            minzoom: 14,
                            paint: { 'fill-color': '#1976d2', 'fill-opacity': 0.08 }
                        });
                        map.addLayer({
                            id: 'geofence-borders', type: 'line', source: 'geofences',
                            minzoom: 14,
                            paint: { 'line-color': '#1976d2', 'line-width': 1.5, 'line-dasharray': [2, 2], 'line-opacity': 0.4 }
                        });
                    }
                });

                // TƯƠNG TÁC: Click vào Nhóm -> Zoom In
                map.on('click', 'clusters', (e) => {
                    const featuresClick = map.queryRenderedFeatures(e.point, { layers: ['clusters'] });
                    const clusterId = featuresClick[0].properties.cluster_id;
                    map.getSource('pois-source').getClusterExpansionZoom(clusterId, (err, zoom) => {
                        if (err) return;
                        map.easeTo({ center: featuresClick[0].geometry.coordinates, zoom: zoom });
                    });
                });
                map.on('mouseenter', 'clusters', () => map.getCanvas().style.cursor = 'pointer');
                map.on('mouseleave', 'clusters', () => map.getCanvas().style.cursor = '');

                // TƯƠNG TÁC: Điểm lẻ (Hover + Click)
                const popup = new mapboxgl.Popup({ closeButton: false, closeOnClick: false, offset: 15 });
                map.on('mouseenter', 'unclustered-point', (e) => {
                    map.getCanvas().style.cursor = 'pointer';
                    const coordinates = e.features[0].geometry.coordinates.slice();
                    const html = e.features[0].properties.tooltipHtml;
                    while (Math.abs(e.lngLat.lng - coordinates[0]) > 180) coordinates[0] += e.lngLat.lng > coordinates[0] ? 360 : -360;
                    popup.setLngLat(coordinates).setHTML(html).addTo(map);
                });
                map.on('mouseleave', 'unclustered-point', () => {
                    map.getCanvas().style.cursor = '';
                    popup.remove();
                });
                map.on('click', 'unclustered-point', (e) => {
                    dotNetHelper.invokeMethodAsync('NavigateToDetailFromMap', e.features[0].properties.id);
                });

                map.fitBounds(bounds, { padding: 50, maxZoom: 16 });
            }

            setTimeout(() => map.resize(), 500);
        });
    }
};
