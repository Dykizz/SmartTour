/**
 * SmartTour - Service Worker: OSM Tile Cache
 * Cache tiles OpenStreetMap khi online → phục vụ lại khi offline
 */
const CACHE_NAME = 'smarttour-map-tiles-v1';
const TILE_HOSTS = [
    'tile.openstreetmap.org',
];

self.addEventListener('install', (e) => {
    console.log('[SW] Installed tile cache worker');
    self.skipWaiting();
});

self.addEventListener('activate', (e) => {
    e.waitUntil(
        caches.keys().then(keys =>
            Promise.all(keys
                .filter(k => k.startsWith('smarttour-map-tiles-') && k !== CACHE_NAME)
                .map(k => caches.delete(k))
            )
        )
    );
    self.clients.claim();
});

self.addEventListener('fetch', (e) => {
    const url = new URL(e.request.url);

    // Chỉ cache request tile map từ OSM
    const isTileRequest = TILE_HOSTS.some(h => url.hostname.includes(h))
        && url.pathname.match(/\/\d+\/\d+\/\d+\.png$/);

    if (!isTileRequest) return; // Không phải tile → bỏ qua

    e.respondWith(
        caches.open(CACHE_NAME).then(async cache => {
            // Kiểm tra cache trước
            const cached = await cache.match(e.request);
            if (cached) return cached;

            // Nếu có mạng, tải mới và lưu cache
            try {
                const response = await fetch(e.request, { mode: 'cors' });
                if (response.ok) {
                    cache.put(e.request, response.clone());
                }
                return response;
            } catch {
                // Offline, không có cache → trả placeholder tile trong suốt
                return new Response(
                    '<svg xmlns="http://www.w3.org/2000/svg" width="256" height="256"><rect width="256" height="256" fill="#e8f4f8"/><text x="50%" y="50%" text-anchor="middle" fill="#aaa" font-size="12" dy=".3em">Offline</text></svg>',
                    { headers: { 'Content-Type': 'image/svg+xml' } }
                );
            }
        })
    );
});

// ── Pre-fetch tiles cho khu vực ──
// Được gọi từ C# khi download Offline Pack: postMessage({type:'PREFETCH_TILES', ...})
self.addEventListener('message', async (e) => {
    if (e.data && e.data.type === 'PREFETCH_TILES') {
        const { minLat, minLng, maxLat, maxLng, minZoom = 10, maxZoom = 15 } = e.data;
        console.log(`[SW] Prefetching tiles for BoundingBox: [${minLat}, ${minLng}] to [${maxLat}, ${maxLng}] for Zoom:${minZoom}-${maxZoom}`);
        await prefetchTilesForBox(minLat, minLng, maxLat, maxLng, minZoom, maxZoom);
        e.source.postMessage({ type: 'PREFETCH_DONE' });
    }
});

async function prefetchTilesForBox(minLat, minLng, maxLat, maxLng, minZoom, maxZoom) {
    const cache = await caches.open(CACHE_NAME);
    let fetched = 0;

    for (let zoom = minZoom; zoom <= maxZoom; zoom++) {
        // Tile góc Tây Bắc (Top-Left)
        const tileTL = latLngToTile(maxLat, minLng, zoom);
        // Tile góc Đông Nam (Bottom-Right)
        const tileBR = latLngToTile(minLat, maxLng, zoom);

        for (let x = tileTL.x; x <= tileBR.x; x++) {
            for (let y = tileTL.y; y <= tileBR.y; y++) {
                const url = `https://tile.openstreetmap.org/${zoom}/${x}/${y}.png`;
                try {
                    const cached = await cache.match(url);
                    if (!cached) {
                        await fetch(url, { mode: 'cors' }).then(r => {
                            if (r.ok) cache.put(url, r);
                        });
                        fetched++;
                        if (fetched % 20 === 0)
                            console.log(`[SW] Prefetched ${fetched} tiles...`);
                    }
                } catch { /* bỏ qua tile lỗi */ }
            }
        }
    }
    console.log(`[SW] Done! Total ${fetched} new tiles cached.`);
}

function latLngToTile(lat, lng, zoom) {
    const n = Math.pow(2, zoom);
    const x = Math.floor((lng + 180) / 360 * n);
    const latRad = lat * Math.PI / 180;
    const y = Math.floor((1 - Math.log(Math.tan(latRad) + 1 / Math.cos(latRad)) / Math.PI) / 2 * n);
    return { x: Math.max(0, x), y: Math.max(0, y) };
}
