# The card DSL

The card DSL turns printed card text into inert JSON that the rules engine can
execute. `Marvel.Cards` owns the parser and interpreter.

The executable ability book is `datasets/abilities/abilities.json`. It contains
all 209 Core Set card faces. A face with no executable text still has an explicit
empty row, so the engine can distinguish “read and does nothing” from “not
authored.” An unrecognized face or node raises instead of resolving as silence.

[Product and repository scope](scope.md) defines the runtime boundary. The DSL
was designed against the complete card pool before that boundary was narrowed.
That work validated the language against known future card patterns to reduce
the chance that later products require a wholesale rewrite. It does not make
those products executable.

## Project boundary

The responsibilities are separate:

| Project or dataset | Responsibility |
|---|---|
| `datasets/cards/` | Generated printed facts for the full card catalog |
| `datasets/abilities/` | Authored executable text for supported faces |
| `Marvel.Rules` | Rules, state, timing, prompts and events |
| `Marvel.Cards.Dsl` | Strict JSON parsing, semantic validation and typed programs |
| `Marvel.Cards.Run` | Initiation, legality and effect interpretation |
| `Marvel.Content` | Card facts and supported setup data |

`Marvel.Cards` takes JSON text. It performs no file or network input. The host
decides where the bytes come from.

The DSL contains no delegate, callback, source code or general expression
escape hatch. Downloadable or user-authored content remains data.

## Ability rows

One card row names a printed face and everything the engine knows how to execute
from it:

```json
{
  "card": "01001a",
  "name": "Spider-Man",
  "note": "Why the printed text was represented this way.",
  "abilities": [
    {
      "name": "Spider-Sense",
      "trigger": {
        "event": "WhenAttackInitiated",
        "timing": "Interrupt",
        "actor": "villain",
        "player": "you"
      },
      "effect": {
        "draw": {
          "player": "trigger.player",
          "count": 1
        }
      }
    }
  ]
}
```

`note` records authoring judgment. The runtime does not read it.

Cards that print placement instructions can also carry `attachTo` or
`controlledBy`. A card whose text gives it counters as it enters play carries
`startingCounters`:

```json
"startingCounters": { "type": "web", "count": 3, "uses": true }
```

`type` is the counter name and `count` is a positive integer. The required
`uses` boolean distinguishes the **Uses (X “type”)** keyword from an ordinary
“enters play with X counters” instruction. A Uses pool discards its card when
the last all-purpose counter leaves; an ordinary pool does not. Rocket
Raccoon's weapons are the latter shape, so they will use `uses: false` when
their product enters the executable boundary.

These are card properties, not triggered abilities. They apply before any
response to the card entering play. A card that uses only placement metadata is
known to be silent when revealed, while its other printed abilities remain
unauthored.

## The ability envelope

Each ability has these fields:

| Field | Meaning |
|---|---|
| `name` | The printed label or the card name |
| `trigger` | The occurrence, ability type and matching roles |
| `when` | An additional live condition |
| `cost` | What must be paid before the effect begins |
| `effect` | One executable effect node |
| `limitPerRound` | A per-instance use limit |
| `maxPerRound`, `maxPerPhase`, `maxPerGame`, `maxPerInstance` | A title-shared maximum |
| `anyPlayer` | Printed permission for any player to use the ability |
| `labels` | Printed attack, defense or thwart labels |
| `printedResources` | Icons physically printed on a resource ability |

Unknown fields are errors. The parser never ignores a spelling it does not
understand, because partial acceptance would make a card appear supported while
dropping part of its text.

### Triggers

A trigger names the engine occurrence directly. It does not use a second DSL
event vocabulary that could drift from the rules engine.

```json
{
  "event": "WhenDamageDealt",
  "timing": "ForcedResponse",
  "subject": "attachedTo",
  "actor": "enemy",
  "target": "you",
  "form": "hero",
  "alsoHappened": "WhenCardDefeated",
  "player": "trigger.player"
}
```

