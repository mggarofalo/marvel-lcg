# The card DSL

`schemeDestination: "correspondingSideScheme"` declares an enemy's printed
replacement for the main-scheme destination. Its compiled relation resolves the
scheming villain's immutable scenario group, independently of the active villain
counter. The villain and paired side scheme must remain in play. Unknown values
are rejected, and this metadata alone does not declare a card implemented.
Activation integration remains tracked by MARVEL-643; the declaration does not
admit Wrecking Crew into public scenario setup.

`defendingCharacter` names the live defender of the current enemy attack.
It selects nothing during an undefended attack, a scheme, after the defender
leaves play, or when only a finished attack remains. It does not fall back to
the attacked identity.

`startingCounters` accepts optional boolean `perPlayer` (default false). Its
positive `count` is multiplied by the number of players who started the scenario
when the card enters play; later eliminations do not reduce that multiplier.

`cardClass: { card, class }` tests the selected card's printed classification
(`Aggression`, `Justice`, `Leadership`, `Protection`, `Basic`, `Hero`, or `'Pool`).
Identity-specific cards retain their printed aspect classification for this test.

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

A printed variable basic power has an intrinsic base definition, for example:

```json
"effect": { "defineBaseValue": {
  "field": "attack", "value": { "remainingHealth": "this" }
} }
```

This is the source's own base before modifiers, as defined by
`rr:non-numerical-variable` and `rr:base-value`. It must be the root of an
unconditional, cost-free constant ability, with at most one definition per
face and field. The supported fields are the five basic powers. The resolved
definition is distinct from an additive `grant`, so quantity explanations and
modified indicators use the same base as numeric evaluation.

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
| `limitPerRound`, `limitPerPhase` | A per-instance use limit; mutually exclusive positive integers |
| `maxPerRound`, `maxPerPhase`, `maxPerGame`, `maxPerInstance`, `maxPerAttack`, `maxPerActivation` | A title-shared maximum |
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
The program holds definitions, not game state. `AbilityRunner` is the public
composition facade: focused query and resource-discovery owners answer read-only
ports, `AbilityResourceExecution` commits a resource ability and records its use,
and `AbilityResolutionExecution` sequences live ability resolution through narrow
encounter and card-play entry ports. Each `World.AbilityRuntime` owns its
`AbilityGameRuntime`, including pending delayed activation work. Activation ids
and card ids cannot identify work in another game. Runners retain the immutable
program; the world retains mutable execution state and verifies its program owner.

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

Encounter `putIntoPlay` with `printedDestination` and `discardUntil` placement
open entry interrupts before moving the retained card, then entry responses before
continuing later text. Cancellation, a moved entrant, or a departed explicit
attachment host completes the instruction with zero applied entries.
`discardOtherEnvironments: "game"` discards the existing environment set while
retaining the triggering environment.

`enemyAttacks` and `enemySchemes` accept an optional numeric `modifier`, evaluated
when each activation is scheduled. It modifies that activation's ATK or SCH and
expires with its activation identity, including cancellation and nested activations.
`enemyAttacks` also accepts `dealBoostCards: "false"` for a printed no-boost
attack. This suppresses normal and additional deals for that activation only;
boost cards assigned before the activation still resolve. The field defaults to
`"true"` and is rejected on `enemySchemes`.
`activationAttacksAgainstYou: "game"` counts completed attacks against the current
player in the most recent scheduled activation collection. Canceled attacks do
not count; a different defending player receives the attack receipt. Each-player
continuations retain these receipts, and an empty new collection resets them.
`undefendedAttack` reads the matching completed attack in an attack-end window.
Form-qualified `WhenRevealed` abilities retain their authored ordinals while only
the revealing player's current form is offered and resolved.

