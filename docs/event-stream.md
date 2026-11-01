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
| `AttackBoundary` | Exact attack onset and final response completion |
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
| `AttackBoundary` | `identity`, `source`, `target`, `started`, `resumed` |
| `WhenRevealedCanceled` | `card`, `source` |
| `CardsShuffledIntoDeck` | `player`, `count`, `public_titles` |
| `ActivityCompleted` | `batch`, `participants`, `phase_step` |
| `PhaseStarted` | `phase`, `round` |
| `AbilityWindowResult` | `source`, `type`, `player`, `occurrence`, `ordinal`, `description`, `completed` |

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

`AttackBoundary` names the attacking `source` and the attacked `target`. An
attack-labeled ability can resolve with no attacked enemy (Giant Stomp's 8 damage
is an independent sentence, per ruling `0fc2a7cc9197e127`); the engine spells that
absent target `-1` on both boundaries.
Likewise, a thwart cause whose thwart has no scheme carries `target` `-1` and an
empty target title; a scheme cause always names the identity it schemes against. Clients accept these
`-1` targets and refuse any other negative target.

`WhenRevealedCanceled` names the card whose When Revealed effects were canceled
and the card whose ability applied that cancellation (`source` is null when no
source was recorded). It is emitted when the
cancellation is consumed successfully, rather than when an interrupt registers
it. It does not cancel revelation or assert that other card effects were
canceled. Both named cards must be readable to retain this fact in a response.
Its engine-chosen verb is `Cancel_When_Revealed`.

`ActivityCompleted` is an engine-authored instruction receipt. Each participant carries
`player`, actual `count`, and authorized `cards`. Covered low-level events carry
optional `activity_batch`, an offset into the owning unfiltered event stream;
their order and payloads remain intact. When independent streams are combined,
`ActivityBatches.Append` rebases both receipts and member references. This also
applies when replay history combines decisions into one history unit. Visibility
filtering preserves these opaque identities; filtered streams must not be rebased
using their reduced event counts. Draws still resolve one at a time and deck resets remain separate.
Only explicit instructions and simultaneous phase procedures create receipts;
matching adjacent verbs never establishes a batch. Restricted visibility keeps
public quantities while removing concealed card ids and names. Optional `round`
captures occurrence context from the game loop. `PhaseStarted` names an actual
phase entry and its round; consumers need not infer boundaries from a final board.
These event additions require a protocol and replay compatibility revision; state
digest v3, decisions and RNG consumption are unchanged.

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

The server filters these events through the viewer scope. `CardsFlipped.PublicFaces`
is engine-only occurrence authority for an explicitly public faceup reveal, such
as the Invocation top. It is ignored by JSON and reconstructed by verified replay.
View retains only that flip's named cards and captured faces, then removes the
authority metadata before export. The existing `subject_faces` and `event_faces`
contracts carry the authorized historical identity even when the final snapshot
conceals the card again. This grants no current location, subsequent movement,
private inspection, or hidden pile order. An ungranted flip still requires final
addressability. A facedown flip cannot use a public-face grant.

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

Each carries `Members`: the area's play areas just after the change, in seat
order with `-1` for the villain's. This is occurrence-time presentation evidence,
like `Subjects`, and the session journal omits it, so recorded event lists and
replay are unchanged. View names the seats from it rather than from the end of
the response, so when two groups join one area in a single response each event
names the area as it then stood: "Spider-Man's play area joined Black Widow's
game area", "… formed its own game area", "… joined the villain's game area",
and at Kang's split, "The villain play area left the shared game area; the
players are now in separate game areas." An event without members names no one
("joined a game area"). The wording is our choice.

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
concealed creation, move, reorder or field change cannot reintroduce a hidden
object id removed from the descriptor. An explicitly authorized public reveal
may retain its occurrence identity without making that object addressable now.

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

Protocol 30's `WorldDescriptor.ActiveAttack` is current resolution context,
separate from the completed-attack event. The engine owns its lifetime through
initiation, defense, boosts, damage and the final response window, including
nested choices. Its current target follows a committed defender. View exposes
the source and target only when both identities are authorized. Retained
last-attack facts do not keep this context alive after the owning occurrence
finishes. This read-only projection changes neither replay nor state digest.

An enemy a begun player attack reaches after its initiation (rr:attack-player-
ability-type.5) -- the enemy Giant Stomp or Melee chooses for its later damage --
is announced by a `LateAttackRecipient` step whose attack-initiated occurrence
targets that enemy. Its interrupt window resolves before the damage. While that
window is open, `ActiveAttack` names the late recipient as the target under the
owning character attack's identity, so the stream carries an `AttackBoundary`
(`started`, `resumed`) for the new target and another when the target returns
to the attack's own. The step name is an engine choice; replay v30 owns the
resulting order ([session-ledger.md](session-ledger.md#replay-identity)).

`AbilityWindowResult` records the initial resolution of a Forced Interrupt or Forced
Response once per occurrence/source/timing/ordinal. `completed=false` means a required decision
suspended resolution; answering that decision does not emit another initial receipt.
Public presentation drops receipts whose source is unreadable. A local acknowledgment
changes presentation only and never initiates another ability or passes a window.


A forced ability's initial execution tags its own emitted results with
`ability_source`, `ability_occurrence`, `ability_timing` and `ability_ordinal`.
These engine-chosen association fields match the receipt's source, occurrence,
type and ordinal; they are not inferred from adjacency, labels or printed text.
The ability executor owns a fresh local event list, so a receipt does not claim
unrelated payment, attack or later-window events from the enclosing response.
Visibility filtering removes the association whenever its source is unreadable,
and filters each result through the normal event policy before any summary.

An optional direct-damage prevention window retains that initial association for
the captured forced invocation's resumed damage segment and subsequent authored
instructions. Placement and synchronous defeat disposal remain associated,
including simultaneous recipients. Nested prevention and reflection execute in
their own invocation; an optional child never borrows a forced ancestor's receipt.
Separately suspended defeat procedures retain their existing event boundary.

The native chronology groups those authorized results under the forced receipt,
with ordered component details retained. Its notification describes actual
outcomes (for example, cards discarded and cards returned), while its separately
authorized source-face reference retains the printed rules for inspection. When
no associated visible result exists, the engine's existing explanation remains;
absence of a visible result does not assert that hidden state stayed unchanged.

Protocol 33 carries engine-authored attack identities and lifetime boundaries,
exact scheme/thwart occurrence causes, resource-ability costs and player-loss
reasons. Attack identities distinguish enemy activation ids from character
occurrence ids. Replay v18 owns their nested completion order; its compatibility
contract is defined in [release-policy.md](release-policy.md). The RNG,
card-state digest and save envelope retain their existing formats. Supporting
travel, reveal and installation receipts may share one presentation beat; every
raw event remains available in diagnostics.
