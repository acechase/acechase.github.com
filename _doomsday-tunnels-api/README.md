# Doomsday Tunnels scoreboard server

A small Cloudflare Worker that gives [Doomsday Tunnels](https://acechase.github.io/doomsday-tunnels/) a global
high-score board and anonymous play counts. It uses one D1 database and stays within Cloudflare's free plan.

This folder starts with an underscore so GitHub Pages does not publish it with the site.

## What is stored, and what is not

| Table | Holds |
| --- | --- |
| `scores` | The name a player typed, that name's best score, the sector reached, and the date. Shown publicly in the game. |
| `times` | That name's fastest time through each sector, and the date. Shown publicly in the game. Only sent along with a posted score. |
| `daily` | One row of counters per day: runs started, players, new players, seconds played, sectors cleared, and so on. |
| `reached` | Per day, how many runs ended in each sector. |
| `meta` | A random signing key the server generates for itself. |

Nothing else is kept. In particular there are no network addresses, no browser or device details, and no
identifier for a player or a device anywhere in the database.

- "Players per day" is counted without identifying anyone: the game remembers, on the device only, which day it
  last played, and tells the server just "first play today: yes or no".
- If a browser sends Do Not Track or Global Privacy Control, its plays are not counted at all.
- A score is only sent when the player chooses to post it, with the name they typed.
- The spam brake uses the caller's network address as a short-lived counter key inside Cloudflare. It is never
  written to the database.

## Deploying

From a computer with Node.js:

```sh
cd _doomsday-tunnels-api
npm install
npx wrangler deploy               # signs you in to Cloudflare and creates the database on first run
npx wrangler secret put ADMIN_KEY # choose a password for the stats page
```

`wrangler deploy` prints the server's address, which looks like
`https://doomsday-tunnels-api.<your-subdomain>.workers.dev`. That address goes into the game (the `API` constant).

If the deploy complains that the database has no ID, create it by hand and paste the ID it prints into
`wrangler.jsonc` as `"database_id"`, next to `"binding": "DB"`:

```sh
npx wrangler d1 create doomsday-tunnels
```

Alternatively, without a computer: in the Cloudflare dashboard choose Workers & Pages, Create, Import a
repository, pick this repository, name the project `doomsday-tunnels-api`, and set the root directory to
`_doomsday-tunnels-api`. Then add `ADMIN_KEY` as a secret under the Worker's Settings, Variables and Secrets.
Connected this way, Cloudflare redeploys whenever this folder changes on `master`.

## The stats page

Open `/admin` on the server's address and enter the admin key. It shows players and runs per day, where runs
end, time played, and the scoreboard names with a Remove button for cleaning up.

## How scores are checked

Scores come from the player's browser, so they can never be fully trusted. The server applies basic checks:

- a run must have been started (the server hands out a signed start time) and enough time must have passed to
  reach the sector claimed;
- the score must be within what a real run can earn by that sector;
- names are 1 to 12 letters, numbers, spaces or `. _ - ! ? '`, and are checked against a list of rude words
  (`src/words.js`), including stretched, spaced-out and number-for-letter spellings;
- posting is limited to a few tries per minute.

Sector times get the same treatment: a time faster than the sector can physically be flown is ignored, and so is a
set of times that adds up to longer than the run itself.

A determined person can still fake a score. The Remove button on the stats page is the remedy.

## Endpoints

| Method and path | Purpose |
| --- | --- |
| `GET /scores` | Top 20 names and scores. |
| `GET /times` | The three fastest names for each sector. |
| `POST /scores` | Post a score: `{ name, score, sector, token, times }`. `times` is optional: the run's time for each sector cleared, in tenths of a second. |
| `POST /event` | `{ t: "start", ... }` returns a run token; `{ t: "end", token, sector, cleared, secs }` closes a run. |
| `GET /admin` | The stats page. |
| `GET /admin/data`, `DELETE /admin/scores?name=` | Used by the stats page; need the admin key. |

Only pages on `https://acechase.github.io` may post. To allow another address, add it to `ALLOWED_ORIGINS` in
`src/index.js`.
