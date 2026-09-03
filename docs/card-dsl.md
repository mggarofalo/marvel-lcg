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
| `Marvel.Cards.Dsl` | Strict JSON parsing and typed ability values |
| `Marvel.Cards.Run` | Ability validation, initiation and interpretation |
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
`controlledBy`. These are card properties, not triggered abilities. A card that
uses only placement metadata is known to be silent when revealed, while its
other printed abilities remain unauthored.

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

An ability value has exactly 4 shapes:

- a signed integer;
- a word such as `this`, `you` or `trigger.player`;
- an ordered list; or
- a map of named values.

An executable node is a map with exactly one entry:

```json
{ "gainSurge": 1 }
{ "giveStatus": { "cards": "you", "status": "tough" } }
{ "seq": [
    { "draw": { "player": "you", "count": 1 } },
    { "heal": { "cards": "yourHero", "amount": 2 } }
] }
```

The interpreter switches on the node name and validates the argument shape it
expects. Unknown nodes and missing arguments name the failure.

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

The source under `src/Marvel.Cards/Run/` is the authoritative node vocabulary.
Every new node needs a parser or interpreter failure test, rule-cited behavior
tests, and a real supported card that needs it.

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
spendEnergyX takeDamage then threatCause thwartDifferentSchemes thwartSchemes
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

## Costs and legality

Target selection and payment are separate. A target answers what the effect will
act on. A payment records what the player spent to initiate it.

The engine preflights an ability before offering it. It checks the printed form,
live conditions, target availability, limits, maxima and ability to pay. An
offered affordance must remain legal when taken against the same state.

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

The runtime refuses these states:

- a reachable face has no authored row;
- a card row contains an unknown field or malformed node;
- an effect names an unsupported query, binding, cost or duration;
- a required choice cannot be represented without guessing; or
- a card crosses a product boundary that setup does not support.

These failures are part of the product boundary. Replacing one with a no-op can
turn an unsupported game into a plausible, incorrect one.
