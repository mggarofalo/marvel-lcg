# Semantic event stream

Every engine decision returns semantic events beside the next prompt and visible
state. Events explain what changed. They are not animation commands and do not
replace the current snapshot.

`Marvel.Rules.Events` defines the event vocabulary.

## Why events exist

Two snapshots can show that a card moved without saying why or how a client
should present the transition. A semantic event names the operation while the
snapshot remains authoritative about the resulting state.

Consumers may use events for animation, logs, accessibility cues, simulation
records and diagnostics. They must not infer new rules from them.

## Event vocabulary

The current public events are:

| Event | Meaning |
|---|---|
| `CardsCreated` | New card objects were created in an area |
| `CardsMoved` | Existing cards moved between areas |
| `AreaReordered` | Existing cards changed order within one area |
| `CardFormChanged` | A multi-face card changed its active face |
| `CardsFlipped` | Cards changed physical face-up state |
| `CardAttached` | A card gained a host |
| `CardDetached` | A card lost its host |
| `ControlChanged` | A card changed controller |
| `PlayAreaJoined` | A play area joined a game area |
| `PlayAreaDetached` | A play area left a game area |
| `FieldSet` | One named gameplay field changed |
| `BoostResolved` | Applied boost icons and the current activation stat |
| `AttackCompleted` | An enemy attack ended with its established target and defender |
| `CardsShuffledIntoDeck` | A completed shuffle of returned cards, with public names and no hidden identities |

Most events use card object ids and `AreaRef` values. A public shuffle receipt
uses only a seat, count and already public names. Events never contain references
to engine objects.

### Derivable events

These nine event kinds describe transitions visible in digest state:

| event | payload |
|---|---|
| `CardsCreated` | `area`, `cards` |
| `CardsMoved` | `from`, `to`, `cards` |
| `AreaReordered` | `area`, `order` |
| `CardFormChanged` | `card`, `from`, `to` |
| `CardsFlipped` | `cards`, `face_up` |
| `CardAttached` | `card`, `host` |
| `CardDetached` | `card`, `host` |
| `ControlChanged` | `card`, `from`, `to` |
| `FieldSet` | `card`, `field`, `from`, `to` |

### Emitted-only events

Game-area topology, attack completion and applied cancellation are outside digest v3, so the engine
emits these facts directly:

| event | payload |
|---|---|
| `PlayAreaJoined` | `play_area`, `game_area` |
| `PlayAreaDetached` | `play_area`, `game_area` |
| `BoostResolved` | `card`, `enemy`, `icons`, `attacking`, `strength` |
| `AttackCompleted` | `enemy`, `target`, `defender`, `damage_dealt` |
| `WhenRevealedCanceled` | `card`, `source` |
| `CardsShuffledIntoDeck` | `player`, `count`, `public_titles` |

`CardsShuffledIntoDeck` is emitted after the selected cards have moved and the
deck has been shuffled. `count` records the returned quantity. `public_titles`
contains only names readable in the public source before the move, sorted by
title independently of deck order. Unreadable names are omitted. The receipt
contains no physical card ids, landing indices or links into the hidden deck.
Its engine-chosen verb is `Shuffle_Into`. Visibility retains this public fact
while stripping inherited `subjects`; it does not make hidden deck cards
addressable. Protocol 25 introduces the event. Replay v9 is required because
replay verification compares each decision's exact semantic event list, even
though this addition does not change decisions, state, or RNG consumption.

`BoostResolved` records each boost after its ability and icon application, before
its discard and the next boost. `icons` is the nonnegative applied contribution;
`strength` is the engine's current modified ATK or SCH, not final damage or threat.
Its engine-chosen verb is `Boost_Resolved`, distinct from dealing, flipping and
discarding a boost card. Both card and enemy must be readable; only their
occurrence-time names survive visibility filtering. An activation that ended before icon application emits no
contribution. Protocol 26 and replay v10 version this added semantic event; save
schema 5, state digest v3, decisions and RNG consumption are unchanged.

`AttackCompleted` identifies the attacker, final attacked character, and defender
(`-1` when undefended). It asserts that the attack ended, including an attack
that ended early. `damage_dealt` records the actual attack-step damage, excluding
boost abilities; it is null when that fact is not recorded. Zero is an explicit
completed outcome. Lethal placement finalizes this total after accounting returns,
so damage dealt remains distinct from the target's remaining HP. Physical damage
and prevention retain their own events. The visibility filter
requires every named participant to be readable and retains only those subjects.
Its engine-chosen verb is `Attack_Completed`, distinct from an `Attack` effect.

`WhenRevealedCanceled` names the card whose When Revealed effects were canceled
and the card whose ability applied that cancellation (`source` is null when no
source was recorded). It is emitted when the
cancellation is consumed successfully, rather than when an interrupt registers
it. It does not cancel revelation or assert that other card effects were
canceled. Both named cards must be readable to retain this fact in a response.
Its engine-chosen verb is `Cancel_When_Revealed`.

