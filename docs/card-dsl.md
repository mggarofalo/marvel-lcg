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

Re-runnable, from `py_src/`:

    python -m tools.dsl.blockers            # the census
    python -m tools.dsl.blockers --greedy   # what each node buys

`tools/dsl/blockers.py` walks every card script's *handler bodies* — skipping
the envelope — and flags constructs a tree of typed nodes cannot hold without a
node designed for them.

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
counted instead of silently going missing, and publishes the result to
`datasets/cards/summary.json`. The five heaviest are `ChooseAbilities` (299
sites), `DiscardControlCards` (51), `MayChooseOneAbility` (40),
`DiscardHandCards` (39) and `AskChooseFace` (35).

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

`src/Marvel.Cards`, and seventeen cards in `datasets/abilities/abilities.json` — seventeen of the Rhino scenario's twenty-four.

**Why it exists now rather than after the design settled.** It was standing in
the way. `Marvel.Content.Cards.CoreSetAbilities` was a compiled class with a
`switch` on printed card id, and the moment the engine could reach a second and
third card it started to grow — which is the "cards as scripts" inversion this
whole document exists to undo. A placeholder that grows is not a placeholder.

### The slice

| | |
|---|---|
| Envelope | `trigger { event, timing, subject }`, `name`, `effect`. Not `when`, `cost`, `target` or `limit` — no authored card carries one yet. |
| Control | `seq`, `if`, `choose` |
| Tests | `and`, `or`, `not`, `exists`, `hasStatus`, `inForm`, `atLeast` |
| Actions | `giveStatus`, `attachTo`, `discard`, `draw`, `grantUntil`, `delayUntil`, `gainSurge`, `enemyAttacks`, `enemySchemes`, `dealDamage`, `placeThreat`, `heal` |
| Queries | `query: villain`, `query: mainScheme`, `query: minionsEngagedWithYou`, `query: heroes` |
| Amounts | a number, `{ "perPlayer": n }`, or `{ "result": "healed" }` |
| Bindings | `this`, `you`, `attachedTo`, `trigger.subject`; players `you`, `controller`, `trigger.player` |

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
- **Nothing resumes an ability part-way through.** An effect *after* a choice
  in a `seq` throws, rather than running before the choice it was written to
  follow. That is the failure that looks like it worked.

`rr:choose-game-element.1` settles who is asked: the player resolving the
ability. For a revealed encounter card that is the player it was dealt to — not
the first player, and not the card's owner, which an encounter card has not got.
`rr:choose-option` gives no way out, so the prompt is not cancellable.

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
`WhenEnemyAttacks`, not a DSL word translated into one. `rr:triggering-condition`
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

The gaps that have that shape today, all from the Rhino scenario's own
twenty-four cards: a query for a card the player controls (Caught Off Guard),
a search of the encounter deck (Rhino's second stage), an attachment that
redirects damage (Armored Rhino Suit), a **Hero Action** with a resource cost
(Enhanced Ivory Horn), a delayed effect on an attack's damage (Stampede), damage
assigned among several characters (Explosion), and the nemesis set (Shadow of
the Past). Seventeen of the twenty-four are written; those seven are what is
left.

### Read and empty is not unread

Five of those seventeen carry no ability at all — Rhino's first stage, Hydra
Mercenary, Sandman, Crowd Control and the main scheme's B side. Each is a
keyword the engine already reads, a printed icon, or a rule restated on the
card, and each is a row in the dataset saying so.

That is the distinction the file exists to be able to make. Revealing one of
them resolves to silence, which is correct. Revealing a card nobody has read
throws, which is also correct. A dataset that could not tell them apart would
have to pick one behaviour, and either choice is wrong for half the pool.
