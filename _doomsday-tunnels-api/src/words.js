// Names that are not allowed on the public scoreboard.
// The lists are stored ROT13-encoded so this file does not spell the words out.
const rot13 = s => s.replace(/[a-z]/g, c => String.fromCharCode((c.charCodeAt(0) - 84) % 26 + 97));
const ANY = 'shpx fuvg ovgpu phag avttre avttn snttbg nffubyr nefrubyr chffl juber fyhg onfgneq jnaxre cravf intvan anmv uvgyre ergneq jrgonpx genaal cbea qvyqb wvmm oybjwbo unaqwbo obyybpx qbhpur zbyrfg'.split(' ').map(rot13);      // blocked anywhere inside a word
const WHOLE = 'nff nefr pbpx pbpxf qvpx qvpxf gjng jnax encr encrq encvfg crqb cnrqb phz gvg gvgf snt sntf xvxr fcvp puvax tbbx qlxr pbba ubzb cvff cevpx fynt xxx nahf nany avt'.split(' ').map(rot13);  // blocked only as a whole word

const LEET = { 0: 'o', 1: 'i', 3: 'e', 4: 'a', 5: 's', 7: 't', 8: 'b', '@': 'a', $: 's', '!': 'i' };

// Lower-case, strip accents, undo common letter-for-number swaps, then split into words.
// A name typed as spaced-out single letters is joined back together.
function wordsOf(name) {
  const s = name.normalize('NFKD').replace(/[\u0300-\u036f]/g, '').toLowerCase().replace(/[0134578@$!]/g, c => LEET[c] || c);
  const toks = s.split(/[^a-z]+/).filter(Boolean);
  return toks.length > 1 && toks.every(t => t.length <= 2) ? [toks.join('')] : toks;
}

// Innocent words and surnames that happen to contain a blocked word.
const HARMLESS = /shii?take|[a-z]+shita$|scunthorpe/g;

// A blocked word still counts when its letters are stretched ("xyyyz" for "xyz"), but a doubled
// letter in the blocked word must be doubled in the name too, so near-misses stay allowed.
const stretchy = word => word.replace(/(.)\1*/g, (run, c) => c + (run.length > 1 ? '{' + run.length + ',}' : '+'));
const ANY_RE = ANY.map(w => new RegExp(stretchy(w)));
const WHOLE_RE = WHOLE.map(w => new RegExp('^' + stretchy(w) + '$'));

export function isRude(name) {
  for (const raw of wordsOf(name)) {
    if (WHOLE_RE.some(re => re.test(raw))) return true;
    const w = raw.replace(HARMLESS, '');
    if (ANY_RE.some(re => re.test(w))) return true;
  }
  return false;
}