Constant keyword grants accept `attackScope: "basic"` for basic ATK only,
`attackScope: {"sourceTrait": "ARROW"}` for attacks with a matching source trait,
and `attackScope: "source"` when the card itself supplies the attack. The last
form must grant to `this`; an event's source grant is read for its own attack
preview and resolution even while it is in hand. Scoped grants do not become
unrestricted character keywords. Damage, retaliation and consequence previews
share the engine's attacker/source/basic-power query.

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
automaticTarget canAutomaticThwart canLegalPractice
canMakeTheCall cancelOccurrence cancelWhenRevealed cardsIn changeForm characters
charactersYouControl choose chooseCard chooseDiscardToShuffle chooseTopForHand
countersOn damageOn dealAttackDamage dealDamage dealEncounterCards
deck defeatedByYou delayUntil discardAtRandom discardFromHand
discardHandWithResource discardTop discardUntil discardable discardedWithResource
doubleResourceFor drawToPrintedHandSize dynamic
eachPlayer else encounterDeck encounterDiscardPile enemies
enemiesEngagedWithChosenPlayer enemiesWithTrait enemyAttacks enemySchemes exists
finalStep generate generateTopDiscard giveAdditionalBoost
grantCharactersControlledBy grantEach grantUntil hasStatus hasTrait heroDefended
heroes heroesAndAllies heroAndAllyTargets identities
identitiesWithinPerPlayerLimit inForm indirectDamage isKind isTitle
isYourIdentity keyword kind legalPractice mainScheme makeTheCall maxBy minions
minionsEngagedWithYou modified moveAttackDamage mul onto options overkill
paidWithResource payOrEffect payOrExhaust peekTop perPlayer placeAccelerationToken
placeAtRandom placeCounters placeThreat power powerAmount powerTargets cardsDiscarded
preventDamage preventDamageFrom preventDamageWhile preventThreat
preventThreatRemoval printedResourceCountDiscarded putIntoPlay
recoverDiscardedByResource reduceNextCardCost remainingHealth removeCounters
removeFromGame removeThreat replaceThreatWithDamage requireAllyDefender
resolveSpecials returnOwnedToHand returnToHand revealTop scheme schemes search
shuffle shuffleInto sideSchemes soakDamage sourceKind sourceTrait spend
spendEnergyX startingCounters takeDamage then threatCause thwartDifferentSchemes thwartSchemes
thwartableSchemes titleInPlay titled tokensOn topEncounterDiscardBoostPlusOne
undefendedAttack until upgradesAndSupportsYouControl
upgradesYouControl wasDefeated withTrait within withoutAnotherCopyAttached
yourAsideMinion yourAsidePile yourAsideSideScheme
```

## Generic selection and temporary identities

`peekTop: "anyDeck"` first asks which nonempty deck to inspect. Its subsequent
`CardLook` prompt grants only the acting player sight of that deck's top card,
including its full printed text, until acknowledgement. It neither moves the
card nor shuffles. The selected card, area and incarnation live in the existing
ability continuation; visibility is prompt-scoped rather than a face-up mutation.
The villain and main-scheme decks expose their current in-play top stages.

`heroAndAllyTargets` selects hero-form identities and allies. The existing
`heroesAndAllies` selector includes identities in either form for effects that
name friendly characters; it is not appropriate when printed text names heroes.
`allies` includes other players' allies, while `alliesYouControl` restricts
selection to the resolving player's control.

`withTrait` filters by current engine traits. Black Panther's upgrades are data:
`{"withTrait":{"cards":{"query":"upgradesYouControl"},"trait":"BLACK_PANTHER"}}`.
`inObjectIdOrder` sorts a selection by ascending stable physical object id.
Projected engagement retains membership, not movement chronology. An unordered
`last` over engaged enemies after projected re-engagement raises
`RulesNotImplementedException`; explicit object-id ordering remains supported.
The global Drone relation authors this order; engaged relations retain area order.
`faceDown` filters physical facing; it never assigns meaning to a concealed card.
`last` selects the final card in the operand's existing order, retaining topmost
public discard semantics. `inPlayerArea` takes `area` and `player`, and
`withMatchingPlayerArea` filters identities by matching cards in their player's
public `area`, optionally by `kind` and `trait`. These public area operations
reject hidden decks and hands. Querying an empty area does not allocate it.

`defineProfile` declares an immutable blank-minion identity at an unconditional,
cost-free Constant ability root. Its fields are `id`, `title`, `kind` (`minion`),
`traits`, and integer `baseValues` (`SCH`, `ATK`, `HP`; positive HP required).
`engageTopAsMinion` takes `player`, `count`, and a declared `profile` id. Lowering
rejects missing, duplicate, nested and unknown definitions before gameplay.
The generic engine operation moves the top physical player card into engagement,
assigns that identity, and turns it facedown without activating the underlying
printed abilities, resources, keywords or traits. Base values precede ordinary
continuous modifiers. Ultron's profile is authored on 01140; selectors compose
Minion, DRONE trait and facing as required by each card's printed text.

`defeatedWithProfile` takes a declared `profile` and optional `faceDown`
(`"true"` or `"false"`, default false). It selects the physical cards defeated
with that assignment in the current occurrence, using captured incarnation and
facing facts. It requires that each selected copy has left play and has not
reentered. `returnOwnedToDiscard` places every selected physical card on top of
its owner's discard. Ultron Drones authors its Forced Response through these
operations; profile loss on departure does not erase the current defeat's facts.
An unrelated defeat or a new incarnation cannot reuse that provenance.

The physical card owns its printed faces and stable object id. Its composed
`CardInstanceState` owns the current copy's profile, damage, readiness and tokens.
An in-play move preserves that state. Leaving play detaches it immediately;
ordinary minion defeat returns the underlying physical card to its owner's
public discard and restores printed characteristics. Active resolutions retain
only their own last-known source quantities, serialized under reserved
`__continuation.source_state.` result keys; a new resolution reads the new copy.
The hidden digest explicitly records the assignment in version 3.

## Questions and continuations

An ability may stop for player input. The engine puts a typed decision on the
agenda and keeps enough information to resume the same ability and node.

The continuation records stable data such as the source card, ability address,
selected targets, paid resources and intermediate results. It never stores a C#
delegate or a copy of the effect tree.

Card-labelled attacks and thwarts carry one `CardPowerContinuation` intact from
the interpreter to the queued power and back. Rules owns status replacement,
legal targets, source/performer identity, target incarnation, enclosing
occurrence, timing and the reveal's gained-Surge fact. Cards owns the
continuation's structural path,
bindings and resumption. `CardPowerInvocation` describes the power being
initiated; it does not flatten the interpreter's suspended work into arguments.
`AbilityPowerResolution` resumes the power and its containing ability, while
`AbilityWindowResolution` owns activation of an ordinary window ability.

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

An `afterActivation` effect receives `activationDamageToYou`: `1` when that
activation dealt damage to the resolving player's identity, otherwise `0`.
This is recipient evidence, not a damage quantity. Ally-only damage does not
qualify; overkill dealt to the identity does. Damage dealt remains distinct from
damage taken or prevented under `rr:damage.3.2`. `activationDamage` retains the
activation's aggregate damage result.

An actor role of `you` requires the player's identity, rather than an ally they
control. `WhenBasicAttackEnds` distinguishes completed basic attacks from card
ability attacks within `WhenAttackEnds`, as required by One-Two Punch.

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
moving cards. The shared calculation refuses unsupported permanent-attachment
departures. Live elimination still owns discard destinations, events, attack
termination and continuous-effect settlement.

`DamageAssignment` distinguishes damage dealt, damage taken and spending one
tough status card. Live damage, damage descriptions and both ability trace paths
use its step-2 calculation. Live damage supplies step-3 prevention and retains every callback,
status discard, placement and defeat window. The calculation does not predict
future choices or make an unsupported replacement safe to project.

`RuleProjection<T>` distinguishes one known result, possible alternatives and
unsupported calculations. Power analysis stores each alternative as a separate
`AbilityPowerState`; an aggregate cannot supply the first alternative's fields
as though a player selected it. Required unsupported facts raise before payment.
Damage descriptions report uncertainty without turning it into a predicted
health value. See [shared domain calculations](architecture-domain-calculations.md)
for the ownership boundary and remaining extraction candidates.

`AbilityInitiationEvidence` holds the checks that live resolution must retain,
including label validation and scoped target exceptions. It belongs to one
ability resolution, not the shared program. Payment or suspension cannot erase
an exception already established before the cost.

Costs are atomic where the rules require them to be. A failed component does not
leave earlier resources, cards or exhaustions spent. When the protocol cannot
represent a required choice, the engine raises before changing the board.

`AbilityPaymentRules` owns cost eligibility, pricing and complete payment-answer
validation. It reads the board, source and payer without a resolution frame or
event sink. `AbilityCostSelection` supplies card-valued choices and identifies
the physical counter pool a cost can remove. Shared typed-tree inspection lives
in `AbilityEffectStructure`, which receives instructions but no game state.

`AbilityEventPayment` prepares an event's resource payment before any cost moves
a card. Its plan captures selected generators, paying hands, allocated resource
types and cost modifiers. Commitment moves the event out of hand, registers its
paid resources, spends the generators and consumes those modifiers. Preparation
and commitment occur together after initiation revalidation; the plan is not a
saveable continuation.

`AbilityCostPayment` prepares an ordered arrow-cost plan from the complete
validated answer. It captures selected cards, resource requirements, variable
amounts and source incarnation. Commitment takes only the counter-pool reader,
trigger and event sink; it returns healed damage, spent energy and suspension,
not a general effect frame. Taking damage precedes irreversible costs; combined
resource components spend once; other components retain authored order.
`AbilityCardOperations` shares exhaustion and counter removal with effects,
using `ICardCounterPools` from the executing interpreter. Event damage modifiers
are read live through `AbilityEventModifiers`, including after earlier costs.
The runner owns post-payment continuation and copies the typed payment results
into that ability's result scope.

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

### Forced “would” interrupts

A forced trigger may include `"would": true` to give it priority over ordinary
forced interrupts to the same condition (`rr:would`). Webbed Up uses this for
attack replacement; status-card priority remains higher. The parser rejects
this marker on other ability timings. Optional “would” subwindows with distinct
pass lifecycles are not represented by this marker. The supported Core optional
damage-prevention and imminent-threat windows already isolate those conditions.

### Campaign rewards and physical storage

`reduceFirstAllyCost` takes a positive integer in a direct, unconditional Constant
ability on an identity face. It reduces the first ally played by any player each
round while that face is active. The selected identities' compiled abilities on
all forms determine whether the engine records ally-play history; unrelated Core
and Rise of Red Skull games allocate no history effects. An accepted ally play
records history even while the reducing face is inactive. Putting an ally into
play and rejected payments do not. Pricing is read-only, and the marker expires
at the end of the round. This is the Living Legend prerequisite, not pack admission.

`returnTitledToHand` is a cost naming the single controlled card with that printed
title in play. It returns that card to its owner's hand before the post-arrow
effect and discards its attachments through ordinary departure handling. The card
may be exhausted. Missing, uncontrolled, out-of-play and ambiguous matches are
unpayable; a named card is bound by incarnation when payment is prepared. This
primitive is required by Shield Toss; it does not admit Captain America by itself.

`exhaustTitled` is a cost naming the single ready card with the printed title under
its acting player's control. Missing or ambiguous cards cannot pay it.
`consumeCampaignCard` binds a campaign TECH reward, discards it, and records its
physical campaign key as removed before resolving the ability effect. Untracked,
already removed, or non-TECH instances cannot pay this cost.

`storeBeneath` moves selected `cards` underneath a selected `host` outside play.
`faceUp` controls public visibility; optional positive `randomCount` selects without
replacement using the seeded game stream. `beneathThis` selects those stored cards,
separately from in-play attachments. Host departure discards stored cards to their
owners' discard piles.

`initializeAuxiliaryDeck` initializes a named `experimentalWeapons` or `sideSchemes`
deck from its selected cards. `revealAuxiliaryTop` resolves its top card through the
ordinary encounter reveal procedure. Deck exhaustion and recycling are owned by
the engine's auxiliary-deck rules.

`cardsIn` also accepts `yourPlayArea`, including the resolver's in-play areas
and attached cards, with the same printed filters. This aggregate is a selection
boundary, not a shuffleable deck. It excludes other players and out-of-play cards.
`preventSideSchemeDeparture: "this"` is a constant instruction, optionally inside
an `if`, that prevents this side scheme leaving play. It also prevents defeat
and its defeat events (ruling:25a8a46bc9925963); it does not prevent threat removal.
`cardsIn` accepts `yourDiscardPile` as well as the named decks. Its optional
positive `top` bound examines the top of one deck before applying kind, trait,
title, or `aspectOnly` filters. `aspectOnly: "true"` selects printed Aggression,
Justice, Leadership, or Protection cards; identity and Basic cards do not match.

`allowPlayBeneath` is a Constant permission with printed `kind: "Event"` and a
`trait`. The engine may play matching owned cards stored under the live controlled
host as if in hand; the query never moves them to hand or makes them resources.

### Restricted resource abilities

A resource ability may declare `"resourceRestriction": "arrowEvent"` (Expert
Marksman) or `"resourceRestriction": "aspectCard"` (Finesse). The payment engine
checks the cost's actual card before offering or spending the source. Finesse
can pay an aspect card's printed cost or a cost within its ability
(`ruling:6800619dc41a043c`); ability-cost context does not activate card-play-only
resource multipliers. The restriction is valid only on a Resource ability and
unknown values fail admission. Printed wild icons use `G` as elsewhere in this DSL.

Discard receipts expose `printedResourceIconsDiscarded` and `distinctPrintedResourceTypesDiscarded` numeric expressions (value `"game"`). They count printed resource icons, including wild as its own type, without payment multipliers. `printedBoostIconsDiscarded` counts numeric boost icons and excludes boost stars. Empty receipts yield zero.
Arrow costs support `spendAny: N` for a positive number of resources without a required type, and `returnToHand: "this"` for returning the source from play to its owner. Event payment combines generic arrow costs with its printed price without charging twice. Returning a source discards attachments and ends its continuous effects before the post-arrow effect.
### Environment trait copying

`{"gainEnvironmentTraits":"this"}` is an unconditional, cost-free Constant
root. Its source gains the current effective traits of every environment in
play. Other grants and losses on those environments participate in the same
constant-effect fixed point. Leaving play ends that environment's contribution;
independent trait grants remain. This operation does not copy abilities.
Encounter discard searches distinguish `then: reveal`, `putIntoPlayFirstPlayer`,
`putIntoPlay` (the printed encounter destination without When Revealed), and
`attachToThis` (the resolving source overrides the attachment's printed host).
Their `kind` also accepts `attachment` and `environment`.

Card ranking accepts `remainingHealth`, including current HP modifiers and damage,
and preserves all ties for the printed chooser. `cardsIn` accepts `yourHand` and
an optional `cardClass` printed classification filter, such as `Hero` for a
hero-specific card. These spellings are authored DSL choices; the printed card
and Rules Reference determine which criteria an ability must use.
Deck arrow costs use `discardTop: { from: "yourDeck" | "encounterDeck", count: N }` or `dealEncounterCard: "you"`. The complete cost is admitted before payment, movement uses ordinary deck-reset rules, and physical discard receipts are available to the post-arrow numeric expressions. Resource-payment discards are excluded from those receipts.

`discardUntil` can explicitly name `player: firstPlayer` for a reveal whose
recipient differs from the resolving player. `attachCards: {cards, host}` attaches
selected encounter attachments to the specified host without revealing them or
re-running their printed host selection.

`floorDiv` takes a numeric `value` and a positive integer `divisor`, returning
mathematical floor division. Stall Tactics uses complete pairs of delay counters.
`discardResourceIcons: 1` or `2` asks the resolving player to discard a minimal
hand-card group containing that many printed icons, or all available icons when
fewer exist. It does not generate resources or permit resource abilities.
`cardsYouControl` selects in-play controlled player cards; `discardable` filters
identity/main-scheme and permanent restrictions. `otherEnvironments` selects live
environments other than the triggering occurrence's subject.

`preventAttackDamage: "attack"` prevents all damage taken from the current enemy
attack, including assigned indirect shares. It preserves damage dealt, Tough's
priority, boost effects and attack completion. The state belongs to that attack,
survives source departure and defender changes, and does not apply to later attacks.
`sumPrintedCost` totals the printed costs of a selected set, allowing one threat
placement after a group of captive choices. `releaseBeneath: {removeHost: true}`
returns stored cards to their physical owners' hands before host departure can
discard them, and removes the host. Omit `removeHost` or use false when ordinary
defeat should discard the host. `attachmentsOnThis` selects encounter attachment
cards only; upgrades and status cards are different types (ruling:81bce25b60d865d5).
`ignoresCrisis` is a scoped thwart modifier: it permits that ability to remove main-scheme threat while a crisis side scheme is in play, without removing the crisis icon or changing other thwarts.
`releaseBeneath` optionally takes `rescue: "true"` for CAPTIVE allies: the player credited with the triggering defeat becomes their physical owner, the campaign records that rescue when present, and they enter that player's hand before host removal. Ordinary release preserves each card's existing owner.

The player relation `defeater` reads the triggering defeat's credited seat, including when a scenario-owned When Defeated ability has no resolving player. It refuses an uncredited defeat rather than choosing the first player.

### Trait-qualified Avengers costs

`exhaustChosen` accepts an optional `trait`, matched against the selected ready
character's current traits as well as its declared control relation. Both the
offer and payment validation use the same restriction.
`charactersYouControlExceptCost` selects controlled characters excluding the
physical cards exhausted for this ability's chosen-card cost. This evidence is
retained across later target choices and continuations, so an effect saying
“another” cannot ready the character that paid it.

`reduceNextCardCost` retains its existing selected-player form. The form
`{ "player": "any", "kind": "ally", "trait": "AVENGER", "amount": 1 }`
creates one reduction usable by the next matching played card across all players
until the end of the phase. Nonmatching cards and price queries do not consume
it. Kind and trait restrictions are typed effect data; they are not inferred
from card names.

The condition discardedWithTrait tests whether a card discarded by the current resolution has the named printed trait. It reads the discard receipt, including cards that have subsequently moved, rather than searching the current discard pile.

The `cardsIn` filter `cardSet` matches the canonical printed card-set identifier, independently of class or title.
`useAttackForThwart: "trigger"` is an interrupt instruction attached to the exact
initiated thwart. It substitutes the acting character's ATK for THW and uses ATK
consequential icons for an ally. It does not grant permission to initiate a
basic thwart with a dash THW, alter fixed card-effect amounts, or change a later
thwart. A card ability's THW expressions are substituted only while that power
body resolves; unrelated interrupts retain their ordinary stat reads.

`eachPlayer` may specify `player: "defeater"` for a body explicitly assigned
by printed text to the player credited with defeating the triggering card.
This schedules only that player's resumable frame, without an ordering choice;
`you`, searches, and choices within the body use that player. Missing defeat
credit raises instead of selecting a default seat. An eliminated recipient is
not scheduled. Omitting `player` retains the all-player frame behavior.

`reflectAttackDamage: "attack"` installs a replacement on the active player attack for this attachment's host. Every damage instance against that target during the attack is prevented and dealt to the attacking player's identity instead. The replacement survives the attachment leaving play and ends with the owning attack.
The card interpreter supplies `UsesThwartStat` on each scheduled power from its
compiled numeric dependencies and resolved actor bindings. Stat substitution
is offered only when that exact power reads its acting character's THW; a
fixed-number effect or a reference to another character's THW offers no choice.

`putIntoPlay` accepts `where: "yourPlayArea"` for a selected ally. Admission
uses engine-owned ally entry legality (including uniqueness and form-only
restrictions). Resolution ignores resource costs, uses the ordinary ally-limit
and enters-play lifecycle, and resumes the containing ability after that work.
A `chosen` ally retains its new entry provenance for a later instruction; if it
leaves during entry responses, that instruction cannot affect a returned copy.

`WhenPlayerTurnEnds` is the active player's end-of-turn occurrence, before advancing to the next player. Its player binding identifies that player. `inPlayerArea` accepts `identity` for the identity occupying the selected player's play area; encounter constants can select `engagedPlayer` without assuming player ownership.
`distributeThreat` takes `schemes` and a positive integer `amount`. It allocates a total threat removal through the engine repeated-target contract, caps each scheme at its removable threat, applies event modifiers to the total once, and schedules one thwart with one removal instance per assigned scheme. Its ordered assignments remain in the existing power continuation for deterministic replay.


`firstAvailable` selects the first nonempty option in an ordered list of two to eight public title, villain, or main-scheme selectors. It preserves every candidate from that option and never consults later options when the preferred host exists. Attachment placement uses the same selection for reveal and put-into-play entry.
`activationIs: "attack"` or `"scheme"` tests the currently resolving enemy activation; boost effects can restrict a grant to that activation kind.

`result: "schemesCleared"` is the number of schemes whose last threat was
actually removed by the most recent removal instruction. It records committed
removal against the threat held before that instruction, so a defeated side
scheme's departure cannot erase the result. A removal that takes nothing records
zero; later conditional text cannot inherit an earlier removal's success.
Red Skull interpretations use `withoutTrait: { cards: <selector>, trait: "ELITE" }`
to exclude a live trait before presenting choices. `preventDamageWhile` accepts
`card: { titled: "Red Skull" }` to protect that in-play character while the
constant source remains in play. `putIntoPlay` accepts
`where: "engagedWithFirstPlayer"` for explicit first-player engagement.
`chooseDiscardToShuffle` defaults to at least one card and distinct titles;
`min: 0, differentTitles: "false"` represents an optional selection permitting
multiple physical cards with the same title.
`attachmentsOnVillain` selects only printed encounter attachments hosted by the current villain, excluding hosted statuses and player upgrades.
`supportsYouControl` selects the resolving player's controlled supports in play.
`forEach` takes a numeric `count` and an `effect` body, repeating that body for the resolved count. It supports printed counted instructions such as one additional boost card per side scheme.

`attackDamagedYou: "you"` checks whether the completed enemy attack caused the attacked identity to take damage, including overkill, independently of damage to an ally defender.

`dealAttackDamage` accepts `simultaneous: 1` inside a labelled attack. It snapshots
its distinct recipients, places all damage before settling any recipient defeat,
and publishes incarnation-bound attacked/damaged facts on that attack's response
window. Retaliation remains the containing attack's aftermath. Ordinary attack
damage retains its default path. Multiple simultaneous damage nodes in one power
are explicitly unsupported; this marker does not supply multi-target selection,
Guard admission or discard-cost cardinality by itself.

An `attack` may specify `count` (a constant or `cardsDiscarded`) alongside its
target selector. This bounded form requires one `dealAttackDamage` body with constant `amount`,
`simultaneous: 1`, and `cards: { "query": "powerTargets" }`. It presents one
engine-owned selection of distinct, jointly legal enemies. Under
`rr:choose-game-element.3`, selection uses the lesser of the requested count and
the number of available legal targets. `cardsDiscarded` can supply the count from
the physical cost receipt; event resource payments do not contribute. The
ordinary attack form remains a single declared target.

Counted targets retain incarnation bindings and public attack facts through
interrupts. Damage resolves only against the original copies still in play;
departed recipients retain their attacked facts with zero damage taken. A new copy
receives neither the old attack's damage nor its retaliation obligation. Damage
results are correlated by physical recipient, so a missing earlier target cannot
shift another target's result. The next stage of the same villain retains the
shared retaliation behavior. After-attack forced-keyword ordering still blocks
Captain America admission. The counted form does not admit arbitrary compound
bodies.
