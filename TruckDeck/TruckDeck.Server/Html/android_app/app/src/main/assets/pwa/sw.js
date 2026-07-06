const CACHE_NAME = 'truck-deck-shell-v7';
const ASSETS = [
    'index.html',
    'style.css',
    'app.js',
    'manifest.json'
];

self.addEventListener('install', (event) => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(CACHE_NAME).then((cache) => cache.addAll(ASSETS))
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys().then((keys) =>
            Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k)))
        ).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // Only handle same-origin GET requests (the shell assets). Everything else
    // - the telemetry server (config.json, skins, dashboard-host, /api, /signalr)
    // and the input bridge - must always hit the network live.
    if (event.request.method !== 'GET' || url.origin !== self.location.origin) {
        return;
    }

    event.respondWith(
        caches.match(event.request).then((cached) => cached || fetch(event.request))
    );
});
