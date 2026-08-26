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
claiming anything about it; the claim is the assertion.

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

There is no report tool; these were the counts when the practice started.

```
Rules Reference v1.8

  entries             9 / 215   cited (4.2%)
  citable records    19 / 1152  cited (1.6%)

  citations made     24
```

```
$ python -m tools.rules.citations --uncited --sort   # the work list
$ python -m tools.rules.citations --cited            # who cites what
```

`--uncited --sort` orders by clause count, a rough proxy for how much engine
surface an entry touches. It is a reading order, not a backlog: a good deal of
the glossary is vocabulary (`rr:you-your`, `rr:and`) that no test should be
expected to assert. Triage is a person's job.

Two things this deliberately does not do:

- **It does not gate.** There is no `--check` and no checked-in fixture. The
  seven byte-gated fixtures exist because the C# port is *accepted against*
  them; a coverage number is a measurement, and gating it would make every
  added test touch a generated file for no oracle value.
- **It does not validate ids.** A report that silently dropped a bad citation is
  how a mistyped citation survives. Validation belongs in the suite, where it
  fails a build.

## Uncited is not untested

A test with no `[Rule]` is not a lesser test — it is a test whose authority is
something other than the Rules Reference:

- **the corpus**, for tests that hold the engine against a recorded game. Most of
  `PlayerPhaseTests` is this, and it is the strongest evidence available.
- **printed card text**, pinned through `datasets/cards/`.
- **a rules pack**, for expansion and scenario rules that amend the base game.
  `datasets/rules-packs/` is a separate dataset with no index, so `[Rule]`
  refuses anything but the `rr:` scheme rather than pretending to cover it.
- **nothing published at all.** `TokenPoolsSurviveLeavingPlay` is the honest
  example: no rule says a card acquires a token pool. It is an artefact of the
  Python engine's serialisation that the digest forces the port to reproduce.
  Leaving it uncited is the accurate statement.
