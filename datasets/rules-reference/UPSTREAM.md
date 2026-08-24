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
| Harvested with | `python -m tools.rules.harvest` |
| Harvested | 2026-08-24 |
| Pinned by | the RR version printed on the document's cover |
| Tier | `rr:` — the glossary. The `pack:` tier is not yet implemented. |

## Why this exists

`py_src/specs/rules/` holds ten rules specifications, and before this dataset
every one of them asserted what the Python engine does, cross-checked against
nothing. AGENTS.md names the hazard: a spec authored from ambiguous printed
words, validated against an engine implementing the same reading of the same
words, enters `specs/trusted.json` having confirmed only that the engine agrees
with itself.

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
| `index.json` | machines — spec pinning, `tools.rules.diff` | every citable unit |
| `entries/*.md` | agents and humans | one linked document per entry |
| `icons.json` | both | glyph legend, derived from the document |

**261 entries, 1,117 citable records, 1,034 resolved cross-references.**

Citations are three-tier, because the grain a spec argues from is not the entry:

```
rr:cost           the entry, and its opening definition
rr:cost.3         the third top-level clause
rr:cost.3.1       the first qualification of that clause
```

Ids are positional rather than derived from the text, so that a citation
survives a rewording — which is exactly when it most needs to. Inserting a
clause does renumber the clauses below it; that is real, and it is what
`tools.rules.diff` will exist to report.

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
`../marvelcdb-faq/`, and `--check` exists for whoever holds the PDFs:

```bash
cd py_src
uv pip install pdfplumber          # local-only; deliberately not in requirements.lock
python -m tools.rules.harvest --check
```

What CI does verify is this snapshot's internal consistency —
`unit_test/test_rules_index.py`, 13 tests covering cross-reference resolution,
clause anchors, icon coverage, and front-matter agreement with the index. Every
one of them is pinned to a defect the parser actually shipped during
development.

## Known gaps

- **The appendices are not parsed.** Twenty cross-references point at Appendix I
  (Deck Customization), II (Setup) and III (Card Anatomy) and resolve to
  nothing. `unit_test/test_rules_index.py` asserts that these are the *only*
  unresolved references, so the gap cannot silently widen.
- **The `pack:` tier does not exist yet.** The 61 rulesheets, inserts and
  expansion rulebooks are prose about one hero or one scenario, with no shared
  structure to parse — and they are where new keywords arrive. They need a
  different reader. This is the second leg of MARVEL-154.
- **Numbered procedures render inline.** The document sets some rules as
  ordered steps ("1. Give boost card… 2. …"). They are preserved as text but
  are not separately citable.

## Refreshing

A new Rules Reference version is the patch loop in
[`docs/rules-provenance.md`](../../docs/rules-provenance.md), entered at step 1.
Re-run the harvest against the new PDF, diff the index, and let provenance
pinning drop every trusted scenario whose cited entry moved.
