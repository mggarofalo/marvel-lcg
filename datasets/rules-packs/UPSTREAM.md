# Pack rules — vendored snapshot

The `pack:` tier of the rules corpus: the Learn to Play guide, the expansion
rulebooks, the scenario inserts and the hero rulesheets. Second of the two
tiers MARVEL-154 defines; [`../rules-reference/`](../rules-reference/UPSTREAM.md)
is the first.

| | |
|---|---|
| Upstream | 61 booklets published by FFG/Asmodee alongside each product |
| Harvested with | `python -m tools.rules.packs` |
| Harvested | 2026-08-24 |
| Pinned by | the harvest date — these documents carry no version |
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

## References are authored, not parsed

Every record carries `references: []`, and every one is currently empty.

The corpus is a **one-way graph**: an exception names the rule it overrides, a
base rule names nothing (see
[`docs/rules-provenance.md`](../../docs/rules-provenance.md)). Which `rr:` rule
a given pack section modifies is a judgement — `pack:mc32:featured-keywords.patrol`
constrains thwarting, but saying *which* clause of `rr:thwart` it overrides is a
reading, not something visible on the page.

Inferring it from a title match would manufacture exactly the kind of
plausible-but-wrong relationship this corpus exists to eliminate, so the field
is emitted empty and filled in by hand.

`python -m tools.rules.refs <id>` walks the graph in both directions — the
reverse index is computed at query time and never stored, because a
relationship written down twice can disagree with itself. `--cycles` gates
against two rules each claiming priority over the other.

## Refreshing

No `--check` gate in CI: the source is 353 MB of copyrighted PDFs that are not
in this repository. `python -m tools.rules.packs --check` is for whoever holds
them. `unit_test/test_rules_packs.py` verifies the committed snapshot's
internal consistency instead.