Every field is optional only where the printed ability does not make that
distinction. Constant abilities have no event. Their effects are read while the
card remains active rather than scheduled into a response window.

Subjects and roles are closed vocabularies. A new printed relationship earns a
named relation and tests. It does not earn an arbitrary predicate.

## Effect values and nodes

A syntactic ability value has exactly 4 shapes:

- a signed integer;
- a word such as `this`, `you` or `trigger.player`;
- an ordered list; or
- a map of named values.

An executable node is a map with exactly one entry:

```json
{ "gainSurge": 1 }
{ "giveStatus": { "card": "you", "status": "tough" } }
{ "seq": [
    { "draw": { "player": "you", "count": 1 } },
    { "heal": { "card": "yourHero", "amount": 2 } }
] }
```

These maps are syntax, not the runtime's definition of an operation.
`AbilityCatalog.Parse` reads JSON and checks the ability envelope.
`AbilityLowering.Book` validates every executable field and produces an immutable
`AbilityProgram`. Constructing an `AbilityRunner` performs that lowering before
the book enters gameplay.

A host can lower once and pass the same `AbilityProgram` to several runners.
The program holds definitions, not game state. Each runner associates delayed
activation work with the exact `World` that registered it. Activation ids and
card ids cannot identify work in another game. The association uses weak keys,
so retaining a runner does not retain abandoned worlds.

The internal vocabulary has closed types for effects, costs, selectors,
conditions and numbers. Lowering rejects unknown names, missing arguments and
invalid shapes throughout the tree, including branches a particular game never
chooses. Diagnostics identify the card, ability ordinal and field path.

Every executable property must map to engine behavior. Adding a language operation
requires its typed representation, lowering, execution, legality checks and
behavioral tests. Authoring another card combines those operations as JSON; it
does not require a C# class for that card or allow executable code in the data.

The program indexes effects by card face, face-local ability ordinal and explicit
DSL paths with ordered list indexes. These are engine-chosen internal addresses,
not CLR type names or a new session-ledger wire format.

Execution, preflight and continuation consumers traverse the compiled program.
The runner does not retain the supplied syntax book or look up instructions by
mutable argument maps. Engine-owned operation spellings preserve option labels,
diagnostics and structural continuation frames; operands and child effects come
from the checked types.

Counter removal before a cost arrow is authored in `cost`, with the card that
pays, the named pool, and the exact positive count:

```json
"cost": {
  "removeCounters": { "card": "this", "counter": "web", "count": 1 }
}
```

The count may be greater than one. `card` may name `this` or `you` for a cost;
the latter covers an upgrade spending counters held by its identity. A cost
requires the explicit card, counter and count; string shorthand is rejected.

A `choose` node may carry a `descriptions` string list parallel to `options`.
Those strings are engine-authored affordance descriptions: clients display
them and do not reconstruct printed choices from effect-node names.

The implemented vocabulary covers these groups:

- control flow such as sequences, conditions, alternatives and repetition;
- player questions and resumable choices;
- card, player, area and occurrence bindings;
- resource, discard, exhaustion, counter, damage and other printed costs;
- damage, healing, threat, status, movement, reveal and activation effects;
- queries over cards, areas, traits, printed values and live state;
- lasting and constant modifiers with explicit expiry;
- setup placement and rules metadata that belongs to a printed card; and
- result-sensitive branches such as “if no damage was healed this way.”

`AbilityLowering` under `src/Marvel.Cards/Dsl/` defines the admitted vocabulary;
the types describe its operations and `src/Marvel.Cards/Run/` implements them.
Every new node needs malformed-data tests, rule-cited behavior tests and a real
supported card that needs it.

### Current vocabulary index

The Core ability book currently uses the following additional node, relation,
query, selector, and value names. This compact index is intentionally exhaustive;
`AbilityDataTests` checks the dataset against it so a new word cannot enter the
language without documentation.

