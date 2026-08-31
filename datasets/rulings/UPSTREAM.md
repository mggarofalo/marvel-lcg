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
| Latest observed ruling | 2026-03 |
| Harvester | `tools/Marvel.Rulings.Harvest` |

## What is here

```
pages/official-ffg-rulings.html  pre-RRG 1.5 compendium, grouped by product
pages/post-rrg-1-5.html          chronological rulings scoped to RRG 1.5
pages/post-rrg-1-6.html          chronological rulings scoped to RRG 1.6
pages/post-rrg-1-7.html          chronological rulings scoped to RRG 1.7 and 1.8
rulings.json                     citable index derived from those four files
```

There is no post-1.8 page. Hall of Heroes titles the last source “post-RRG 1.7
& 1.8”, so its URL is recorded explicitly rather than constructed by pattern.

The captured page hashes are:

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
snapshot. With no complete local harvest it reports that rulings are
unavailable and exits successfully: absence is not misrepresented as an empty
upstream corpus. CI uses the committed `pages/` input, so its gate is entirely
offline and catches a hand edit to either leg.

Read the HTML and JSON diffs before replacing the pin and harvest date. A moved
ruling is an authority change, not a routine data sync.

## Provenance and licence

Hall of Heroes publishes no licence for these pages. The rulings are attributed
to Fantasy Flight Games designers, rules specialists, official FAQs, and
printed rule inserts, transcribed by the Hall of Heroes community. They are
vendored here as a research authority for engine and spec authors, not for
redistribution as a product.
