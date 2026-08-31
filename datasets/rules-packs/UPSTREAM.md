# Pack rules — vendored snapshot

The `pack:` tier of the rules corpus: the Learn to Play guide, the expansion
rulebooks, the scenario inserts and the hero rulesheets. Second of the two
tiers MARVEL-154 defines; [`../rules-reference/`](../rules-reference/UPSTREAM.md)
is the first.

| | |
|---|---|
| Upstream | 61 booklets published by FFG/Asmodee alongside each product |
| Harvested with | `tools/Marvel.Rules.Packs.Harvest` |
| Harvested | 2026-08-24 |
| Pinned by | `sources.manifest.json`: every local PDF byte and the committed snapshot |
| Tier | `pack:<code>:<section>[.<rule>]` |

**61 documents → 525 sections → 859 citable rules.**

| Kind | Documents | Records |
|---|---|---|
| Expansion rulebook | 10 | 486 |
| Scenario insert | 20 | 229 |
| Hero rulesheet | 30 | 76 |
| Learn to Play | 1 | 68 |

Campaign logs (9 documents) are excluded: they are play aids for recording
state between sessions, not statements of rules.

## Why this tier is separate from the Rules Reference

They are different kinds of document. The Rules Reference is one alphabetical
glossary with a rigid, parseable shape and the standing of an authority. These
are 61 heterogeneous booklets that share a publisher's template and nothing
else — and they are where **new keywords and scenario-specific rules actually
arrive**, months before the Rules Reference absorbs them.

## Finding rules inside a marketing document

Most of a hero rulesheet is not rules. It is cover copy, a designer credit
list, and a paragraph of prose about Steve Rogers. Three typographic signals
separate the rules, chosen over reading the wording because wording is exactly
what a parser should not be guessing at:

- **Size.** Credits and legal fine print are set in the body face two to three
  points smaller than rules text. A size floor drops them.
- **Slant.** Flavour is oblique — the S.H.I.E.L.D. briefing that opens each
  scenario is a whole page of it. Rules are roman.
- **Section.** What remains is grouped under headings, and a 14-name denylist
  removes the sections that are reliably not rules (`CREDITS`, `PLAYTESTERS`,
  `S.H.I.E.L.D. BRIEFING`, `STRATEGY TIPS`, `EXPANSION SYMBOL`…).

The denylist is deliberately not an allowlist. There are **531 distinct
headings** across the corpus — mostly scenario, keyword and villain names that
no list could anticipate. Excluding the known-not-rules keeps the unknown,
which is the right default for a corpus meant to be complete.

## One rule per rule

A pack's `NEW RULES` section is not one rule. It is a list of them — *When the
Villain Changes Form*, *When a Villain Stage is Defeated*, *When Norman Osborn
Attacks* — each set in the bold face, each self-contained, each something a
reader looks up on its own. Run together into a blob they are unusable: you
have to find the sentence that applies inside four paragraphs about a different
situation.

So bold sub-headings become named, anchored, individually citable rules:

```
pack:mc02:new-rules                                  the section
pack:mc02:new-rules.when-the-villain-changes-form    one rule
pack:mc32:featured-keywords.hinder-x                 a keyword definition
```

334 of the 859 records are named rules found this way.

## References are authored, and live somewhere else

**Nothing in this index carries a reference field**, by design.

The corpus is a **one-way graph**: an exception names the rule it overrides, a
base rule names nothing (see
[`docs/rules-provenance.md`](../../docs/rules-provenance.md)). Those edges are
hand-authored in [`../rules-graph.json`](../rules-graph.json), with a stated
reason each, and they are kept out of this file for a blunt reason: everything
here is destroyed and rebuilt from a PDF on every harvest. An authored edge
stored alongside generated data would be lost on the next refresh, silently and
completely. Generated and authored data do not share a file.

Which `rr:` rule a given pack section modifies is a judgement —
`pack:mc32:featured-keywords.patrol` constrains thwarting, but saying *which*
clause of `rr:thwart` it overrides is a reading, not something visible on the
page. Inferring it from a title match would manufacture exactly the
plausible-but-wrong relationship this corpus exists to eliminate.

Reverse lookup is computed at query time and never stored — the
reverse index is computed at query time and never stored, because a
relationship written down twice can disagree with itself. `--cycles` gates
against two rules each claiming priority over the other.

## Refreshing

No gate in CI: the source is 353 MB of copyrighted PDFs that are not in this
repository. The harvester is manual and reads only a local library; it never
downloads a document. `check` holds that library to `sources.manifest.json` and
also catches a hand edit anywhere in the committed snapshot:

```bash
dotnet run --project tools/Marvel.Rules.Packs.Harvest -- check [library]
```

When a new booklet or errata changes those pins, review a candidate before
replacing the snapshot:

```bash
dotnet run --project tools/Marvel.Rules.Packs.Harvest -- list [library]
dotnet run --project tools/Marvel.Rules.Packs.Harvest -- diff [library]
dotnet run --project tools/Marvel.Rules.Packs.Harvest -- write [library] [candidate-directory]
```

The checked-in snapshot predates the PdfPig harvester, so `diff` also exposes
the extraction-library migration. That delta is not silently accepted: read it
alongside the actual source change, then write the reviewed result into this
directory. `write` updates the PDF and snapshot pins with the output. `pin`
exists only for adopting an already-reviewed snapshot whose original writer is
unavailable; it must not be used to bless an unexplained source change.
