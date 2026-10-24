// Inlined into the exported shell so a cached helper cannot hide a new release.
function createParkingUpdates(build) {
  const state = { build, latest: build, status: 'checking', error: '', check, reload, prepare };
  let pending;
  const base = new URL('./', location.href);
  const fresh = () => Date.now().toString(36) + Math.random().toString(36).slice(2);
  async function check() {
    if (pending) return pending;
    pending = (async () => {
      try {
        const url = new URL('version.json', base);
        url.searchParams.set('_fresh', fresh());
        const response = await fetch(url, { cache: 'no-store', signal: AbortSignal.timeout(12000) });
        if (!response.ok) throw new Error('Update check returned ' + response.status);
        const version = await response.json();
        if (!version || typeof version.build !== 'string' || !/^[a-zA-Z0-9-]{1,100}$/.test(version.build)) throw new Error('Invalid release identifier');
        state.latest = version.build;
        state.status = version.build === build ? 'current' : 'available';
        state.error = '';
      } catch (error) {
        state.error = String(error.message || error);
        // Keep an already discovered update available during a temporary outage.
        if (state.status !== 'available') state.status = 'unreachable';
      } finally { pending = null; }
      return state;
    })();
    return pending;
  }
  function reload() {
    const url = new URL(location.href);
    url.searchParams.set('_parking_build', state.latest);
    url.searchParams.set('_fresh', fresh());
    location.replace(url);
  }
  async function prepare() {
    await check();
    if (state.status === 'unreachable') throw new Error('Unable to check the latest game version. Check your connection and reload.');
    if (state.status === 'available') {
      // An edge cache may temporarily return old HTML. Never silently start it,
      // and bound retries so a partial publication cannot cause a reload loop.
      const url = new URL(location.href);
      const attempts = Number(url.searchParams.get('_parking_retry') || 0);
      if (attempts >= 2) throw new Error('The new version is still being published. Please reload in a moment.');
      url.searchParams.set('_parking_retry', String(attempts + 1));
      url.searchParams.set('_parking_build', state.latest);
      url.searchParams.set('_fresh', fresh());
      location.replace(url);
      return false;
    }
    const url = new URL(location.href);
    url.searchParams.set('_parking_build', build);
    url.searchParams.delete('_parking_retry');
    history.replaceState(null, '', url);
    await ensureWorker();
    // Active games are never reloaded by a background check.
    setInterval(check, 60000);
    addEventListener('online', check);
    addEventListener('focus', check);
    document.addEventListener('visibilitychange', () => { if (!document.hidden) check(); });
    return true;
  }
  async function ensureWorker() {
    if (!('serviceWorker' in navigator)) throw new Error('This browser needs service worker support to run the game.');
    const registration = await navigator.serviceWorker.register(new URL('index.service.worker.js', base), { scope: base.pathname, updateViaCache: 'none' });
    await registration.update();
    // ready alone can resolve to the legacy worker. Wait for our protocol so
    // the old cache cannot supply any executable or game data during migration.
    const deadline = Date.now() + 15000;
    while (Date.now() < deadline) {
      if (await isCurrentWorker(navigator.serviceWorker.controller)) return;
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    throw new Error('Browser support could not finish updating. Please reload.');
  }
  function isCurrentWorker(worker) {
    if (!worker) return Promise.resolve(false);
    return new Promise(resolve => {
      const channel = new MessageChannel();
      const finish = value => { clearTimeout(timer); channel.port1.close(); resolve(value); };
      const timer = setTimeout(() => finish(false), 400);
      channel.port1.onmessage = event => finish(event.data?.protocol === 2);
      worker.postMessage({ type: 'parking-protocol' }, [channel.port2]);
    });
  }
  return state;
}
