// Doomsday Tunnels: global scoreboard and anonymous play counts.
//
// What this server stores:
//   scores  - the name a player typed, their best score, the sector reached and the date. Public.
//   daily   - one row of counters per day (plays, players, seconds played and so on).
//   reached - per day, how many runs ended in each sector.
// What it never stores: network addresses, device or browser details, or any identifier for a
// player or device. Counting "players per day" works because the game itself remembers, on the
// device only, whether it has already played today, and says just "first play today: yes/no".

import { isRude } from './words.js';
import { ADMIN_HTML } from './admin.js';

const ALLOWED_ORIGINS = new Set(['https://acechase.github.io']);
const MAX_NAME = 12;          // characters
const TOP_N = 20;             // rows returned for the scoreboard
const MAX_PER_SECTOR = 6000;  // no honest run scores more than this per sector reached
const TOKEN_MAX_AGE = 12 * 3600 * 1000;

const today = () => new Date().toISOString().slice(0, 10);
const clampInt = (v, lo, hi) => { v = Math.floor(Number(v)); return Number.isFinite(v) ? Math.floor(Math.min(hi, Math.max(lo, v))) : lo; };

function json(body, status, headers, maxAge) {
  return new Response(JSON.stringify(body), { status: status || 200, headers: Object.assign({
    'Content-Type': 'application/json; charset=utf-8',
    'Cache-Control': maxAge ? 'public, max-age=' + maxAge : 'no-store'
  }, headers || {}) });
}

const devMode = env => env.DEV === '1';
const originOk = (origin, env) => ALLOWED_ORIGINS.has(origin) || devMode(env);
function corsHeaders(origin, env) {
  if (!originOk(origin, env)) return {};
  return { 'Access-Control-Allow-Origin': devMode(env) ? '*' : origin, 'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
    'Access-Control-Allow-Headers': 'Content-Type', 'Access-Control-Max-Age': '86400', Vary: 'Origin' };
}

/* ---------- database ---------- */
let schemaReady = null;
function ensureSchema(env) {
  if (!schemaReady) {
    const q = s => env.DB.prepare(s);
    schemaReady = env.DB.batch([
      q(`CREATE TABLE IF NOT EXISTS scores (name_key TEXT PRIMARY KEY, name TEXT NOT NULL, score INTEGER NOT NULL, sector INTEGER NOT NULL, day TEXT NOT NULL)`),
      q(`CREATE INDEX IF NOT EXISTS scores_by_score ON scores (score DESC)`),
      q(`CREATE TABLE IF NOT EXISTS daily (day TEXT PRIMARY KEY, plays INTEGER NOT NULL DEFAULT 0, players INTEGER NOT NULL DEFAULT 0,
          new_players INTEGER NOT NULL DEFAULT 0, touch_plays INTEGER NOT NULL DEFAULT 0, tilt_plays INTEGER NOT NULL DEFAULT 0,
          finished INTEGER NOT NULL DEFAULT 0, seconds INTEGER NOT NULL DEFAULT 0, sectors_cleared INTEGER NOT NULL DEFAULT 0,
          finales INTEGER NOT NULL DEFAULT 0, scores_posted INTEGER NOT NULL DEFAULT 0)`),
      q(`CREATE TABLE IF NOT EXISTS reached (day TEXT NOT NULL, sector INTEGER NOT NULL, runs INTEGER NOT NULL DEFAULT 0, PRIMARY KEY (day, sector))`),
      q(`CREATE TABLE IF NOT EXISTS meta (k TEXT PRIMARY KEY, v TEXT NOT NULL)`)
    ]).catch(e => { schemaReady = null; throw e; });
  }
  return schemaReady;
}

/* ---------- run tokens ----------
   Starting a run hands the game a signed timestamp. A score is only accepted with one, and only
   if enough time has passed to have plausibly reached the sector claimed. The token names
   nobody: it is just "a run began at this moment". */
let signKey = null;
async function getSignKey(env) {
  if (signKey) return signKey;
  let row = await env.DB.prepare(`SELECT v FROM meta WHERE k = 'sign_key'`).first();
  if (!row) {
    const fresh = [...crypto.getRandomValues(new Uint8Array(32))].map(b => b.toString(16).padStart(2, '0')).join('');
    await env.DB.prepare(`INSERT OR IGNORE INTO meta (k, v) VALUES ('sign_key', ?1)`).bind(fresh).run();
    row = await env.DB.prepare(`SELECT v FROM meta WHERE k = 'sign_key'`).first();
  }
  signKey = await crypto.subtle.importKey('raw', new TextEncoder().encode(row.v), { name: 'HMAC', hash: 'SHA-256' }, false, ['sign']);
  return signKey;
}
async function signature(env, text) {
  const sig = await crypto.subtle.sign('HMAC', await getSignKey(env), new TextEncoder().encode(text));
  return [...new Uint8Array(sig)].slice(0, 16).map(b => b.toString(16).padStart(2, '0')).join('');
}
async function makeToken(env) { const ts = Date.now().toString(36); return ts + '.' + await signature(env, ts); }
async function tokenTime(env, token) {            // start time in ms, or 0 if the token is not ours or too old
  if (typeof token !== 'string' || token.length > 64) return 0;
  const [ts, sig] = token.split('.');
  if (!ts || !sig || sig !== await signature(env, ts)) return 0;
  const t = parseInt(ts, 36), age = Date.now() - t;
  return age >= 0 && age < TOKEN_MAX_AGE ? t : 0;
}

