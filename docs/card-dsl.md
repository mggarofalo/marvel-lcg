`completedAttackDamageDealt: "game"` reads the damage dealt by the enemy attack
completed in the current occurrence. Damage prevention does not reduce it;
boost ability damage does not contribute. The value remains fixed throughout
forced and optional attack-end responses and is zero without a completed attack.

# The card DSL

`invocationTop: "you"` captures the resolving player's exact public typed
Invocation top and adds its printed resource cost to the ability payment.
`resolveInvocation: "you"` resolves that captured physical card's Special through
the existing Special scheduler. `discardInvocation: "this"` sends the resolving
Invocation to its own discard, immediately revealing or recycling its typed deck.
`returnInvocation: "you"` returns the captured physical copy faceup after the
Special procedure completes, including its intermediate discard/reveal/reset.
These spellings are engine choices. Production Doctor Strange rows use them on
nonpublic research boards; complete pack admission remains separate.

`statusCards` selects physical statuses on live characters. `replaceStatus` takes
`card: "chosen"` and a fixed `status` of `tough`, `stunned`, or `confused`.
The destination must differ from the selected kind and obey the host's status
limits and restrictions. A nested three-option `choose` filters illegal kinds;
an independent empty replacement sentence does not suppress Invocation self-discard.
Replacement emits `Replace_Status`, rather than `Give_Status`. These vocabulary
spellings are engine choices, used by production Vapors on nonpublic boards.

`defeatedAllyInYourDiscardPile: "you"` selects the exact physical ally from the
current defeat receipt when its captured controller was the resolving player,
its incarnation has not reentered play, and it is currently in that player's
own discard pile. Ownership and present discard control do not prove control at
defeat. Out-of-play movement followed by return to that discard remains eligible;
reentry into play creates a new incarnation and invalidates the old receipt.
This selector spelling is an engine choice. Putting this selection into
`yourPlayArea` uses the ordinary ally-entry lifecycle and binds `chosen` to the
returned incarnation for subsequent instructions, including after entry windows.
These primitives do not admit Rapid Response or the Black Widow product.

`villainAndMinionsEngagedWithYou` selects the current villain and every effective
minion engaged with the resolving player. It follows the active villain and
projected stage/engagement changes, excludes other players' minions, and can feed
one simultaneous `dealDamage`. The query spelling and villain-first ordering are
engine choices. This primitive does not itself admit Lightning Strike.

`blankPrintedTextUntilEndOfPhase: "this"` is an engine spelling for a standalone
self-blank Action on a live nonpermanent minion. It suppresses the whole printed
text box for every player until the phase scheduler expires the lasting effect.
Traits, printed basic stats and icons outside the text box remain; independently
created lasting effects and already-triggered continuations remain. The marker
binds the in-play incarnation, so departure and reentry restore printed text.
Compound shapes, other card kinds and permanent targets are unsupported. Preflight
checks constant-ending consequences before costs. Actual 05028 admission to the
ability book is gated by `PrintedTextBlankingTests` and the ability-data inventory;
it does not admit the incomplete Ms. Marvel product.

`WhenBoostCardTurnedFaceup` identifies one physical boost card during either an
attack or a scheme. That same occurrence also carries
`WhenAttackBoostCardTurnedFaceup` only when its own `BoostFaceupOperation.Attacking`
is true. This engine-chosen alias does not describe attack initiation and does not
borrow an enclosing or suspended attack. An attack-only villain trigger combines
the alias with `actor: "villain"`; a Defense label alone does not restrict timing.
Both descriptions share one interrupt/response window and cancellation receipt.

`cancelBoostIcons: "trigger"` cancels the remaining numeric icons on the exact
physical boost card in its faceup interrupt occurrence. Constants and amplify
are included; its star ability still resolves, and icons gained by that later
Boost ability are not cancelled (ruling `1ea208998735ef4b`). A zero or previously
cancelled contribution cannot initiate this effect. The captured result is
`{"result":"cancelledBoostIcons"}`. Admission projects this result only through
a direct dependent `then` predecessor; an unbound or unsupported projection
raises before costs. These instruction/result spellings are engine choices.

`attackAnotherEnemy: { "amount": <positive constant> }` is the second sentence
of one attack whose first sentence deals constant damage to its chosen enemy.
It waits for the first damage and defeat procedures, then offers another legal
enemy. The first physical enemy remains excluded across villain stage changes;
a departed and reentered minion is a new copy. Both sentences retain one attack
context and consume a whole-attack modifier once. Arbitrary choice-inside-power
shapes remain unsupported. This spelling is an engine choice for Melee.

`forEachDiscarded` uses the `eachTime` fields (`effect`, `when`, `then`) for an
encounter-deck `discardTop`, but finishes that discard before traversing its own
ordered physical receipt. `that` binds each discarded card, including after an
immediate deck reset or a suspended child choice. Nested discards do not join this
receipt. This spelling and shared traversal representation are engine choices;
printed "each time" timing continues to use `eachTime`.

`result: "activationUndefended"` counts completed undefended enemy attacks in the
most recent scheduled activation collection. Completion captures the final
defender before attack-end responses; prevented damage still counts, while
stun and other attack replacements do not. A new collection clears this receipt.

`result: "activationDamagePlaced"` sums damage taken after prevention in the
most recent scheduled attack collection. It includes owned Boost ability damage
resolved while that activation is live (`rr:attack-enemy-activation.step.3.b`),
step-5 damage and each overkill placement. Ally damage is the specified token
placement, including excess beyond its remaining hit points (`rr:damage.2`).
The exact activation id binds each share; unrelated damage is excluded.
A boost continuation retains its original causal identity, but damage requested
after that activation completes does not change its frozen receipt or join a
subsequent activation (`rr:activation.8`). `activationDamage` retains its separate
attack-step damage-dealt meaning.

Static `enemyAttacks` and `enemySchemes` support a following `then`: every
requested activation must actually be made. Stun or confuse replacement does not
complete an activation; an attack whose damage is prevented still does. The
receipt survives suspension and waits for the activation and its responses.
Dynamic activation collections remain unsupported as dependent predecessors.