Every event also carries `kind`, `trigger`, and `verb`. An event may carry a
`subjects` object mapping card object ids to visibility-safe names captured when
the event occurred. This preserves an effective identity through later changes
in the same response; when absent, presentation uses the resulting snapshot.
The tables above are the public serialized union and are checked directly
against the C# records.

## Areas

`AreaRef` identifies an area with:

```text
(Zone, Owner, Host, Id)
```

`Zone` is the stable area kind. `Owner` distinguishes player and scenario areas.
`Host` identifies attached-card areas. `Id` distinguishes multiple runtime areas
that otherwise share the same shape.

Area identity matters because later rules can create several play areas or group
them into game areas. Those broader patterns validate the state model but do not
open an expansion product boundary. See [scope.md](scope.md).

## Creation and movement

`CardsCreated` carries each new object id and printed face id. It says where the
objects first exist.

`CardsMoved` carries the source, destination and one `Landing` per moved card.
The landing index describes the card’s final position in the destination after
the complete move. For several cards entering one area, the indices describe the
final area rather than a series of transient insertion points.

Creation and movement stay separate. A card that did not exist cannot be moved,
and a created card has no prior area for a client to animate from.

## Ordering

`AreaReordered` carries the complete ordered object-id list for one area. It is
used when the membership stays the same and only order changes, such as a
shuffle.

A shuffle is not represented as several moves. That would invent intermediate
states and could reveal hidden card identity through a sequence of positions.

Visibility filtering may remove hidden ids from the client event while the
visible snapshot still reports the resulting pile size.

## Faces and physical visibility

`CardFormChanged` names a game face change such as changing identity form. It
records the previous and next printed face ids.

`CardsFlipped` records physical face-up state. The distinction matters: changing
form and turning a card faceup are different rules operations even when both
change what is readable.

The server filters these events through the viewer scope. An event never grants
access to a face the resulting descriptor hides.

## Attachments and control

Attachments have explicit attach and detach events. A move alone cannot describe
the host relationship because the attached card may remain in the same play
area.

Control changes are also explicit. Ownership, control and physical area are
separate state concepts and may change independently.

## Game-area topology

Play areas can join and leave game areas without moving any card. The digest does
not serialize this topology, so `PlayAreaJoined` and `PlayAreaDetached` are
emitted-only events backed by rule-cited state tests.

These events name public topology and survive visibility filtering. They do not
expose a concealed card or private seat state.

## Fields

`FieldSet` names the card, field, previous value and next value. A missing value
means the field was absent, not zero.

Named fields keep unrelated changes separate. Damage, threat, counters, ready
state and printed modifiers do not collapse into one arithmetic delta.

The state descriptor after the decision is still the source of truth. A consumer
that misses an event can synchronize instead of replaying guessed deltas.

## Transaction boundary

Events are emitted in deterministic resolution order. They describe committed
operations from one engine decision.

If an operation is refused before mutation, it emits nothing. If a decision
opens a prompt part-way through an ability, the response contains events already
committed and the continuation holds the remaining work.

The event list does not contain wall-clock timestamps or random identifiers.
Those would make the same seed and decisions produce different records.

## Wire boundary

`Marvel.Server` serializes events as a versioned polymorphic union. Adding an
event kind or payload field is a protocol compatibility decision because an
older client cannot safely interpret the changed contract.

The server filters the prompt, events and world descriptor as one result. A
concealed creation, move, reorder, flip or field change cannot reintroduce a
hidden object id removed from the descriptor.

## Verification

Tests hold the stream in 3 ways:

- focused rule tests assert the semantic event emitted by an operation;
- projection tests assert that visibility filtering cannot leak hidden state;
- server tests round-trip the versioned wire records through both transports.

The state digest is not an event-stream completeness oracle for game-area
topology because that topology is deliberately outside digest v3. Direct
rule-cited tests cover that emitted-only surface.

## Relationship to affordances

Affordances describe what the player may do next. Events describe what the last
decision did. Both are domain wire types and arrive in the same engine response.

The protocol 19 `DeferredTargetSelection` affordance marker identifies an
effect's separate target-choice boundary after current costs commit. Cost
events in the response are already committed even while that target prompt
awaits an answer. The marker supplies no future candidates and does not make
the committed payment a reversible target draft. It shares the unreleased
protocol 19 compatibility change. The marker itself changes no replay or save
format; exposing the current encounter before its When Revealed interrupt
window changes intermediate events/information and uses engine replay v3.

See [affordances.md](affordances.md) for input and
[presentation-layer.md](presentation-layer.md) for transport and visibility.
