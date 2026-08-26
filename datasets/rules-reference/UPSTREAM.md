# Rules Reference — vendored snapshot

The **Rules Reference** is the rules authority for Marvel Champions: phases,
timing, keywords, damage, costs. It is the third of the three authorities named
in [`docs/rules-provenance.md`](../../docs/rules-provenance.md), and until
MARVEL-154 it was the missing one — [`../marvelsdb/`](../marvelsdb/) carries
printed card text and [`../marvelcdb-faq/`](../marvelcdb-faq/UPSTREAM.md)
carries rulings, and neither carries a *rule*.

| | |
|---|---|
| Upstream | Rules Reference **v1.8**, `mc_rulesreference_v18_compressed.pdf` (FFG/Asmodee) |
| Harvested with | `tools/Marvel.Rules.Harvest` |
| Harvested | 2026-08-24 |
| Pinned by | the RR version printed on the document's cover |
| Tier | `rr:` — the glossary, plus Appendix II. The `pack:` tier is not yet implemented. |

## Why this exists

AGENTS.md's first non-negotiable is that the rulebook decides, and before this
dataset there was no rulebook in the repository to decide with. The hazard it
names is a behaviour argued from ambiguous printed words and confirmed against
an implementation that read the same words the same way — which confirms only
that the implementation agrees with itself.

The worked example is MARVEL-169. The engine models resource generation and
cost payment as a single step — a static table of what each card would yield,
computed against a frozen board — and two entries in that table could both
promise the same exhausted ally. Establishing that this is wrong required
knowing that generation and payment are *distinct acts*, which nothing in this
repository could say. `rr:cost.3` says it:

> To pay a resource cost, a player spends resources that they generate by
> discarding cards from their hand or by using "Resource" card abilities.

## Two artefacts, one parse

| | For | Grain |
|---|---|---|
| `index.json` | machines — citation checking, version diffs | every citable unit |
| `entries/*.md` | agents and humans | one linked document per entry |
| `icons.json` | both | glyph legend, derived from the document |

**262 entries, 1,218 citable records, 1,038 resolved cross-references.**

One of the 262 is not a glossary entry. **Appendix II: Setup** is a numbered
procedure on page 51, and it is here because the glossary *cites* it and does
not *contain* it — seven entries name it in their see-also, and three rules the
engine has to implement are defined nowhere else:

| | |
|---|---|
| `rr:appendix-ii-setup.step.11` | "Put Setup Cards Into Play. Search **each deck and the set aside area** for any cards with the setup keyword and put them into play." |
| `rr:appendix-ii-setup.step.15` | "Resolve Mulligans. Each player may **discard** any number of cards from hand, and then draw up to their starting hand size." |
| `rr:appendix-ii-setup.step.12` | Where a "Setup" ability resolves, and which of the three sub-steps it belongs to |

Its position among the steps is not a detail: step 11 comes **before** step 14's
draw, so a setup card is in play before anybody has a hand. MARVEL-211 had
inferred the opposite from the step's name.

Adding it resolved four dangling cross-references, which is the second reason
it belongs here: `see_also_unresolved` is meant to name what the snapshot does
not carry, and "Appendix II: Setup" was in four entries' lists while being a
page of the same document.

Citations name the grain a spec actually argues from, which is not the entry:

```
rr:cost                     the entry, and its opening definition
rr:cost.3                   the third top-level clause
rr:cost.3.1                 the first qualification of that clause
rr:villain-phase.step.3     the third step of a numbered procedure
rr:ability.step.2.a         a lettered sub-step
```

The `step` tier exists because the Rules Reference writes several of its most
load-bearing rules as ordered procedures and **cites them that way itself** —
"during step three of the villain phase" is its own phrasing, in three separate
entries. 90 step records across 13 entries. Without them the
Simultaneous Timing Priority chart is a run-on sentence inside one clause, and
"was a step added to the Attack process?" — a question RR v1.8 poses directly
— cannot be asked at all.

Ids are positional rather than derived from the text, so that a citation
survives a rewording — which is exactly when it most needs to. Inserting a
clause does renumber the clauses below it; that is real, and it is what
`-- check` reports when a new version is harvested.

`entries/*.md` carries the **full normative text**, not the one-sentence
`fragment` the index uses. That distinction is the point: `fragment` exists to
make a citation legible in a diff, and an agent adjudicating "can this
already-exhausted ally be exhausted to pay for that" needs the rule itself.
Each clause is anchored, so `rr:cost.3` resolves to
[`entries/cost.md#cost-3`](entries/cost.md).

## Why the PDF is not here, and what follows from it

The source is copyrighted and stays local — `~/Documents/Marvel Champions LCG`,
71 PDFs, 353 MB, gitignored and not redistributable. Only this derived index is
committed.