`activeVillainSideScheme` selects the side scheme physically paired with the
current active villain. Execution and projected boards follow their own active
counter, including movement and stage promotion. Missing topology or a missing
live pair raises; an ordinary scenario never substitutes its main scheme.

`revealFromVillainDeck: <single-card selection>` reveals the top card of a
living physical villain's paired encounter deck. It preserves the active counter
and resolving player. An empty draw resets only that deck from its paired discard
pile; taking its last card resets immediately, before the child reveal resolves.
The parent continuation waits for the child reveal, including its responses.
Missing or foreign group ownership raises instead of falling back to the active
deck. Admission inspects the public group relation, never hidden deck contents.
Power, area and repeated traces refuse to invent the board after an unresolved
selected-deck reveal.

`villains` selects all living villains independently of the active counter.
Projected queries use projected stage membership, so promotion replaces one
physical stage and final defeat removes that villain from the count.

`moveActiveCounter: <single-card selection>` moves the scenario counter only to a
different living physical villain. It does not change an activation in progress.
The operation requires configured encounter groups; ordinary scenarios do not
substitute a singleton villain. Ranked target selection happens before movement
legality excludes the current holder. A unique extreme at the current holder
therefore leaves no destination. Subsequent text still resolves unless its DSL
explicitly requires completed movement with `then`. Power, area and repeated
projections carry the active relation independently of immutable group ownership.
When a `chooseCard` body also schedules an `afterActivation` effect against the
chosen villain, that effect participates in target admission. The current counter
holder remains eligible if the deferred effect can affect it, even though the
counter move itself cannot resolve.

`schemeDestination: "correspondingSideScheme"` declares an enemy's printed
replacement for the main-scheme destination. Its compiled relation resolves the
scheming villain's immutable scenario group, independently of the active villain
counter. The villain and paired side scheme must remain in play. Unknown values
are rejected, and this metadata alone does not declare a card implemented.
Scheme steps consult this relation before constructing the threat occurrence,
so the existing placement, interruption and response procedures receive the
selected scheme. Whole-pack verification remains tracked by MARVEL-643; the
declaration does not admit Wrecking Crew into public scenario setup.

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

`setHitPoints` takes `card` and a positive constant `amount`, assigning that
many remaining hit points on an in-play identity or villain dial. It uses the
modified maximum, independently of excess lethal damage. Zero, values above
that maximum and targets without such a dial raise as unsupported. It records
the existing health assignment event and a full result only when the dial
changes; a following `then` uses that committed result.
Repeated `eachPlayer`, `forEach` and `eachTime` frames containing this operation
are explicitly unsupported: their transfer analysis does not model absolute
assignment. Admission refuses them before costs or effects change state.

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

The numeric modifier `other_villains_cannot_be_attacked` on a villain implements
"players cannot attack other villains": a positive value permits attacks on that
villain while prohibiting attacks on every other villain. Grant it to
`attachedTo` from an attachment's constant ability to follow the current host and
source lifecycle. Minion targets and damage without an attack label are unaffected;
Guard still prohibits attacks on the host. The modifier spelling is an engine choice.

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

Damage interrupts share `WhenDamageWouldBeDealt` and may qualify that condition
with `"damage": "taken"` or `"damage": "dealt"`. The default is `dealt`.
The field spelling and shared-window representation are engine choices.
Under `rr:prevent.1.1`, prevention reduces damage taken while leaving damage
dealt unchanged. A `taken` trigger therefore requires positive damage remaining
after the recipient's pending prevention, both when offered and before costs.
The qualifier describes the triggering condition independently of actor/target
roles and the effect body. Only interrupt timing on this damage condition accepts
the qualifier. Step-one mandatory soak replacements observe dealt damage and
do not use `taken`.

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
`villainsWithLeastSchemeThreat` and `villainsWithMostSchemeThreat` select every
living villain tied at the corresponding scheme-threat extreme, including the
active villain. Engine choice: their order follows immutable setup groups,
including after stage promotion. Projected evaluation uses projected threat and
physical stage membership; a trace without those facts raises. These queries
select activation targets; they do not move the active counter.

