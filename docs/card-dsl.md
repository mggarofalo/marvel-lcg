# The card DSL

MARVEL-92. The design, and the measurements it was designed against.

[migration.md](migration.md) settled that cards become data rather than
sandboxed scripts, and set one design rule:

> **Design the DSL against the hardest ~30 cards first, not the common ones.**
> The common cases fall out for free; the tail does not. Projects like this
> routinely hit 90% quickly, then bolt escape hatches onto the DSL until it is a
> scripting language again.

This document follows that rule. The 30 were identified in MARVEL-92; all 30
were read, and the node set below exists because of specific ones. Where a node
is here for one card, that card is named.

This was the design the port was measured against, with nothing implemented.
**A first slice now runs** — see [What is implemented](#what-is-implemented) at
the end. The rest is still design, and the place to argue with it is still
before it is built.

## The finding that reframes the problem

**Most of a card is already data.** The imperative handler is not the card — it
is one field of an `Ability` whose other fields are declarative today:

```python
AbilityFactory.WhenInYourPlayTurn(AbilityType.HeroAction, hex_bolt)
    .SetPlay()
    .SetTarget(Enemy, canbe_heal=True)
    .SetCost(Cost("1"))
    .SetCostFunc(CostFunc.Discard("YourPlayerDeckTop"))
    .LimitOncePerPhase()
```

Every line but the handler name is a literal. `SetTarget` is a 220-line
declarative selector; `SetCondition` takes fourteen named printed conditions
(`only_if_your_identity_has_trait`, `only_if_you_control_face`, …); the six
`Limit*` methods are the printed use-limits. Measured over `GetAbilities`
bodies, **22.9% of statements are this envelope** — counting every `ast.stmt`
inside a `GetAbilities` and splitting on whether it sits inside a nested
function, with the handler `def` itself counted as envelope. And **531 scripts
(15.4%) are nothing else**: they have no handler function at all. That second
figure is the card dataset's `no_imperative_handler` stratum, recomputed on every
run of `python -m tools.cards.extract`, not a number measured here.

So the work is not "invent a card language". It is:

1. Formalise the envelope, which is already declarative, into a schema.
2. Replace the `operation` callback with an **effect tree**.
3. Decide where the effect tree stops and compiled code begins.

Point 3 is the whole design. Points 1 and 2 are transcription.

## What is measured

The numbers below were measured once, by walking every card script in the
implementation this design replaced and flagging constructs a tree of typed
nodes cannot hold without a node designed for them. They are not re-runnable:
the scripts are gone, and what replaces them is this document plus
`datasets/abilities/abilities.json`.

| | |
|---|---|
| Card scripts | 3,454 (three files under `cards/pack/` define no `GetAbilities`) |
| Carrying no blocker at all | **2,708 (78.4%)** |
| Trigger vocabulary | 303 `AbilityFactory` methods, 5,667 sites; top 25 = 73.2%, top 100 = 91.7% |
| Operation vocabulary | 866 distinct, 24,984 sites; top 100 = 80.5% |
| Operations used by ≤2 cards | 335 names, but only **2.0% of call sites** |
| Ways a card asks a player something | **21** in the engine, 17 used by cards, 514 sites |
| Durations `RegisterTemp` accepts | **7**, plus `afterExec`; cards use 5 |
| Recompute triggers for continuous effects | **34** `OnEvent` kinds defined, 24 used, 96 scripts |
| Engine queries a card can answer | **7** distinct, 212 sites |
| Cards installing an ability onto a card they do not own | **2** |
| Scripts with a setup-triggered ability | 65 (1.9%) |

Two of these deserve comment because they change the shape of the problem.

**The long tail is a tail of names, not of volume.** 335 operations are used by
two cards or fewer, which reads alarming; those 335 account for 2.0% of call
sites. The risk they carry is correctness, not size — each is a rule nobody will
think about twice — but they are not a reason the DSL has to be large.

**The question vocabulary is 21 operations.** In the engine architecture
(`(state, input) -> (state, prompt)`) every one of these is a suspend point, so
this is the single most important bounded set in the design. **Do not hand-write
the list.** `tools/cards/scripts.py` derives it from `PlayerAsk` plus three entry
points on `PlayerAction`, precisely so that a prompt added to the engine is
counted instead of silently going missing. The five heaviest were
`ChooseAbilities` (299 sites), `DiscardControlCards` (51),
`MayChooseOneAbility` (40), `DiscardHandCards` (39) and `AskChooseFace` (35).

Those counts are a measurement of the engine that was being replaced, and both
it and the tool that counted have been removed. They are here as the evidence
the vocabulary was sized against, not as something to re-run.

### Two superseded measurements

**"Top 100 operations cover 86.3% of call sites"** (MARVEL-92) recorded no
counting rule, and no rule reproduces it: attribute calls give 80.5%, adding
bare-name calls 80.0%, excluding `CastTo`/`Unused` boilerplate 77.5% and 74.8%.
The figure above states its rule — every attribute call over every
`cards/pack/**.py` — and supersedes it.

**"Twelve ways a card asks a player something"**, which an earlier draft of this
document asserted from a grep. It was wrong in both directions: it missed six
real prompts (`DiscardControlCards`, `DiscardHandCards`, `AskDiscardFace`,
`AskSpendResources`, `AskDiscardFaces`, `MayChooseFace` — 114 sites, 22% of the
total) and included `DeclareDefender`, which takes the already-chosen unit as an
argument and suspends nothing. Both mistakes are the failure mode the paragraph
above them describes, committed while describing it. The rule now cited is the
engine's own.

## The node set

### Layer 0 — the ability envelope

Already data; this only writes down its shape.

```
ability := {
  trigger:  { event, timing, subject }      # 303 methods factor into these
  when:     [ named condition ]             # the 14 printed conditions
  cost:     { resources, exhaust, discard, removeCounters, ... }
  target:   selector                        # the existing SetTarget vocabulary
  limit:    oncePerRound | oncePerPhase | oncePerEvent | ... (+ key)
  effect:   node
}
```

`timing` is the existing `TimingPriority` enum, whose adjacent-pair ordering is
proven by specs (`specs/rules/timing-priority.feature`, MARVEL-83). It is not a
new invention and must not become one.

Three envelope forms are **not** trigger-and-effect and need their own shapes,
because trying to express them as effects is what produces escape hatches:

- **`static`** — a continuous modifier with no trigger. `Laser Swords` (44055)
  reads "+1 ATK for each crisis, acceleration, amplify and hazard in play (to a
  maximum of +4)". That is a standing expression, not an event. It carries
  `dependsOn`, an invalidation mask drawn from the `OnEvent` kinds — 34 defined,
  24 used by cards — plus a provenance list so the client can show which cards
  are being counted. The mask a card declares is not the whole dependency:
  `asset_helper.py:47` adds `OnEvent.AssetEffect()` on top, and `WhileValid`
  separately anchors on card-enters-play, treat-as-blank and a bind-face
  recheck, none of which are `OnEvent` kinds. A `static` node has to carry all
  of it, not just the card's line.
- **`prevents`** — a continuous prohibition with a live predicate.
  `Proxima Midnight` (21092) "cannot be defeated while Corvus Glaive has any hit
  points remaining", which must re-evaluate on `OnEvent.Health` and defeat her
  retroactively when it lapses.
- **`answers`** — a card that responds to an engine question rather than causing
  anything. `GetVillain`, `GetMainScheme`, `CanPlayThisUpgradeCard`,
  `CanGenerateResources` and three others; 212 sites. Sinister Synchronization
  (27100a) redefines "who is the villain" for a four-villain scenario.

### Layer 1 — the effect tree

**Control.** `seq`, `if`, `forEach`, `let`, `switch`, `stop`.

**Questions.** One node per measured suspend point. Two properties the imperative
form has and a naive design would lose:

- Alternatives are **effect subtrees**, not card targets. `Luck Be a Lady`
  (40041) offers "heal 2 / remove 2 threat / deal 3 damage" as three sub-effects
  each with its own target selector and legality filter. This is the single most
  common shape in the corpus: 473 `ForChoiceAbility` sites.
- Questions **nest**. `Erratic Teleportation` (39019) has "you may spend a mental
  resource to look at the top card" — an optional cost paid *inside* a When
  Revealed, whose payment opens its own resource-selection dialogue, and whose
  outcome gates everything after. A flat prompt model cannot hold this; the engine
  can.

**Bindings.** Four kinds, each demanded by more than one card:

| binding | what it names | forced by |
|---|---|---|
| `trigger.*` | fields of the event being responded to | 51017 reads how much threat was removed and places exactly that much |
| `cost.*` | what the cost actually consumed | 53018 reads *which cards paid for it*; 44056 reads the card the cost discarded; 37006 reads how many counters another ability removed |
| `result.*` | what an action actually did, not what it asked for | 01146 gains surge "if no damage was healed this way" — and `HealthUnits` returns 0 when the villain is at full health, so a pre-check is silently wrong |
| `let` | a value or collection computed and read later | 03030 must remember which players were *not* attacked, computed during a pass that changes the board |

`result.*` is the one most likely to be skipped and it is not optional. Three of
the thirty print "if no X was made this way".

**Actions.** The ~100 operations covering 80.5% of call sites, typed. Damage and
threat carry property records (`piercing`, `overkill`, `additional_value`)
rather than being separate operations — 37006 turns a counter count into
`ranged`/`piercing`/`overkill` on one attack.