That has a consequence worth stating plainly: **this dataset has no
regenerate-or-fail CI gate**, unlike `../rng/`, `../digest/` and `../cards/`.
CI has nothing to regenerate from. It is *vendored*, on the same footing as
`../marvelcdb-faq/`.

```
$ dotnet run --project tools/Marvel.Rules.Harvest -- write [pdf] [into]
$ dotnet run --project tools/Marvel.Rules.Harvest -- check [pdf]
```

`check` reads the document and reports how what it produces differs from what
is committed here. It is not a CI gate and cannot be: CI has no PDF.

What is verified is the harvester's reading of the document — how a heading
becomes a citation id, and how the document's emphasis becomes Markdown. Those
need no PDF, so they are held in the suite: `HarvestTests`.

## What the harvester reproduces

Against the v1.8 document, as of the harvester's first version:

| | |
|---|---|
| entries | 261 of 262 — Appendix II is page 51 and the reader stops at the glossary |
| citable records | 1,218, the same number the snapshot holds |
| record ids | 26 differ, all in five entries |
| first sentences | 1,168 of 1,218 identical |
| whole documents | 196 of 260 byte-identical |

**The remaining differences are not all defects in the harvester**, and three
classes are the other way round:

- The document sets a hyphenated word across a line break — "ally-turned-" and
  "minion" — and the snapshot joined them with a space. The harvester joins
  them into a word.
- A bold word opening a clause, "**Exception**: For abilities that…", lost its
  opening emphasis in the snapshot.
- `rr:cost.1` quotes the cost arrow icon, and the snapshot dropped the glyph.

The rest are `rr:teamwork`, whose heading prints "(TRAIT)" that the snapshot
dropped, and lettered sub-steps attaching one level differently in four
entries. **Regenerating this dataset is therefore a deliberate act and not a
routine one** — the ids are what the suite cites, and 26 of them would move.

## The gutter, and why it is found rather than fixed

Worth knowing before touching `Pages.Gutter`. This document sets recto and verso
with **different margins**, so the empty band between its two columns is at
roughly 291-308pt on one and 303-321pt on the other. The first version of the
harvester used a single measured split at 300pt, which is correct for the first
layout and lands *inside the left column's text* on the second.

The effect was not a crash and not a loss. The final character of any
left-column line that ran long was filed under the other column and reappeared
elsewhere in the page — so "a hero does not exhaust" entered the corpus as
"a hero does not exhaus". **33 of the glossary's 261 entries were affected**, each by a
dropped trailing character, in a dataset whose entire purpose is to be
quotable.

The split is now found per page as the widest character-free band. It is
searched only across the middle half of the page, because the margins are wider
than the gutter and would win.

## Known gaps

- **The appendices are not parsed.** Twenty cross-references point at Appendix I
  (Deck Customization), II (Setup) and III (Card Anatomy) and resolve to
  nothing. They are listed in each entry's `see_also_unresolved`, which is what
  that field is for: naming what the snapshot does not carry, rather than
  handing back a quietly shorter list. Appendix II is here anyway, added by hand
  — see above.
- **The `pack:` tier does not exist yet.** The 61 rulesheets, inserts and
  expansion rulebooks are prose about one hero or one scenario, with no shared
  structure to parse — and they are where new keywords arrive. They need a
  different reader. This is the second leg of MARVEL-154.
- **Sub-headings inside an entry are not split.** The overview entries use a
  bold `Name —` sub-heading form (`Constant Abilities —`, `Triggered
  Abilities —`). These are preserved as text but do not start a new clause, so
  a citation lands on the clause they were appended to.

## Refreshing

A new Rules Reference version is the patch loop in
[`docs/rules-provenance.md`](../../docs/rules-provenance.md), entered at step 1.
Run `check` against the new PDF first — it says which entries moved — then
`write`, then read what changed. `RuleCitationTests` fails the build on a
citation whose id no longer resolves, which is the point: a renumbered clause
should stop the build rather than quietly re-aim a test at a different rule.

## What the harvester knows about the document

Five things, each of which cost a defect to find, and each written down where
the code does it:

| | |
|---|---|
| The gutter moves between recto and verso | found per page as the widest character-free band, not measured once |
| Section titles are overprinted | `OOVVEERRVVIIEEWW` is one word drawn twice |
| Headings carry no space glyphs | `THEGOLDENRULES` — the words divide by a measured gap |
| The second-level bullet is a glyph its font does not carry | `U+0000` on one page and `U+0020` on another, so the *face* is the signal |
| The document reproduces cards, and a card has text on it | Rhino's boost icons read as part of the prose beside them |