```text
addToHand afterActivation allies alliesYouControl alsoAttackEachOtherHero among
atLeast attachedToThis attackDamaged attackableEnemies attackableMinions
automaticTarget blackPantherUpgrades canAutomaticThwart canLegalPractice
canMakeTheCall cancelOccurrence cancelWhenRevealed cardsIn changeForm characters
charactersYouControl choose chooseCard chooseDiscardToShuffle chooseTopForHand
countersOn createDrones damageOn dealAttackDamage dealDamage dealEncounterCards
deck defeatedByYou delayUntil discardAtRandom discardFromHand
discardHandWithResource discardTop discardUntil discardable discardedWithResource
doubleResourceFor drawToPrintedHandSize drones dronesEngagedWithYou dynamic
eachPlayer else encounterDeck encounterDiscardPile enemies
enemiesEngagedWithChosenPlayer enemiesWithTrait enemyAttacks enemySchemes exists
finalStep generate generateTopDiscard giveAdditionalBoost
grantCharactersControlledBy grantEach grantUntil hasStatus hasTrait heroDefended
heroes heroesAndAllies identities identitiesWithTechInDiscard
identitiesWithinPerPlayerLimit inForm indirectDamage isKind isTitle
isYourIdentity keyword kind legalPractice mainScheme makeTheCall maxBy minions
minionsEngagedWithYou modified moveAttackDamage mul onto options overkill
paidWithResource payOrEffect payOrExhaust perPlayer placeAccelerationToken
placeAtRandom placeCounters placeThreat power powerAmount powerTargets
preventDamage preventDamageFrom preventDamageWhile preventThreat
preventThreatRemoval printedResourceCountDiscarded putIntoPlay
recoverDiscardedByResource reduceNextCardCost remainingHealth removeCounters
removeFromGame removeThreat replaceThreatWithDamage requireAllyDefender
resolveSpecials returnOwnedToHand returnToHand revealTop scheme schemes search
shuffle shuffleInto sideSchemes soakDamage sourceKind sourceTrait spend
spendEnergyX startingCounters takeDamage then threatCause thwartDifferentSchemes thwartSchemes
thwartableSchemes titleInPlay titled tokensOn topEncounterDiscardBoostPlusOne
topmostTechInChosenDiscard undefendedAttack until upgradesAndSupportsYouControl
upgradesYouControl wasDefeated withTrait within withoutAnotherCopyAttached
yourAsideMinion yourAsidePile yourAsideSideScheme
```

## Questions and continuations

An ability may stop for player input. The engine puts a typed decision on the
agenda and keeps enough information to resume the same ability and node.

The continuation records stable data such as the source card, ability address,
selected targets, paid resources and intermediate results. It never stores a C#
delegate or a copy of the effect tree.

This preserves the engine contract:

```text
(state, input) -> (state, prompt, events)
```

No gameplay thread blocks while waiting for input. Replaying the same decisions
against the same seed reaches the same continuation points.

Delayed activation effects live in `AbilityGameRuntime`, separately from the
compiled program. Completing an activation consumes its ordered list before
running the effects. Continuation result maps and card bindings remain local to
their resolution; agenda continuations capture the data needed to resume them.

Card and selector evaluation uses `AbilityQueryContext`, which captures binding
incarnations and ordered power targets while reading the board in place. The
query services receive no event sink or execution continuation.
`AbilityExpressionContext` also captures result bindings, discarded cards,
payment and the current power amount. Numeric and predicate evaluation use
these inputs through the concrete `AbilityExpressionEvaluation` collaborator.

Each evaluation returns its information observations. Live effect consumers
publish them when resolving a number, condition or card selection. Repetition
counts and per-card alteration conditions use the same boundary. Legality,
projection and prompt construction do not publish observations. Short-circuited
branches neither read cards nor report exposure.

