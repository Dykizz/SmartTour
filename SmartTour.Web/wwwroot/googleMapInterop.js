// ============================================
// GOOGLE MAP INTEROP
// Tương đương mapInterop.js cho Google Maps
// ============================================

var googleMaps = {};
var googleMarkers = {};
var googleMarkersList = {};     
var googleClusterers = {};
var googleGeofences = {};
var autocompleteService = null;
var placesService = null;

function waitForGoogleMaps(callback, retries) {
    retries = retries || 0;
    if (typeof google !== 'undefined' && google.maps && google.maps.places) {
        callback();
    } else if (retries < 60) {
        setTimeout(function () { waitForGoogleMaps(callback, retries + 1); }, 150);
    } else {
        console.error("Google Maps SDK failed to load.");
    }
}

function loadGoogleMapsScript(apiKey) {
    if (typeof google !== 'undefined' && google.maps) return;
    if (document.getElementById('google-maps-script')) return;
    var script = document.createElement('script');
    script.id = 'google-maps-script';
    script.src = 'https://maps.googleapis.com/maps/api/js?key=' + apiKey + '&libraries=places';
    script.defer = true;
    script.async = true;
    document.head.appendChild(script);
}

// ── Nút định vị vị trí người dùng (dùng chung) ──
function addGeolocateControl(map, onLocate) {
    if (!navigator.geolocation) return;
    var locMarker = null;

    var iconDefault = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#555" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><line x1="12" y1="2" x2="12" y2="6"/><line x1="12" y1="18" x2="12" y2="22"/><line x1="2" y1="12" x2="6" y2="12"/><line x1="18" y1="12" x2="22" y2="12"/></svg>';
    var iconActive = '<svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="#1976d2" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="3"/><line x1="12" y1="2" x2="12" y2="6"/><line x1="12" y1="18" x2="12" y2="22"/><line x1="2" y1="12" x2="6" y2="12"/><line x1="18" y1="12" x2="22" y2="12"/></svg>';

    var btn = document.createElement('button');
    btn.title = 'Vị trí hiện tại';
    btn.innerHTML = iconDefault;
    Object.assign(btn.style, {
        background: 'white', border: 'none', outline: 'none',
        width: '40px', height: '40px', margin: '10px 10px 0 0',
        borderRadius: '2px', boxShadow: '0 1px 4px rgba(0,0,0,0.3)',
        cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center'
    });
    btn.addEventListener('mouseenter', function () { btn.style.background = '#f5f5f5'; });
    btn.addEventListener('mouseleave', function () { btn.style.background = 'white'; });
    btn.addEventListener('click', function () {
        btn.innerHTML = iconActive;
        navigator.geolocation.getCurrentPosition(
            function (pos) {
                btn.innerHTML = iconDefault;
                var latlng = { lat: pos.coords.latitude, lng: pos.coords.longitude };
                
                if (locMarker) {
                    locMarker.setMap(null);
                    if (locMarker.accuracyCircle) locMarker.accuracyCircle.setMap(null);
                }

                var svgData = window.btoa(
                    '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100">' +
                    '<circle cx="50" cy="50" r="10" fill="#1976D2" opacity="0">' +
                    '<animate attributeName="r" values="10;45" keyTimes="0;1" dur="2s" begin="0s" repeatCount="indefinite" />' +
                    '<animate attributeName="opacity" values="0;0.5;0" keyTimes="0;0.1;1" dur="2s" begin="0s" repeatCount="indefinite" />' +
                    '</circle>' +
                    '<circle cx="50" cy="50" r="12" fill="#fff" />' +
                    '<circle cx="50" cy="50" r="9" fill="#1976D2" />' +
                    '</svg>'
                );

                locMarker = new google.maps.Marker({
                    position: latlng,
                    map: map,
                    icon: {
                        url: 'data:image/svg+xml;base64,' + svgData,
                        scaledSize: new google.maps.Size(80, 80), 
                        anchor: new google.maps.Point(40, 40)
                    },
                    title: 'Vị trí của bạn',
                    zIndex: 999999
                });

                // Vòng báo sai số thật (Met). Phóng/thu theo Map giống như các Geofence khác
                locMarker.accuracyCircle = new google.maps.Circle({
                    strokeColor: '#1976D2', strokeOpacity: 0.1, strokeWeight: 1,
                    fillColor: '#1976D2', fillOpacity: 0.15,
                    map: map,
                    center: latlng,
                    radius: pos.coords.accuracy || 150
                });

                // Hiệu ứng "di map dần dần" (Giả lập Mapbox flyTo)
                var currentCenter = map.getCenter();
                var dx = currentCenter.lng() - latlng.lng;
                var dy = currentCenter.lat() - latlng.lat;
                var dist = Math.sqrt(dx * dx + dy * dy);

                var startZoom = map.getZoom();
                var targetZoom = 16;

                // Nếu cách xa hơn ~5km, zoom ra rồi bay tới để mượt mà nhất
                if (dist > 0.05 && startZoom > 12) {
                    map.setZoom(12);
                    setTimeout(function() {
                        map.panTo(latlng);
                        setTimeout(function() {
                            var z = 12;
                            var zoomIn = setInterval(function() {
                                z++; map.setZoom(z);
                                if (z >= targetZoom) clearInterval(zoomIn);
                            }, 150);
                        }, 500); // chờ panTo xong
                    }, 100);
                } else {
                    map.panTo(latlng);
                    var z = startZoom;
                    if (z < targetZoom) {
                        setTimeout(function() {
                            var zoomIn = setInterval(function() {
                                z++; map.setZoom(z);
                                if (z >= targetZoom) clearInterval(zoomIn);
                            }, 150);
                        }, 400);
                    } else if (z > targetZoom) {
                        setTimeout(function() { map.setZoom(targetZoom); }, 400);
                    }
                }
                if (onLocate) onLocate(latlng);
            },
            function () { btn.innerHTML = iconDefault; }
        );
    });
    map.controls[google.maps.ControlPosition.RIGHT_TOP].push(btn);
}

