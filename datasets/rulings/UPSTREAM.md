# Hall of Heroes rulings — vendored snapshot

Official FFG rulings and developer clarifications transcribed by the Hall of
Heroes community. This complements `../marvelcdb-faq/`: MarvelCDB associates a
ruling with a card, while these pages also retain questions that cite no card
and therefore adjudicate the rules themselves.

This dataset is **vendored**. The four HTML files are the copied upstream
authority; `rulings.json` is their deterministic, reviewable index. Both are
committed so the index can be regenerated and checked offline.

| | |
|---|---|
| Upstream | https://hallofheroeslcg.com |
| Harvested | 2026-08-31 |
| Latest observed ruling | 2026-08 |
| Harvester | `tools/Marvel.Rulings.Harvest` |

## What is here

```
pages/official-ffg-rulings.html  pre-RRG 1.5 compendium, grouped by product
pages/post-rrg-1-5.html          chronological rulings scoped to RRG 1.5
pages/post-rrg-1-6.html          chronological rulings scoped to RRG 1.6
pages/post-rrg-1-7.html          chronological rulings scoped to RRG 1.7 and 1.8
pages.manifest.json              byte length and SHA-256 of every vendored page
rulings.json                     1,100 citable entries derived from those pages
```

There is no post-1.8 page. Hall of Heroes titles the last source “post-RRG 1.7
& 1.8”, so its URL is recorded explicitly rather than constructed by pattern.

`pages.manifest.json` is the machine-readable pin. Its captured page hashes are:

```
8fc4a5b08e2bf8b46004a0fbfb89aeb369fb24e53dbbaa5298b70ca928679b3b  official-ffg-rulings.html
72c4800ed7cc6aefa5c98fc6b36a561760937d1fb8dcb03e06da3dca3ee8147c  post-rrg-1-5.html
edf164a75000b6dd160203b3f897d004b6d11f0eacf1b098322329e678a494d0  post-rrg-1-6.html
dd6a3f230e93f0a6b914d91f3de1751bf271bc0a7898ec3976424854f71eda47  post-rrg-1-7.html
```

## Identity and change detection

Hall of Heroes gives a ruling no stable anchor. The repository therefore
chooses its id: `ruling:<sha>` is derived from the page, section, and normalized
question. Reordering a page leaves it alone; editing the question deliberately
changes it. The separate `hash` covers the question, answer, attribution,
scope, observed month, and linked MarvelCDB card codes. An answer edit keeps the
id and is reported as **revised**.

The chronological pages use three source shapes: question blockquotes followed
by paragraph or list answers; question blockquotes followed by answer
blockquotes; and bare paragraph questions followed by answer blockquotes. Lists
can nest. The parser retains the complete question/answer pair in every shape.

An attribution date normally supplies `observed`. Month headings are checked as
an independent sanity check: later bylines are accepted because Hall of Heroes
does not always add a heading when appending a month, but a byline that moves
backward fails the harvest unless it is an audited correction. The one current
correction is a ruling grouped under February 2026 whose byline transcribes
“February 20, 2025”; the grouping and surrounding chronology establish
`2026-02`.

Attributions are normalized to the full established byline where the page uses
short forms such as `-Caleb`, `-Alex`, or `-Boggs`. `via` names Hall of Heroes
as the transcriber; `source` names the attributed authority. A record with no
MarvelCDB card link keeps `cards: []` and is explicitly `kind: "rules"`.

## Refreshing

Acquisition is the only networked step, is explicit, and writes one local
cache. The parser never refetches a page:

```bash
dotnet run --project tools/Marvel.Rulings.Harvest -- fetch /tmp/hall-of-heroes
dotnet run --project tools/Marvel.Rulings.Harvest -- check /tmp/hall-of-heroes
dotnet run --project tools/Marvel.Rulings.Harvest -- write /tmp/hall-of-heroes /tmp/rulings 2026-08-31
```

`check` distinguishes added, revised, and removed ids against the committed
snapshot. An explicitly named candidate cache is optional: if it is incomplete,
the command reports that rulings are unavailable and exits successfully rather
than misrepresenting absence as an empty upstream corpus. `write` always fails
for incomplete input.

The no-argument committed check is stricter. It first hashes the raw page bytes
against `pages.manifest.json`, then compares the regenerated `rulings.json`
bytes. The JSON wire format is UTF-8 without a byte-order mark and uses LF on
every platform. CI therefore remains entirely offline while detecting a hand
edit to either the source pages or derived index on both Windows and Linux.

Read the HTML and JSON diffs before replacing the pin and harvest date. A moved
ruling is an authority change, not a routine data sync.

## Rules Reference relationships

This vendored dataset records what Hall of Heroes published; it does not guess
which Rules Reference clause a ruling changes. Audited ruling-to-base mappings
live in the hand-authored relationship layer at `datasets/rules-graph.json`.
`tools/Marvel.Rules.Index` joins the two without altering either source, pins
each mapping to the base record hash, and keeps absorbed rulings citable.

## Provenance and licence

Hall of Heroes publishes no licence for these pages. The rulings are attributed
to Fantasy Flight Games designers, rules specialists, official FAQs, and
printed rule inserts, transcribed by the Hall of Heroes community. They are
vendored here as a research authority for engine and spec authors, not for
redistribution as a product.
