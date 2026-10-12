// Doomsday Tunnels: offline support.
// Keeps a copy of the game, its fonts and icons, the manual and the changelog on the device, so the game
// opens and plays with no connection. It never touches requests to other sites (the scoreboard server),
// which simply fail while offline; the game handles that itself.
// Icons, fonts, images and the manifest are served from the saved copy without checking the network, so when one of
// them changes, raise this number: that makes every device fetch the whole set again.
const CACHE = 'doomsday-tunnels-v2';
const FILES = ['./', 'manifest.webmanifest', 'icon-180.png', 'icon-192.png', 'icon-512.png',
  'fonts/ShareTechMono-Regular.woff', 'fonts/Syncopate-Bold.woff', 'about/', 'about/fork.webp', 'changelog/'];
const SCOPE = new URL('./', self.location).pathname;

self.addEventListener('install', e => {
  e.waitUntil(caches.open(CACHE)
    .then(c => Promise.all(FILES.map(f => c.add(new Request(f, { cache: 'no-cache' })).catch(() => {}))))   // one missing file must not block the rest
    .then(() => self.skipWaiting()));
});
self.addEventListener('activate', e => {
  e.waitUntil(caches.keys()
    .then(keys => Promise.all(keys.filter(k => k.startsWith('doomsday-tunnels-') && k !== CACHE).map(k => caches.delete(k))))
    .then(() => self.clients.claim()));
});

const keep = (req, res) => { if (res && res.ok && !res.redirected) { const copy = res.clone(); caches.open(CACHE).then(c => c.put(req, copy)).catch(() => {}); } return res; };
const saved = req => caches.match(req, { ignoreSearch: true, ignoreVary: true });

// The game: show the saved copy at once, so it never sits on a blank screen waiting for a slow connection, and
// fetch the latest version in the background for next time. If that version is different, tell the open page, which
// restarts itself if it is still on the title screen. With nothing saved yet, wait for the network.
// The manual and the changelog are for reading, and a saved copy of those is always one visit out of date. So they
// ask the network first and fall back to the saved copy only with no connection, or when the answer takes too long.
const READ_WAIT = 2500;                              // how long a reading page waits for the network before using its saved copy
const isGame = path => path === SCOPE || path === SCOPE + 'index.html';
const stamp = res => (res && (res.headers.get('etag') || res.headers.get('last-modified'))) || '';
async function refresh(req, had) {
  const stop = new AbortController(), timer = setTimeout(() => stop.abort(), 10000);
  try {
    const res = await fetch(req.url, { cache: 'no-cache', signal: stop.signal });
    if (!res.ok || res.redirected) return null;
    await (await caches.open(CACHE)).put(req, res.clone());
    if (had && stamp(had) && stamp(res) && stamp(had) !== stamp(res)) announce(new URL(req.url).pathname);
    return res;
  } catch (_) { return null; }
  finally { clearTimeout(timer); }
}
async function announce(path) {
  for (let i = 0; i < 4; i++) {                      // the page may still be starting up: look for it a few times
    await new Promise(r => setTimeout(r, 500));
    const open = (await self.clients.matchAll({ type: 'window' })).filter(c => new URL(c.url).pathname === path);
    if (open.length) { open.forEach(c => c.postMessage({ type: 'updated' })); return; }
  }
}
async function page(e) {
  const req = e.request, had = await saved(req), fresh = refresh(req, had);
  if (had && isGame(new URL(req.url).pathname)) { e.waitUntil(fresh); return had; }
  if (had) { e.waitUntil(fresh); return (await Promise.race([fresh, new Promise(r => setTimeout(r, READ_WAIT, null))])) || had; }
  return (await fresh) || (await saved(SCOPE)) || Response.error();
}
// Fonts, icons and images never change under the same name: use the saved copy, fetch only if there is none.
async function asset(req) { return (await saved(req)) || keep(req, await fetch(req)); }

self.addEventListener('fetch', e => {
  const req = e.request, url = new URL(req.url);
  if (req.method !== 'GET' || url.origin !== self.location.origin || !url.pathname.startsWith(SCOPE)) return;
  if (url.pathname.endsWith('/sw.js')) return;
  const isPage = req.mode === 'navigate' || url.pathname.endsWith('/') || url.pathname.endsWith('.html');
  e.respondWith(isPage ? page(e) : asset(req));
});