Preflight can refuse a singular area lookup through a narrow admission policy.
That policy receives only the requested area types. It cannot execute an effect
through the evaluator. Counter-placement preflight admits its numeric reads
before payment, using separate candidate bindings when an earlier choice is
pending. Live resolution recomputes the amount from the resulting state.

## Costs and legality

Target selection and payment are separate. A target answers what the effect will
act on. A payment records what the player spent to initiate it.

The engine preflights an ability before offering it. It checks the printed form,
live conditions, target availability, limits, maxima and ability to pay. An
offered affordance must remain legal when taken against the same state.

`AbilityReachabilityContext` holds immutable assumptions for one speculative
path: possible prior effects, payment changes, form changes and selected cards.
Sequence steps and continuation candidates receive separate snapshots. A child
probe cannot overwrite its parent's assumptions or leave a mode set afterward.

Elimination and damage share bounded calculations owned by `Marvel.Rules`.
`EliminationLayout` reads ordered placements and eliminated seats through
`IEliminationLayout`. It identifies the next player, retained minion trees and
cards leaving the eliminated play area. The live reader uses current placements;
the ability reader applies known departures and engagement changes without
moving cards. Live elimination still owns discard destinations, permanent-card
checks, events, attack termination and continuous-effect settlement.

`DamageAssignment` distinguishes damage dealt, damage taken and spending one
tough status card. Live damage and both ability trace paths use its step-2
calculation. Live damage supplies step-3 prevention and retains every callback,
status discard, placement and defeat window. The calculation does not predict
future choices or make an unsupported replacement safe to project.

`AbilityInitiationEvidence` holds the checks that live resolution must retain,
including label validation and scoped target exceptions. It belongs to one
ability resolution, not the shared program. Payment or suspension cannot erase
an exception already established before the cost.

Costs are atomic where the rules require them to be. A failed component does not
leave earlier resources, cards or exhaustions spent. When the protocol cannot
represent a required choice, the engine raises before changing the board.

## Results and ordered resolution

Effect interpretation reports whether an effect applied. This is gameplay state,
not coverage bookkeeping. Conditional text such as “otherwise” and “if no damage
was healed this way” depends on the result of the preceding effect.

Structural nodes do not claim their children’s work. A scheduled attack,
activation, reveal or choice finishes only after its continuation knows what
happened. Cancellation remains distinct from successful resolution.

A source leaving play does not stop the ability it already initiated. The tree
continues with its captured source and occurrence context unless a rule or node
explicitly stops it.

## Constant effects

Constant abilities describe values the engine reads from the current board.
They do not run once and cache their answer. The interpreter evaluates their
conditions against the same projected state used for legality and resolution.

Temporary grants name their expiry, such as the end of an attack, activation,
phase or round. An unknown field or duration fails rather than registering a
modifier that nothing reads.

## Authoring a supported card

Use this order:

1. Read the joined, corrected printed face in `datasets/cards/`.
2. Check `datasets/marvelcdb-faq/`, `datasets/rulings/` and applicable rules
   packs for a ruling or product rule.
3. Add or update the card row in `datasets/abilities/abilities.json`.
4. Use an existing node when it has the exact printed meaning.
5. Add a node only when a supported card exposes a missing reusable concept.
6. Add rule-cited behavior tests and mutation coverage for every new decision.
7. Run the full dataset, behavior and test gates.

Do not author from an old implementation or a live card website. Printed text
comes from `datasets/cards/`, and rules come from the vendored authorities.

## Fail-closed contract

Malformed authored data raises `AbilityException` before gameplay. A valid
instruction can still reach a rule situation the engine cannot implement;
that raises `RulesNotImplementedException` rather than guessing an outcome.

The engine refuses these states:

- a reachable face has no authored row;
- a card row contains an unknown field or malformed node;
- an effect names an unsupported query, binding, cost or duration;
- a required choice cannot be represented without guessing; or
- a card crosses a product boundary that setup does not support.

These failures are part of the product boundary. Replacing one with a no-op can
turn an unsupported game into a plausible, incorrect one.
