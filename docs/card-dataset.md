# The card dataset

`datasets/cards/` is the input to spec authoring and to the card port. It joins
what the designers printed on each card, what the Python engine believes each
card says, and which script implements it today.

Tracked as MARVEL-19. **Its generator no longer exists — MARVEL-252.** The
committed dataset is correct; what is missing is the ability to rebuild it.

## Why it exists

Behavioral specs have to be authored from **printed card text**. Text written by
the game's designers is authoritative in a way that an implementation is not:
infer a spec from code you only partly understand and you get a confident, wrong
spec, and then you implement the C# engine to match it.

An implementation's own card table looked like a reasonable source until it was
compared against the printed text. It is not:

- **36 cards** have lost a character to an encoding round-trip. Card 05001a
  reads `"Morphogenetics" <U+FFFD> <b>Response</b>` where the card says `—`.
- **197 cards** say something materially different from the printed text.
  `03025` Honorary Avenger is missing the line **"Max 1 per character."**
  entirely — a spec authored from the engine's copy would permit a board state
  the game forbids.
- **81 cards** have printed text that the engine stores none of.

None of those are visible unless you go and look. That is what this dataset is:
having gone and looked, once, so nobody authors 314 specs on a bad premise.

## Sources

| Source | Provides | Authority |
|---|---|---|
| `datasets/marvelsdb/` | printed text, typed stats, traits, flavour, errata | **canonical** — see [UPSTREAM.md](../datasets/marvelsdb/UPSTREAM.md) |
| `datasets/marvelcdb-faq/` | official rulings | what an ambiguous card actually does |

MarvelSDB's `code` is the same identifier the engine calls `card_id`, so the two
join directly — 3,953 of 4,344 cards are in both.

`datasets/marvelcdb-faq/` sits alongside these rather than in them. It carries
official **rulings**, not printed text, and nothing in `cards.json` is built from
it — a ruling is an input an author reads, not a field the dataset derives. It is
read through `tools/cards/rulings.py` and surfaced by
`python -m tools.spec.coverage --rulings`. See
[its UPSTREAM.md](../datasets/marvelcdb-faq/UPSTREAM.md) (MARVEL-143).

## Generating

**There is no extractor.** Rebuilding this dataset from
`datasets/marvelsdb/` is MARVEL-252. Whatever does it should keep the two
properties below: the byte-for-byte comparison, and the layout the comparison
exists to protect.

