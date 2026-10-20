# Pack rules — vendored snapshot

The `pack:` tier of the rules corpus: the Learn to Play guide, the expansion
rulebooks, the scenario inserts and the hero rulesheets. Second of the two
tiers MARVEL-154 defines; [`../rules-reference/`](../rules-reference/UPSTREAM.md)
is the first.

| | |
|---|---|
| Upstream | 62 booklets published by FFG/Asmodee alongside each product |
| Harvested with | `tools/Marvel.Rules.Packs.Harvest` |
| Harvested | 2026-08-24 |
| Pinned by | `sources.manifest.json`: every local PDF byte and the committed snapshot |
| Tier | `pack:<code>:<section>[.<rule>]` |

**62 documents → 553 sections → 902 citable rules.**

| Kind | Documents | Records |
|---|---|---|
| Expansion rulebook | 11 | 529 |
| Scenario insert | 20 | 229 |
| Hero rulesheet | 30 | 76 |
| Learn to Play | 1 | 68 |

Campaign logs (9 documents) are excluded: they are play aids for recording
state between sessions, not statements of rules.

## Why this tier is separate from the Rules Reference

They are different kinds of document. The Rules Reference is one alphabetical
glossary with a rigid, parseable shape and the standing of an authority. These
are 62 heterogeneous booklets that share a publisher's template and nothing
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

349 of the 902 records are named rules found this way.

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

## Rise of Red Skull admission (2026-10-19)

The official FFG booklet was retrieved directly from
[FFG's support CDN](https://images-cdn.fantasyflightgames.com/filer_public/92/1d/921ddef8-c28c-4096-a9e9-63142230a602/mc10_the_rise_of_red_skull_rules_web.pdf).
Its 20-page PDF is pinned as `mc10_the_rise_of_red_skull_rules_web.pdf`,
2,864,641 bytes, SHA-256
`9e783e8cc325009f44d1cf0a9f5b845c7b41b50d0e88d4e5a7c05ae5816a88c6`.
Copyright Marvel / Fantasy Flight Games, 2020; no open-content license is asserted.
The original PDF remains in the local document library, outside Git.

The 28 `mc10/` sections and 15 anchored rules are a reviewed transcription of
rules-bearing regions on printed pages 2, 3, 5, 7, 10, 12, 15, 17 and 18.
This vendored snapshot retains italic expert-stage/customization instructions,
trait names, ordered campaign instructions and their Expert Campaign qualifiers.
`[per-player]` transcribes the player icon on page 15. Scenario names qualify
repeated Campaign Instructions and Rules Clarification headings for stable ids;
whitespace and line-end hyphenation are normalized. Card illustrations, story,
strategy prose, credits and the blank page-20 log are not rules records. Printed
card authority remains `datasets/cards/`; the original page-20 log is the tracker
field reference, not an executable rule.

The generic typography harvester does not reproduce this reviewed transcription:
it omits Exo2-Regular headings, italic rules and Komika trait words and can mix
card illustrations into rule text. Run it into a candidate directory and review
against the PDF, never directly over this snapshot. Adoption preserves every
pre-existing source pin and record byte; only `mc10`, the combined index and
snapshot pin change. Offline index/manifest and RoRS authority tests gate the
committed corpus without downloading or requiring the source PDF.

Read `ruling:1516351f87fc4382` through `datasets/rules-graph.json` when interpreting
persistent damage and Condition upgrades. Current RR `modes-of-play.10` preserves
campaign removals on retries; clauses 11–12 separate campaign difficulty from
scenario modes. The PDF is preserved as published, not silently rewritten to
incorporate later authorities.

## Wrecking Crew authority repair (2026-10-20)

The [official FFG booklet](https://images-cdn.fantasyflightgames.com/filer_public/87/8c/878c0089-00d6-4485-9696-49e623936c6c/mc03_wrecking_crew_rules_insert-compressed.pdf)
was downloaded and visually inspected at printed pages 4, 6 and 7. Its 861,302 bytes
and SHA-256 `443f135a8c5d90a8bfd3719323e6c00501ed166d88f517bec37b59ee9d10124c`
match the existing source pin. Copyright Marvel / Fantasy Flight Games, 2019;
no open-content license is asserted. The PDF remains outside Git.

The reviewed mc03 transcription includes the italic setup exclusion on page 4,
the italic basic-target note on page 6, and the complete bold victory sentence
continuing onto page 7. Existing section/rule identifiers remain stable. The
combined index hashes and byte-level snapshot pin cover these exact changes;
all other pack entries and PDF pins remain unchanged. The generic typography
harvester is a candidate generator, not a byte-identical reproduction of this
reviewed vendored text. `WreckingCrewAuthorityTests` holds these operative clauses;
`PackRulesHarvestTests` independently checks the offline snapshot pin.