// ── Hàm tạo GeoJSON Polygon Circle (Sao chép tư duy từ MapBox) ──
function createGeoJSONCircle(center, radiusInMeters, points) {
    points = points || 64;
    var coords = { latitude: center[1], longitude: center[0] };
    var km = radiusInMeters / 1000;
    var ret = [];
    var distanceX = km / (111.320 * Math.cos(coords.latitude * Math.PI / 180));
    var distanceY = km / 110.574;

    for (var i = 0; i < points; i++) {
        var theta = (i / points) * (2 * Math.PI);
        var x = distanceX * Math.cos(theta);
        var y = distanceY * Math.sin(theta);
        ret.push([coords.longitude + x, coords.latitude + y]);
    }
    ret.push(ret[0]);
    return { type: "Feature", geometry: { type: "Polygon", coordinates: [ret] } };
}

// ── Custom Cluster Renderer giống style Mapbox ──
function createClusterRenderer() {
    return {
        render: function (cluster, stats) {
            var count = cluster.count;
            if (count === 1) {
                return cluster.markers[0]; // Trả lại marker gốc nếu không bị gom cụm
            }
            var position = cluster.position;
            var color, size;
            
            // Giảm size một chút (36, 46, 56) để nhìn thanh thoát + không bị đè nặng như Mapbox
            if (count < 10)       { color = '#51bbd6'; size = 36; }
            else if (count < 50)  { color = '#f1f075'; size = 46; }
            else                  { color = '#f28cb1'; size = 56; }

            var half = size / 2;
            var svg = window.btoa(
                '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 ' + size + ' ' + size + '">' +
                '<circle cx="' + half + '" cy="' + half + '" r="' + half + '" fill="' + color + '"/>' +
                '<text x="50%" y="50%" text-anchor="middle" dominant-baseline="central" ' +
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
            clusterMarker.addListener('mouseover', function () {
                if (clusterMarker.getMap()) clusterMarker.getMap().getDiv().style.cursor = 'pointer';
            });
            clusterMarker.addListener('mouseout', function () {
                if (clusterMarker.getMap()) clusterMarker.getMap().getDiv().style.cursor = '';
            });

            return clusterMarker;
        }
    };
}

window.googleMapInterop = {

    // ── Khởi tạo bản đồ POI Editor ──
    initMap: function (lat, lng, elementId, dotNetHelper, apiKey) {
        loadGoogleMapsScript(apiKey);
        waitForGoogleMaps(function () {
            var container = document.getElementById(elementId);
            if (!container) return;

            if (googleMaps[elementId]) {
                google.maps.event.clearInstanceListeners(googleMaps[elementId]);
                delete googleMaps[elementId];
            }
            if (googleMarkers[elementId]) {
                googleMarkers[elementId].setMap(null);
                delete googleMarkers[elementId];
            }

            var map = new google.maps.Map(container, {
                center: { lat: lat, lng: lng },
                zoom: 15,
                mapTypeControl: false, streetViewControl: false,
                zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_TOP },
                fullscreenControl: false,
                gestureHandling: 'greedy'
            });
            googleMaps[elementId] = map;

            if (elementId === 'editor-map') {
                var marker = new google.maps.Marker({
                    position: { lat: lat, lng: lng },
                    map: map, draggable: true, animation: google.maps.Animation.DROP
                });
                googleMarkers[elementId] = marker;

                marker.addListener('dragend', function () {
                    var p = marker.getPosition();
                    dotNetHelper.invokeMethodAsync('UpdateCoordinates', p.lat(), p.lng());
                });

                map.addListener('click', function (e) {
                    marker.setAnimation(google.maps.Animation.BOUNCE);
                    setTimeout(function () { marker.setAnimation(null); }, 300);
                    marker.setPosition(e.latLng);
                    dotNetHelper.invokeMethodAsync('UpdateCoordinates', e.latLng.lat(), e.latLng.lng());
                });

                addGeolocateControl(map, function (p) {
                    marker.setPosition(p);
                    dotNetHelper.invokeMethodAsync('UpdateCoordinates', p.lat, p.lng);
                });
            } else {
                addGeolocateControl(map, null);
            }

            if (!autocompleteService) autocompleteService = new google.maps.places.AutocompleteService();
            if (!placesService) placesService = new google.maps.places.PlacesService(map);
        });
    },

    setMarker: function (lat, lng, elementId) {
        var map = googleMaps[elementId];
        var marker = googleMarkers[elementId];
        if (map && marker) {
            var pos = new google.maps.LatLng(lat, lng);
            marker.setPosition(pos);
            map.panTo(pos);
            if (map.getZoom() < 16) { setTimeout(function () { map.setZoom(17); }, 300); }
        }
    },

    invalidateSize: function (elementId) {
        var map = googleMaps[elementId];
        if (map) google.maps.event.trigger(map, 'resize');
    },

    getPredictions: function (input, apiKey) {
        return new Promise(function (resolve) {
            if (!input || !autocompleteService) { resolve([]); return; }
            autocompleteService.getPlacePredictions(
                { input: input, componentRestrictions: { country: 'vn' } },
                function (predictions, status) {
                    if (status !== google.maps.places.PlacesServiceStatus.OK || !predictions) {
                        resolve([]); return;
                    }
                    resolve(predictions.map(function (p) { return { description: p.description, place_id: p.place_id }; }));
                }
            );
        });
    },

    getPlaceDetails: function (placeId, apiKey) {
        return new Promise(function (resolve) {
            if (!placeId || !placesService) { resolve(null); return; }
            placesService.getDetails(
                { placeId: placeId, fields: ['name', 'formatted_address', 'geometry'] },
                function (place, status) {
                    if (status === google.maps.places.PlacesServiceStatus.OK && place && place.geometry && place.geometry.location) {
                        resolve({
                            lat: place.geometry.location.lat(), lng: place.geometry.location.lng(),
                            name: place.name || '', address: place.formatted_address || ''
                        });
                    } else { resolve(null); }
                }
            );
        });
    },

    // ── Bản đồ tổng quan (Rebuilt with Source Update Pattern like Mapbox) ──
    showOverviewMap: function (elementId, pois, dotNetHelper, apiKey) {
        loadGoogleMapsScript(apiKey);

        waitForGoogleMaps(function () {
            var container = document.getElementById(elementId);
            if (!container) return;

            var existingMap = googleMaps[elementId];

            // 1. Nếu map từng tồn tại nhưng DOM bị Blazor route thay đổi -> Phá hủy hoàn toàn
            if (existingMap && (existingMap.getDiv() !== container || !document.body.contains(existingMap.getDiv()))) {
                google.maps.event.clearInstanceListeners(existingMap);
                delete googleMaps[elementId];
                delete googleClusterers[elementId];
                if (googleMarkersList[elementId]) {
                    googleMarkersList[elementId].forEach(function (m) { m.setMap(null); });
                    delete googleMarkersList[elementId];
                }
                existingMap = null;
            }

            var bounds = new google.maps.LatLngBounds();
            var newMarkers = [];
            
            // Xóa geofence cũ nếu có
            if (googleGeofences[elementId]) {
                googleGeofences[elementId].forEach(function(c) { try { c.setMap(null); } catch(e){} });
            }
            googleGeofences[elementId] = [];

            // Thiết lập Global Mapbox-styled Fake Popup
            var popupDiv = document.getElementById('gg-mapbox-tooltip');
            if (!popupDiv) {
                popupDiv = document.createElement('div');
                popupDiv.id = 'gg-mapbox-tooltip';
                Object.assign(popupDiv.style, {
                    position: 'absolute', background: '#fff', borderRadius: '4px',
                    boxShadow: '0 1px 4px rgba(0,0,0,0.3)', padding: '10px 15px',
                    fontFamily: 'sans-serif', fontSize: '13px', pointerEvents: 'none',
                    display: 'none', zIndex: 99999, transform: 'translate(-50%, -100%)',
                    marginTop: '-20px' // offset from cursor
                });
                popupDiv.innerHTML = '<div id="gg-mapbox-tooltip-content"></div><div style="position:absolute;bottom:-5px;left:50%;margin-left:-5px;width:0;height:0;border-left:5px solid transparent;border-right:5px solid transparent;border-top:5px solid #fff;"></div>';
                document.body.appendChild(popupDiv);
            }

            // 2. Chuẩn bị Data
            if (pois && pois.length > 0) {
                pois.forEach(function (poi) {
                    var pos = { lat: poi.lat, lng: poi.lng };
                    bounds.extend(pos);

                    var dotColor = poi.isActive ? '#4caf50' : '#9e9e9e';
                    var svgDot = window.btoa(
                        '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 22 22">' +
                        '<circle cx="11" cy="11" r="9" fill="' + dotColor + '" stroke="#fff" stroke-width="2.5"/>' +
                        '</svg>'
                    );

                    var marker = new google.maps.Marker({
                        position: pos,
                        map: null, // Sẽ được add qua Array update
                        title: poi.name,
                        icon: { url: 'data:image/svg+xml;base64,' + svgDot, scaledSize: new google.maps.Size(22, 22), anchor: new google.maps.Point(11, 11) },
                        cursor: 'pointer'
                    });

                    // Geofence Native Google Circle thay vì GeoJSON
                    // Bí quyết: Gắn sự kiện map_changed trên marker để tắt vòng khi marker bị gom cụm (ẩn)
                    if (poi.radius && poi.radius > 0) {
                        var circle = new google.maps.Circle({
                            strokeColor: '#1976d2', strokeOpacity: 0.4, strokeWeight: 1.5,
                            fillColor: '#1976d2', fillOpacity: 0.08,
                            map: null, 
                            center: pos, 
                            radius: poi.radius
                        });
                        
                        marker.addListener('map_changed', function() {
                            circle.setMap(marker.getMap());
                        });

                        googleGeofences[elementId].push(circle);
                    }

                    var statusHtml = poi.isActive ? '<span style="color:#4caf50;font-weight:bold">Hoạt động</span>' : '<span style="color:#f44336;font-weight:bold">Tạm dừng</span>';
                    var tooltipHtml = '<div style="text-align:center;font-family:sans-serif;padding:4px 2px"><strong style="font-size:14px;color:#1976d2">' + poi.name + '</strong><br/><small>Bán kính: <b>' + poi.radius + 'm</b></small><br/><small>Trạng thái: ' + statusHtml + '</small></div>';

                    marker.addListener('mouseover', function (e) {
                        if (existingMap) existingMap.getDiv().style.cursor = 'pointer';
                        var pDiv = document.getElementById('gg-mapbox-tooltip');
                        var cDiv = document.getElementById('gg-mapbox-tooltip-content');
                        if (pDiv && cDiv) {
                            cDiv.innerHTML = tooltipHtml;
                            pDiv.style.display = 'block';
                            if (e.domEvent) {
                                pDiv.style.left = e.domEvent.clientX + 'px';
                                pDiv.style.top = (e.domEvent.clientY + window.scrollY) + 'px';
                            }
                        }
                    });
                    
                    marker.addListener('mousemove', function (e) {
                        var pDiv = document.getElementById('gg-mapbox-tooltip');
                        if (pDiv && pDiv.style.display === 'block' && e.domEvent) {
                            pDiv.style.left = e.domEvent.clientX + 'px';
                            pDiv.style.top = (e.domEvent.clientY + window.scrollY) + 'px';
                        }
                    });

                    marker.addListener('mouseout', function () {
                        if (existingMap) existingMap.getDiv().style.cursor = '';
                        var pDiv = document.getElementById('gg-mapbox-tooltip');
                        if (pDiv) pDiv.style.display = 'none';
                    });

                    marker.addListener('click', function () {
                        if (dotNetHelper) dotNetHelper.invokeMethodAsync('NavigateToDetailFromMap', poi.id);
                    });

                    newMarkers.push(marker);
                });
            }

            // 3. Update in-place nếu đã có Map
            if (existingMap) {
                // Remove map.data clear logic, circles auto-update tracking markers via map_changed event

                // Update Markers
                if (googleMarkersList[elementId]) {
                    googleMarkersList[elementId].forEach(function(m){ m.setMap(null); });
                }
                googleMarkersList[elementId] = newMarkers;

                // Thay vì reuse clusterer (dễ lỗi state), ta xóa và tạo cái mới mỗi lần data thay đổi
                if (googleClusterers[elementId]) {
                    try { googleClusterers[elementId].clearMarkers(); } catch(e) {}
                    try { googleClusterers[elementId].setMap(null); } catch(e) {}
                    delete googleClusterers[elementId];
                }

                var clusterer = googleClusterers[elementId];
                if (window.markerClusterer && newMarkers.length > 0) {
                    // Radius 80 giúp gom cụm mạnh hơn, tránh việc các điểm nằm quá sát nhau tạo cảm giác rối
                    var algorithm = new window.markerClusterer.SuperClusterAlgorithm({ maxZoom: 14, radius: 80 });
                    googleClusterers[elementId] = new window.markerClusterer.MarkerClusterer({ map: existingMap, markers: newMarkers, renderer: createClusterRenderer(), algorithm: algorithm });
                } else {
                    newMarkers.forEach(function (m) { m.setMap(existingMap); });
                }

                // Không nhảy Bounds để giữ vị trí khung hình đang trễ, chỉ trigger resize
                setTimeout(function () { google.maps.event.trigger(existingMap, 'resize'); }, 100);
                return;
            }

            // 4. Khởi tạo lúc đầu
            var map = new google.maps.Map(container, {
                center: { lat: 10.802622, lng: 106.660172 },
                zoom: 12,
                mapTypeControl: false, streetViewControl: false,
                zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_TOP },
                fullscreenControl: false,
                gestureHandling: 'greedy'
            });
            googleMaps[elementId] = map;

            addGeolocateControl(map, null);
            googleMarkersList[elementId] = newMarkers;

            function doCluster() {
                if (window.markerClusterer && window.markerClusterer.MarkerClusterer && newMarkers.length > 0) {
                    var algorithm = new window.markerClusterer.SuperClusterAlgorithm({ maxZoom: 14, radius: 80 });
                    googleClusterers[elementId] = new window.markerClusterer.MarkerClusterer({
                        map: map, markers: newMarkers, renderer: createClusterRenderer(), algorithm: algorithm
                    });
                } else {
                    newMarkers.forEach(function (m) { m.setMap(map); });
                }
                if (pois && pois.length > 0) {
                    map.fitBounds(bounds, { padding: 50 });
                    google.maps.event.addListenerOnce(map, 'idle', function () { if (map.getZoom() > 16) map.setZoom(16); });
                }
            }

            if (window.markerClusterer && window.markerClusterer.MarkerClusterer) {
                doCluster();
            } else {
                var s = document.getElementById('marker-clusterer-script');
                if (!s) {
                    s = document.createElement('script');
                    s.id = 'marker-clusterer-script';
                    s.src = 'https://unpkg.com/@googlemaps/markerclusterer/dist/index.umd.js';
                    s.onload = doCluster; s.onerror = doCluster;
                    document.head.appendChild(s);
                } else {
                    var wait = setInterval(function () {
                        if (window.markerClusterer && window.markerClusterer.MarkerClusterer) {
                            clearInterval(wait); doCluster();
                        }
                    }, 100);
                }
            }
        });
    }
};