**Grants.** `grantUntil { ability, duration }`. `FaceEffect.RegisterTemp`
(`game/card/face/effect/face_effect.py:97`) accepts seven durations —
`turnEnd`, `nextTurnEnd`, `phaseEnd`, `roundEnd`, `thisLeavePlay`, `eventEnd`,
`resolveEffect` — plus the `afterExec` flag, and the node has to carry all
eight even though cards currently use five of the seven. (`nextVillainPhaseBegin`
is *not* one of them; it belongs to `TreatAsIfBlank` and is used by one card.
An earlier draft listed it here and dropped two real ones, which is what a
vocabulary transcribed from usage rather than from the API looks like.)

**56 cards** use `RegisterTemp` today, plus three more that hand-roll the same
thing with `Registers` and a matching `UnRegister`. This node retires all 59.

**Queries and arithmetic.** Collection queries over zones and the board, plus:
`count`, `sum`, `min`, `max`, `minBy`, `maxBy`, `any`, `all`, `firstWhere`,
`filter`. `maxBy` must keep ties as a set — Breakout 1B (07001b) moves the active
counter to the villain whose scheme has the most threat, "if there is a tie, the
first player chooses". Trait-derived ordinals count as keys: 27111 selects the
villain by "activation order N", which is encoded in a trait name.

**Card-local state.** Counters, declared by the card. `Blackout` (44053) tracks
three colours to two each; `Tic-Tac-Toe` (44057) tracks a 3×3 grid. Both are
counters plus a literal table, which is data — but note that today they are
*not*: `CardFace.COUNTER` is a hardcoded `Literal` in engine source listing every
counter name any card uses, including all nine of Tic-Tac-Toe's cells. Making
counters card-declared is part of this node, not a given.

**Selection constraints.** A predicate over the *whole* chosen set, plus a
per-candidate prefilter. `Suit Up` (45017) searches for "an ally and an upgrade
that can be attached to *that ally*" — the legality of the second pick depends on
the first. The engine has both hooks (`check_effect_fn` per candidate,
`check_again_fn` over the selection), and the DSL must expose them as a fixed
vocabulary of relations, **not** as a general "run this predicate" hook. That
hook is the escape hatch wearing a different hat.

What the DSL should *not* copy is the engine's failure behaviour: today a player
may pick a pair that cannot attach, and the ability then fizzles with the card
spent. A node that presents only legal combinations is better and is a
behaviour change — worth a spec before it is worth an implementation.

### Layer 2 — two interpreter semantics, not nodes

- **A source leaving play does not halt its own tree.** 45156 discards itself in
  branch two and branch three still runs; 51017 moves itself to the victory
  display mid-resolution.
- **Cancellation is `beInstead`, not a return value.** `stunned` and `confused`
  suppress a pending event and stop its broadcast, so ordering between two
  statuses on one unit is observable. This is engine-internal today, but dozens
  of authorable cards cancel or replace events, so it belongs in the DSL
  regardless of whether the status cards themselves stay compiled.

## What each node buys

`python -m tools.dsl.blockers --greedy`, adding one node at a time, most cards
first:

```
start: 2708 of 3454 (78.4%) carry no blocker at all

+ node                        clears   of 746   all cards
effect subtree as a value        166      166    83.2%
observe                          310      476    92.2%
count / sum query                 64      540    94.0%
grantUntil                        55      595    95.6%
collect                           53      648    97.2%
filter                            23      671    97.8%
lookup                            23      694    98.5%
typed subject match               20      714    99.1%
take / drop                       14      728    99.5%
any / all / firstWhere            13      741    99.9%
card-local counters                3      744    99.9%
let                                1      745   100.0%
```

**The curve is a ceiling, not a forecast.** Every row is a claim that one node
covers a whole construct class, and the tool cannot check that claim — `observe`
is asserted to cover all 385 closure cards on the strength of being designed
against four of them. The shape is the finding: two nodes take 78.4% to 92.2%,
and the remaining ten buy 7.7 points between them.

**Two corrections that moved these numbers**, kept here because the first one
inverts the caveat the tool used to carry:

- The scanner flagged a `lambda` passed inside a handler but not a locally
  defined function passed *by name* — the same construct, and the same node.
  61 cards were counted expressible on that basis. Both forms are now
  `callback`, which is why row 1 covers 559 cards rather than 311.
- Handler *parameters* were not collected as bound names, so a nested function
  capturing `effect` — nearly all of them — read as capturing nothing, while
  mere name reuse between siblings read as capture. Both directions are fixed.

The tool still says it is "a static approximation and deliberately pessimistic",
and for over-flagging that holds. It did not hold for the largest error actually
found, which was **under**-flagging: the correction moved the headline down, from
80.8% to 78.4%. A hedge that only points one way is worth less than no hedge.

**A second surface this table does not measure.** `conditions=[lambda …]` in the
envelope costs a card nothing here. Most are named printed conditions; 55 of the
317 are more than one call or comparison, including one that walks a causal chain
to decide whether an attack belongs to it. That is the *condition language*, and
sizing it is separate work — `python -m tools.dsl.blockers` prints the count so
it cannot be mistaken for free.

## Where compiled code begins

The trust boundary is **provenance, not expressiveness** (migration.md).
Everything a user can author or download is data. A small first-party set stays
compiled. Measured, that set is:

**1. Permanent installation of an ability onto a card the source does not own —
2 cards.** Stephen Strange (09001b) seeds five Invocation cards and installs a
zone-redirect on each; Breakout (07001a) installs a when-defeated on each of four
villains as it builds their decks.

Seven scripts call `.Registers()`, but five call it on **themselves**, with
`AbilityType.Temp0`, and every one of those five tears it down again with
`Effects.UnRegister`. Three are duration-scoped grants written the long way and
belong to `grantUntil`; the other two are scoped observation (§3). Counting all
seven as escape hatches — as an earlier draft of this document did — inflates the
carve-out three and a half times and files two different mechanisms under one
name.

Even the two are less a case for an escape hatch than for a **missing engine
concept**. 09001b's real content is "the Invocation deck is a zone with these
properties: auto-reshuffle when empty, top card face-up, discards and draws
redirect into it". Model that as a zone kind and the card becomes data. Left as a
card-authored ability tree it does not fit, and no node will make it fit.

**2. Scenario and campaign setup — 65 scripts (1.9%).** Only 3 of the 30 hardest
carry a setup ability, but they are 3 of the top 4: Breakout (07001a), The Rise
of Red Skull (04128a), Sinister Synchronization (27100a). Setup handlers are 2.7%
of the statements inside `GetAbilities` and hugely over-represented at the top of
the hardness ranking, because they build decks, set engine flags
(`skip_create_encounter_deck`), read the campaign log, and put named cards into
play. (The 65 reproduces under a rule worth stating, since the obvious one does
not give it: scripts defining a handler whose `message` parameter is annotated
with a setup message. Counting `AbilityFactorySetup` calls instead gives 82.)

Much of it is *already data written as code* — Breakout's four encounter decks
are four literal lists of card ids. The recommendation is a **scenario setup
format** separate from the card DSL, not an extension of it. They have almost
nothing in common, and merging them is how the card DSL acquires deck
construction.

**3. Two cards that watch their own sub-resolution.** Promised Prosperity
(24005b) prints "each player who was not dealt at least 1 facedown encounter card
**this way**"; Crossfire's Crew (24023) deals 1 damage and needs to know whether
that damage defeated the character. Both install a temporary listener, filter it
on causal ancestry, run one sub-step, and tear it down.

