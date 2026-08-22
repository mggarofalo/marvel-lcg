# MarvelCDB FAQ rulings — vendored snapshot

Official rulings and developer clarifications for Marvel Champions cards,
recorded against card codes by the MarvelCDB community. This is the only
**rulings** data in this repository: [`../marvelsdb/`](../marvelsdb/) carries
printed card text and errata, and neither carries a ruling.

| | |
|---|---|
| Upstream | https://marvelcdb.com |
| Harvested with | `marvelcdb v0.1.0` ([source](https://github.com/mggarofalo/marvelcdb-cli)) |
| Harvested | 2026-08-22 |
| Pinned by | the harvest date — see below |

## Why this exists

A spec authored from printed card text alone encodes contested timing by guess.
It is then validated against the Python engine, which implements the same guess,
and passes into `py_src/specs/trusted.json` having confirmed only that the engine
agrees with itself. Where an official ruling exists it is the only independent
check available, and only while the Python engine is still the reference
(MARVEL-143, feeding MARVEL-68).

The worked example is `01001a`. Spider-Man's printed Interrupt fires "when a
villain **initiates an attack**"; Ultron's Forced Interrupt fires "when Ultron
**attacks**". The printed words describe two different moments. The ruling says
there is no timing difference and the Forced Interrupt takes priority. Nothing
else in this repository can tell an author that.

## Why the harvest date is the only pin

`../marvelsdb/` pins a git SHA because upstream is a git repository. MarvelCDB is
a website. It publishes no version identifier, no changelog and no content hash
for FAQ entries, so there is nothing to pin except when the harvest ran.

Individual entries carry their own `updated` timestamp, which is upstream's and
is recorded verbatim. That dates the *ruling*; it does not date the *snapshot*,
because a ruling added after the harvest has no timestamp here at all.

## Why vendored rather than fetched

The same reason as `../marvelsdb/`: everything under `datasets/` must be readable
offline, years from now, by anyone with a bare Python. Fetching at read time
makes correctness depend on a community-run site staying up and unchanged.

This one is **vendored, not generated**. `datasets/cards/` has a
`tools.cards.extract --check` gate that regenerates it and compares byte for
byte; this has no such gate, because there is nothing a machine without a network
could regenerate. What guards it instead is `unit_test/test_card_rulings.py`,
which checks the snapshot is internally consistent and that every ruling still
lands on a card the dataset has.

## What is here

```
faq.json    every FAQ entry MarvelCDB served, verbatim
```

```json
{
  "version": 1,
  "harvested": "2026-08-22",
  "source": "https://marvelcdb.com",
  "harvester": "marvelcdb v0.1.0",
  "queried": ["01001a", "01001b", ...],
  "entries": [{"code": "01001a", "html": "...", "text": "...", "updated": {...}}]
}
```

**`queried` is load bearing.** It lists every code the harvest asked about, so a
code in `queried` but not in `entries` has *no ruling*, while a code in neither
was *never asked*. Without it those two are indistinguishable, and the second
silently masquerading as the first is how a card with a ruling gets a spec
written against the printed words instead.

Entries are stored raw. HTML, markdown, smart quotes and upstream's `updated`
shape are all untouched, so a transcription problem is distinguishable from a
harvest problem. One entry per line, sorted by code, so `git diff` on a refresh
names the rulings that changed.

## A code can appear twice

`05005` is served as two entries with the same text and `updated` stamps five
seconds apart — an upstream double-submit. Both are kept, because this file
mirrors MarvelCDB rather than curating it.
`py_src/tools/cards/rulings.py` resolves the repeat on read: it keeps the first
and records the code, the same way the MarvelSDB loader treats a repeated card
code. A dict assignment would have dropped one without saying so.

## Two codes for one card

MarvelCDB serves a double-sided card under one unsuffixed code where
`marvelsdb-json-data` — and therefore `datasets/cards/` — splits it into printed
faces. Site `01097` is `01097a` and `01097b` here. Measured 2026-08-22, 76 of the
site's codes are shaped that way, almost all of them main schemes.

Codes are stored as MarvelCDB served them. The mapping happens on read, in
`py_src/tools/cards/rulings.py`, which fans a ruling out to every face: a ruling
is about the card, and which face prints the sentence in question is a printing
detail.

## Refreshing

Unlike a card-text refresh, this one is low risk — a ruling is an input to
authoring, not something a spec is regenerated from. It is still worth reading
the diff, because a *changed* ruling means a spec written against the old one may
now be wrong.

```bash
cd py_src
python -m tools.cards.harvest_faq        # ~10 minutes
python -m tools.cards.rulings --summary
```

Then update the harvest date and CLI version in the table above, and read the
diff on `faq.json`. Needs the CLI on `PATH`:

```bash
go install github.com/mggarofalo/marvelcdb-cli/cmd/marvelcdb@latest
export PATH="$PATH:$(go env GOPATH)/bin"
```

`marvelcdb` is an acquisition tool, not a dependency of this software. It runs
here and nowhere else — see AGENTS.md.

## Provenance and licence

MarvelCDB publishes no licence for its FAQ content. The rulings are Fantasy
Flight Games' and Marvel's, transcribed from official FAQ documents, developer
posts and Hall of Heroes rulings by the MarvelCDB community. This repository
already carries the same publishers' card text in `../marvelsdb/`, so vendoring
adds no exposure that was not already present. It is here to be read by the
people writing specs, not redistributed as a product.
