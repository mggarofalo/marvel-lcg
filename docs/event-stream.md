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

Events use card object ids and `AreaRef` values. They never contain references to
engine objects.

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

Game-area topology is outside digest v2, so the engine emits these changes
directly:

| event | payload |
|---|---|
| `PlayAreaJoined` | `play_area`, `game_area` |
| `PlayAreaDetached` | `play_area`, `game_area` |

Every event also carries `kind`, `trigger`, and `verb`. The tables above are the
public serialized union and are checked directly against the C# records.

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
event kind is a protocol compatibility decision because an older client cannot
interpret an unknown discriminator.

The server filters the prompt, events and world descriptor as one result. A
concealed creation, move, reorder, flip or field change cannot reintroduce a
hidden object id removed from the descriptor.

## Verification

Tests hold the stream in 3 ways:

- focused rule tests assert the semantic event emitted by an operation;
- projection tests assert that visibility filtering cannot leak hidden state;
- server tests round-trip the versioned wire records through both transports.

The state digest is not an event-stream completeness oracle for game-area
topology because that topology is deliberately outside digest v2. Direct
rule-cited tests cover that emitted-only surface.

## Relationship to affordances

Affordances describe what the player may do next. Events describe what the last
decision did. Both are domain wire types and arrive in the same engine response.

See [affordances.md](affordances.md) for input and
[presentation-layer.md](presentation-layer.md) for transport and visibility.
