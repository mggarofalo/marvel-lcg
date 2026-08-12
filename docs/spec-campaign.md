# The per-card spec campaign

How 3,996 cards get behavioral specs, in what order, and how deep. MARVEL-68.

Read [spec-harness.md](spec-harness.md) first — it is how to write one. This is
how many to write, for which cards, and when to stop.

## The number that matters

```bash
cd py_src
python -m tools.spec.coverage
python -m tools.spec.coverage --tier interactive --pack core
python -m tools.spec.coverage --out coverage.json
```

**Coverage is a card with at least one *trusted* scenario tagged `@card:<id>`.**
A quarantined scenario is a claim that failed, so it is reported separately and
never counted — otherwise the number would go up when authoring goes wrong,
which is the one direction a coverage metric must never move.

## The denominator is 3,996, not 3,781

3,781 cards have a script. 3,996 are specifiable. The difference is the 215
cards the engine has but does not script: their hit points, attack values and
printed keywords come from `game/card/face/attribute/`, which the engine applies
to every card that prints them.

This is not a technicality. `specs/rules/keywords.feature` pins Hydra
Mercenary's Guard and Sandman's Toughness, and **neither card has a script**. A
rule that read "no script" as "nothing to specify" — which is what the first
version of `tools/spec/coverage.py` did — put both of them, and 561 others,
outside the campaign while the suite already had specs for them.

What is genuinely out of scope is `absent`: **348 cards the engine does not
implement**. A scenario cannot name a card the engine has never heard of.

`unit_test/test_spec_coverage.py` guards the denominator from both sides: no
scripted card may be called `absent`, and the specifiable population must stay
strictly larger than the scripted one. If those ever come out equal the rule has
collapsed back to the bug.

## Depth per card

Not a fixed quota. "Three scenarios per card" over-serves the 602 cards with no
branch to take and under-serves the 479 that stop mid-resolution to ask a
question. The tier is read from `engine.script` in `datasets/cards/cards.json`,
built by `python -m tools.cards.extract`:

| Tier | Cards | Plan | What it means |
|---|---:|---:|---|
| `interactive` | 479 | 4 | calls `PlayerAsk` / `ChooseAbilities` / `MayChooseOneAbility` / `AskSpendResources` — the card asks the player something |
| `imperative` | 2,700 | 2 | a handler that does something, but never suspends |
| `declarative` | 602 | 1 | declarative factory calls, no branch a scenario could take differently |
| `stats_only` | 215 | 1 | no script; printed stats and keywords, implemented generically |
| `absent` | 348 | 0 | the engine has no such card |

**The plan column is a target, not a gate.** A card that needs four scenarios
gets four whatever its tier says; the tier decides which cards are worth arguing
about. The one hard rule is the format's: **one scenario per decision path**, so
a card with three branches gets three scenarios and not one scenario with three
assertions.

An `interactive` card is where the transcript format earns its verbosity —
`Then I am prompted to choose one` pins the *question*, and only these cards ask
one. Nick Fury needs three. That is the shape to calibrate against.

## Sharding: by pack, deepest tier first

Two orderings were considered and one of them is a trap.

**By coverage gap** — author whatever MARVEL-16 says self-play cannot reach —
sounds efficient and is the wrong primary axis. Those cards are scattered across
every pack, so each one costs a fresh scenario bring-up: a new villain, a new
encounter set, new filler that has to be checked for triggers of its own. The
bring-up dominates the authoring.

**By pack** amortises that. One scenario boots once and serves every card in the
set, and the packs are already the unit the game ships in. So:

1. **Shard by pack**, largest specifiable population first.
2. **Within a shard, deepest tier first** — `interactive`, then `imperative`,
   then `declarative` and `stats_only`. `tools/spec/coverage.py --pack <p>
   --tier <t>` prints exactly that list, ordered by script size so the head of
   it is where a scenario buys the most.
3. **MARVEL-16's unreachable list is a mandatory overlay, not the order.** A
   card self-play cannot reach has no corpus evidence at all, so it must have a
   hand-authored spec before its pack is called done. It does not get to
   reorder the packs.

The core set is the first shard: 211 cards, all specifiable, 22 `interactive`.
It is also the only pack the harness has proven it can boot, which is the second
reason to start there — every other pack has an unknown bring-up cost, and
finding that out on a 236-card pack is worse than finding it out on this one.

## Triage is not optional

Every disagreement is a spec bug or an engine bug and both are worth finding.

- `FAIL-spec-wrong` — the engine never offered what the scenario describes.
  Usually a misread card. Fix the scenario.
- `FAIL-engine-suspected` — it ran cleanly and disagreed anyway. **Read this
  one carefully**, and raise it as its own issue rather than editing the
  scenario until it passes.

A scenario is never deleted to make a number go up. If it cannot be resolved it
stays quarantined with its root cause recorded, which is what the quarantine is
for.

**Every count so far has been the spec's fault, not the engine's.** MARVEL-23
authored 33 rule scenarios and every first-run failure was a misreading — twice
the same one, forgetting that a villain's activation is boosted. That is the
expected ratio, and it is why `FAIL-engine-suspected` deserves attention when it
does appear.

## What "done" means for a shard

- every `interactive` card in the pack has a scenario per decision path
- every `imperative` and `declarative` card has at least one trusted scenario
- every card on MARVEL-16's unreachable list for that pack has one
- `python -m tools.spec.coverage --pack <p>` reports no uncovered card that is
  not quarantined with a recorded reason