Both modes exit 1, writing nothing, if an identity prints a deck-building line
nobody has classified — see [Deck-building rules](#deck-building-rules). That is
the one way this tool refuses to produce output rather than reporting an
anomaly, because the thing it is protecting against is a rule going missing
quietly.

The output is deterministic: cards sorted by id, fixed key order, every
collection sorted before writing, no wall-clock anywhere. Regenerating without
changing an input produces byte-identical files, and `--check` is how you prove
it. The dataset is checked in so the C# side can read it without running Python
and so drift shows up in review.

A staleness check compares **byte for byte** rather than parsed JSON, because
the layout
below is the review story: a comparison that ignored it would let the property
the layout exists for rot unnoticed. A working tree with CRLF line endings
fails the gate too, and says so in those words rather than reporting staleness.

## Files

### `cards.json`

A header, then one record per line — 4,344 of them. One card per line is
deliberate: `git diff` after a regenerate shows the cards that changed rather
than a reflowed six-megabyte blob.

The header records the MarvelSDB commit and the SHA-256 of each engine file it
was built from, so a consumer holding only this file can still say exactly what
produced it.

Those hashes are taken over the file's content with **CRLF line endings
normalised to LF**, so they are the same on every machine. That is not what
`sha256sum` prints on a Windows checkout with `core.autocrlf=true`, and the
difference is the point: hashing the raw bytes made the header a property of
whoever cloned the repository, so `--check` called the dataset stale on Windows
over two hex strings with nothing semantic changed. Change any character of
`data/cards.json` and the hash still moves (MARVEL-73).

Every record has every field; absence is a value, never a missing key.

```jsonc
{
  "card_id": "01002",
  "in_marvelsdb": true, "in_engine": true,

  // Printed text. MarvelSDB's when it has the card, the engine's otherwise.
  "text": "<b>Forced Response</b>: After you play Black Cat, discard ...",
  "text_plain": "Forced Response: After you play Black Cat, discard ...",
  "text_source": "marvelsdb",

  "name": "Black Cat", "subname": "Felicia Hardy", "unique": true,
  "type": "ally", "type_name": "Ally",
  "faction": "hero", "faction_name": "Hero",
  "traits": ["Hero for Hire"],
  "flavor": "\"I'm not a hero, I'm a thief.\"",
  "errata": "",
  "stats": {"attack": 1, "cost": 2, "health": 2, "resource_energy": 1, "thwart": 1, "thwart_cost": 1},
  "pack": "core", "pack_name": "Core Set",
  "set": "spider_man", "set_name": "Spider-Man",
  "position": 2, "quantity": 1, "deck_limit": 1, "hidden": false,
  "reprint_of": null, "back_link": null, "back_name": "", "back_text": "",

  // The printed deck-building rule, on identity faces that have one.
  // null on every other card. See "Deck-building rules" below.
  "deckbuilding": null,

  // What the Python engine believes, and what implements it. null if the
  // engine has never heard of this card.
  "engine": {
    "pack": "core", "set_name": "Spider-Man", "type": "Ally",
    // The engine's own trait list. NOT the printed `traits` above, and the
    // list the state digest is built from. See "Two trait lists" below.
    "traits": ["HERO FOR HIRE"],
    "attributes": {"Cost": "2", "HP": "2", "ATK": "1", "THW": "1*", "RES": "Y", "Class": "Hero"},
    "text": "<b>Forced Response</b>: After you play Black Cat, discard ...",
    "text_comparison": "exact",
    "link": null,
    "script": {
      "path": "cards/pack/core/spider_man/01002.py",
      "lines": 27,
      "has_imperative_handler": true,
      "player_choice_calls": ["ChooseAbilities"],
      "player_choice_helpers": [],
      "ability_factories": ["AfterPlayerPlayedCard"]
    }
  }
}
```

Notes on the fields that are easy to misread:

- **`text` is markup.** `<b>` for the ability keyword, `<i>` for reminder text,
  `<hr />` between printed faces. `[mental]` and `[[Black Panther]]` are *not*
  markup — they are printed symbols and a spec needs them. `text_plain` strips
  the HTML, turns `<hr />` into a line break, unescapes entities and leaves the
  symbols alone. Grep `text_plain`; render `text`.
- **`stats` is MarvelSDB's vocabulary, `engine.attributes` is the engine's.**
  They overlap but are not translations of each other: `stats` holds typed
  printed numbers, `attributes` mixes stats with keywords (`Guard`, `Surge`,
  `Permanent`) under the engine's own keys. `stats` is every MarvelSDB key that
  is not identity, so a stat field added upstream flows through without a code
  change.
- **`[[X]]` in the printed text is a trait; `[x]` is a resource icon.** The
  distinction is load-bearing wherever a card's rules text names other cards:
  Cyclops' "You may include `[[X-MEN]]` allies" is `"X-Men" in traits`, while
  Wonder Man's "events with a printed `[energy]` resource icon" is
  `stats.resource_energy`. Both readings are made once, in the `deckbuilding`
  block below, so no consumer re-derives them from the sentence.
- **A trait can contain periods.** `S.H.I.E.L.D.` (114 cards) and `A.I.M.` (10)
  are single traits, and are stored with their terminal period so they read as
  printed. Upstream stores the trait line as one string, `"Location.
  S.H.I.E.L.D."`, and the separator is a period *and a space* — splitting on
  every period shredded both acronyms into single letters until MARVEL-85.

### Two trait lists, and the digest is built from the second one

`traits` is MarvelSDB's printed spelling. `engine.traits` is the Python
engine's own list. **They are different lists, and a port must read the
second.** `CardFace.GetInfoTraits` keys every `t_` field in the state digest
from `engine.traits`, so that is what byte equality is measured against.

The spelling differs and is derivable — the engine stores `HERO FOR HIRE` where
MarvelSDB prints `Hero for Hire`, and `S.H.I.E.L.D` where MarvelSDB prints
`S.H.I.E.L.D.`. The digest key is then
`trait.replace(' ', '_').replace('!', '')` and nothing else, because the
engine's traits are already upper-case and already carry no trailing stop.

**The `!` is not cosmetic.** Two traits carry one — `CHASE!` and `TRAP!`, on
five cards (`27102a`, `27102b`, `47031`, `47032`, `47033`). The digest key is
`t_TRAP`. A port deriving keys from the printed traits emits `t_TRAP!` and
fails the byte comparison on every step one of those cards is in play.

**The contents differ too, and that is not derivable.** Compared across the
3,999 cards both sources have, they disagree about the card itself on
**twelve** — reported as `engine_traits_diverge` and enumerated in
`anomalies.json`. Seven are traits the engine has and the printed card does not
(`01172` Whiplash is `CRIMINAL` to the engine and untraited in print), two are
the other way round (`42016` Taunt is `TACTIC` in print and untraited to the
engine), and three disagree outright — including `39029`, where the engine
spells the trait **`THESPYAN`**.

Twelve is small, and it was filed as 142. The original measurement compared the
raw lists, which counts `Vehicle` against `VEHICLE` as a disagreement; 2,489 of
3,999 differ that way and none of those is a defect. See MARVEL-177.
- **`player_choice_calls` and `player_choice_helpers` answer the same question
  with different evidence.** The first is the set of prompt APIs the script
  itself names. The second is the `game/operate/` helpers it calls that reach a
  prompt on *every* path given the arguments passed at that call site --
  fourteen cards ask a question they never write down, and reading only the
  first tiered them as cards that never suspend (MARVEL-114). They are kept
  apart rather than merged because one is a fact about the file and the other
  is an inference about somebody else's function. **A card asks the player
  something when either is non-empty**; that is the rule `tools/spec/coverage.py`
  tiers on.

  The second field is deliberately an **under-approximation**. A helper whose
  prompt depends on board state -- `Faces.DiscardAll` under `simultaneous=True`,
  `Worlds.FindMainScheme` when more than one main scheme is in play -- is not
  counted, even though those cards genuinely may ask. 512 cards are in that
  residual. Crediting them would make `--tier interactive` useless as a work
  list, and a false "this card asks" is exactly as wrong in a cross-language
  contract as a false "it does not". The rule and its measurements are in
  `tools/cards/helper_prompts.py`.
- **`reprint_of` and `engine.link` are different relationships.** The first is
  MarvelSDB's `duplicate_of`, a card printed again in a later pack. The second
  is the engine's `full_link`/`ability_link`, which also decides which script
  the card resolves to.

### Deck-building rules

Seven identities print a rule about what may go in a deck. They used to be a
hand-written table in `tools/decks/rules.py`; they are now a field, so the
Python checker, the deck builder and the C# engine all read the same reading of
the same sentence (MARVEL-88).

```jsonc
"deckbuilding": {
  "aspects": 1,             // how many aspects the deck draws on
  "equal_aspects": false,   // whether they must be the same size
  "copy_limit": null,       // a cap below the printed deck_limit (Adam Warlock's 1)
  "allowances": [           // cards let in from an aspect the deck did not choose
    {
      "what": "X-Men allies",
      "card_type": "ally",  // matched against `type`
      "traits": ["X-Men"],  // any one of these, matched against `traits`
      "resource": null,     // a printed resource icon, e.g. "energy"
      "from": "any_aspect", // or "other_aspects", as printed
      "limit": null,        // how many, null for no cap
      "counted_by": "cards" // or "titles" -- Maria Hill's 3 are titles
    }
  ],
  "source_card": "33001b",
  "source_hash": "ba3d1502cd0af797",
  "source_text": "You may include [[X-MEN]] allies from any aspect in your deck."
}
```

The block is repeated on **every face** of the identity, so a consumer keyed on
`set` need not know which face carries the printing. `source_text` is the
printed line verbatim, so the parse can be audited without leaving the file,
and `source_hash` is what pins it.

**The mechanism that matters is the failure, not the field.** `include|`
`deck-?building|your deck|instead of one|aspect|max \d|per deck|`
`cannot include|must include` is run over every identity face. It matches 48
lines across 37 heroes: 7 rules and 41 ordinary abilities that merely touch a
deck — "Search your deck and discard pile for Mjolnir". Every one of those 48
must be either parsed into a rule or listed in an explicit *reviewed, and not a
deck-building rule* table, each keyed by card id and a hash of the line. **A
line in neither fails `python -m tools.cards.extract`**, in both write and
`--check` modes, and prints the row to paste once a human has read the card.

That is the whole point. The broad net is ~15% precise and the narrow one
(`/deck[- ]?building/`) was ~29% complete; no regex separates *a rule about
building a deck* from *an ability that touches a deck*, because the distinction
is semantic and every new card gets to phrase it freshly. So the net is not
asked to be right — it is asked to notice, and a human decides. Reword a card
and its hash moves, which fails the build twice over: the new wording is
unclassified, and the old parse pins a sentence nothing prints.

The limit, stated plainly: a rule phrased with none of those words is invisible
here, exactly as it was to any grep. Widening the net costs a few more rows in
the reviewed table; missing a rule costs legal decks being called illegal, which
is what MARVEL-85 cost. Everything lives in `tools/cards/deckbuilding.py`.

### `anomalies.json`

Every place the sources disagree or fall short, grouped by kind, each group
sorted by card id and carrying its own description. Read this before authoring
anything.

| Kind | Count | Meaning |
|---|---:|---|
| `card_not_implemented` | 549 | MarvelSDB has the card, no engine script implements it |
| `engine_text_diverges` | 197 | engine text says something different from the printed text |
| `engine_traits_diverge` | 12 | engine trait list says something different from the printed traits |
| `engine_text_missing` | 81 | printed text exists, the engine stores none |
| `card_not_in_marvelsdb` | 46 | engine-only: internals, status tokens, the fan-made challenges |
| `no_text_anywhere` | 43 | neither source has text and nothing implements it |
| `engine_text_corrupt` | 36 | the engine's copy contains U+FFFD |
| `script_without_text` | 7 | a script exists with nothing printed to spec it against |
| `engine_markup_escaped` | 5 | `<\/b>` in the engine's copy — no renderer closes a tag with that |
| `engine_pack_without_expansion` | 5 | pack absent from `data/sets_info.json` |
| `unclaimed_script` | 4 | file under `cards/pack/` no card resolves to |
| `upstream_text_key_typo` | 1 | card 28022 spells its text `scheme text` upstream |

The `engine_*` kinds are the ones that change how you work: **314 cards**
(197 + 81 + 36) have engine text you must not author from. The twelve
`engine_traits_diverge` cards are a different warning — there the engine is
*authoritative*, because the digest is built from its list, and the printed
card is the thing that disagrees. The four unclaimed
scripts are expected — `endless/endless.py`, two `campaign.py` modules and one
disabled variant are engine code that happens to live under `cards/pack/`.

`card_not_implemented` is not a defect. It is the port's backlog, sized.

### `summary.json`

Counts and inventories, each stated next to the rule that produced it: the
type and pack breakdowns, the script/text cross-tab, the 303 distinct
`AbilityFactory` triggers with frequencies, both stat vocabularies, the
deck-building tally (48 identity lines matched, 7 parsed, 41 reviewed and
classified) and the stratification below.

## How the engine's text compares

`engine.text_comparison`, for the 3,955 cards the engine knows and that have text
on at least one side:

| | | |
|---|---:|---|
| `exact` | 3,086 | byte-identical |
| `formatting` | 562 | equal once tags, entities and whitespace are normalised |
| `wording` | 197 | **the words differ** |
| `engine_missing` | 81 | printed text exists, engine has none |
| `marvelsdb_missing` | 29 | engine has text, MarvelSDB has no such card |

`marvelsdb_missing` is not agreement or disagreement — there was nothing to
check against. An engine-only card is never reported as agreeing with the
printed text, because no printed text exists for it.

`formatting` differences are safe to ignore — mostly `<b>When Revealed:</b>`
against `<b>When Revealed</b>:`. `wording` differences are not, and each one is
either a stale transcription in the engine or an engine bug. Both are worth
finding, which is the point of validating specs against the running engine
(MARVEL-21) rather than trusting either side.

## Script coverage

|  | has script | no script |
|---|---:|---:|
| **has printed text** | 3,774 | 520 |
| **no printed text** | 7 | 43 |

3,453 of the 3,457 script files are claimed by at least one card; 98,020 lines
of card script in total.

## Stratification

For varying spec depth by card complexity rather than writing a fixed number per
card. Both rules are recomputed on every generate and written into
`summary.json` alongside their counts — a figure without its rule is not a
measurement.

**No imperative handler — 531 scripts.** The script's syntax tree contains no
function defined inside another: purely declarative `AbilityFactory` calls, so
there is no hand-written behaviour to port and the least spec attention is
needed.

**Suspends for player choice — 440 scripts.** The script calls a method of
`PlayerAsk` (`game/player/model/player_ask.py`) or one of `ChooseAbilities`,
`MayChooseOneAbility`, `AskSpendResources`
(`game/player/action/player_action.py`). These stop mid-resolution for a human
answer — the interaction-heavy cards, and the ones that most need specs.
Random-choice helpers (`ChooseRandom`, `RandomChoice`) are deliberately excluded:
they draw from the seeded RNG and suspend nothing.

The `PlayerAsk` list is parsed out of the engine source at generation time
rather than hardcoded, so a prompt added to that class is counted next time
instead of silently going missing. The resolved list ships in `summary.json`.

> An earlier ad-hoc measurement recorded in `docs/migration.md` put the second
> figure at 334. Its rule was never written down and no principled rule
> reproduces it. The 440 above supersedes it; the 531 reproduced exactly.

## Design notes

**The tool imports nothing from the engine.** `cards/paper.py` and
`cards/database.py` cannot be imported outside a full engine bootstrap —
`game.object` has a circular import — and the dataset should stay generatable by
anyone with a bare Python 3.13, including the C# side. The cost is that the
engine's load rules are mirrored in `tools/cards/engine.py` rather than reused.
Every mirror names the `file:line` it copies, and the test suite cross-checks
`CleanName` against the real `FileManager.CleanName` on every set name in
`data/cards.json` whenever the engine happens to be importable.

**Card scripts are read, never executed.** `CardsDB.FindAbilities` `exec()`s
them; this tool parses them with `ast`. Given that arbitrary code execution in
card loading is [reason one for the migration](migration.md), a preparation tool
should not add another place it happens.

**Refreshing the snapshot changes printed text**, which changes every spec
authored from it. Treat it as a deliberate act — see
[UPSTREAM.md](../datasets/marvelsdb/UPSTREAM.md).