/* ---------- helpers ---------- */
async function readJson(request) {
  try { const text = await request.text(); if (text.length > 600) return null; const b = JSON.parse(text); return b && typeof b === 'object' ? b : null; }
  catch (_) { return null; }
}
async function limited(limiter, request) {
  if (!limiter) return false;
  try { const { success } = await limiter.limit({ key: request.headers.get('CF-Connecting-IP') || 'unknown' }); return !success; }
  catch (_) { return false; }
}
function cleanName(raw) {
  if (typeof raw !== 'string') return null;
  const s = raw.normalize('NFKC').replace(/\s+/g, ' ').trim();
  if (!s || [...s].length > MAX_NAME) return null;
  return /^[\p{L}\p{N} ._\-!?']+$/u.test(s) ? s : null;
}
const topScores = async env =>
  (await env.DB.prepare(`SELECT name, score, sector, day FROM scores ORDER BY score DESC, day ASC LIMIT ?1`).bind(TOP_N).all()).results;

/* ---------- play counts ---------- */
async function postEvent(request, env, cors) {
  const b = await readJson(request);
  if (!b) return json({ error: 'bad request' }, 400, cors);
  if (await limited(env.EVENT_LIMIT, request)) return json({ error: 'slow down' }, 429, cors);
  const day = today(), counted = b.count !== false;      // the game sends count:false when the browser asks not to be tracked

  if (b.t === 'start') {
    if (counted) await env.DB.prepare(
      `INSERT INTO daily (day, plays, players, new_players, touch_plays, tilt_plays) VALUES (?1, 1, ?2, ?3, ?4, ?5)
       ON CONFLICT(day) DO UPDATE SET plays = plays + 1, players = players + ?2, new_players = new_players + ?3,
         touch_plays = touch_plays + ?4, tilt_plays = tilt_plays + ?5`
    ).bind(day, b.firstToday ? 1 : 0, b.firstEver ? 1 : 0, b.touch ? 1 : 0, b.tilt ? 1 : 0).run();
    return json({ token: await makeToken(env) }, 200, cors);
  }

  if (b.t === 'end') {
    const t0 = await tokenTime(env, b.token);
    if (!t0) return json({ error: 'token' }, 400, cors);
    if (counted) {
      const elapsed = (Date.now() - t0) / 1000;
      const sector = clampInt(b.sector, 1, 99), cleared = clampInt(b.cleared, 0, sector), secs = clampInt(b.secs, 0, Math.min(elapsed + 30, 4 * 3600));
      await env.DB.batch([
        env.DB.prepare(
          `INSERT INTO daily (day, finished, seconds, sectors_cleared, finales) VALUES (?1, 1, ?2, ?3, ?4)
           ON CONFLICT(day) DO UPDATE SET finished = finished + 1, seconds = seconds + ?2, sectors_cleared = sectors_cleared + ?3, finales = finales + ?4`
        ).bind(day, secs, cleared, sector > 5 ? 1 : 0),
        env.DB.prepare(`INSERT INTO reached (day, sector, runs) VALUES (?1, ?2, 1) ON CONFLICT(day, sector) DO UPDATE SET runs = runs + 1`).bind(day, Math.min(sector, 30))
      ]);
    }
    return json({ ok: true }, 200, cors);
  }
  return json({ error: 'bad request' }, 400, cors);
}

/* ---------- scoreboard ---------- */
async function postScore(request, env, cors) {
  const b = await readJson(request);
  if (!b) return json({ error: 'bad request' }, 400, cors);
  if (await limited(env.SCORE_LIMIT, request)) return json({ error: 'slow down' }, 429, cors);

  const name = cleanName(b.name);
  if (!name) return json({ error: 'name', message: 'Use up to ' + MAX_NAME + ' letters or numbers.' }, 422, cors);
  if (isRude(name)) return json({ error: 'name', message: 'Please pick a different name.' }, 422, cors);

  const score = Math.floor(Number(b.score)), sector = Math.floor(Number(b.sector));
  if (!(score > 0) || !(sector >= 1 && sector <= 500) || score > sector * MAX_PER_SECTOR) return json({ error: 'score' }, 422, cors);
  const t0 = await tokenTime(env, b.token);
  if (!t0) return json({ error: 'token' }, 422, cors);
  if ((Date.now() - t0) / 1000 < 5 + (sector - 1) * 20) return json({ error: 'score' }, 422, cors);    // nobody clears a sector in under 20 seconds

  const key = name.toLowerCase(), day = today();
  await env.DB.batch([       // one row per name, holding that name's best score
    env.DB.prepare(
      `INSERT INTO scores (name_key, name, score, sector, day) VALUES (?1, ?2, ?3, ?4, ?5)
       ON CONFLICT(name_key) DO UPDATE SET name = excluded.name, score = excluded.score, sector = excluded.sector, day = excluded.day
       WHERE excluded.score > scores.score`
    ).bind(key, name, score, sector, day),
    env.DB.prepare(`INSERT INTO daily (day, scores_posted) VALUES (?1, 1) ON CONFLICT(day) DO UPDATE SET scores_posted = scores_posted + 1`).bind(day)
  ]);
  const best = (await env.DB.prepare(`SELECT score FROM scores WHERE name_key = ?1`).bind(key).first()).score;
  const rank = (await env.DB.prepare(`SELECT COUNT(*) + 1 AS r FROM scores WHERE score > ?1`).bind(best).first()).r;
  return json({ ok: true, name, best, rank, top: await topScores(env) }, 200, cors);
}

/* ---------- owner's page ---------- */
function adminAllowed(request, env) {
  const key = env.ADMIN_KEY, got = (request.headers.get('Authorization') || '').replace(/^Bearer\s+/i, '');
  if (!key || got.length !== key.length) return false;
  let diff = 0; for (let i = 0; i < key.length; i++) diff |= key.charCodeAt(i) ^ got.charCodeAt(i);
  return diff === 0;
}
async function adminData(env) {
  const all = s => env.DB.prepare(s).all().then(r => r.results);
  const [daily, reached, scores, totals] = await Promise.all([
    all(`SELECT * FROM daily ORDER BY day DESC LIMIT 90`),
    all(`SELECT sector, SUM(runs) AS runs FROM reached WHERE day >= date('now', '-30 days') GROUP BY sector ORDER BY sector`),
    all(`SELECT name_key, name, score, sector, day FROM scores ORDER BY day DESC, score DESC LIMIT 300`),
    env.DB.prepare(`SELECT COALESCE(SUM(plays), 0) AS plays, COALESCE(SUM(new_players), 0) AS new_players, COALESCE(SUM(seconds), 0) AS seconds,
      COALESCE(SUM(finished), 0) AS finished, COALESCE(SUM(finales), 0) AS finales, COALESCE(SUM(scores_posted), 0) AS scores_posted FROM daily`).first()
  ]);
  return { today: today(), daily, reached, scores, totals };
}

export default {
  async fetch(request, env) {
    const url = new URL(request.url), path = url.pathname, method = request.method;
    const origin = request.headers.get('Origin') || '', cors = corsHeaders(origin, env);
    if (method === 'OPTIONS') return new Response(null, { status: 204, headers: cors });
    try {
      if (path === '/') return new Response('Doomsday Tunnels scoreboard. The game lives at https://acechase.github.io/doomsday-tunnels/\n', { headers: { 'Content-Type': 'text/plain; charset=utf-8' } });
      if (path === '/admin' && method === 'GET') return new Response(ADMIN_HTML, { headers: { 'Content-Type': 'text/html; charset=utf-8', 'Cache-Control': 'no-store', 'X-Robots-Tag': 'noindex', 'Referrer-Policy': 'no-referrer' } });
      await ensureSchema(env);

      if (path === '/scores' && method === 'GET') return json({ top: await topScores(env) }, 200, cors, 15);
      if ((path === '/scores' || path === '/event') && method === 'POST') {
        if (!originOk(origin, env)) return json({ error: 'forbidden' }, 403);      // only the game's own page may post
        return path === '/scores' ? await postScore(request, env, cors) : await postEvent(request, env, cors);
      }

      if (path.startsWith('/admin/')) {
        if (!env.ADMIN_KEY) return json({ error: 'ADMIN_KEY is not set on this Worker yet.' }, 503);
        if (!adminAllowed(request, env)) return json({ error: 'wrong key' }, 401);
        if (path === '/admin/data' && method === 'GET') return json(await adminData(env));
        if (path === '/admin/scores' && method === 'DELETE') {
          await env.DB.prepare(`DELETE FROM scores WHERE name_key = ?1`).bind(url.searchParams.get('name') || '').run();
          return json({ ok: true });
        }
      }
      return json({ error: 'not found' }, 404, cors);
    } catch (e) {
      console.error(e);
      return json({ error: 'server error' }, 500, cors);
    }
  }
};
