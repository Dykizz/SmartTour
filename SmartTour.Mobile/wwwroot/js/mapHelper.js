/**
 * SmartTour Map Helper - Dual Mode
 * - ONLINE  : Google Maps API + MarkerClusterer (tính năng đầy đủ)
 * - OFFLINE : Mapbox GL JS với tile tile được cache sẵn (OpenStreetMap raster)
 */
window.mapHelper = {
    // ── Shared State ──
    dotNetRef: null,
    _pois: [],
    _lastClosestId: 0,
    _lastSelectedId: 0,
    _mode: 'none', // 'google' | 'mapbox'

    // ── Google Maps State ──
    _gmap: null,
    _gmarkers: {},
    _guserMarker: null,
    _clusterer: null,

    // ── Mapbox State ──
    _mbmap: null,
    _mbUserMarker: null,
    _mbPoiMarkers: {},

    // ─────────────────────────────────────────────
    //  PUBLIC API (được gọi từ C#)
    // ─────────────────────────────────────────────

    /** Hàm khởi tạo chính, tự phát hiện online/offline và có Timeout cứu hộ */
    initMap: async function (googleApiKey, lat, lng, zoom, dotNetRef) {
        this.dotNetRef = dotNetRef;
        const isOnline = navigator.onLine;
        console.log(`[mapHelper] Network: ${isOnline ? 'ONLINE → Google Maps' : 'OFFLINE → Mapbox GL'}`);

        if (isOnline) {
            const googleSuccess = await this._initGoogle(googleApiKey, lat, lng, zoom);
            if (!googleSuccess) {
                console.warn("[mapHelper] Mạng chậm hoặc Google Maps bị lỗi tải, Ép hạ cấp xuống Offline/Mapbox!");
                return this._initMapbox(lat, lng, zoom);
            }
            return true;
        } else {
            return this._initMapbox(lat, lng, zoom);
        }
    },

    updateUserLocation: function (lat, lng) {
        if (this._mode === 'google') this._gUpdateUser(lat, lng);
        else if (this._mode === 'mapbox') this._mbUpdateUser(lat, lng);
    },

    updateMarkers: function (pois, closestId, selectedId) {
        this._pois = pois || [];
        this._lastClosestId = closestId;
        this._lastSelectedId = selectedId;
        if (this._mode === 'google') this._gRenderPOIs(this._gmap ? this._gmap.getZoom() : 15);
        else if (this._mode === 'mapbox') this._mbRenderPOIs();
    },

    flyTo: function (lat, lng, zoom) {
        if (this._mode === 'google' && this._gmap) {
            this._gmap.panTo({ lat, lng });
            if (zoom) this._gmap.setZoom(zoom);
        } else if (this._mode === 'mapbox' && this._mbmap) {
            this._mbmap.flyTo({ center: [lng, lat], zoom: zoom || 15, speed: 1.2 });
        }
    },

    fitBoundsToPOIs: function () {
        if (!this._pois || this._pois.length === 0) return;
        if (this._mode === 'google') this._gFitBounds();
        else if (this._mode === 'mapbox') this._mbFitBounds();
    },

    // ─────────────────────────────────────────────
    //  GOOGLE MAPS IMPLEMENTATION
    // ─────────────────────────────────────────────

    _initGoogle: function (apiKey, lat, lng, zoom) {
        return new Promise((resolve) => {
            this._loadGoogleScript(apiKey, () => {
                const container = document.getElementById('map');
                if (!container) { resolve(false); return; }

                // Reset
                if (this._gmap) {
                    if (this._clusterer) this._clusterer.clearMarkers();
                    google.maps.event.clearInstanceListeners(this._gmap);
                }
                this._gmarkers = {};
                this._guserMarker = null;
                this._clusterer = null;

                this._gmap = new google.maps.Map(container, {
                    center: { lat, lng },
                    zoom,
                    mapTypeControl: false,
                    streetViewControl: false,
                    fullscreenControl: false,
                    gestureHandling: 'greedy',
                    zoomControlOptions: { position: google.maps.ControlPosition.RIGHT_TOP },
                    styles: [{ featureType: 'poi', elementType: 'labels', stylers: [{ visibility: 'off' }] }]
                });

                this._gmap.addListener('zoom_changed', () => {
                    const z = this._gmap.getZoom();
                    if (this._pois.length > 0) this._gRenderPOIs(z);
                    if (this._guserMarker) this._gRenderUserSize(z);
                });

                this._mode = 'google';
                console.log('[mapHelper] ✅ Google Maps ready');
                resolve(true);
            }, () => {
                // Call back lỗi (do Offline ngầm) 
                resolve(false);
            });
        });
    },

    _loadGoogleScript: function (apiKey, callback, errorCallback) {
        let loaded = 0;
        let hasErrored = false;

        const checkTimeout = setTimeout(() => {
            if (loaded < 2 && !hasErrored) {
                hasErrored = true;
                console.warn('[mapHelper] Google Maps load timeout sau 5 giây!');
                if (errorCallback) errorCallback();
            }
        }, 5000);

        const done = () => { 
            if (hasErrored) return;
            if (++loaded === 2) {
                clearTimeout(checkTimeout);
                callback(); 
            }
        };

        const errorOut = () => {
            if (hasErrored) return;
            hasErrored = true;
            clearTimeout(checkTimeout);
            if (errorCallback) errorCallback();
        };

        if (window.markerClusterer) {
            done();
        } else if (!document.getElementById('markerclusterer-script')) {
            const s = document.createElement('script');
            s.id = 'markerclusterer-script';
            s.src = 'https://unpkg.com/@googlemaps/markerclusterer/dist/index.min.js';
            s.onload = done;
            s.onerror = errorOut;
            document.head.appendChild(s);
        } else { done(); }

        if (window.google && window.google.maps) {
            done();
        } else if (!document.getElementById('google-maps-script')) {
            const s = document.createElement('script');
            s.id = 'google-maps-script';
            s.src = `https://maps.googleapis.com/maps/api/js?key=${apiKey}&libraries=places,marker&callback=gmCallback`;
            s.async = true;
            s.onerror = errorOut;
            window.gmCallback = done;
            document.head.appendChild(s);
        } else { done(); }
    },

    _gUpdateUser: function (lat, lng) {
        if (!this._gmap) return;
        if (this._guserMarker) {
            this._guserMarker.setPosition({ lat, lng });
            return;
        }
        this._guserMarker = new google.maps.Marker({ position: { lat, lng }, map: this._gmap, zIndex: 999 });
        this._gRenderUserSize(this._gmap.getZoom());
    },

    _gRenderUserSize: function (z) {
        if (!this._guserMarker) return;
        let s = z <= 11 ? 40 : z <= 13 ? 56 : z <= 15 ? 72 : z >= 18 ? 96 : 80;
        const svg = window.btoa(
            `<svg xmlns="http://www.w3.org/2000/svg" width="${s}" height="${s}" viewBox="0 0 100 100">` +
            `<circle cx="50" cy="50" r="10" fill="#00C853" opacity="0">` +
            `<animate attributeName="r" values="10;45" dur="2s" repeatCount="indefinite"/>` +
            `<animate attributeName="opacity" values="0;0.5;0" keyTimes="0;0.1;1" dur="2s" repeatCount="indefinite"/>` +
            `</circle><circle cx="50" cy="50" r="12" fill="#fff"/><circle cx="50" cy="50" r="9" fill="#00C853"/></svg>`
        );
        this._guserMarker.setIcon({
            url: 'data:image/svg+xml;base64,' + svg,
            scaledSize: new google.maps.Size(s, s),
            anchor: new google.maps.Point(s / 2, s / 2)
        });
    },

    _gFitBounds: function () {
        if (!this._gmap || !this._pois.length) return;
        const bounds = new google.maps.LatLngBounds();
        this._pois.forEach(p => bounds.extend({ lat: p.latitude, lng: p.longitude }));
        this._gmap.fitBounds(bounds, { padding: 50 });
    },

    _gRenderPOIs: function (z) {
        if (!this._pois.length) return;
        let scale = z <= 11 ? 0.5 : z <= 13 ? 0.7 : z <= 15 ? 0.9 : z >= 18 ? 1.2 : 1.0;
        const newIds = new Set(this._pois.map(p => p.id));
        Object.keys(this._gmarkers).forEach(id => {
            if (!newIds.has(+id)) {
                if (this._clusterer) this._clusterer.removeMarker(this._gmarkers[id]);
                this._gmarkers[id].setMap(null);
                delete this._gmarkers[id];
            }
        });

        const arr = [];
        this._pois.forEach(poi => {
            const closest = poi.id === this._lastClosestId;
            const selected = poi.id === this._lastSelectedId;
            let sz = Math.max(12, Math.round((closest ? 36 : selected ? 30 : 20) * scale));
            const color = closest ? '#FF5722' : '#0e6eb8';
            const sw = sz <= 16 ? 1 : selected ? 2.5 : 1.5;
            const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${sz}" height="${sz}" viewBox="0 0 ${sz} ${sz}"><circle cx="${sz/2}" cy="${sz/2}" r="${sz/2-sw}" fill="${color}" stroke="#fff" stroke-width="${sw}"/></svg>`;
            const iconUrl = 'data:image/svg+xml;charset=UTF-8,' + encodeURIComponent(svg);

            let m = this._gmarkers[poi.id];
            if (m) {
                m.setPosition({ lat: poi.latitude, lng: poi.longitude });
                m.setIcon({ url: iconUrl, anchor: new google.maps.Point(sz / 2, sz / 2) });
                m.setZIndex(closest || selected ? 99 : 10);
            } else {
                m = new google.maps.Marker({
                    position: { lat: poi.latitude, lng: poi.longitude },
                    icon: { url: iconUrl, anchor: new google.maps.Point(sz / 2, sz / 2) },
                    zIndex: closest || selected ? 99 : 10
                });
                m.addListener('click', () => {
                    if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnMarkerClick', poi.id);
                });
                this._gmarkers[poi.id] = m;
            }
            arr.push(m);
        });

        if (window.markerClusterer && window.markerClusterer.MarkerClusterer) {
            if (this._clusterer) {
                this._clusterer.clearMarkers();
                this._clusterer.addMarkers(arr);
            } else {
                this._clusterer = new window.markerClusterer.MarkerClusterer({
                    map: this._gmap,
                    markers: arr,
                    algorithm: new window.markerClusterer.SuperClusterAlgorithm({ maxZoom: 15, radius: 60 }),
                    renderer: this._gClusterRenderer()
                });
            }
        } else {
            arr.forEach(m => m.setMap(this._gmap));
        }
    },

    _gClusterRenderer: function () {
        return {
            render: function (cluster) {
                const count = cluster.count;
                if (count === 1) return cluster.markers[0];
                const color = count < 10 ? '#51bbd6' : count < 50 ? '#f1f075' : '#f28cb1';
                const size = count < 10 ? 28 : count < 50 ? 36 : 44;
                const half = size / 2;
                const svg = window.btoa(
                    `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">` +
                    `<circle cx="${half}" cy="${half}" r="${half-1.5}" fill="${color}" stroke="#fff" stroke-width="3"/>` +
                    `<text x="50%" y="${half+1}" text-anchor="middle" dominant-baseline="central" font-family="Arial,sans-serif" font-weight="bold" font-size="13px" fill="#000">${count}</text></svg>`
                );
                const m = new google.maps.Marker({
                    position: cluster.position,
                    icon: { url: 'data:image/svg+xml;base64,' + svg, scaledSize: new google.maps.Size(size, size), anchor: new google.maps.Point(half, half) },
                    zIndex: 1000 + count
                });
                m.addListener('click', () => {
                    const map = m.getMap();
                    if (map) {
                        if (cluster.bounds) map.fitBounds(cluster.bounds, { padding: 50 });
                        else { map.setZoom(map.getZoom() + 2); map.panTo(cluster.position); }
                    }
                });
                return m;
            }
        };
    },

    // ─────────────────────────────────────────────
    //  MAPBOX GL JS IMPLEMENTATION  (OFFLINE)
    // ─────────────────────────────────────────────

    _initMapbox: function (lat, lng, zoom) {
        return new Promise((resolve) => {
            const container = document.getElementById('map');
            if (!container || typeof maplibregl === 'undefined') {
                console.warn('[mapHelper] MapLibre GL JS chưa load!');
                resolve(false);
                return;
            }

            // Đăng ký Custom Protocol (Giải pháp cứu tinh cho MAUI: Dùng HTMLImageElement thay vì Buffer)
            if (!this._protocolRegistered) {
                maplibregl.addProtocol('smartmap', (params, callback) => {
                    let realUrl = params.url.replace('smartmap://', 'https://');
                    realUrl = realUrl.replace(/\.0\//g, '/').replace(/\.0@2x/g, '@2x');

                    const resolveImage = (response) => {
                        response.blob().then(blob => {
                            const safeBlob = new Blob([blob], { type: 'image/png' });
                            const objectUrl = URL.createObjectURL(safeBlob);
                            
                            const img = new Image();
                            img.crossOrigin = "Anonymous";
                            img.onload = () => {
                                callback(null, img, null, null);
                                URL.revokeObjectURL(objectUrl);
                            };
                            img.onerror = () => {
                                response.arrayBuffer().then(buffer => callback(null, buffer, null, null));
                                URL.revokeObjectURL(objectUrl);
                            };
                            img.src = objectUrl;
                        }).catch(e => callback(new Error("Lỗi đọc Blob: " + e)));
                    };

                    caches.open('smarttour-map-tiles-v2').then(cache => {
                        cache.match(realUrl, { ignoreSearch: true }).then(response => {
                            if (response) {
                                resolveImage(response);
                            } else {
                                fetch(realUrl).then(res => {
                                    if (res.ok) cache.put(realUrl, res.clone());
                                    resolveImage(res);
                                }).catch(err => {
                                    callback(new Error("Lỗi fetch: " + err)); 
                                });
                            }
                        }).catch(() => callback(new Error("Lỗi logic cache")));
                    }).catch(() => {
                        fetch(realUrl).then(res => resolveImage(res)).catch(e => callback(new Error("Lỗi fallback fetch: " + e)));
                    });
                    
                    return { cancel: () => {} };
                });
                this._protocolRegistered = true;
            }

            if (this._mbmap) {
                this._mbmap.remove();
                this._mbmap = null;
                this._mbPoiMarkers = {};
                this._mbUserMarker = null;
            }

            this._mbmap = new maplibregl.Map({
                container: 'map',
                style: {
                    version: 8,
                    sources: {
                        'osm-tiles': {
                            type: 'raster',
                            // Dùng CARTO Voyager BaseMap HD (@2x) - Miễn phí tĩnh, Nét căng và Không chặn CORS
                            tiles: ['smartmap://a.basemaps.cartocdn.com/rastertiles/voyager/{z}/{x}/{y}@2x.png'],
                            tileSize: 256,
                            attribution: '© <a href="https://carto.com/attributions">CARTO</a>'
                        }
                    },
                    layers: [{
                        id: 'osm-layer',
                        type: 'raster',
                        source: 'osm-tiles',
                        minzoom: 0,
                        maxzoom: 19
                    }]
                },
                center: [lng, lat],
                zoom: zoom || 15,
                attributionControl: false
            });

            this._mbmap.addControl(new maplibregl.NavigationControl(), 'bottom-right');

            // Force resolve sau 5 giây nếu MapLibre kẹt không trigger "load"
            const mapLoadTimer = setTimeout(() => {
                console.warn('[mapHelper] Tự động Mở khóa Loading WebGL do Timeout (Offline/Lỗi Mạng nặng)');
                this._mode = 'mapbox';
                resolve(true);
            }, 6000);

            this._mbmap.on('load', () => {
                clearTimeout(mapLoadTimer);
                this._mode = 'mapbox';
                console.log('[mapHelper] ✅ MapLibre GL Ready');
                resolve(true);
            });

            this._mbmap.on('error', (e) => {
                console.warn('[mapHelper] MapLibre error / 404 cache:', e);
                // Vẫn mở khoá để người dùng tương tác các chức năng Text/Audio offline
                clearTimeout(mapLoadTimer);
                this._mode = 'mapbox';
                resolve(true);
            });
        });
    },

    _mbUpdateUser: function (lat, lng) {
        if (!this._mbmap) return;
        if (this._mbUserMarker) {
            this._mbUserMarker.setLngLat([lng, lat]);
            return;
        }
        const el = document.createElement('div');
        el.style.cssText = 'width:24px;height:24px;border-radius:50%;background:#00C853;border:3px solid #fff;box-shadow:0 0 0 4px rgba(0,200,83,0.3);animation:pulse 2s infinite;';
        this._mbUserMarker = new maplibregl.Marker({ element: el }).setLngLat([lng, lat]).addTo(this._mbmap);
    },

    _mbRenderPOIs: function () {
        if (!this._mbmap) return;

        // Xóa markers cũ không còn trong danh sách
        const newIds = new Set(this._pois.map(p => p.id));
        Object.keys(this._mbPoiMarkers).forEach(id => {
            if (!newIds.has(+id)) {
                this._mbPoiMarkers[id].remove();
                delete this._mbPoiMarkers[id];
            }
        });

        this._pois.forEach(poi => {
            const closest = poi.id === this._lastClosestId;
            const selected = poi.id === this._lastSelectedId;
            const color = closest ? '#FF5722' : '#0e6eb8';
            const size = closest ? 20 : selected ? 16 : 12;
            const border = selected ? '3px solid #fff' : '2px solid #fff';

            if (this._mbPoiMarkers[poi.id]) {
                // Cập nhật style
                const el = this._mbPoiMarkers[poi.id].getElement();
                el.style.backgroundColor = color;
                el.style.width = size + 'px';
                el.style.height = size + 'px';
            } else {
                const el = document.createElement('div');
                el.style.cssText = `width:${size}px;height:${size}px;border-radius:50%;background:${color};border:${border};box-shadow:0 2px 6px rgba(0,0,0,0.3);cursor:pointer;transition:transform 0.15s;`;
                el.addEventListener('click', () => {
                    if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnMarkerClick', poi.id);
                });
                el.addEventListener('mouseenter', () => el.style.transform = 'scale(1.3)');
                el.addEventListener('mouseleave', () => el.style.transform = 'scale(1)');

                this._mbPoiMarkers[poi.id] = new maplibregl.Marker({ element: el })
                    .setLngLat([poi.longitude, poi.latitude])
                    .addTo(this._mbmap);
            }
        });
    },

    _mbFitBounds: function () {
        if (!this._mbmap || !this._pois.length) return;
        const lngs = this._pois.map(p => p.longitude);
        const lats = this._pois.map(p => p.latitude);
        this._mbmap.fitBounds(
            [[Math.min(...lngs), Math.min(...lats)], [Math.max(...lngs), Math.max(...lats)]],
            { padding: 50, maxZoom: 16, duration: 800 }
        );
    },

    // ─────────────────────────────────────────────
    //  Misc
    // ─────────────────────────────────────────────
    onDetailClick: function (id) { window.location.href = '/poi-details/' + id; },
    onPlayClick: function (id) {
        if (this.dotNetRef) this.dotNetRef.invokeMethodAsync('OnPlayClick', id);
    },

    /**
     * Pre-fetch OSM tiles cho Bounding Box dùng Caches API nội bộ
     * (Vì Webview trên MAUI không hỗ trợ Service Worker tốt)
     */
    prefetchTilesForBox: async function (minLat, maxLat, minLng, maxLng) {
        console.log(`[mapHelper] Bắt đầu tải Map Tiles Offline vào Cache: [${minLat}, ${minLng}] -> [${maxLat}, ${maxLng}]`);
        
        try {
            const cache = await caches.open('smarttour-map-tiles-v2');
            let fetched = 0;
            const minZoom = 12; // Chỉ cần quét từ 12 để nhẹ hơn
            const maxZoom = 16; // Tới 16 để phóng to thấy đường nhỏ

            // Hàm chuyển đổi
            const latLngToTile = (lat, lng, zoom) => {
                const n = Math.pow(2, zoom);
                const x = Math.floor((lng + 180) / 360 * n);
                const latRad = lat * Math.PI / 180;
                const y = Math.floor((1 - Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) / Math.PI) / 2 * n);
                return { x: Math.max(0, x), y: Math.max(0, y) };
            };

            for (let zoom = minZoom; zoom <= maxZoom; zoom++) {
                const tileTL = latLngToTile(maxLat, minLng, zoom);
                const tileBR = latLngToTile(minLat, maxLng, zoom);
                for (let x = tileTL.x; x <= tileBR.x; x++) {
                    for (let y = tileTL.y; y <= tileBR.y; y++) {
                        // Ảnh Map chuẩn xác
                        const url = `https://a.basemaps.cartocdn.com/rastertiles/voyager/${zoom}/${x}/${y}@2x.png`;
                        try {
                            const cached = await cache.match(url, { ignoreSearch: true });
                            if (!cached) {
                                const response = await fetch(url);
                                if (response.ok) {
                                    await cache.put(url, response.clone());
                                }
                                fetched++;
                            }
                        } catch (e) { /* Lướt qua lỗi đi tiếp */ }
                    }
                }
            }
            console.log(`[mapHelper] Báo cáo: Tải thành công ${fetched} mảnh bản đồ mới vào kho chứa của WebView!`);
            return true;
        } catch (e) {
            console.warn('[mapHelper] Lỗi pre-fetch tiles:', e);
            return false;
        }
    }
};

// CSS animation cho user marker của Mapbox
(function () {
    const style = document.createElement('style');
    style.textContent = '@keyframes pulse{0%{box-shadow:0 0 0 0 rgba(0,200,83,0.5)}70%{box-shadow:0 0 0 10px rgba(0,200,83,0)}100%{box-shadow:0 0 0 0 rgba(0,200,83,0)}}';
    document.head.appendChild(style);
})();