**The printed phrase is not what forces this**, and an earlier draft of this
document claimed it was. Hail Hydra (03030) prints the identical wording — "Each
player who was **not attacked this way**" — and needs nothing: it is a plain
local boolean and an accumulator list, and it appears above as the example of an
ordinary `let`. The difference is not the text. In 03030 the card performs the
attacks itself, so it can see the outcome. In 24005b the deal happens inside a
*foreign* ability the card invokes by name (The Hood's "Foul Play"), and
`ResolveSpecialAbility` reports nothing back.

So the requirement is narrower and better-shaped than "watch my own
descendants": **an invocation has to yield what it did**, the same way
`result.*` makes `heal` yield what it applied. 24023 wants it on a primitive
action, 24005b on a named sub-ability, 37006 on another ability's cost payment.
One binding covers all three, and it is already in the node set.

That leaves general causal observation as a construct the corpus does **not**
demand. Do not build it. It is close enough to a scripting language to be worth
resisting, and the two cards that look like they need it need a return value
instead.

## Four cards, written out

Sketches, not a schema — the point is to show the node set carrying a card end to
end, including the parts that are easy to wave at. Printed text is quoted from
`datasets/cards/`.

### Spectrum (53018) — the shape most of the corpus has

> **Response**: After you play Spectrum, tuck 1 card used to pay for her under
> her. If that card's printed resource has: [mental] – +2 THW. [physical] – +2
> ATK. [energy] – +2 hit points. [wild] – All of the above.

```json
{ "trigger": { "event": "afterYouPlay", "subject": "this", "timing": "response" },
  "target":  { "from": "cost.paidWith", "count": 1 },
  "effect":
   { "seq": [
     { "tuckUnder": { "cards": "$target", "under": "this" } },
     { "forEach": { "in": "$target", "as": "paid", "do":
       { "seq": [
         { "if": { "test": { "printedResource": ["$paid", "mental", "wild"] },
                   "then": { "modify": { "card": "this", "thwart": 2 } } } },
         { "if": { "test": { "printedResource": ["$paid", "physical", "wild"] },
                   "then": { "modify": { "card": "this", "attack": 2 } } } },
         { "if": { "test": { "printedResource": ["$paid", "energy", "wild"] },
                   "then": { "modify": { "card": "this", "health": 2 } } } } ] } } } ] } }
```

Two things are load-bearing and neither is control flow. `cost.paidWith` is a
binding: the cards have already moved to the discard by the time the response
resolves, so this cannot be a query. And wild is a *member of each set* rather
than a fourth branch, which is why "all of the above" needs no node.

### Repair Sequence (01146) — why `result` is not optional

> **When Revealed**: Ultron heals 2 damage for each [[Drone]] minion engaged with
> you. If no damage was healed this way, this card gains surge.

```json
{ "trigger": { "event": "whenRevealed", "subject": "this" },
  "effect":
   { "let": { "villain": { "query": "villain" },
     "in": { "if": { "test": { "exists": "$villain" }, "then":
       { "let": { "healed":
           { "heal": { "card": "$villain",
                       "amount": { "mul": [2, { "count": { "query": "minionsEngagedWith",
                                                           "player": "trigger.player",
                                                           "trait": "Drone" } } ] } } },
         "in": { "if": { "test": { "eq": ["$healed", 0] },
                         "then": { "gainSurge": 1 } } } } } } } } } }
```

`heal` yields what it *applied*, not what it was asked for. `HealthUnits`
(`game/card/face/card_face.py:743`) returns the total healed, and the script
branches on that return value: with no Drones engaged the answer is zero, and so
is it with three Drones and an undamaged Ultron. A DSL whose actions return
nothing gets this card silently wrong in the second case, and nothing in a replay
corpus would notice, because the corpus records what the engine did.

The outer `if exists` is not decoration. The script wraps everything in
`if villain:`, so with no villain in play the card gains **no** surge — and a
first draft of this sketch dropped that guard and surged instead. It also
argues for naming the binding `amountApplied` rather than "did it happen":
`HealHealth` returns `None` when the target is not in play and `0` at full
health, and only one of those two should reach `gainSurge`.

### Blackout (44053) — card-local state and a computed question

> **Hero Action**: Spend 1 resource of any type → move 1 threat from a scheme to
> an empty space above that matches the spent resource. If all spaces above are
> filled, discard this card and confuse the villain.

```json
{ "trigger": { "event": "heroAction", "subject": "you" },
  "cost":    { "resources": 1 },
  "target":  { "query": "schemes", "count": 1 },
  "counters": { "y": 2, "r": 2, "b": 2 },
  "effect":
   { "let": { "space":
       { "askOneOf": { "options": { "filter": { "in": ["y", "r", "b"],
                                                "where": { "and": [
                                                  { "costPaidColour": "$it" },
                                                  { "lt": [{ "counter": "$it" }, 2] } ] } } } } },
     "in": { "seq": [
       { "removeThreat": { "from": "$target", "amount": 1 } },
       { "placeCounter": { "on": "this", "name": "$space", "amount": 1 } },
       { "if": { "test": { "all": { "in": ["y", "r", "b"],
                                    "where": { "eq": [{ "counter": "$it" }, 2] } } },
                 "then": { "seq": [
                   { "discard": "this" },
                   { "giveStatus": { "card": { "query": "villain" },
                                     "status": "confused" } } ] } } } ] } } } }
```

The card's six spaces are three named counters with a printed maximum. The
question's option list is *computed* — a wild resource offers all three colours,
a mental offers one — which is why `askOneOf` takes an expression rather than a
literal list.

**This sketch is not a transcription.** The script asks over the colours the
payment allows and tests fullness *after* the answer, so a player who picks a
full colour today gets a silent no-op: no threat removed, no counter placed, turn
spent. The `lt(counter, 2)` clause above removes the option instead. That is
almost certainly the better behaviour and it is still a behaviour change, so it
belongs in a spec and a Plane issue, not smuggled in through a design sketch.
The differential oracle would report it, correctly, as a divergence.

### Laser Swords (44055) — the static form, which has no trigger at all

> Your hero gets +1 ATK for each [crisis], [acceleration], [amplify], and
> [hazard] in play (to a maximum of +4 ATK).

```json
{ "static": { "subject": { "attachedTo": "hero" },
              "attack": { "min": [4, { "countIcons": ["crisis", "acceleration",
                                                      "amplify", "hazard"] } ] },
              "dependsOn": ["icons.inPlay"] } }
```

There is no event here and no effect tree. Writing this as a trigger — recompute
on every icon change — is how a DSL ends up with a hundred `after…` handlers
that are really one expression, and it is why `static` is Layer 0 rather than a
node.

Two things this sketch flattens. The engine's shape is `slot × scalar`:
`GiveKeywordToAttached(attack=1, get_new_value=…)` multiplies them
(`face_gain.py:117`), and it reads as a cap here only because the slot is 1. And
`dependsOn` is the card's declared mask, not the whole dependency — see the
`static` entry in Layer 0. A node that models only what the card writes down will
recompute at the wrong times.

### Tic-Tac-Toe: a correction

migration.md names two cards as genuinely not fitting a data DSL: Breakout
(07001a) and Tic-Tac-Toe (44057), the latter because it "computes win-lines over
a counter grid". Breakout holds up — it is scenario setup and foreign
installation. **Tic-Tac-Toe does not.**

> **Hero Action**: Spend 1 resource of any type → move 1 damage from a character
> to an empty space above matching the spent resource. If there are 3 damage
> tokens in a line, deal all damage on this card to an enemy and discard this
> card.

The grid is *printed on the card*. Nine spaces arranged 3×3, a row per resource
colour; "in a line" is a claim about that printed geometry, not a computation the
script invented. So the nine cells are nine named counters, the eight lines are a
constant table, the win check is `any(line, all(cell, counter > 0))`, and the
damage dealt is `count(cells where counter > 0)` — the same `count` query
Repair Sequence needs. Its blockers (`augassign`, `break`, `close`, `grow`,
`slice`, `string-build`) are all artefacts of writing that in Python; `close` is
a false positive on `to_counter_name`, which captures nothing, and is the bug
that prompted rewriting the scanner's scope analysis.

**The correction has a prerequisite the first draft did not mention.** Counters
are not card-local today: `CardFace.COUNTER` (`game/card/face/card_face.py:103`)
is a hardcoded `Literal`, and all nine grid cells are engine source
(`"tic_tac_toe_1_1"`, …), as are Blackout's three. Under today's engine, shipping
a card with a new counter means editing the engine — which is the opposite of
cards being data. So `card-local counters` is a real node with a real engine
change behind it, not a relabelling.

Three more demands this card makes, none of which are new nodes but all of which
a sketch would miss: the win branch asks a **second, nested** question
(`AskChooseFace` for the enemy, inside the resolution begun by
`AskChooseOneText`); the option list depends on the **wild-expansion rule**, since
`HasColor` treats one wild as satisfying every colour, so a single wild unlocks
all nine cells; and with no enemy in play the card deals nothing **and does not
discard itself**, leaving a completed line on the board.

This is still a real widening and the cost is worth stating: literal tables plus
`any`/`all` is exactly the direction that ends in a scripting language. The
judgement is that a table of constants is data and a quantifier over it is a
query, so both stay inside the line. If that judgement is wrong, 44057 is where
it will show.

## What this does not settle

- **The 78.4% is a static approximation, and it errs in both directions.** A card
  flagged `augassign` may be a one-line sum a `count` query expresses directly;
  a card the scanner calls clean may still be doing something a node tree cannot
  hold. Individual verdicts are not trustworthy. The ranking is more so, but the
  two rows that carry most of the curve are the two mechanisms that were wrong
  the first time, so treat even that as provisional until an interpreter exists.
- **Most nodes have not been executed.** Fourteen of them have; see below. The
  four cards written out above are still on paper, and two of those four
  sketches were wrong on first writing — one dropped a guard, one quietly
  changed behaviour — which is the argument for fixtures rather than prose.
- **Prompt order will change.** 40041's imperative form interleaves resource
  colours in a `while` loop that the printed text does not describe. A flat
  `forEach` produces different prompt order, which the differential oracle will
  report as a divergence. It will be right to.
- **`observe` (385 cards) is one name over what may be several mechanisms.**
  It is the largest unexamined claim here.
- **The condition language is unmeasured.** 317 envelope lambdas, 55 of them
  more than a single call or comparison. Sizing it is separate work.
- **Selection constraints are a behaviour change, not an exposure.** Suit Up
  (45017) uses `check_again_fn`, but on `False` the engine *fizzles the ability*
  — the played card is discarded, the search shuffles back, and the action is
  spent (`player_action.py:417-436`, whose comment reads `# Fix "45017"`). A node
  that offers only legal combinations is better and is not what the engine does.
  45017 also needs a second hook this document's Layer 1 does not name: the
  per-candidate prefilter `check_effect_fn`.

## The dependency that gates all of it

**This needs a behavioural oracle before it starts, not after.** Rewriting 3,454
card scripts without one is how a project introduces silent regressions across an
entire card pool: the engine keeps running, every game completes, and the cards
quietly do something slightly different.

The replay corpus is not a substitute. It pins that the engine reproduces
*itself*, so it would faithfully reproduce a mis-translated card. The oracle is
the behavioural spec campaign, MARVEL-68 — currently 5 of 3,996 cards.

So: **convert cards that have a scenario; do not convert cards that do not.**
Then measure the share expressible without an escape hatch per tier, and stop
widening the DSL rather than widening it to reach the tail.

## What is implemented

`src/Marvel.Cards`, and 64 card faces in `datasets/abilities/abilities.json` — every card the Rhino scenario reaches, the whole of the Standard set among them.

**Why it exists now rather than after the design settled.** It was standing in
the way. `Marvel.Content.Cards.CoreSetAbilities` was a compiled class with a
`switch` on printed card id, and the moment the engine could reach a second and
third card it started to grow — which is the "cards as scripts" inversion this
whole document exists to undo. A placeholder that grows is not a placeholder.

### The slice

| | |
|---|---|
| Envelope | `trigger { event, alsoHappened, timing, subject, actor, target, form, player }`, `name`, `cost`, `limitPerRound`, `effect`; and `attachTo` beside the abilities rather than in one. `event` is absent on a constant and on a "Setup" ability, and required on every other — see below. `actor` and `target` match explicit attack roles. |
| Costs | `spend` (resource letters), `exhaust`, `discardFromHand` (a count) |
| Control | `seq`, `if`, `choose`, `chooseCard` |
| Tests | `and`, `or`, `not`, `exists`, `hasStatus`, `hasTrait`, `isTitle`, `inForm`, `atLeast`, `titleInPlay`, `attackDamaged`, `discardedWithResource`, `defeatedByYou`, `wasDefeated`, `heroDefended`, `undefendedAttack`, `inExpertMode`, `isKind`, `defeatedBy`, `threatCause` |
| Actions | `giveStatus`, `attachTo`, `discard`, `draw`, `drawToHandSize`, `drawToPrintedHandSize`, `dealEncounterCards`, `createDrones`, `grant`, `grantEach`, `grantUntil`, `delayUntil`, `gainSurge`, `enemyAttacks`, `enemySchemes`, `dealDamage`, `placeThreat`, `removeThreat`, `preventThreat`, `replaceThreatWithDamage`, `preventThreatRemoval`, `preventDamageFrom`, `preventDamageWhile`, `heal`, `search`, `exhaust`, `ready`, `revealTop`, `reveal`, `shuffleInto`, `shuffle`, `discardUntil`, `discardAtRandom`, `changeForm`, `removeFromGame`, `indirectDamage`, `placeAtRandom`, `putIntoPlay`, `returnToHand`, `soakDamage`, `generate`, `doubleResourceFor`, `requireAllyDefender` |
| Node fields | `card`, `cards`, `player`, `amount`, `count`, `status`, `keyword`, `trait`, `title`, `area`, `areas`, `until`, `within`, `condition`, `effect`, `options`, `from`, `among`, `onto`, `enemies`, `against`, `engagedWith`, `first`, `where`, `scheme`, `sourceKind`, `sourceTrait`, `to`, `of`, `by` |
| Queries | `query: villain`, `query: mainScheme`, `query: minions`, `query: drones`, `query: dronesEngagedWithYou`, `query: minionsEngagedWithYou`, `query: heroes`, `query: identities`, `query: characters`, `query: heroesAndAllies`, `query: charactersYouControl`, `query: alliesYouControl`, `query: upgradesYouControl`, `query: supportsYouControl`, `query: upgradesAndSupportsYouControl`, `query: attachedToThis`, `query: identitySpecificInYourHand`, `query: yourAsideMinion`, `query: yourAsideSideScheme`, `query: yourAsidePile`, `query: sideSchemes`; `withTrait` filters another card collection |
| Card sources | `cardsIn { area | areas, kind, trait, title }` over `encounterDeck` and `encounterDiscardPile`; `enemiesWithTrait`; `titled`; `withoutAnotherCopyAttached`; `minBy` / `maxBy` over a query, `by` `cost`, `attack`, or `printedHealth` |
| Dynamic amounts | `count`, `damageOn`, `remainingHealth`, `discardedWithResource`, `modified`, `perPlayer`; arithmetic `min`, `add`, `mul`; conditional `if` |
| Players | `you`, `controller`, `trigger.player`, `engagedPlayer` for an engaged minion, `firstPlayer`, and `chosenPlayer` after choosing an identity |
| Amounts | a number, `{ "perPlayer": n }`, `{ "result": "healed" }`, `{ "tokensOn": … }`, `{ "damageOn": … }` |
| Bindings | `this`, `you`, `yourHero`, `chosen`, `attachedTo`, `trigger.subject`, `trigger.actor`, `trigger.target`, `defeated`, `activatingEnemy`; players `you`, `controller`, `trigger.player`, `defeater`; subjects `this`, `attachedTo`, `you`, `game`; attack roles `this`, `attachedTo`, `you`, `villain`, `minion`, `hero`, `ally`, `friendly`, `enemy` |

**`enemyAttacks` and `enemySchemes` schedule; they do not resolve.** An
activation is the six steps of `rr:attack-enemy-activation`, one of which asks a
player who is defending, so a card that causes one cannot resolve it and return
a list of events. The node puts a step on the agenda, and `Agenda.Then` places
it after the step that is running — which is also what `rr:surge.2` asks for:
the card that caused the activation finishes resolving first.

Which of the two happens is the *card's* to say, not the form's. `rr:activation.1`
reads a player's form to choose between attacking and scheming, but that rule is
about the activation the villain phase schedules. "Assault" says the villain
attacks you, and reading the form again here would make it do nothing to a hero
who had flipped since the card was dealt.

**`dealDamage` and `placeThreat` go through the engine's rule, never at the
token.** Damage written straight to `k_damage` walks past `rr:tough.2`, which
prevents *all* of an instance of damage and discards a status card instead, and
past `rr:defeat`, which is the other half of the same moment — leaving a
defeated character standing. Threat written straight to `k_threat` walks past
`rr:main-scheme-main-scheme-deck.2`, which completes a scheme the moment its
threat reaches its target however the threat arrived. Both are one-line
mutations that no state assertion in the card tests would catch, which is why
the tests assert the tough card and the ending rather than the number.

### Damage that happens to something else

`rr:damage` lists **nine steps**, and the engine now does six of them: step 1
(abilities that trigger when damage *would be* dealt), step 2 (tough cards),
step 5 (placing), step 6 (would be defeated), step 7 (When Defeated) and step 8
(discarding). Steps 3, 4 and 9 are ability windows nothing opens yet.

Step 1 is where a replacement effect sits — `rr:replacement-effect`'s "when
[triggering condition] would happen, do [replacement effect] instead", on 64
cards. Armored Rhino Suit is one: "when any amount of damage would be dealt to
Rhino, place it here instead".

**Being step 1 rather than anywhere else is the whole of it.** The tough card is
step 2, so a replacement leaves nothing for it to prevent — and a tough card
spent on damage that never arrived would be a tough card gone for nothing.
`rr:replacement-effect.1` then holds for free: the damage is no longer imminent,
so nothing later in the order can respond to it.

**Placed, not dealt.** The damage goes onto the attachment as tokens rather than
through `Damage.Deal`, which would start the nine steps again on a card that is
not a character.

**Forced only.** `rr:ability.11` makes everything optional unless prefaced by
"Forced", and an optional interrupt is a question — which needs a window, which
dealing damage has not got. A card that would ask here is refused by name rather
than resolved without asking.

### Damage the player divides

`rr:indirect-damage`, on **101 cards**. `.1` divides it "among characters under
their control"; `.2` is the group form, "among friendly characters in play",
which is what Explosion's *"assign X damage among heroes and allies"* means.

**It only asks when there is something to ask.** A player with no ally has one
character, so every point goes to their identity and there is no division to
choose — which is most of those 101. The question is put only when the eligible
characters can hold the damage more than one way.

Three clauses shape the rest:

- `.3` — "all indirect damage from a single source is **first assigned and then
  resolved simultaneously**", so the whole assignment is worked out before any
  of it is dealt. That is what stops the first point defeating a character and
  making the rest illegal.
- `.3.1` — no character is assigned "more than would cause it to be defeated",
  assessed "without accounting for interactions with other abilities". A
  character with nothing left is not eligible at all.
- `.4` — "characters that cannot take damage cannot be assigned indirect
  damage". A support has no hit points and is not among the heroes and allies
  however close it sits.

The prompt names a character **per point**, so the same character may appear
more than once: three damage on one hero is three entries.
`rr:choose-game-element.3.1`'s "the same target cannot be chosen multiple times"
is about *targets*, and a division is not a target list.

### An ability can ask more than once

Eviction Notice says *"you may flip to alter-ego form"* and then *"choose:"* —
two questions in a row. **36 cards in the pool pair a "may" with a listed
choice**, and every "may" is itself a question, so this is not one card's
peculiarity.

A suspended ability now remembers **where**, and that place is an index into its
top-level sequence: one number, which is what a `PhaseStep` can carry and what
survives a save. `Chose` runs the option and then runs the rest of the sequence
from there; if the rest holds another choice, it suspends again and says where
to pick up next.

**The resume point belongs to the top-level sequence and nowhere else.** Carried
on the `Cast` it leaked into any `seq` the chosen option itself contained — an
option of three effects resumed at two ran only the third. It is a parameter to
`Sequence` instead, which is why `Run` never sees one.

A choice nested inside an `if` inside a `seq` is still refused by name. Nothing
in the pool needs one, and inventing a path notation for it would be inventing
the general case for no card.

**`choose` is the shape the interpreter did not have.** Everything else a card
could do was something the engine could finish; "choose to either take 2 damage
or place 1 threat on the main scheme" is not — the ability has to stop, a player
has to answer, and only then does anything happen.

The mechanism is the agenda, the same one an activation uses. `choose` suspends
the ability and puts a `ChooseOption` step behind the step that is running; the
step asks; the answer runs the option. That gets `rr:surge.2`'s "finish
resolving the current card first" for free, which an inline question could not.

Two bounds are charged by name rather than guessed at:

- **A step carries a card, not an effect tree.** So the node is found again
  from the card, and a card holding *two* choices is refused — which of them
  was waiting would otherwise be a guess.
- **A `seq` resumes; anything deeper does not.** A choice nested inside an `if`
  is refused by name, because the resume point is one index into the top-level
  sequence.

`rr:choose-game-element.1` settles who is asked: the player resolving the
ability. For a revealed encounter card that is the player it was dealt to — not
the first player, and not the card's owner, which an encounter card has not got.
`rr:choose-option` gives no way out, so the prompt is not cancellable.

**`chooseCard` is the other question, and the rules already knew.**
`rr:choose-option` picks a branch the card lists; `rr:choose-game-element` picks
a card on the table. `Question.Option` and `Question.Element` were written with
those two citations long before anything asked either, so the second shape cost
a branch rather than a design.

The bound is the same one `choose` has, and it is what decided how Caught Off
Guard is written. "Discard an upgrade or support you control. If no cards were
discarded this way, this card gains surge" *looks* like a choice followed by a
check — which the interpreter cannot resume. But the instruction is mandatory,
so "no cards were discarded this way" happens exactly when there was nothing to
discard, and the card becomes an `if` on whether there is one. The choice ends
its branch, and nothing has to resume.

**`search` schedules the reveal it found.** Revealing an encounter card is a
step with an interrupt window and a response window around it, and a reveal
called inline would have neither — so the card found goes on the agenda as the
same `RevealEncounterCard` step the villain phase uses, and takes
`rr:reveal`'s four steps exactly as a dealt card does.

`rr:search.2` makes looking free: "cards being searched are not considered to
leave the searched area", so only the card found moves. `rr:search.3` shuffles
the deck afterwards — the *deck*; the discard pile is searched too and is not
one, and shuffling it would draw from the game's single random stream, which is
a wire format. `rr:search.1` gives the player the choice when several cards
match, which would be a second suspension inside an ability that may already
have one, so it is refused by name until a card needs it.

**`result.*` is the first thing the design called for that could not be faked.**
"Rhino heals 4 damage. **If no damage was healed this way**, this card gains
surge" cannot be answered by checking the villain's health first, because
`rr:heal.1` caps a heal at full health: a villain damaged by one heals one
however large the number on the card, and a villain at full health heals
nothing. So `Damage.Heal` answers with what it actually moved, `heal` records it
as `result.healed`, and the card compares with `atLeast`. The scope is one
resolution of one ability, because that is the scope the cards use — "this way"
is about this sentence and not about the game.

A target that is not on the board heals nothing rather than throwing. The
sentence has an answer for a table with no Rhino on it, and the answer is the
surge — which is the opposite of what `giveStatus` does with a missing card,
because the sentences are different.

**`you` is a card as well as a player.** `rr:you-your` is emphatic: "if the word
'you' **can** be resolved as referring to the player's identity, it **must** be
resolved as such", and `.5` spells out this exact case — "if a card ability
places a status card on 'you' *(such as 'you are stunned')*, the player
resolving that card ability places that status card on their identity."

**`query: heroes` is not every identity.** `rr:form-change-form.5`: "while a
player is in alter-ego form, card abilities that interact with their hero do not
interact with their identity." So Shocker's *"Deal 1 damage to each hero"*
passes over a player who has flipped down — a distinction that is invisible at
one player and invisible again at two if the flipped-down player happens to be
last, which is why the test has three with the alter-ego in the middle.

**`{ "perPlayer": n }` counts eliminated players.** `rr:player-elimination.6`:
"effects that refer to the players in the game ignore eliminated players,
**except for the per player icon**." So this multiplies by `World.Players` and
not by the number still playing.

**`inForm` is a test and not a trigger field**, because the printed `(Hero)` and
`(Alter-Ego)` parentheses gate two abilities that are exclusive — `rr:form-change-form`
opens with "a player can be in either hero or alter-ego form at a given time" —
so they collapse to one `if`. It reads `Forms.In`, which answers from the faceup
side rather than a flag, and answers a *set*: an identity can print more than
two faces.

`grantUntil` and `delayUntil` are two nodes rather than one because the rules
make them two things: `rr:lasting-effects` is a condition that holds for a
duration, `rr:delayed-effect` is an effect that resolves at a point. The engine
already told them apart (`EffectSource`), and collapsing them in the DSL would
have put the distinction back on the interpreter.

### Three decisions worth arguing with

**The parser knows no vocabulary.** An object stays an object; whether a given
object is a node or a map of fields is decided by the interpreter when it asks.
The alternative was tried first and does not work: `{"not": {"hasStatus": …}}`
holds a node and `{"hasStatus": {"card": …, "status": …}}` holds two fields, and
a reader that guessed would need the node list — making every new node a change
to the reader as well as to the interpreter.

**A trigger names a triggering condition, spelled as the engine spells it** —
`WhenAttackInitiated`, not a DSL word translated into one. `rr:triggering-condition`
is a rules vocabulary, and a second vocabulary beside it is a table that drifts.
`Steps.EveryCondition` is derived from the engine's own step table and every
authored trigger is held against it, so a card naming a condition nothing fires
is a failing test rather than a card that never triggers.

**`timing` is the ability type, not the tier.** card-dsl.md said `TimingPriority`
and "must not become" a new invention. A card prints its *type* — "Forced
Interrupt", "When Revealed" — and `rr:ability` gives types an order, so the type
is what the data carries and the tier is derived from it. Same enumeration, one
level down, and no new invention either way.

### What the data has that code did not

Being data buys three checks that a class per card could not have:

- every trigger names a condition some step actually produces;
- every authored id is a printed card id;
- every timing sits in a window or is the occurrence.

And one distinction it could not express: **authored-and-does-nothing is not the
same as nobody having read the card.** A card in the dataset with no abilities
has been read; one absent from it throws when revealed. Without that, an
unported encounter card resolves to silence and the board is plausible and
wrong.

### What is deliberately still missing

`gainSurge` used to be the honest example: "I'm Tough" had a surge branch, the
data said so, and the interpreter threw naming the node. It is written now, and
the shape it demonstrated is the one every gap should have — the card is
complete, the engine is not, and the message says which node to write. Growing
the engine is adding a case; growing the game is adding a row; they are
different activities and they read differently.

The gaps that have that shape today, from the Rhino scenario's own twenty-four
cards: an attachment that redirects damage (Armored Rhino Suit), a **Hero
Action** with a resource cost (Enhanced Ivory Horn), and damage assigned among
several characters (Explosion). Twenty-one of the twenty-four are written; those
three are what is left, and beside them the nemesis set's own five.

### How big the job actually is

Measured across the 135 campaigns in the dataset: **1,477 distinct
encounter-side cards**. Of those, 61 print no text at all and want a row saying
"read, does nothing"; the rest have something to say.

The distribution is what makes it tractable. The **Standard** sets reach almost
everything — `01190` Shadow of the Past appears in 132 of the 135 campaigns, and
`01191` Exhaustion, `01192` Masterplan and `01193` Under Fire in 75 each. No
other encounter card in the pool comes near, and after them the curve falls away
to nine scenarios and fewer.

**All ten Standard cards are written.** Advance, Assault, Caught Off Guard,
Gang-Up, Shadow of the Past, Exhaustion, Masterplan and Under Fire, and the two
that are read and empty. So the set every scenario is built on resolves, and
what is left is each scenario's own cards.

### One card, two tiers

Sweeping Swoop is a treachery when it is revealed and a boost card when it is
turned faceup during an activation, and it says different things in the two
places. That is what the boost guard was tightened for: *"is this card
authored"* passes on the strength of the half somebody wrote, and the other half
goes back to being silent. It asks whether **this half** is written.

Two readings the card forces:

- **"Your hero" is not "you".** `rr:form-change-form.5` — "while a player is in
  alter-ego form, card abilities that interact with their hero do not interact
  with their identity" — so the binding names nothing at all for a flipped-down
  player, and a card with something to say about that says it with `exists`.
- **"In play" is a place.** Vulture sits in the player's set-aside pile from the
  deal, so a game that has not revealed Shadow of the Past has him on the table
  and out of play. `titleInPlay` asks only of the areas
  `rr:in-play-and-out-of-play` counts, and compares titles rather than printed
  ids because `rr:identity.2` makes a title name one card.

**A scheme is an activation and is not an attack.** The boost half is bounded by
`EndOfActivation`, which `rr:activation.6` names outright — "that minion's
activation ends immediately". Bounded by the end of an *attack* it would survive
a scheme activation entirely and then fire during the next attack, against
somebody it was never about.

### An effect can be written before the card it acts on exists

"Rhino attacks you. **If a character is damaged by this attack, that character
is stunned.**" The second sentence cannot name anybody when it is written: the
attack has not happened, and who defends is a question nobody has been asked. So
the effect is registered with `Affects: null`, and the occurrence names a card
when the effect comes due.

It is a **delayed effect** and not a response. `rr:delayed-effect.1` resolves it
"immediately after [its] future condition occurs or becomes true, and **before
responses to that point or condition may be used**", and `.2` says it "is not
treated as a new triggered ability" — so it opens no window and nobody is asked.

**Damaged, not attacked**, and the difference is reachable: `rr:tough.3` says a
character whose tough status card ate the damage "is not considered to have
taken damage". `Damage.Attack` therefore answers with who it *actually* damaged,
measured off the dial rather than assumed from the aim.

**Bounded by the attack as well as by the condition.** "By **this** attack" is
false once the attack is over, so an attack that damaged nobody must not leave
the effect waiting to stun the wrong character two rounds later. `Duration`
already carried both halves — a condition and a timing point — so the effect is
"the next time damage is dealt, and not past the end of this attack".

The status lives in the effect's `Kind` because `ContinuousEffect` has nowhere
else to put it: it carries a number, two card ids and a duration, and it has to
survive a save, so it cannot carry a closure either. Confusing or toughening the
damaged character is another constant beside `StunTheSubject`.

### A card that draws on the random stream

One MT19937 stream runs the whole game, so **how many numbers a card takes and
in what order is a wire format**, not a detail. `EngineRandom.Choice` is the
ported primitive and is already pinned against recorded RNG vectors;
`discardAtRandom` is the first card ability to reach it.

Two consequences the tests assert directly rather than the board:

- **A player with an empty hand takes no draw.** The draw is inside the loop
  rather than counted ahead, so a hand with nothing in it costs the stream
  nothing. A board that took one draw and a board that took two are the same
  board and different games — which is why the test compares the *next* number
  off the stream against a control that made exactly one `Choice` by hand.
- **"Each player" goes in player order.** `rr:each-player.1`, and the order is
  what the stream sees. `rr:player-elimination.6` is why it is `PlayerOrder`:
  "effects that refer to the players in the game ignore eliminated players".

### A search of a deck is bounded, and the bound is a rule

`discardUntil` takes the top card each time rather than counting ahead, because
`rr:discard.4` says so: "if multiple cards are discarded from a deck by a
singular effect, place those cards in the appropriate discard pile **one at a
time (without changing the order)**". It goes through `EncounterDeck.TakeTop`,
so a deck that empties mid-search reshuffles rather than ending it.

Which is exactly why it needs a bound. A search for a card that is in neither
the deck nor the discard pile would reshuffle for ever. The bound is how many
cards there are, so a card that exists is always found and one that does not
ends the search instead of the game.

### A reveal moves the card now and resolves it later

`reveal` puts the card in the revealing area at once and schedules the step that
resolves it. The scheduling is the same reason `search` has: a reveal is a step
with an interrupt window and a response window around it, and the card revealed
may itself ask a player something.

The *moving* is Shadow of the Past's doing. It reveals two cards out of the
player's set-aside pile and then shuffles **the rest** of that pile into the
encounter deck — so a reveal that only scheduled would shuffle away the two
cards it had just chosen. Nothing else in the pool has yet needed the
distinction, and it is one line apart.

### A constant ability can grant a trait

`rr:traits.1`: "traits have no inherent effects on the game. Instead, some card
abilities reference cards that possess or lack specific traits." So a trait is a
name other cards ask about, and a card that gives one is giving an attribute
rather than copying text — `rr:traits.2` says traits "are not considered to be
part of a card's printed text box for the purpose of card abilities".

```json
{ "grant": { "card": "attachedTo", "trait": "AERIAL" } }
```

It carries no amount, which is the difference from a keyword: `steady` is
present or absent and `retaliate` states a number, but a card either has the
**AERIAL** trait or it does not. `State.Traits.Of` is the one place that knows
both sources, the same way `Keywords.Has` does for keywords, and it is what the
trait queries, the reveal's trait check and the digest's `t_` keys all read.

**The digest carries it.** A villain wearing Super Strength has the **BRUTE**
trait, so `t_BRUTE` is on its record — a wire format that emitted only the
printed list would describe a board nobody is playing, and two engines would
agree about it.

### "Attach to" is a rule about a phrase, not an ability

`rr:attach-to`: "if a card uses the phrase 'attach to', it must be attached to
*(placed beneath and slightly overlapped by)* the specified game element **as it
enters play**." So the engine does the attaching, on every path into play, and
the card supplies only the element:

```json
{ "card": "01099", "name": "Charge",
  "attachTo": { "query": "villain" },
  "abilities": [ … ] }
```

It sits beside `abilities` rather than inside one, because it is not one.
`ICardAbilities.AttachesTo` answers the question and moves nothing —
`Reveal.Resolve` is where the card is placed.

**The modelling it replaces was a "When Revealed".** That reads correctly for a
card revealed off the encounter deck, and is wrong everywhere else:
`rr:when-revealed-abilities.2` says a card put into play *without being revealed*
does not trigger one, and a setup attachment is put into play without being
revealed. That is what blocked step 11 (MARVEL-211).

Two other things fell out of reading it as the rule:

- **`rr:attach-to.3`** — legality is checked once, when the card would be
  attached, and a card that fails "remains in its prior state or game area. If
  such a card cannot remain in its prior state or game area, discard it." So an
  attachment naming an element that is not there stays on the table in front of
  the player, where the reveal's own step 4 discards it. Naming nothing and
  naming something absent end in the same place, which is why the answer is
  nullable rather than a throw.
- **`rr:attach-to.3.1`** — "the 'attach to' phrase on a card is not resolved if
  another ability causes that card to attach to a specific game element." That
  is why the `attachTo` *node* stays in the effect vocabulary: Genetic
  Experiments' "**Boost:** Attach this card to an [[Infinite]] minion" is an
  ability doing the attaching, and it moves the card itself.

**A quieter bug it fixed.** `Reveal.Resolve` sent every attachment to nowhere, so
`Reveal.EnterPlay` never ran for one. Eleven attachments in the pool print
`uses X`, a keyword that fires on entering play — each had been arriving with an
empty counter pool and an ability that spends from it.

### An ability can answer a moment that is about nobody

`AbilitySubjects` narrows which occurrences an ability answers, and until now
every member named a card: `this`, `attachedTo`, `you`. Hunting Gene Traitors
answers **"after resolving step one of the villain phase"**, which names a
moment and nothing in it — the step places threat on the main scheme without the
occurrence naming any card.

`game` is that: the condition alone decides. It is a fourth member rather than
letting the card use `you`, which would have fitted *by accident* — an encounter
card's owner and an unattributed occurrence's player are both the scenario, so
`you` would have matched for the wrong reason and stopped matching the moment
either changed.

### The board carries which mode it was dealt for

`rr:modes-of-play` names four — expert, heroic, skirmish, campaign — and lets
them combine. What expert mode changes about the *deal* is already in the
blueprints: `.2` is "the listed expert mode villain stages, and add the Expert
encounter set", and the `_expert` campaigns say both. What was missing is that
**86 cards in the pool read the mode**, 59 of them main schemes.

`World.Expert` is one flag and deliberately not an enum or a set. Heroic mode is
why: `.4` gives it a level number rather than a flag, so a set with `Heroic` in
it would be wrong about it. The other three arrive when a card reads one, and
until then `{ "inExpertMode": "heroic" }` throws naming the mode rather than
quietly answering about expert.

### A "Setup" ability runs during the deal

`rr:setup-triggered-ability`: "'**Setup**' is a type of triggered ability that
is resolved during setup", and `.1` makes it mandatory — so there is nothing to
offer and nothing to decline. `.2` puts an encounter card's at
`rr:appendix-ii-setup.step.12` and `.3` puts a player card's at a later step,
with the opening hands drawn in between, so which step is the dealer's business
rather than the card's.

It carries no `event`, for a different reason from a constant's: it *is* a
triggered ability, but it is timed to a step of setup rather than to anything
happening in the game. Setup is not on the agenda, so no condition in
`Steps.EveryCondition` names it, and inventing one would put a triggering
condition nothing produces into the data.

**Setup's abilities schedule as well as act, and the deal has to drain what they
scheduled.** Rhino II searches the encounter deck for a side scheme and
*reveals* it, and a reveal is a step with windows around it rather than a card
moving — so it goes on the agenda. An agenda nobody drains is an ability that did
half of what it said: the deck shuffled by the search, the scheme still in it.
`rr:appendix-ii-setup` ends "the game is now ready to begin", so there is no
later moment — it drains there or never. A question raised while draining throws
rather than taking a default, because `rr:ability.6` says player card abilities
cannot resolve during setup and there is nobody else to ask.

**What this changed.** The expert Rhino deck opens on stage II, so the expert
scenario is supposed to begin with Breakin' & Takin' already on the table. Until
step 12 ran, it did not — every expert game in the suite had been played on a
board that was materially the wrong board.

### A constant ability is read, not run

`rr:ability.5` splits abilities in two: "an ability prefaced by a bold timing
trigger followed by a colon is referred to as a triggered ability. An ability
without a bold timing trigger is referred to as a constant ability." Everything
above this line is the first kind. A constant ability has no trigger, and
therefore no moment at which anything could run it.

So it is not run. `ICardAbilities.Constant` is asked what a card in play is
doing, and `ContinuousEffects.Active` asks it of every card in play every time
anything reads the effect list — which is what `rr:modifiers` describes the game
as doing: "the game constantly checks and (if necessary) updates the count of
any variable quantity that is being modified."

Unus is the card that makes the difference visible rather than theoretical:

> Toughness. If the amount of threat on Gene Pool is at least: 3 — Unus gains
> retaliate 1. 6 — Unus also gains stalwart. 9 — Unus also gains a [amplify]
> icon.

```json
{ "trigger": { "timing": "Constant", "subject": "this" },
  "effect": { "seq": [
    { "if": { "test": { "atLeast": { "value": { "tokensOn": { "titled": "Gene Pool" } },
                                     "count": 3 } },
              "then": { "grant": { "card": "this", "keyword": "retaliate", "amount": 1 } } } },
    { "if": { "test": { "atLeast": { "value": { "tokensOn": { "titled": "Gene Pool" } },
                                     "count": 6 } },
              "then": { "grant": { "card": "this", "keyword": "stalwart" } } } },
    { "if": { "test": { "atLeast": { "value": { "tokensOn": { "titled": "Gene Pool" } },
                                     "count": 9 } },
              "then": { "grant": { "card": "this", "keyword": "amplify" } } } } ] } }
```

This is `rr:ability.9` word for word — "some constant abilities continuously
seek a specific condition *(denoted by words such as "during", "if", or
"while")*. The effects of such abilities are active anytime the specific
condition is met." Unus retaliates on one turn and not the next with nothing
happening to him in between; what changed was a scheme on the other side of the
table. An engine that registered his keywords when the stage entered play would
have him retaliating for the rest of the game.

Three things follow from reading rather than running, and each is a refusal:

- **A constant carries no `event`.** The parser refuses one, because an `event`
  on a constant is not a stray key — it is an author having believed the card
  fires on something.
- **`grant` is the only verb.** `Grants` walks `seq` and `if` and stops at
  `grant`; every other node throws by name. A constant ability that dealt damage
  would need an answer to *when*, and there isn't one.
- **A constant may not read the effect list.** Working out what is in force is
  what called the card. `ContinuousEffects` throws rather than answer with the
  half of the list it happens to have.

**Where this leaves the `static` sketch.** Laser Swords above proposes `static`
as a Layer 0 field with a computed value and a declared `dependsOn` mask. Two of
its three parts turned out to be unnecessary: the timing is `Constant`, which
the rules already name, and nothing declares dependencies because nothing is
cached — the condition is re-read on every ask, so there is no stale value for a
mask to invalidate. What `static` still buys is the third part, a *computed*
scalar: `grant` takes a fixed amount, and "+1 ATK for each [crisis] in play"
does not have one. That is the next thing this node needs, and it is additive.

### Read and empty is not unread

Five of those seventeen carry no ability at all — Rhino's first stage, Hydra
Mercenary, Sandman, Crowd Control and the main scheme's B side. Each is a
keyword the engine already reads, a printed icon, or a rule restated on the
card, and each is a row in the dataset saying so.

That is the distinction the file exists to be able to make. Revealing one of
them resolves to silence, which is correct. Revealing a card nobody has read
throws, which is also correct. A dataset that could not tell them apart would
have to pick one behaviour, and either choice is wrong for half the pool.

### A defeat is a triggering condition of whatever caused it

`Steps.CardDefeated` was a name with nothing behind it. It labelled the
occurrence `WhenDefeated` built for the defeated card's *own* ability, and no
other card could ever see it — so "**Forced Response:** after an ally is
defeated…" was a sentence the dataset could hold and the engine could not run.
Gene Pool was authored, resolved to silence, and was pulled back out again.

The fix is **not** to schedule a defeat. `rr:triggering-condition.2` covers this
exact case and uses it as its own example:

> If a single game occurrence creates multiple triggering conditions *(such as a
> single attack causing a character to both take damage and be defeated)*, those
> triggering conditions are handled with **a single interrupt window and a
> single response window**.

So the defeat joins the occurrence that caused it, and the window that was
already going to open covers both. `Occurrence.Conditions` therefore grows while
the occurrence is happening: which conditions an attack creates is not knowable
when it is scheduled, because whether the damage defeats anybody is not knowable
until it is dealt.

An engine that gave the defeat windows of its own would let an ability that
answers both conditions fire twice against what the rules call one moment, and
would put the damage responses and the defeat responses in a fixed order that
the rules leave to the player.

#### Provenance lives on the occurrence

Who defeated the card and how travels in `Occurrence.Defeats`, not on the board.
The reason is lifetime: a response is asked *after* the defeat and after
everything else the occurrence did, so a field on `World` would have to be set
before and cleared after — and the clearing is the half nobody remembers. The
occurrence lasts exactly as long as its two windows do, which is exactly as long
as anything can ask.

Two bindings and one test read it:

| | |
|---|---|
| `defeated` | the card that was defeated — separate from an attack's `trigger.actor` and `trigger.target` |
| `defeater` | the identity of the player whose character did it, for "the player who defeated this scheme" |
| `defeatedBy` | what kind of thing did it |

`defeatedBy` names **the rule**, not the engine's spelling of it. Gene Pool's
data says `"consequentialDamage"` because `rr:consequential-damage` is what the
card means; the interpreter maps that to the verb the event stream records
damage under. One word, because one card asks — anything else is refused by
name rather than guessed at.

#### The only negative condition in the dataset

> **Forced Response:** After an ally is defeated **by anything other than
> consequential damage**, place 3 threat here.

It is written as printed — `{"not": {"defeatedBy": "consequentialDamage"}}` —
and not as a list of the causes that *do* count. A list would be wrong the
moment a set added another way to die, and it would be wrong silently: the card
would simply stop firing for the new one. The card names the single cause that
does not count, so every cause that has not been invented yet is already
handled.

`not` was in the vocabulary already. What was missing was something for it to
negate.

#### Two defeats at once are refused where the ambiguity is

`Occurrence.Defeats` is a list, because one effect can defeat two characters.
`Occurrence.Defeat` — "*the* defeated card", which is how cards are written —
refuses when there is more than one. `rr:triggering-condition.1` lets an
answering ability trigger once per occurrence, and once is the wrong number for
two dead allies; nothing in the rules says which of them the response is about.
That is a real unanswered question, so it is refused at the point a card asks it
rather than at every multiple defeat.

#### A defeat with nothing happening is refused

`Defeat` throws when the agenda holds no occurrence for the defeat to join. Not
defensiveness: every way a card can be defeated is something happening in the
game, and something happening in the game is a step. If it fires, the missing
piece is the **cause** — some way of doing damage or removing threat that the
engine still performs as a call rather than as a step, and whose own windows are
therefore missing too. Silence would hide that, and hide it inside the cards
written to notice.

Making the thwart a step is what this turned up; see
[player-phase.md](player-phase.md).

#### The two defeat interrupt tiers are separate

`rr:damage.step.6` holds abilities that trigger when a character **would be
defeated**. It runs after step 5 places damage and before the defeat happens, so
there is no upstream prediction: `Damage.Deal` can see that the character has no
remaining hit points, resolve the interrupt, and check again. Biomechanical
Upgrades uses this tier to heal its minion and invalidate the imminent defeat.

`rr:damage.step.7` is the later **when defeated** tier. It holds a card's own
When Defeated ability and another card's forced interrupt on that defeat.
Genetic Experiments uses this tier because it places threat without preventing
the minion's defeat. Both tiers resolve inside the damage sequence, before step
8 removes the defeated card.

Forced abilities work in both tiers. Optional interrupts and simultaneous
forced abilities still require prompts inside the damage sequence; the engine
raises by name until those prompts exist.

### A response can cost something

`rr:cost-arrow-icon` puts a payment before an effect — "pay cost → resolve
effect" — and nothing in it is about which tier the ability sits in. An
**action** with an arrow cost has worked since `01100` Enhanced Ivory Horn, the
attachment on Rhino a player discards for three physical resources. A
**response** with the same arrow could not be paid for at all:
`Sequence.Answer` had the player's payment in its hand and did not pass it on,
and `AbilityRunner.Resolve` paid `Pay(cost, [], cast)` — an empty list, against
a cost of nothing, which was true of every card written until now.

Prelate Armor (`45064`) is the same card one tier over. "**Hero Response:**
After you make a basic attack against Unus, spend [mental] [physical] resources
→ discard this card."

```json
{ "trigger": { "event": "WhenAttackInitiated", "timing": "Response",
               "actor": "you", "target": "attachedTo", "form": "hero" },
  "cost": { "spend": "BR" },
  "effect": { "discard": "this" } }
```

Everything the payment needed was already built and unused. `CostOption.Sources`
has modelled the menu of generators since MARVEL-169, `Decision.Resources` has
carried which of them the player spent, and `CardPlay.Spend` has refused a
payment that does not meet the cost. What was missing was ten characters of
plumbing between the two.

#### The two questions an action never had to ask

An action is *taken by a seat*, so the request carries who is acting; both the
form requirement and the hand to spend from arrive with it. A window is opened
around an occurrence and offered down the table, and neither answer comes for
free.

- **Whose form.** `rr:initiating-abilities.step.2` — "if the card or ability has
  a form requirement *(for example, 'Hero form only' or '**Hero** Action')*,
  the form of the player playing that card or initiating that ability is checked
  now." Step 2 is about any ability, and `Actions` had been asking it since the
  first ported card. A window had never been asked, because until this card no
  authored ability printed a **Hero Response**. It does now, and an alter-ego is
  not offered it.
- **Whose hand.** `rr:initiating-abilities.step.3` puts the cost and "the
  player's ability to pay them" in one step, and only "if both conditions are
  met" do the later steps happen. So an ability nobody can pay for is not an
  offer that aborts at step 5 — it never reaches the window.

`rr:you-your.7` answers both: "for abilities that trigger 'after [enemy] attacks
you,' 'you' refers to the attacked player, even if that player defended with an
ally." The seat is the occurrence's.

#### Which is written on the card, not inferred from it

The tempting shortcut is to say that an ability on a card the scenario owns
belongs to whoever the occurrence happened to. It is right for this card and
wrong as a rule: `rr:ability.8` lets **any** player trigger an optional ability
on an encounter card, and "any player may" and "the player it happened to" are
both things an encounter card can say. Only the card knows which it said, so the
card says it — `"player": "trigger.player"`, a closed set of one, closed for the
reason `AbilitySubjects` is.

An encounter-card ability with a cost and *without* that field is refused rather
than priced against the first player's hand, and so is one that names a form.
Both refusals name the field that is missing.

#### Two holes this leaves, named where they are

- **A mandatory ability with a cost.** `rr:forced.1` makes a forced ability
  resolve when its condition is met, so `Offering.Work` runs it without asking
  anybody anything — and a payment is an answer to a question.
  `rr:initiating-abilities.step.5` would still have to be paid, out of a hand
  nobody chose from. No card in the pool prints one. The runner refuses it by
  name; the day a card needs it, the window has to ask.
- **Requirement resources are not the same shape.** `cost: { "spend": "BR" }`
  reads as two resources of which one is mental and one physical, which is
  `rr:resource.4`. A cost of a bare number with a `Requirement` printed beside
  it goes through `Resources.Required` instead, and no *ability* prints one —
  only cards being played do.

### "Attacks **and** defeats" is one occurrence, and the trigger names both

Prelate Sidearm (`45063`): "[star] **Forced Response:** After Unus attacks and
defeats an ally, place 1 threat on Gene Pool."

`rr:triggering-condition.2` is what makes the sentence writable at all — the
attack and the defeat are one occurrence with one window pair between them, so
by the time the card answers the defeat, the attack is still the thing that is
happening. Nothing has to remember a moment ago.

But a trigger names one condition, and its roles say who did what. The condition
and one ambiguous subject are not enough:

| the occurrence | actor | target | conditions |
|---|---|---|---|
| Unus attacks, and kills the defending ally | Unus | the ally | `WhenDamageDealt`, `WhenCardDefeated` |
| an ally attacks Unus, and his **retaliate** kills it | the ally | Unus | `WhenAttackInitiated`, `WhenCardDefeated` |
| a **minion** attacks, and kills the defending ally | the minion | the ally | `WhenDamageDealt`, `WhenCardDefeated` |

The actor tells the third apart from the first. It also tells the second apart:
`rr:retaliate-x.1` is "after **this character is attacked**, deal X damage to
the attacker", and Unus being the target is not Unus being the actor. The
trigger still requires the damage condition because the printed sentence says
"attacks and defeats":

```json
{ "event": "WhenCardDefeated", "alsoHappened": "WhenDamageDealt",
  "timing": "ForcedResponse", "actor": "attachedTo" }
```

**It gates the trigger and not the effect**, which is the part worth arguing
with. The obvious place for a qualifier is an `if` in the effect, which is where
"an **ally**" lives on this same card and on Gene Pool. The difference is
`rr:forced.1`: a forced ability "must be resolved when its triggering condition
is met", so an ability that initiates and does nothing has already answered
wrongly — and it is observable, because `rr:forced.5` then asks the first player
to order two forced abilities when only one of them had a condition to meet.
Retaliate killing an ally produced exactly that question before this existed.

The line that falls out: **the trigger matches the occurrence, and the effect
reads the cards involved.** `alsoHappened` is a fact about the occurrence.
`isKind` on the defeated card is a fact about a card, and stays in the effect.

### A cost can be a card rather than a number

Hunted (`45072`): "**Alter-Ego Action:** Discard a card from your hand →
discard this card." An obligation, so `rr:reveal.4` puts it into the revealing
player's play area and it stays there; its printed hazard icon is a field the
engine already reads, and this one sentence is the whole way out.

Every cost written before it was resources. `rr:cost.3` spends them "by
discarding cards from their hand to generate the resource or resources indicated
at the bottom-left corner of the card" — the letters are what is spent and the
discard is how they are generated. **This cost reads no letters at all.** A card
printing no `RES` pays it; a card printing two does not pay twice.

```json
{ "trigger": { "event": "WhenActionTriggered", "timing": "Action",
               "subject": "this", "form": "alter-ego" },
  "cost": { "discardFromHand": 1 },
  "effect": { "discard": "this" } }
```

**So it travels as a target and not as a payment.** `Decision.Resources` is "the
generators spent, by `ResourceSource.Effect`", and a card being handed over for
what it *is* rather than what it makes is not a generator; describing it as one
would put a price on the wire that a client would try to meet with resources.
`Affordance.Targets` already says "what still has to be chosen before this can
resolve", which is exactly the question. `rr:initiating-abilities` keeps step 2's
choosing and step 5's paying in different steps, and the answer carries the two
separately for the same reason.

The affordance carries the hand and a count of exactly one, and the engine never
picks. A payment that is not the cost — none chosen, or two — is refused rather
than trimmed: `rr:initiating-abilities.step.5` aborts "without paying any
costs", and an engine that trimmed would be making a decision the player was
asked to make.

### A suspended choice has to say which ability it came from

`choose` and `chooseCard` stop the ability and put a `ChooseOption` step on the
agenda; the answer picks it up again. A step is a small value on the board and
cannot hold an effect tree, so what it carries is the **card**, and the node is
found again from the card.

That works until a card has a choice in two of its abilities. Infinite Hunter is
the first: a "When Revealed" that chooses an ally and a "Boost" that chooses
between two effects. `rr:boost-boost-icon.2` keeps the two halves apart, and
neither the card nor the position in the effect says which one stopped — the
lookup had been taking the first ability on the card with a choice in it, so
resuming the boost would have asked the reveal's question. Silent, and
legal-looking: two real options about the wrong thing.

So the step carries the **tier** as well, and the ability is found by both.

What is still ambiguous, and refused by name: two abilities at the *same* tier
each holding a choice. The tier is as fine as the step gets, so that is the next
thing it would have to carry rather than something to guess at. No printed card
needs it.

### The activating enemy is read off the board, not off the moment

Infinite Hunter (`45065`): "[star] **Boost:** Choose to either place 2 threat on
Gene Pool, or **the activating enemy** gets +2 SCH and +2 ATK for this
activation."

A boost card is turned faceup in the middle of an activation, and its own
occurrence is about the boost card — so there is nothing in the moment to answer
with. `rr:activation` is what makes one answer serve both kinds: "whenever an
enemy attacks or schemes, it is considered to have activated". Only the attack
half had a value on the board, so the binding needed `World.Activation`: which
enemy, against which seat, and which kind — the umbrella `EnemyAttack` sits
under rather than a second copy of it.

**"This activation" is `EndOfActivation`, not `EndOfAttack`.** `rr:activation.6`
gives an activation an end, and a scheme is not an attack: a +2 that outlived a
scheme would go off during somebody's attack, against somebody it was never
about. Both timing points already existed and the two are one word apart, which
is exactly the kind of mistake that reads correctly and plays wrong.

**"+2 SCH" is the same node as "gains overkill".** `grantUntil` registers a
modifier, and a stat field is something the engine reads modifiers into — so the
two are one mechanism and the card names the field. It is now held against the
fields the engine actually reads, which a constant ability's `grant` always was:
an unrecognised name registered happily, expired on time, and modified nothing
in between.

The two grants are one printed sentence and two nodes, because the engine reads
two fields.

### Core Spider-Man primitives

The first complete identity set adds four reusable effect words. `discardTop`
remembers cards discarded by this resolution and
`recoverDiscardedByResource` returns the remembered cards that print a named
resource. `preventDamage` installs a one-use replacement for the imminent
attack damage. `cancelWhenRevealed` suppresses only the revealed treachery's
When Revealed effects; the card remains revealed and is discarded normally.

The same slice adds `removeCounters` as a payable cost, `removeThreat` as an
action, and the board queries `enemies` and `schemes`. An event action or
interrupt is played from hand: its printed cost is paid before its tree runs,
and it is discarded after that tree finishes, including after a suspended
`chooseCard` answer.

`isYourIdentity` distinguishes damage the resolving player's identity would
take from damage aimed at an ally defending for that player.
