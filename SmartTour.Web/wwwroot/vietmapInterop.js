
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

            const geolocate = new vietmapgl.GeolocateControl({
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
    },

    showOverviewMap: function(elementId, pois, dotNetHelper, apiKey) {
        waitForVietmap(() => {
            const container = document.getElementById(elementId);
            if (!container) return;

            if (vietmapMaps[elementId]) {
                vietmapMaps[elementId].remove();
            }

            const map = new vietmapgl.Map({
                container: elementId,
                style: `https://maps.vietmap.vn/api/maps/light/style.json?apikey=${apiKey}`,
                center: [106.660172, 10.802622],
                zoom: 12
            });

            vietmapMaps[elementId] = map;
            map.addControl(new vietmapgl.NavigationControl());
            map.addControl(new vietmapgl.GeolocateControl({
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
                const bounds = new vietmapgl.LngLatBounds();
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
                const popup = new vietmapgl.Popup({ closeButton: false, closeOnClick: false, offset: 15 });
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
