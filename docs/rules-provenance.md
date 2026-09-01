# Rules provenance

Published authorities decide behavior. The engine, old implementations and
passing tests do not become authorities by agreeing with themselves.

Every authority used by the runtime is committed, pinned and readable offline.
Tests and behavioral obligations name stable ids into those datasets.

## Authority layers

The repository uses these sources:

| Authority | Dataset | Grain |
|---|---|---|
| Rules Reference v1.8 | `datasets/rules-reference/` | citable rule record |
| Printed card text | `datasets/marvelsdb/` to `datasets/cards/` | printed face |
| Product rules | `datasets/rules-packs/` | linked expansion rule |
| MarvelCDB FAQ | `datasets/marvelcdb-faq/` | card ruling |
| Hall of Heroes rulings | `datasets/rulings/` | rules question |
| Supported setup facts | `datasets/setup/` | hero, scenario or encounter set |

The Rules Reference supplies general rules. Printed card and scenario text may
override it where the golden rules say they do. A ruling clarifies or modifies
another authority only when `datasets/rules-graph.json` records that relationship.

[Product and repository scope](scope.md) decides which records can become
executable. Vendoring an expansion rule or card does not open its product.

## Dataset kinds

Every dataset is one of the 3 kinds defined by [AGENTS.md](../AGENTS.md):

- generated data is rebuilt offline and byte-identically from committed inputs;
- vendored data is copied from a pinned upstream and read as-is; and
- authored data is written here because no upstream records it and has a
  purpose-built test gate.

There is no network-dependent build step. Acquisition commands may use the
network, but they write a complete local candidate. Generation and comparison
then run offline.

Each vendored dataset carries an `UPSTREAM.md` that records its source, version
or retrieval pin, license and regeneration procedure.

## Rules Reference index

The Rules Reference harvest produces 2 views from one local PDF parse:

| Artifact | Use |
|---|---|
| `index.json` | stable ids, fragments, hashes and citation checks |
| `entries/*.md` | complete linked entry text for human and agent reading |
| `icons.json` | glyph names used by both views |

A citation can name an entry, clause, qualification or enumerated step:

```text
rr:forced
rr:forced.4
rr:forced.3.1
rr:villain-phase.step.2.b
```

The fragment makes a citation legible in a report. The Markdown entry provides
the context needed to interpret it. Tests quote the decisive clause in a comment
and use `[Rule("rr:...")]` for the machine-checked id.

The harvester is intentionally not a CI gate because the copyrighted source PDF
is local. The committed snapshot and its upstream metadata are the offline input
used by CI.

## Printed card data

`Marvel.Cards.Extract` joins the vendored MarvelSDB snapshot with the committed
supplement. It writes `datasets/cards/cards.json` deterministically.

```bash
dotnet run --project tools/Marvel.Cards.Extract -- write
dotnet run --project tools/Marvel.Cards.Extract -- check
dotnet run --project tools/Marvel.Cards.Extract -- diff
```

Behavioral specs and ability rows are authored from this joined, corrected
dataset. A live website and a retired engine are research aids only.

The supplement records facts missing or incorrect upstream. Every entry states
its printed authority. Expansion entries remain research data until their
product boundary opens.

## Product rules

Some expansions add rules that the base Rules Reference does not contain.
`Marvel.Rules.Packs.Harvest` reads local expansion PDFs listed in the committed
source manifest and writes the vendored pack snapshot.

The source PDFs stay local. The manifest checks both the local inputs and the
committed output offline. A product-rule id does not become a `[Rule]` citation;
the base Rules Reference and product rules remain different authorities.

## Rulings

The 2 ruling datasets serve different queries:

- MarvelCDB FAQ entries are organized by card code; and
- Hall of Heroes rulings can answer general rule questions without naming one
  card.

Their harvesters separate acquisition from generation:

1. acquire a complete local candidate or cache;
2. review the source metadata and content;
3. build the vendored index offline; and
4. run the offline check against the committed snapshot.

A ruling does not silently replace a rule because it is newer or later in a
file. `datasets/rules-graph.json` must name the relationship and pin both source
hashes.

## Rules graph

The rules graph points from an exception or modification to the base rule it
changes. Reverse relationships are computed.

```bash
dotnet run --project tools/Marvel.Rules.Index -- refs rr:tough
dotnet run --project tools/Marvel.Rules.Index -- refs --orphans
dotnet run --project tools/Marvel.Rules.Index -- resolve rr:tough 1.8
```

Each edge states why it exists. Every id and hash is tested. A changed base or
ruling hash therefore invalidates the relationship instead of letting it drift.

Controls cite the base rule. A test that proves ordinary damage stays on its
target cites damage, not Overkill. An exception test cites the exception. If the
exception vanished and the test would still pass, it is a base-rule test.

## Citation report

`Marvel.Rules.Index` reads `[Rule]` attributes directly from source:

```bash
dotnet run --project tools/Marvel.Rules.Index -- citations
dotnet run --project tools/Marvel.Rules.Index -- citations --uncited --sort
dotnet run --project tools/Marvel.Rules.Index -- citations --cited
```

The report is a measurement, not a percentage gate. Some Rules Reference records
are vocabulary, examples or physical procedure with no independent engine
decision.

The authority-derived catalog under `specs/behavior/` classifies the complete
Rules Reference and supported Core authorities. It is the current coverage
contract. See [behavioral-specification.md](behavioral-specification.md).

## Patch loop

Use this sequence when a rulebook, ruling, card snapshot or product insert moves:

1. Update the pinned source metadata and acquire any required local source.
2. Rebuild the affected dataset offline.
3. Review the diff as an authority change, not a formatting change.
4. Update `datasets/rules-graph.json` when a ruling modifies another source.
5. Regenerate the behavior catalog.
6. Review every obligation whose source fingerprint changed.
7. Update executable transcripts, ability rows and rule-cited tests.
8. Run dataset checks, the Release build, the complete test suite and the Godot
   wall.

The source fingerprint identifies the review surface. It never blesses an old
expected result against changed authority text.

## Adding an expansion

An expansion refresh can add card facts and product rules without changing
runtime scope. Opening the product is a separate coherent change.

Before claiming support, provide its complete setup records, every reachable
ability row, applicable product rules, rulings, executable transcripts and
fail-closed boundary tests. See [scope.md](scope.md).

This separation lets the repository research later content without presenting a
partly authored expansion as playable.
