# Rules citations

How a C# test says **which published rule** it is holding the engine to, and how
the rules nothing holds the engine to become a list.

## The attribute

```csharp
[Rule("rr:scheme-enemy-activation.step.2.c")]
[Rule("rr:scheme-enemy-activation.step.3")]
[Theory]
[InlineData(0, 1)]
[InlineData(1, 2)]
public void BoostIconsAddToTheSchemeValue(int boost, int expected)
```

The id is a citation into `datasets/rules-reference/index.json`, the vendored
Rules Reference described by [`rules-provenance.md`](rules-provenance.md). Three
grains are citable, and a test should cite the narrowest one that is actually
what it asserts:

| Form | Means |
|---|---|
| `rr:forced` | the entry |
| `rr:forced.4` | its fourth clause |
| `rr:forced.3.1` | a qualification of the third clause |
| `rr:villain-phase.step.2.b` | an enumerated step, where the entry has steps |

`[Rule]` goes on tests only. On the implementation it would name a rule without
claiming anything about it; the claim is the assertion. Most citations use the
`rr:` forms above. A test that specifically asserts a published modification
uses its audited `ruling:` id from `datasets/rules-graph.json`; an arbitrary
Hall of Heroes ruling is not a rules citation until that relationship exists.

## Why an attribute rather than a comment

The citations began as comments, and six of the nineteen named clauses that do
not exist — `rr:villain-phase.2b`, `rr:scheme-enemy-activation.2c` and four
others, all of them missing the `.step.` infix that separates an enumerated step
from a clause. Each read as authority in a code review and none of them resolved
to anything.

`RuleCitationTests.EveryCitedRuleExists` is what makes the difference. It is
linked into all three test projects, so each holds its own citations, and a
citation naming no rule fails the build. When the Rules Reference is
re-harvested and a clause is renumbered or withdrawn, the build says so rather
than the comment quietly going stale.

## The report

```
$ dotnet run --project tools/Marvel.Rules.Index -- citations

Rules Reference v1.8

  entries             186 / 262   cited (71.0%)
  citable records     867 / 1221  cited (71.0%)
  modifications         2 / 3     cited (66.7%)

  citations made  2571
```

```
$ dotnet run --project tools/Marvel.Rules.Index -- citations --uncited --sort
$ dotnet run --project tools/Marvel.Rules.Index -- citations --cited
```

It reads the `[Rule("...")]` attributes off the source under `tests/` rather
than reflecting over the built assemblies, so a report of what has been written
does not depend on whether the suite currently compiles.

`--uncited --sort` orders by clause count, a rough proxy for how much engine
surface an entry touches. It is a reading order, not a backlog: a good deal of
the glossary is vocabulary (`rr:you-your`, `rr:and`) that no test should be
expected to assert. Triage is a person's job.

[`rules-reference-v18-record-audit.md`](rules-reference-v18-record-audit.md)
records that triage for all 1,218 records. It is a work list, not another
coverage gate.

Two things this deliberately does not do:

- **It does not gate.** There is no `--check` and no checked-in fixture. A
  coverage number is a measurement, and gating one would make every added test
  touch a generated file for no gain. What gates is whether a citation
  *resolves*, and that is the suite's job.
- **It does not validate ids.** A report that silently dropped a bad citation is
  how a mistyped citation survives. Validation belongs in the suite, where it
  fails a build. An id the index does not know is counted and marked
  `(no such rule)` under `--cited`, so the report is not silent about it either.

## The graph

`datasets/rules-graph.json` is the other half, and the one the harvested index
cannot carry: which rule qualifies which, and which published ruling modifies
which base record. It is hand-authored, one-way — "an exception names the rule
it overrides or extends; a base rule names nothing" — and every relationship
records why, because a plausible-but-wrong relationship is the failure mode it
exists to eliminate.

```
$ dotnet run --project tools/Marvel.Rules.Index -- refs rr:tough

rr:tough  TOUGH
  Tough is a status that prevents a character from taking damage.

names:
  -> rr:damage  DAMAGE
     A tough status card cancels the damage a character would take, which is an
     exception to base damage application.

named by:
  <- rr:piercing  PIERCING
     Tough prevents damage. Piercing discards tough status cards from the
     attacked character before damage is dealt, so it removes the prevention
     rather than the damage.
  <- rr:toughness  TOUGHNESS
     Tough is a status a character can be given. Toughness gives it automatically
     on entering play.
```

**"Named by" is the query the graph is for**, and it is computed rather than
stored: a stored reverse edge is a second place for the same fact to be wrong.
An id is matched by its entry as well as itself, so asking about `rr:thwart`
finds the three edges that name `rr:thwart.1`.

`RulesGraphTests` gates the file the way `RuleCitationTests` gates the
attributes — every id resolves, every edge says why, and no rule names itself.
`refs --orphans` lists the same failures on one screen for whoever is fixing
them.

Modification relationships add two stronger gates. Their `supersedes_hash`
must still equal the vendored base record, and their id must still resolve to a
Hall of Heroes record carrying source, RRG scope, date when the source provides
one, and content hash. `refs rr:id` includes them in the reverse direction, so
a base citation is in the blast radius of a new ruling. `resolve rr:id 1.8`
shows whether the base text or a modification is current; an absorbed ruling
remains available through `refs ruling:id` and as a direct citation.

## Uncited is not untested

A test with no `[Rule]` is not a lesser test — it is a test whose authority is
something other than the Rules Reference:

- **printed card text**, pinned through `datasets/cards/`. Most of the card
  tests are this, and for an authored card it is the strongest evidence there
  is.
- **a rules pack**, for expansion and scenario rules that amend the base game.
  `datasets/rules-packs/` is a separate dataset with no index, so `[Rule]`
  refuses anything but the `rr:` scheme rather than pretending to cover it.
- **a wire format the engine chose.** The state digest's spelling and the
  MT19937 stream are pinned by `StateDigestTests` and `MersenneTwisterTests`,
  and no rule decides either — one is our choice and the other is ISO/IEC
  14882 §rand.predef. Leaving them uncited is the accurate statement.
