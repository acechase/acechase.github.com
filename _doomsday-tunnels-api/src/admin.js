// The owner's page: play counts and scoreboard clean-up. Served at /admin.
// The page itself holds no data; it asks for the admin key and then fetches /admin/data with it.
export const ADMIN_HTML = `<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="robots" content="noindex">
<title>Doomsday Tunnels mission control</title>
<style>
:root{color-scheme:dark;--void:#07060f;--panel:#110f20;--line:#2a2745;--ink:#e8f1ff;--dim:#8a94b3;--cyan:#35d9ff;--gold:#ffe14a;--warn:#ff4a2e;
  --mono:ui-monospace,"SF Mono",Menlo,Consolas,monospace}
*{box-sizing:border-box}
[hidden]{display:none!important}
body{margin:0;background:var(--void);color:var(--ink);font:14px/1.5 var(--mono)}
main,#out{display:flex;flex-direction:column;gap:28px}
main{max-width:980px;margin:0 auto;padding:24px 16px 48px}
h1{font-size:18px;letter-spacing:.12em;margin:0;text-wrap:balance}
h2{font-size:13px;letter-spacing:.12em;color:var(--dim);margin:0 0 10px;font-weight:400}
p{margin:0;color:var(--dim);max-width:70ch}
header{display:flex;flex-wrap:wrap;gap:8px 16px;align-items:baseline;justify-content:space-between}
button,input{font:inherit;color:var(--ink);background:transparent;border:1px solid var(--dim);padding:8px 12px;min-height:38px}
button{cursor:pointer}button:hover{border-color:var(--ink)}
:focus-visible{outline:2px solid var(--gold);outline-offset:2px}
#gate{display:flex;flex-wrap:wrap;gap:8px;align-items:center}#gate input{flex:1 1 220px;min-width:0}
#msg{color:var(--gold);min-height:1.5em}
.tiles{display:grid;grid-template-columns:repeat(auto-fit,minmax(150px,1fr));gap:12px}
.tile{border:1px solid var(--line);background:var(--panel);padding:12px 14px}
.tile b{display:block;font-size:26px;font-weight:400;color:var(--cyan);font-variant-numeric:tabular-nums}
.tile span{color:var(--dim);font-size:12px}
.scroll{overflow-x:auto}
table{border-collapse:collapse;width:100%;font-variant-numeric:tabular-nums}
th,td{text-align:right;padding:5px 10px;border-bottom:1px solid var(--line);white-space:nowrap}
th{color:var(--dim);font-weight:400;font-size:12px}
th:first-child,td:first-child{text-align:left}
td button{min-height:30px;padding:3px 10px;font-size:12px}
svg{display:block;width:100%;height:auto}
.bar{display:grid;grid-template-columns:5.5em 1fr 4em;gap:10px;align-items:center;margin:3px 0}
.bar i{display:block;height:12px;background:var(--cyan);min-width:1px}
.bar span:last-child{text-align:right;font-variant-numeric:tabular-nums}
.legend{display:flex;gap:18px;color:var(--dim);font-size:12px;margin-bottom:6px}
.legend i{display:inline-block;width:10px;height:10px;margin-right:6px;vertical-align:-1px}
</style>
</head>
<body>
<main>
  <header><h1>DOOMSDAY TUNNELS / MISSION CONTROL</h1><button type="button" id="signout" hidden>Forget key on this device</button></header>
  <form id="gate" autocomplete="off">
    <label for="key">Admin key</label>
    <input id="key" type="password" autocomplete="off" required>
    <button type="submit">Open</button>
  </form>
  <p id="msg" role="status"></p>
  <div id="out" hidden>
    <section><h2>AT A GLANCE</h2><div class="tiles" id="tiles"></div></section>
    <section><h2>PLAYS PER DAY, LAST 30 DAYS</h2>
      <div class="legend"><span><i style="background:var(--cyan)"></i>Runs started</span><span><i style="background:var(--gold)"></i>Players</span></div>
      <div id="chart"></div></section>
    <section><h2>WHERE RUNS END, LAST 30 DAYS</h2><div id="reached"></div></section>
    <section><h2>DAY BY DAY (DAYS WITH ACTIVITY)</h2><div class="scroll"><table id="daily"></table></div></section>
    <section><h2>SCOREBOARD NAMES</h2><p>Newest first. Remove deletes that name's entry for good.</p><div class="scroll"><table id="scores"></table></div></section>
    <section><h2>WHAT IS COUNTED</h2><p>Only daily totals. "Players" is how many devices played that day, counted by the game telling the server "first play today" once per day. No network addresses, device details or identifiers are stored. Days are in UTC.</p></section>
  </div>
</main>
<script>
(() => {
  const $ = id => document.getElementById(id), msg = $('msg');
  let key = ''; try { key = localStorage.getItem('doomsday-admin-key') || ''; } catch (_) {}
  const el = (tag, text, attrs) => { const e = document.createElement(tag); if (text != null) e.textContent = text; if (attrs) for (const k in attrs) e.setAttribute(k, attrs[k]); return e; };
  const n = v => Number(v || 0).toLocaleString('en-US');
  const call = (path, method) => fetch(path, { method: method || 'GET', headers: { Authorization: 'Bearer ' + key } });

  function tile(value, label) { const t = el('div'); t.className = 'tile'; t.append(el('b', value), el('span', label)); return t; }
  function table(node, heads, rows) {
    node.replaceChildren(); const tr = el('tr'); heads.forEach(h => tr.append(el('th', h))); node.append(tr);
    rows.forEach(r => { const row = el('tr'); r.forEach(c => { const td = el('td'); if (c instanceof Node) td.append(c); else td.textContent = c; row.append(td); }); node.append(row); });
  }
  function chart(days) {                            // days: oldest first, every day present
    const W = 900, H = 220, L = 34, B = 22, T = 8, max = Math.max(4, ...days.map(d => d.plays)), bw = (W - L) / days.length;
    const y = v => T + (H - T - B) * (1 - v / max), ns = 'http://www.w3.org/2000/svg';
    const svg = document.createElementNS(ns, 'svg'); svg.setAttribute('viewBox', '0 0 ' + W + ' ' + H); svg.setAttribute('role', 'img'); svg.setAttribute('aria-label', 'Runs started and players per day');
    const add = (tag, a, text) => { const e = document.createElementNS(ns, tag); for (const k in a) e.setAttribute(k, a[k]); if (text != null) e.textContent = text; svg.append(e); return e; };
    [0, 0.5, 1].forEach(f => { add('line', { x1: L, x2: W, y1: y(max * f), y2: y(max * f), stroke: '#2a2745' }); add('text', { x: L - 6, y: y(max * f) + 4, fill: '#8a94b3', 'font-size': 11, 'text-anchor': 'end' }, Math.round(max * f)); });
    days.forEach((d, i) => {
      const x = L + i * bw;
      if (d.plays) add('rect', { x: x + bw * 0.15, y: y(d.plays), width: bw * 0.7, height: y(0) - y(d.plays), fill: '#35d9ff' }).append(Object.assign(document.createElementNS(ns, 'title'), { textContent: d.day + ': ' + d.plays + ' runs, ' + d.players + ' players' }));
      if (d.players) add('rect', { x: x + bw * 0.15, y: y(d.players) - 1.5, width: bw * 0.7, height: 3, fill: '#ffe14a' });
      if (i % 5 === 0 || i === days.length - 1) add('text', { x: x + bw / 2, y: H - 6, fill: '#8a94b3', 'font-size': 11, 'text-anchor': 'middle' }, d.day.slice(5));
    });
    $('chart').replaceChildren(svg);
  }
  function render(d) {
    const by = {}; d.daily.forEach(r => by[r.day] = r);
    const zero = day => ({ day, plays: 0, players: 0, new_players: 0, touch_plays: 0, tilt_plays: 0, finished: 0, seconds: 0, sectors_cleared: 0, finales: 0, scores_posted: 0 });
    const days = []; for (let i = 29; i >= 0; i--) { const day = new Date(Date.parse(d.today) - i * 864e5).toISOString().slice(0, 10); days.push(by[day] || zero(day)); }
    const t = days[29], wk = days.slice(23), sum = (a, k) => a.reduce((s, r) => s + r[k], 0);
    $('tiles').replaceChildren(
      tile(n(t.players), 'players today'), tile(n(t.plays), 'runs today'),
      tile(n(sum(wk, 'plays')), 'runs, last 7 days'), tile((sum(wk, 'players') / 7).toFixed(1), 'players per day, last 7'),
      tile(n(d.totals.new_players), 'devices ever'), tile(n(d.totals.plays), 'runs ever'),
      tile((d.totals.seconds / 3600).toFixed(1), 'hours played ever'), tile(n(d.totals.finales), 'runs that beat sector 5'));
    chart(days);
    const top = Math.max(1, ...d.reached.map(r => r.runs));
    $('reached').replaceChildren(...(d.reached.length ? d.reached.map(r => { const row = el('div'); row.className = 'bar'; const bar = el('i'); bar.style.width = (r.runs / top * 100) + '%'; const wrap = el('span'); wrap.append(bar); row.append(el('span', 'Sector ' + r.sector + (r.sector >= 30 ? '+' : '')), wrap, el('span', n(r.runs))); return row; }) : [el('p', 'No finished runs yet.')]));
    table($('daily'), ['Day', 'Players', 'New', 'Runs', 'Minutes', 'Avg run', 'Sectors cleared', 'Beat sector 5', 'Scores posted', 'On touch', 'With tilt'],
      days.slice().reverse().filter(r => r.plays || r.finished || r.scores_posted).map(r => [r.day, n(r.players), n(r.new_players), n(r.plays), n(Math.round(r.seconds / 60)), r.finished ? Math.round(r.seconds / r.finished) + 's' : '-', n(r.sectors_cleared), n(r.finales), n(r.scores_posted), r.plays ? Math.round(r.touch_plays / r.plays * 100) + '%' : '-', n(r.tilt_plays)]));
    table($('scores'), ['Name', 'Score', 'Sector', 'Day', ''], d.scores.map(s => {
      const b = el('button', 'Remove', { type: 'button' });
      b.addEventListener('click', async () => {
        if (b.textContent === 'Remove') { b.textContent = 'Really remove?'; return; }
        b.disabled = true; const r = await call('/admin/scores?name=' + encodeURIComponent(s.name_key), 'DELETE');
        if (r.ok) b.closest('tr').remove(); else { b.disabled = false; b.textContent = 'Failed, try again'; }
      });
      return [s.name, n(s.score), s.sector, s.day, b];
    }));
    $('out').hidden = false; $('gate').hidden = true; $('signout').hidden = false; msg.textContent = '';
  }
  async function load() {
    msg.textContent = 'Loading...';
    try {
      const r = await call('/admin/data');
      if (r.status === 401) { msg.textContent = 'That key was not accepted.'; return; }
      if (r.status === 503) { msg.textContent = 'This Worker has no ADMIN_KEY yet. Add one under Settings, Variables and Secrets, then reload.'; return; }
      if (!r.ok) { msg.textContent = 'The server returned an error. Try again in a moment.'; return; }
      try { localStorage.setItem('doomsday-admin-key', key); } catch (_) {}
      render(await r.json());
    } catch (_) { msg.textContent = 'Could not reach the server. Check your connection and reload.'; }
  }
  $('gate').addEventListener('submit', e => { e.preventDefault(); key = $('key').value.trim(); if (key) load(); });
  $('signout').addEventListener('click', () => { try { localStorage.removeItem('doomsday-admin-key'); } catch (_) {} location.reload(); });
  if (key) load();
})();
</script>
</body>
</html>`;
