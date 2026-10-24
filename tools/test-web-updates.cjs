// Real-browser lifecycle regressions; a tiny Engine fixture isolates deployment
// behavior from the unrelated 100 MB game runtime.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const { chromium } = require(path.join(root, '.cache/web-tests/node_modules/playwright'));
const client = fs.readFileSync(path.join(root, 'web/update-client.js'), 'utf8');
const worker = fs.readFileSync(path.join(root, 'web/service-worker.js'), 'utf8');
const shell = fs.readFileSync(path.join(root, 'web/shell.html'), 'utf8');
async function fixture(run) {
  let release = 'releaseA', unavailable = false, staleHtml = false, legacy = false;
  const requests = [];
  const server = http.createServer((req, res) => {
    const url = new URL(req.url, 'http://localhost');
    requests.push(url);
    res.setHeader('Cache-Control', 'public, max-age=600');
    if (url.pathname === '/game/version.json') {
      res.setHeader('Content-Type', 'application/json');
      res.statusCode = unavailable ? 503 : 200;
      res.end(JSON.stringify({ build: release }));
    } else if (url.pathname === '/game/index.service.worker.js') {
      res.setHeader('Content-Type', 'text/javascript');
      res.end(legacy ? `self.addEventListener('install',e=>e.waitUntil(caches.open('Parking Practice-sw-cache-legacy').then(c=>c.addAll(['./index.html','./index.js'])))); self.addEventListener('fetch',e=>e.respondWith(caches.match(e.request).then(r=>r||fetch(e.request))));` : worker);
    } else if (url.pathname === '/game/' || url.pathname === '/game/index.html') {
      res.setHeader('Content-Type', 'text/html');
      if (legacy) {
        res.end(`<script src="index.js"></script><script>navigator.serviceWorker.register('index.service.worker.js');</script>`);
      } else {
        const build = staleHtml && (staleHtml === 'always' || !url.searchParams.has('_fresh')) ? 'releaseA' : release;
        res.end(shell.replace('__PARKING_UPDATE_CLIENT__', () => client)
          .replaceAll('__PARKING_BUILD__', build)
          .replace('$GODOT_PROJECT_NAME', 'Test game').replace('$GODOT_HEAD_INCLUDE', '')
          .replace('$GODOT_CONFIG', JSON.stringify({ executable: 'game-' + build, serviceWorker: 'index.service.worker.js' }))
          .replace('$GODOT_URL', 'game-' + build + '.js'));
      }
    } else if (/\/game\/game-[\w-]+\.js$/.test(url.pathname)) {
      res.setHeader('Content-Type', 'text/javascript');
      const build = url.pathname.match(/game-([\w-]+)\.js$/)[1];
      res.end(`class Engine { constructor(config){this.config=config;} static getMissingFeatures(){return crossOriginIsolated?[]:['isolation'];} async startGame(){window.startedBuild=${JSON.stringify(build)}; window.pack=await (await fetch(this.config.executable+'.pck')).text();} }`);
    } else if (url.pathname.endsWith('.pck')) {
      res.end(url.pathname);
    } else if (url.pathname === '/game/index.js') {
      res.setHeader('Content-Type', 'text/javascript'); res.end("window.startedBuild='legacy';");
    } else { res.statusCode = 404; res.end('Not found'); }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const browser = await chromium.launch({ headless: true, ...(process.env.PARKING_TEST_BROWSER ? { executablePath: process.env.PARKING_TEST_BROWSER } : {}) }).catch(error => { server.close(); throw error; });
  const context = await browser.newContext();
  const url = `http://127.0.0.1:${server.address().port}/game/`;
  try {
    await run({ context, url, requests, deploy: build => { release = build; }, outage: value => { unavailable = value; }, stale: value => { staleHtml = value; }, legacy: value => { legacy = value; } });
  } finally { await context.close(); await browser.close(); await new Promise(resolve => server.close(resolve)); }
}
async function started(page, build) {
  await page.waitForFunction(expected => window.startedBuild === expected && window.pack, build, { timeout: 25000 });
  assert.equal(await page.evaluate(() => crossOriginIsolated), true);
}
test('new tab gets B while A keeps running; background checks and chosen reload', async () => fixture(async f => {
  const old = await f.context.newPage();
  await old.clock.install();
  await old.goto(f.url + '?check=1');
  await started(old, 'releaseA');
  f.deploy('releaseB');
  const fresh = await f.context.newPage();
  await fresh.goto(f.url);
  await started(fresh, 'releaseB');
  assert.match(await fresh.evaluate(() => window.pack), /releaseB/);
  assert.equal(await old.evaluate(() => window.startedBuild), 'releaseA');
  assert.match(await old.evaluate(() => window.pack), /releaseA/);
  // Exercise the actual timer; no user-triggered update check is needed.
  await old.clock.fastForward(61000);
  await old.waitForFunction(() => window.parkingUpdates.status === 'available');
  assert.equal(await old.evaluate(() => window.startedBuild), 'releaseA');
  await old.evaluate(() => window.parkingUpdates.reload());
  await started(old, 'releaseB');
  assert.equal(new URL(old.url()).searchParams.get('check'), '1');
  assert(f.requests.filter(u => u.pathname.endsWith('version.json')).every(u => u.searchParams.has('_fresh')));
}));
test('stale HTML is refreshed before any old executable loads', async () => fixture(async f => {
  f.deploy('releaseB'); f.stale(true);
  const page = await f.context.newPage();
  await page.goto(f.url);
  await started(page, 'releaseB');
  assert(!f.requests.some(u => u.pathname.endsWith('game-releaseA.js')));
}));
test('legacy cached explicit index.html migrates with an old tab left open', async () => fixture(async f => {
  f.legacy(true);
  const old = await f.context.newPage();
  await old.goto(f.url + 'index.html');
  await old.evaluate(() => Promise.race([navigator.serviceWorker.ready, new Promise((_,reject)=>setTimeout(()=>reject(new Error('Legacy worker readiness timeout')),8000))]));
  await old.reload();
  await old.waitForFunction(() => navigator.serviceWorker.controller);
  assert.equal(await old.evaluate(() => window.startedBuild), 'legacy');
  f.legacy(false); f.deploy('releaseB');
  const fresh = await f.context.newPage();
  await fresh.goto(f.url + 'index.html');
  await started(fresh, 'releaseB');
  await started(old, 'releaseB');
}));
test('failed version discovery does not silently launch a stale game', async () => fixture(async f => {
  f.outage(true);
  const page = await f.context.newPage();
  await page.goto(f.url);
  await page.waitForFunction(() => document.querySelector('#error').textContent.includes('Unable to check'));
  assert(!f.requests.some(u => /game-release.*\.js$/.test(u.pathname)));
}));
test('background outage preserves an update notice; foreground recovery checks automatically', async () => fixture(async f => {
  const page = await f.context.newPage(); await page.goto(f.url); await started(page, 'releaseA');
  f.deploy('releaseB');
  await page.evaluate(() => dispatchEvent(new Event('online')));
  await page.waitForFunction(() => window.parkingUpdates.status === 'available');
  f.outage(true);
  await page.evaluate(() => dispatchEvent(new Event('online')));
  await page.waitForFunction(() => window.parkingUpdates.error);
  assert.equal(await page.evaluate(() => window.parkingUpdates.status), 'available');
  assert.equal(await page.evaluate(() => window.startedBuild), 'releaseA');
}));

test('partial publication stops with a useful error rather than a reload loop', async () => fixture(async f => {
  f.deploy('releaseB'); f.stale('always');
  const page = await f.context.newPage(); await page.goto(f.url);
  await page.waitForFunction(() => document.querySelector('#error').textContent.includes('still being published'));
  assert.equal(f.requests.filter(u => u.pathname === '/game/').length, 3);
  assert(!f.requests.some(u => /game-release.*\.js$/.test(u.pathname)));
}));
