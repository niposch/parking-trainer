// Stable URL: replaces the original Godot worker, including in existing browsers.
// Executable filenames identify a release; navigation and release discovery must
// always go to the network. We retain old asset caches for still-running tabs.
const ASSET_CACHE = 'Parking Practice-versioned-assets-v2';
const LEGACY_CACHE_PREFIX = 'Parking Practice-sw-cache-';
const scope = new URL('./', self.location.href);
self.addEventListener('install', event => event.waitUntil(self.skipWaiting()));
self.addEventListener('activate', event => event.waitUntil((async () => {
  await self.clients.claim();
  // One-time migration: a tab with a legacy cached shell has no version gate.
  // New shells mark their URL before registration and are left undisturbed.
  for (const client of await self.clients.matchAll({ type: 'window' })) {
    const url = new URL(client.url);
    if (url.origin === scope.origin && url.pathname.startsWith(scope.pathname) && !url.searchParams.has('_parking_build')) {
      url.searchParams.set('_fresh', Date.now().toString());
      // Do not await navigation inside activation: its fetch waits for this
      // activate event to finish, so awaiting it would deadlock both operations.
      client.navigate(url.href).catch(() => {});
    }
  }
})()));
self.addEventListener('message', event => {
  if (event.data?.type === 'parking-protocol' && event.ports[0]) event.ports[0].postMessage({ protocol: 2 });
});
async function pruneAssets(cache, currentBuild) {
  const keep = new Set([currentBuild]);
  for (const client of await self.clients.matchAll({ type: 'window', includeUncontrolled: true })) {
    const url = new URL(client.url);
    if (url.origin !== scope.origin || !url.pathname.startsWith(scope.pathname)) continue;
    const build = url.searchParams.get('_parking_build');
    if (!build) return; // An unmarked tab may still be completing migration.
    keep.add(build);
  }
  await Promise.all((await cache.keys()).map(request => {
    const name = new URL(request.url).pathname.slice(scope.pathname.length);
    const match = /^game-([a-zA-Z0-9-]+)\./.exec(name);
    return match && !keep.has(match[1]) ? cache.delete(request) : Promise.resolve();
  }));
}
function isolated(response) {
  if (response.status === 0) return response;
  const headers = new Headers(response.headers);
  headers.set('Cross-Origin-Opener-Policy', 'same-origin');
  headers.set('Cross-Origin-Embedder-Policy', 'require-corp');
  return new Response(response.body, { status: response.status, statusText: response.statusText, headers });
}
self.addEventListener('fetch', event => {
  const request = event.request;
  const url = new URL(request.url);
  if (request.method !== 'GET' || url.origin !== scope.origin || !url.pathname.startsWith(scope.pathname)) return;
  event.respondWith((async () => {
    const name = url.pathname.slice(scope.pathname.length);
    if (request.mode === 'navigate' || name === 'version.json') {
      // A unique query also bypasses GitHub Pages' shared ten-minute edge cache.
      url.searchParams.set('_network', Date.now().toString(36) + Math.random().toString(36).slice(2));
      return isolated(await fetch(new Request(url, { headers: request.headers, credentials: request.credentials, cache: 'no-store', redirect: 'follow' })));
    }
    if (/^game-[a-zA-Z0-9-]+\.(js|wasm|pck|audio\.worklet\.js|audio\.position\.worklet\.js)$/.test(name)) {
      let cache;
      try {
        cache = await caches.open(ASSET_CACHE);
        const cached = await cache.match(request);
        if (cached) return isolated(cached);
      } catch (_) { /* Storage can be denied; network loading still works. */ }
      const response = await fetch(request);
      if (response.ok && cache) {
        // Do not make a quota/storage failure prevent a successful network load.
        event.waitUntil(cache.put(request, response.clone()).then(() => {
          const pack = /^game-([a-zA-Z0-9-]+)\.pck$/.exec(name);
          if (pack) return pruneAssets(cache, pack[1]);
        }).catch(() => {}));
      }
      return isolated(response);
    }
    // Legacy games may still request a worklet after migration. Their cache is
    // kept separate and is never consulted for HTML or versioned new assets.
    if (/^index\.(js|wasm|pck|audio\.worklet\.js|audio\.position\.worklet\.js)$/.test(name)) {
      for (const key of await caches.keys()) {
        if (!key.startsWith(LEGACY_CACHE_PREFIX)) continue;
        const cached = await (await caches.open(key)).match(request);
        if (cached) return isolated(cached);
      }
    }
    return isolated(await fetch(request));
  })());
});
