# The per-card spec campaign

How 3,996 cards get behavioral specs, in what order, and how deep. MARVEL-68.

Read [spec-harness.md](spec-harness.md) first — it is how to write one. This is
how many to write, for which cards, and when to stop.

## The number that matters

```bash
cd py_src
python -m tools.spec.coverage
python -m tools.spec.coverage --tier interactive --pack core
python -m tools.spec.coverage --shallow          # covered, but short of plan
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

## Delegating authoring

MARVEL-87 ran the experiment: cheap agents on the same six core cards under two
protocols. The result that matters is not a pass rate, it is a **gradient**:

| protocol | scenarios | pass rate | load-bearing |
|---|---:|---:|---|
| Haiku, engine access, iterate to green | 14 | 100% | 2 of 14 |
| Haiku, blind, printed text only | 17 | 24% | 2 of 4 passes |
| Large model, blind | 10 | 60% | all |
| Large model, deliberately vacuous but plausible | 10 | 90% | none |

**Weakening a spec raises its pass rate.** A reviewer looking only at colour
cannot tell a good batch from a hollow one, and the coverage number goes up
either way.

The iterate protocol also destroyed a real finding. Authoring Jessica Jones, an
agent hit `FAIL-engine-suspected`, deleted the scenario, and replaced it with a
health check. That failure was real and is now MARVEL-86. A process that
converts findings into deleted lines is not a weaker version of this suite; it
is the opposite of it.

### Two protocols work. Pick by model tier and reviewer budget.

**Blind.** Withhold the engine — Read/Write only, no validator, no
`py_src/cards/`. Strip the `engine` block from the card data handed over; it
carries `script.ability_factories`, which is engine behaviour, not printed text.
Require a prediction ledger: one row per numeric assertion, deriving it from
printed numbers. 60–75% of output lands in quarantine and a human walks the
queue. This is the only protocol that has been shown to work with cheap models.

**Sighted, attributed, mutation-checked.** Give the agent the engine and the
`--no-write` validator, and impose two rules: a failing scenario may be edited
**only** when the author can name the specific thing they misread in the printed
text, and any failure they cannot attribute comes back unresolved with the
transcript. Then the reviewer **mutation-tests the result** rather than reading
the colour.

The first core-set batch used the second protocol with session-tier models: 24
scenarios, 24 passing, no unresolved cases, and three independent discoveries of
a real vocabulary gap (MARVEL-94). Spot mutation showed the scenarios were
load-bearing in both directions. That is a genuinely different outcome from the
study's iterate row, and the difference is not the engine access — it is the
attribution rule and the fact that nobody accepted a pass rate as evidence.

**Do not add a pass-rate floor to `--check-drift`.** It is the obvious response
to the table above and it is wrong: the batch just described would have tripped
it at 100%, and a vacuous batch sits at 90%. Pass rate does not separate them in
either direction. Mutation does.

### What still holds regardless of protocol

- **One card per file.** A single unparsable step aborts the whole validate run
  — exit 2, no results for any file in the tree.
- **Do not delegate `interactive` blind.** It is where the format earns its
  verbosity, and 117 of 467 `ForChoiceAbility` calls pass an empty label, so a
  quarter of interactive choice points have no domain name a printed-text author
  could possibly guess. That is MARVEL-41 work and it blocks faithful blind
  authoring whoever does it.
- **`declarative` is not the easy tier.** It is read from script shape, not
  behaviour: a sample of 25 includes Thanos, Baron Zemo, and a card whose text
  begins "Create your own game area".
- **Watch `at depth`, not `covered`.** `python -m tools.spec.coverage` reports
  both. `covered` credits a card for one trusted scenario whatever its tier
  plans; the gap between the two columns is how much of the covered set is one
  scenario deep, which is exactly what mass delegation produces if nothing
  watches for it. `--shallow` lists what is in that gap, biggest shortfall
  first, the way the default listing does for uncovered cards. Finishing a card
  from that list is cheaper than starting one, because whoever wrote its first
  scenario already read the script — so clear it before opening a new shard.

## A reprint is not a second card of work

318 specifiable cards carry a `reprint_of` link, and every one prints text
byte-identical to the card it reprints. **308 of them also run the same script
module**, and for those a scenario is not a second claim about a second card —
it is the same claim about the same code and the same text, written twice.
`coverage.py` credits the original's scenarios to them, so they do not appear as
work to do, and the report says how many it credited rather than folding them in
silently.

The credit is earned per card by comparing `engine.script.path`, never assumed
from the reprint link. The other **10 reprints run a script file of their own**;
no pair is byte-identical and six of the ten disagree in behaviour, so they are
the one group where the two ids provably do different things. They are never
credited and the report names them (MARVEL-105, MARVEL-106).

This matters most for later packs, which are substantially reprints. Without it
`The Power of Aggression` alone is one card counted seven times, and a shard
walking a late pack would author the same scenario against the same module six
times with every count applauding.

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