`chooseCard` accepts `chooser: "firstPlayer"` when the first player must select
the target. A mandatory encounter instruction with this declaration resolves its
sole legal target without a prompt; multiple targets ask the first player.
`you`, the original resolving player, and continuation bindings retain their
meanings. Omission uses the resolving player and retains explicit target selection.
Other chooser values are rejected.
`isEnemyAttacker` accepts a singular card selector and tests whether that in-play
card is the current enemy attack's attacker. It does not use the active-villain
counter or a completed attack. Stage-transition projection refuses this condition
when it cannot establish the future attack relationship.
With no active enemy attack, promotion can prove a newly entering constant
inactive when its sole branch requires `isEnemyAttacker: "this"` (directly or
as the first AND operand). An earlier test, an ability-level `when`, an ELSE
branch, another selector, or an active attack keeps
the existing unsupported-stage-constant boundary.
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
deck defeatedByYou delayUntil discardAtRandom discardAnyFromHand discardFromHand
discardHandWithResource discardTop discardUntil discardable discardedWithResource
doubleResourceFor drawToPrintedHandSize dynamic
eachPlayer else encounterDeck encounterDiscardPile enemies
enemiesEngagedWithChosenPlayer enemiesWithTrait enemyAttacks enemySchemes exists
finalStep generate generateTopDiscard giveAdditionalBoost
grantCharactersControlledBy grantEach grantUntil hasStatus hasTrait heroDefended
heroes heroesAndAllies heroAndAllyTargets identities
identitiesWithinPerPlayerLimit inForm indirectDamage isEnemyAttacker isKind isTitle
isYourIdentity keyword kind legalPractice mainScheme makeTheCall maxBy minions
minionsEngagedWithYou modified moveAttackDamage mul onto options overkill
inspectEncounterTop paidWithResource payOrEffect payOrExhaust peekTop perPlayer placeAccelerationToken
placeAtRandom placeCounters placeThreat power powerAmount powerTargets cardsDiscarded
preventDamage preventDamageFrom preventDamageWhile preventThreat
preventThreatRemoval printedResourceCountDiscarded putIntoPlay
recoverDiscardedByResource reduceNextCardCost remainingHealth removeCounters
removeFromGame removeThreat replaceStatus replaceThreatWithDamage requireAllyDefender
resolveSpecials returnOwnedToHand returnToHand revealTop scheme schemes search
shuffle shuffleInto sideSchemes soakDamage sourceKind sourceTrait spend
spendEnergyX startingCounters statusCards takeDamage then threatCause thwartDifferentSchemes thwartSchemes
thwartableSchemes titleInPlay titled tokensOn topEncounterDiscardBoostPlusOne
undefendedAttack until upgradesAndSupportsYouControl
upgradesYouControl wasDefeated withStatus withTrait within withoutAnotherCopyAttached
yourAsideMinion yourAsidePile yourAsideSideScheme
```

## Generic selection and temporary identities

`peekTop: "anyDeck"` first asks which nonempty deck to inspect. Its subsequent
`CardLook` prompt grants only the acting player sight of that deck's top card,
including its full printed text, until acknowledgement. It neither moves the
card nor shuffles. The selected card, area and incarnation live in the existing
ability continuation; visibility is prompt-scoped rather than a face-up mutation.
The villain and main-scheme decks expose their current in-play top stages.

`inspectEncounterTop: {"count":3,"result":"treacheriesLookedAt"}` privately
shows up to the requested number of encounter deck cards, in top-to-bottom
order, without moving, revealing, shuffling or replenishing them. Its single
acknowledgement retains the treachery count as `result: "treacheriesLookedAt"`
for subsequent effects. Alternatively, `result: "boostIconsLookedAt"` retains
the sum of printed numeric boost icons on those inspected cards; star icons and
boost ability text contribute nothing. The result is recorded on acknowledgement,
and admission does not inspect hidden printed values to predict it.
Only the acting seat receives those prompt context
faces; acknowledgement revokes that visibility. The pending decision excludes
intervening game actions. This inspection does not support pre-then completion.

`inspectDiscardOrderEncounterTop: 3` privately retains up to three physical
encounter top cards, discards one chosen card, then asks for the remaining
physical cards in top-to-bottom order. The cards remain in that deck during
inspection; only the acting seat sees their faces until the ordered return.
The discard follows ordinary encounter-deck reset rules.

`heroAndAllyTargets` selects hero-form identities and allies. The existing
`heroesAndAllies` selector includes identities in either form for effects that
name friendly characters; it is not appropriate when printed text names heroes.
`allies` includes other players' allies, while `alliesYouControl` restricts
selection to the resolving player's control.

`withStatus` filters a selection to in-play characters currently afflicted by
`tough`, `stunned`, or `confused`. For example,
`{"count":{"withStatus":{"cards":{"query":"heroesAndAllies"},"status":"stunned"}}}`
counts stunned friendly characters across all players. An identity in either form
and each in-play ally can count; an enemy, a card out of play, and a Steady
character with only one stunned card do not. The selector reads the current
board when its numeric expression resolves, and projected admission reads
projected status counts and Steady.

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

`wasDefeatedByDamage` takes a card selector and tests whether the most recent
direct damage node in this resolution defeated that exact card incarnation.
The receipt survives suspended choices and the defeated card leaving play or
returning; a retained `chosen` binding identifies the original incarnation.
An empty receipt is false. An attack-damage completion clears this direct-damage
receipt and does not supply attack-defeat results. Selecting the latest receipt is an
engine convention for expressing printed "defeated this way" conditions; this
does not change `wasDefeated`, which reads defeats in the current occurrence.

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

`cost: {"discardUntilMinionEngaged":"you"}` discards from the encounter deck
until a minion is discarded, then puts that exact physical minion into play
engaged with the resolving player. These two instructions together are the arrow
cost. Offers inspect available counts and legal effects, without inspecting
hidden deck identities. The retained payment waits for entry, Quickstrike and
engagement responses before initiating the effect once. A no-match or prevented
entry leaves the cost unpaid and completes the event without its effect;
encounter exhaustion still resets the deck and adds acceleration. The current
Defender clarifications are pinned in `datasets/rules-graph.json` as
`ruling:6e301a347a5392fb` and `ruling:a1f4982cfebcd768`.

This cost is admitted only as a standalone arrow cost. Any `seq` containing it,
including nested or single-component sequences, raises `RulesNotImplementedException`
during lowering before payment. Entry responses cannot expose partially paid
compound costs under `rr:cost.5`; simultaneous compound payment remains unsupported.


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

A labelled-power sequence may finish with an independent live tail, including
`giveStatus` on `chosen` guarded by `isInPlay: "chosen"`. The targeted power
retains that recipient's incarnation; use `chosen` inside its body when later
instructions must affect the same copy. Departure makes the guard false, even
if the physical card reenters before the tail runs. Damage and its defeat
interrupts, choices and responses settle before the tail and ability completion.
Suspension admission still projects the prefix to validate damage batches and
unsupported departures. Only an independent final tail permits an opaque minion
defeat interrupt to settle live. Full state projection preserves every ordered
predecessor, including a nested sequence ending with an independent draw.
If any later instruction needs a projected board read, ordinary projection and
unsupported-window guards still apply before costs. This bounded composition
does not predict hidden encounter draws or admit Black Widow content.

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

`{"isInPlay":"chosen"}` asks whether the retained selected incarnation is
currently in play. It is false after departure, including when the same physical
card has reentered as a new incarnation. Retained out-of-play facts remain
available to other conditions. A follow-up that engages an attacked enemy can
combine this condition with its printed trait requirement before `putIntoPlay`.
This condition also participates in projected entry and departure checks.

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
Marksman), `"resourceRestriction": "aspectCard"` (Finesse), or
`"resourceRestriction": "event"` (Biokinetic Polymer Suit). The test-local
Black Widow's Gauntlet interpretation uses `"resourceRestriction": "preparationCard"`
for a Preparation card's resource or ability cost, excluding unrelated ability
costs. Its Resource
trigger has no form restriction. This spelling is an engine choice and does not
admit Black Widow content. The payment engine
checks the cost's actual card before offering or spending the source. Finesse
can pay an aspect card's printed cost or a cost within its ability
(`ruling:6800619dc41a043c`); ability-cost context does not activate card-play-only
resource multipliers. The restriction is valid only on a Resource ability and
unknown values fail admission. Printed wild icons use `G` as elsewhere in this DSL.
The event restriction includes a cost within the event's own ability; a cost paid
for a chosen ally uses that ally's context instead. The distinction follows
`ruling:5b44b2adcc60d4b7`. Hero-form requirements remain explicit trigger fields.
The Preparation interpretation applies the same matching-card semantics inferred
from the Finesse and event-cost rulings to 08007's "for a Preparation card".
The Finesse clarification is scoped pre-1.5, while the October 2024 event-cost
ruling is scoped 1.6 and reiterates the distinction between the actual card's
cost and an enclosing action. Current RR 1.8 `rr:resource-ability.1.1` requires
the specific cost type without limiting it to card play; neither cited ruling
is a Gauntlet-specific clarification. The restriction never supplies the
card context of an enclosing action to an unrelated nested ability payment.

Discard receipts expose `printedResourceIconsDiscarded` and `distinctPrintedResourceTypesDiscarded` numeric expressions (value `"game"`). They count printed resource icons, including wild as its own type, without payment multipliers. `printedBoostIconsDiscarded` counts numeric boost icons and excludes boost stars. Empty receipts yield zero. `printedCostDiscarded` sums the printed resource costs of the physical cards discarded in this resolution, with undefined X equal to zero. `distinctCardTypesDiscarded` counts distinct printed card types in that same receipt. Both take `"game"`; random hand discards retain the same physical receipt as top-deck discards.
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
`discardResourceIcons: <number expression>` asks the resolving player to discard
a minimal hand-card group containing that many printed icons, or all available
eligible icons when fewer exist. Zero skips the choice. The object form
`{ "count": <number expression>, "resources": "BR" }` restricts eligible icons
to mental or physical; resource letters use the existing `B`, `Y`, `R`, `G`
encoding. Omitted `resources` permits any type. This effect does not generate
resources or permit resource abilities. Wild remains its own printed type
(`rr:wild-resource.3`) and does not satisfy a mental or physical restriction.
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

The `cardsIn` filter `set` matches the canonical printed card-set identifier, independently of class or title.
The separate `cardSet` condition tests one selected card's printed set; `cardSet`
is not a `cardsIn` filter.
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

`cardsExhaustedForCost: "game"` counts the physical cards committed by the
ability's `exhaustChosen` cost. It does not count resource generators, automatic
source exhaustion, or allies that were already exhausted. The retained receipt
survives later card movement and resolution continuations. Action admission
checks legal physical cost selections without exhausting cards; a provided
selection is validated against the exact ready candidates and selection range.
`exhaustChosen` accepts `anyNumber: "true"` for a variable positive number of
ready cards from its declared relation. A zero-card payment that produces no
effect cannot initiate Strength in Numbers.

`eachPlayer` accepts `order: "playerOrder"` when printed text explicitly
requires first-player-then-clockwise resolution (`rr:in-player-order`). Without
that field, the first player chooses the ordering (`rr:each-player.1`). Both
forms use the same continuation frames and skip eliminated players.

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


`preventThwarting` is a constant card selection prohibition scoped to threat removed
by basic or labeled thwarting. Non-thwart threat removal remains legal. Projection
supports unconditional `this`/`attachedTo` prohibitions, departed sources, and area
projection attachment-host changes; unsupported projected predicates refuse.
`villainOfAttachedScheme` selects the current physical villain paired with the
source attachment's scheme, independently of the active counter. It requires an
explicit encounter topology and never falls back to the active villain.


`removeStatuses: { cards, statuses }` removes every physical status card of the
listed kinds from selected in-play characters. `statuses` is a nonempty distinct
list using `stunned`, `confused`, or `tough`. Completion is Full when at least one
status card was actually removed and None when none was removed; dependent
`otherwise` observes the committed removals. Invalid character targets are omitted,
and unrelated status kinds remain. Unsupported projected compositions raise before
execution rather than infer future status membership.


`discardRandomFromHand: N` is a positive fixed-count arrow cost. Offer and
admission queries inspect only affordability; commitment samples physical cards
from the seeded game RNG without replacement. The resulting discard receipt can
supply a count to the effect, but admission that depends on the random cards'
printed facts explicitly raises. One random instruction may combine with fixed
exhaustion costs. Multiple random instructions, simultaneous resource spending or
other cost kinds, and event printed-payment composition explicitly raise before
payment. Descriptions state that the hand discard is random; no card-selection
prompt is offered for this cost.

### Ranked selection pools

`minBy` and `maxBy` rank every card in their explicit `of` selector and retain
all ties. Supported `by` values include `cost`, `attack`, `printedHealth`,
`remainingHealth`, and `threat`. Threat reads the projected scheme state during
admission. Ranking alone does not imply removal; a removal effect ranks an
explicit `discardable` pool when permanent cards must be excluded before the
comparison (rr:permanent.4.1).

`otherVillainSideSchemes` selects live schemes paired with living villains other
than the source attachment's physical host, in scenario group order. It excludes
unpaired schemes and follows projected stage membership and attachment hosts. A
missing topology or unpaired host raises; a departed source or defeated host has
no targets. A living selected villain must retain its live paired scheme. The
selector spelling is an authored DSL choice motivated by Wrecker's Command
(07007); it does not itself implement simultaneous threat placement.

`grant: { card: "attachedTo", keyword: "excessDamageAsThreat" }` represents a continuous excess-damage routing instruction. This engine modifier is not a printed game keyword. Admission requires a direct, unconditional, cost-free Constant unit grant to attachedTo; conditional, scoped, lasting and activated forms raise during lowering. Attack and direct damage retain unprevented excess against preplacement remaining health and the physical corresponding scheme; routed excess replaces overkill spill and schedules normal threat placement before attack-end responses.

`requireAttackedPlayerAllyDefender: "attack"` installs a forced-interrupt
requirement for the current enemy attack activation. It requires an ally
controlled by the originally attacked player when one can legally defend. Basic
defense still requires readiness; card-declared defense follows its own readiness rules. Its
persistent scope contains the activation id, attacking enemy and attacked identity;
other activations and other players cannot inherit it. This instruction differs
from the constant `requireAllyDefender: "engagedPlayer"` policy.

### Ordered threat-move endpoints

`moveThreat` accepts singular endpoint selectors and the public `sideSchemes`
pool, optionally wrapped in `minBy` or `maxBy` with `by: "threat"`. Both pools
are evaluated independently before identical source/destination pairs are
excluded (`rr:move.1`). More than one legal ordered pair asks the first player
to choose; one pair resolves directly and no legal pair completes without moving.
The engine chooses pair ordering by source and then destination object id.
The exact offered incarnations survive suspension; leaving and reentering
does not authorize a replacement endpoint. The existing move receipt owns the
subsequent removal, destination placement and response windows.

`amount: "all"` requests the selected source's threat when the move is scheduled;
a numeric expression retains ordinary requested-amount semantics. Result slots
still describe the completed move. These DSL spellings and the single pair prompt
are engine choices. No expansion card is admitted by this syntax.

`attack_hand_discard_cost` is an engine-selected derived modifier for additional
hand cards paid to initiate a basic attack. A constant `grant` to the attacking
character supplies the quantity. It does not add a registered digest field.
The basic-power offer checks the controller's hand and describes this cost.
The attack then asks for exactly that many physical hand cards before paying
them simultaneously with exhaustion. Stun cancels only after payment, including
when no enemy can be targeted. Permission to attack without exhausting does not
waive this separate cost. The suspended cost decision reconstructs through the
ordinary session decision replay; it does not introduce a separate save format.

`inAttackChain: "attack"` is a strict typed condition on the current scheduled
work. It is true during an enemy attack and schemes causally initiated by that
attack, including suspended boost choices and deeper causal activations. A
standalone scheme is false even in hero form. The immutable historical attack
origin survives completion and departure; unrelated later work has no origin.
This spelling is an engine choice implementing ruling:8369361114d2929b's
interpretation of "this attack". Combine it with the player's form condition and
`afterActivation`; it neither moves the active counter nor schedules an attack.
Activations remain sequential under rr:activation.8.

`grantCharactersWithTrait` registers a live lasting combat bonus:
`{"trait":"AVENGER","fields":["attack","thwart"],"amount":1,"until":"EndOfPhase"}`.
The affected set includes all in-play characters with the current effective
trait, across players, including later entrants. It is not a snapshot. Only
positive constant ATK/THW bonuses are admitted; other fields raise explicitly.
Trait reads filter trait-modifying effects before testing live affected sets,
so unrelated combat bonuses cannot recursively consult their own trait filter.


`WhenPlayerTurnBegins` is an observed occurrence before the active player's
ordinary actions, including the first turn and later rounds. A `subject: "you"`
response belongs to the controller whose turn begins. Observation checks live
printed ability declarations; an unobserved empty occurrence is omitted.
`putIntoPlay` with `where: "yourPlayArea"` retains successful physical ally-entry
completion through ally-limit and entry-response windows for dependent `then`
text. Unavailable entry has no completion. A `chooseCard` conditional with no
reachable branch does not offer an unaffected candidate.

`discardHandCards: <number>` asks the resolving player to choose exactly the
computed number of distinct cards from their own hand, capped by available
cards. Zero completes without a selection prompt. The entire group is selected
in one engine-owned request; invalid or foreign selections leave state unchanged.
The physical discarded cards are retained in the ordinary discard receipt.
This operation suspends and is not admitted as a dependent-completion predecessor.


`grant`/`grantEach` may use the derived `thwart_prohibited` modifier. A positive
value prevents the selected character from performing basic or labeled thwarts,
including an attempt canceled by Confused. It does not prevent nonthwart threat
removal or another character's thwart. The field is an engine-selected constant
modifier name, not a registered card-state or digest field.

`trigger.discardedEvent` binds the exact physical event named by a card-play
occurrence only while it remains in the playing owner's discard pile and that
owner is the resolving player. It binds nothing before discard or after removal
from the game. This engine-selected binding supports post-resolution recovery
without selecting a different copy of the same printed card.

`recruitDiscarded: {"trait":"GOBLIN"}` puts the first physical discarded minion
with that printed trait into play engaged with the resolving player. Receipt order
is discard order, including after the encounter deck resets; current discard-pile
membership is not the selection. The string form continues to require at most one
physical match for its named face. Putting the card into play does not reveal it.
The object spelling and printed-trait filter are engine choices.


`modifyPlayedEvent: { "quantity": "damage", "amount": 2 }` increases every
instance of damage from the physical event in `WhenEventPlayInitiated`.
`quantity: "threatRemoval"` instead increases actual removal, independently of
threat prevention. The quantity names and positive fixed amount syntax are engine
choices. The modifier is retired at event disposal, before recovery responses,
including canceled event plays. Controller, hero form and printed trait conditions
remain explicit authored trigger requirements. This mechanism alone does not admit
Embiggen!, Shrink, or Ms. Marvel's pack; printed removal caps and characteristic
nested/cancellation/prevention proofs remain separate delivery work.

### Ordered deck-bottom placement

`{"placeOnBottomOfDeck":"chosen"}` moves one selected card to the resolving
player's deck at its bottom, preserving the other cards' order without shuffling.
The selection uses the same typed selectors as other card instructions. A
`chooseCard` over `cardsIn: { area: "yourDiscardPile" }` supplies an exact discard
selection. The committed movement supplies completion evidence for `then`; an
absent or blocked card does not complete the instruction. Normal draw and deck
reset rules apply to a following draw.

### Distinct resource arrow costs

Arrow costs also support `spendDistinct: N` for one through four resources whose
paid declarations must all have different types. This is an engine spelling for
the printed constraint, not a list of alternative fixed-type actions. Each wild
icon declares its own type (`rr:wild-resource.1`, `.1.1`). Pricing retains the
constraint in `ResourceCost.DistinctTypes`; preparation snapshots the selected
allocation, validates it before any cost, and retains it while resource generators
suspend. Event printed costs combined with this arrow cost remain unsupported.

Ordinary single arrow prices keep an implicit resource component. Their durable
prompt has `Components: null`, with typed requirements such as `["Y"]` or the
generic requirement `[""]`. Combined event and simultaneous ordinary prices retain
that generic requirement on the corresponding explicit component. These are
engine choices preserving exact replay-v24 prompt identity; distinct-resource
prices carry an explicit component to retain their additional constraint.
Combined event pricing has exactly two components: its printed price and one
aggregate arrow price. A sequence of ordinary arrow resource costs contributes
its summed amount and concatenated typed requirements to that aggregate, matching
the committed event payment ledger and its paid-resource receipt.

`chooseCardsToHand` takes a `from` card selector and positive `maximum`. It offers
one physical-card group with zero through that maximum distinct ids; the entire
answer is validated before any card moves. Only out-of-play player-owned cards outside the resolving hand
are candidates. The selected cards enter the resolving player's hand faceup,
preserving physical ownership. `from: {"query":"beneathThis"}` selects only the
source host's stored cards. The selector follows the captured source incarnation; it cannot retrieve from a
reentered new copy. An ongoing ability may finish after its source leaves play. Costs precede the
choice; accepting an empty group does not undo costs. This operation suspends and
resumes the ordinary ability continuation once. Its spelling and movement order
(the submitted physical-id order) are engine choices.

### Committed effect exhaustion

`{ "result": "exhausted" }` is the number of ready-to-exhausted transitions
committed by the latest `exhaust` effect in the current resolution. Each exhaust
instruction replaces it, including with zero; later readying does not change it.
The engine chooses this result name and replacement behavior. Exhaustion paid as
a cost remains in the separate `cardsExhaustedForCost` receipt.

Live resolution can consume this receipt after the exhaustion commits, including
after nested choices. Area and labelled-power projections cannot produce it.
They reject projected trees combining exhaustion and reads of this receipt,
including exhaustion in prior sequence steps, before payment. The guard is
conservative across branches and instruction order; it raises
`RulesNotImplementedException` rather than substituting a live or zero receipt.
Ordinary When Revealed exhaustion followed by healing or Surge resolves live.

## Ordinary play from discard

`{"allowPlayFromDiscard":"this"}` is a standalone, unconditional, cost-free
Constant permission on an ally. It permits that physical card to be offered
through ordinary play from its owner's own discard pile during that player's
turn. It neither makes the discard pile a hand nor makes its cards resources.
Printed cost, discounts, generators, uniqueness and mandatory ally-limit entry
all use the ordinary play pipeline. The bounded permission has no `when`, cost,
limit or maximum and does not generalize to other card kinds or another owner.

### Complete in-play census

`{"query":"cardsInPlay"}` selects physical cards in in-play engine areas, in
object-id order, independently of controller, card kind or attachment host.
It uses `Places.CanAffect` for game-area reach, including central shared cards.
Use the existing `withTrait` selector and one `count` numeric expression to
snapshot the additional threat quantity of Family Feud (06027). Its printed
starting threat remains the ordinary reveal pipeline's responsibility.
Out-of-play areas, including beneath-card storage in either orientation, do not
contribute or produce search information. This engine selector does not admit
Thor or author its actual ability row. Area-changing and repeated projections
requiring complete projected placement explicitly refuse this selector.

### Variable energy arrow cost with a printed event price

An event whose sole arrow cost is `spendEnergyX: "Y"` uses one payment with
separate components for its adjusted printed price and X energy. The displayed
sum (for example `1+X`) is an engine spelling; the component contracts own
allocation. The player defines positive X and assigns each used physical icon
once. Wild declarations, overgeneration and suspended resource abilities use the
ordinary payment pipeline. Printed-price reductions affect only that component;
the committed `result: "energy"` remains the chosen X.

Action admission evaluates feasible future positive X receipts against the same
adjusted combined price and typed allocation bounds as payment. Effect admission
reads the proposed energy result without committing generators, discounts or RNG.
Payment-dependent branches additionally require a feasible declared receipt; the
selected payment and X are checked again for a valid effect before any cost is
committed. Synthetic damage, trait-conditional, no-effect and suspended-generator
coverage exercises this boundary without admitting an expansion ability.

This bounded spelling retains the existing positive-X requirement. It does not
admit zero-X variants whose other effects or modifiers make them significant,
or sequences containing variable arrow costs. Synthetic payment coverage does
not admit Lightning Strike, ignore-tough damage, or Thor's product.

`reduceSelfCostPerEngagedMinion` takes a positive integer and requires a direct,
unconditional Constant ability on `this`. It reads only the physical candidate
in the playing seat's hand and counts that seat's current engaged minions. It
supplies a read-only reduction to the existing card payment calculation, stacks
with existing discounts, and stops at zero. It registers no continuous effect
from the hand and consumes no resource or RNG state. The engine chooses this
bounded instruction and pricing-port spelling; printed 06011 supplies the
behavior. The accepted cost snapshot survives resource-generator suspension.
This mechanism does not admit the Thor product or author its actual ability row.


`reduceSelfCostPerControlledTrait` takes `{ "trait": "PREPARATION", "amount": 1 }`.
The engine chooses this instruction spelling. Like the engaged-minion variant,
it requires a direct unconditional Constant ability on `this` and supplies only
a candidate pricing reduction. Printed 08002 requires one reduction for every
Preparation card its future controller currently controls in play, across card
kinds and in-play areas. The census uses live traits and `CardControl`, so
ownership does not replace changed control; cards in hand or discard are excluded.
Final payment validation owns the zero floor, discount consumption, uniqueness
and ally limit. An accepted payment retains its price through resource suspension.
Test-local Winter Soldier coverage does not admit Black Widow publicly.


## Basic-use interrupt prerequisite

`WhenHeroBasicAttackInitiated` joins the existing attack occurrence only for a
hero's printed basic attack, including permission to attack without exhausting.
It excludes fixed card attacks, labeled attack continuations and ally attacks.
`boostBasicUse: 3` adds three to that occurrence's basic attack amount. The typed
`BasicUse` retains the performer incarnation, matching field and accumulated
bonus; it creates no general ATK effect and cannot affect another attack.

`exhaustChosen` accepts `from: {query: "upgradesOnYourHero"}` and an optional
current `trait`. Candidates must be ready upgrades controlled by the resolving
player and physically attached to that player's hero. Exact selected cards are
validated before payment and the existing prepared payment binds their
incarnations. `CostDescription` names attachment/exhaustion; the effect describes
the one-attack bonus and the interrupt prompt exposes its accumulated amount.
Compound arrow sequences containing this relation raise before execution because
their simultaneous-payment transaction is not represented. This prerequisite
neither authors 06015 nor admits Thor or Teamwork.

`WhenHeroBasicUseInitiated` additionally names a hero's basic attack or thwart.
The isolated `boostBasicUse: "exhaustedAllyPower"` effect requires a standalone
`exhaustChosen` cost with `from: {query: "alliesYouControl"}`, `count: 1`, and
`matchingBasicPower: "true"`. It snapshots the selected physical ally's current
modified matching ATK or THW after exhaustion, with no consequential damage.
The receipt retains incarnation and controller across suspension; a departed,
reentered or foreign-controlled copy cannot contribute its captured power.
An already paid continuation may finish while its bonus fizzles. The basic use's
nominal attack/thwart selects the ally statistic even if the hero substitutes
another statistic for its own power. The bonus belongs only to that occurrence.

This engine-chosen bounded shape permits one matching application directly or
inside sequential effects. Matching applications inside another power, branch,
choice or repetition, unpaired effects/costs and compound matching costs are
rejected at lowering. Cost selection requires the live agenda occurrence;
out-of-agenda direct matching invocations are refused before payment. Positive
adjusted event prices and supplied generators are outside this payment boundary.
This mechanism does not author the 06032 ability-book row or admit its product.

Attached-upgrade costs additionally refuse any supplied resource payment during
preparation, including gratuitous generation at price zero. Event-price preflight
raises for a positive adjusted price or required icons before payment. A discount
to zero remains within the no-generation boundary. Dedicated negative tests
assert untouched source, generator, Weapon, digest, RNG and pending work.

### Original card price at entry

For an entry response on the card that was played, `paidWithResource` reads the
original card-play price, excluding excess generated icons. It does not read the
response ability's own resource cost. The receipt belongs to the played/entry
occurrence and identifies the physical card, its entered incarnation and the
playing seat. Mandatory ally-limit choices and suspended resource generators
retain that receipt. Putting an ally into play, revealing it, and ignoring its
resource cost create no paid-resource credit from an earlier entry.

Ordinary card play retains submitted `ResourceAllocation` declarations and
validates them before exposing the card or committing a contributor. Wild icons
use the declared type; an undeclared wild does not imply energy. Without an
explicit allocation, the existing deterministic `Resources.Paid` selection
excludes overpayment. The receipt is transient engine data reconstructed by
command replay; it adds no card state or digest field.
`dealDamage` accepts `ignoresTough: 1`, an engine-chosen marker for one
instruction's damage instances. It bypasses Tough prevention without discarding
those physical status cards, and leaves damage replacement, other prevention,
and defeat handling intact. It creates no attack or piercing context. Direct
damage recipients retain their physical incarnation. Multi-recipient damage
finishes all replacements and prevention before placing every share, then settles
defeat windows. The property is retained in area, labelled-power and repeated-effect projections. Unsupported
follow-on projections still raise before costs; in particular, a changing
singular area query and unprojected defeat windows remain unsupported.

This prerequisite is synthetic coverage for Thor E3 (MARVEL-745), motivated by
printed `06006` and its vendored non-attack erratum. It does not admit Lightning
Strike or implement its energy payment.

Simultaneous direct damage follows vendored ruling `3eb442853617fe14`: drones
remain in play when Ultron III’s damage eligibility is fixed, even if the same
damage defeats every drone. The direct procedure creates no attack aftermath.

The replacement/prevention/placement barrier does not establish simultaneous
defeat-window eligibility for every recipient. Settlement currently completes
one recipient’s damage steps 6–8 before the next recipient begins step 6.
Vendored ruling `304a4798e0b453b4` says each damage step resolves simultaneously;
this remaining boundary requires independent review and a distinguishing control
where an earlier departure changes a later defeat interrupt. Lightning Strike
admission remains withheld pending that review and any required defeat-stage
barrier. Save/replay and frozen-route compatibility also require integration
evidence against the eventual admitted candidate.

### Choosing a discard card before the arrow

The bounded cost `{"seq":[{"exhaust":"this"},{"chooseFromDiscard":"PREPARATION"}]}`
requires one explicit own-discard card in the activation's target request before
any exhaustion is paid. Its only admitted effect is `{"addToHand":"chosen"}` in
an unlabelled Action. This models the cost-side choice printed on Safe House #29
(08005); it does not admit Black Widow as a playable product.

Lowering refuses other cost/effect compositions. The engine validates the exact
selection before paying, preserves the existing incarnation-aware chosen binding
across suspended payment, and never substitutes a different discard card. Recovery
follows completion of the exhaustion cost. If the captured card has left its
selected area or incarnation while payment settles, recovery has no remaining
card to move. This contract adds no wire, RNG, digest or save fields and changes
no already authored ability or existing replay prompt.

A `thwart` with no `target` represents only an independent villain-status sentence.
Its body must be a single `giveStatus` over `villain`; `automaticTarget` and other
bodies are rejected. This engine spelling preserves one saved thwart occurrence,
its performer, and no thwarted-scheme facts. RR 1.8 `target.3`,
`labeled-ability.4`, and ruling `0fc2a7cc9197e127` permit Covert Ops to resolve
confusion alone, including when Patrol prevents thwarting the main scheme.
A conditional guarded by `exists(thwartableSchemes)` may choose from that same
query for removal plus status, or use an identical independent status sentence.
Both branches retain the same outer thwart label and its whole-ability cancellation.
Other power suspension and conditional admission shapes retain their existing gates.


### Received enemy-attack damage

`{"receivedAttackDamage":"game"}` reads the positive damage actually taken by
this resolving player's exact identity from the matching enemy-attack damage
occurrence. It is independent of damage dealt, current health and later damage.
The engine retains attacker and recipient incarnations, controller at placement,
and each actual placement, including overkill. Tough, complete prevention, zero,
boost effects, and an ally's own damage supply no identity credit. The spelling
and occurrence-local receipt are engine choices implementing `rr:damage.3.2`.

Damage responses bind the captured actual recipient; an overkill identity need
not be the original defending ally. The same receipt survives a suspended ability
and its labeled counterattack. Ordinary attack legality, Guard, Stun and retaliate
continue to apply. Receipts are transient, omitted from diagnostic occurrence
serialization and reconstructed by accepted-decision replay. They add no digest,
card-state or protocol field. This capability neither authors Counterattack
(08030) nor admits Black Widow.
`cancelBoostAbility: "trigger"` cancels only the exact faceup boost card ability
receipt in its Response window. Numeric icons retain their separate receipt.
Target Acquired uses this operation after the flip and before the star resolves
(ruling a66ca21bca34a71f); invalid activation or card incarnation cannot cancel.
## Independent boost cancellation in an attack

The closed `attack` body `then(cancelBoostIcons(trigger),
dealAttackDamage(chosen, cancelledBoostIcons))` with the automatic villain query
can resolve its cancellation sentence when the villain is not a valid attack
target. Current RR 1.8 `rr:target.3`, `.3.8`, `rr:guard.1`, and
`rr:labeled-ability.2` agree with ruling `0fc2a7cc9197e127` (October 2024).
The earlier pre-1.5 ruling `6919a92cb0376eac` does not govern this case.

The interpreter retains one labeled attack, its identity performer and its exact
live boost-icon receipt. A blocked villain is absent from the attack's targets,
damage and retaliation recipients. Its source-only attack boundary can support
an after-you-attack response, without an attacked-enemy response. The discard
cost and successful Preparation completion remain part of the original ability.
Stun cancels both sentences after costs; zero remaining icons cannot admit an
otherwise ineffective ability. Explicit player-selected targets and all other
attack body shapes keep their existing target validation. This adds no general
targetless attack syntax, public pack admission, protocol or save format change.

`damageFromAttack: "trigger"` tests the exact imminent damage assignment owned by
this occurrence. Attack step damage, indirect attack assignments, and overkill
qualify; boost ability damage, nested nonattack damage and damage costs do not
borrow an enclosing attack. The spelling and occurrence marker are engine choices.
Backflip combines this condition with its identity target and remaining damage.
A defense label alone does not impose it (`rr:defend-defense.4.8`).

`query: "invocationTop"` selects only the public physical top of the unique
initialized typed Invocation deck, independently of the resolving player.
Natural Talent and Wong say **the** Invocation deck; Wong's separate heal uses
**your identity** and follows his controller. Before public exposure or without
a top, the query selects no card. Multiple initialized typed decks on a
synthetic research board raise `RulesNotImplementedException` rather than
selecting a seat. This spelling and ambiguity policy are engine choices.
An explicit `discard` uses ordinary discard routing, separate Invocation
recycling and top disclosure. This primitive creates no Special execution path
or public pack admission.

### Exact printed-set search

`cardsIn` accepts optional `set: "doctor_strange"`. This engine-chosen spelling
compares the current face catalog set exactly with ordinal case sensitivity,
using the existing `ICardFacts.EncounterSet` metadata port. It combines with all
other search filters and never broadens the named areas. `top` bounds the physical
search range before set or other criteria apply. Omitting `set` retains existing
search behavior. This prerequisite does not author Mystical Studies or admit the
Doctor Strange product.

### Independent enemy selection and self-discard

An independent `chooseCard(enemies)` sentence with a sequential body of
`giveStatus(chosen)` and positive constant ordinary `dealDamage(chosen)` can be
empty while another sentence still resolves. This bounded engine composition
uses the complete body to determine eligible enemies; it adds no fallback enemy
and does not create status or damage when none is eligible. Attack damage,
ignore-Tough damage, computed quantities, nested choices and dependent `then`
predecessors retain their existing admission gates. Whole-ability target and
partial-effect checks still apply, and costs do not supply a valid target.

Current RR 1.8 `rr:target.2`, `.3.4`, `.3.7` and `rr:ability.4` govern this
sentence boundary. Rulings `8883bc6b0af1c9b8` (May 2024) and
`8c9c9d30f4c01189` (November 2023) agree: the choose sentence limits itself,
and an Invocation's independent self-discard supplies a valid target. The older
pre-1.5 Vapors answer `5bafad5aafa9093b` is superseded on that question.
Test-local printed Crimson (09032) plus synthetic damage prohibition proves
Spell Mastery and Master of the Mystic Arts payment, discard, top disclosure,
recycling and exact physical return; this prerequisite authors no ability-book
row and admits no product.

### Bounded distinct status recipients

`chooseStatusRecipients` accepts `from`, a fixed `status` and positive `maximum`.
It uses the ordinary bounded distinct `TargetRequest` and ability continuation:
the resolving player confirms one complete subset of zero through the maximum
before any character receives a status card. Candidates are in-play characters
that can receive the status according to `Statuses.Count` and `Statuses.Limit`,
including enemies. No candidates skips this instruction; independent later
instructions still resolve. Zero selected supplies no successful effect.
The engine chooses this spelling and physical-id commit order. Candidate
consequences, resolving seat, source and optional count are engine-authored prompt
facts. Unsupported dependent/projection compositions retain explicit refusal.

This prerequisite tests actual printed Seven Rings of Raggadorr (09034) through
Spell Mastery and Master of the Mystic Arts with a test-local Special. It authors
no production row and admits no product. Before public delivery, View/Decisions/
Protocol must preserve the complete-subset prompt, source/cost context and
independent Invocation discard/return consequence; native grouped staging,
unstaging, explicit zero confirmation, multiplayer authority, stale answers and
visible result comprehension remain downstream acceptance obligations. Engine
tests do not certify those contracts or native interactions.
