# Deterministic session ledger

The session ledger is the server-owned authority for a playable game. It records
setup and accepted player decisions, reconstructs the mutable rules engine by
deterministic replay, and commits a versioned save before acknowledging a state
change.

The ledger supports 5 related jobs:

- save and restore;
- replay and divergence diagnosis;
- undo and redo;
- legal reordering of committed actions; and
- structured observation of session work.

These jobs share one trace, but they do not share authority. The save and its
verified replay are canonical. Logs, metrics, traces, client snapshots and
semantic-event presentation are consumers.

This document specifies project choices. The tabletop rules do not define save
files, revisions, durable command ids, undo, telemetry or atomic file writes.
The rules remain authoritative for which replayed decisions are legal.

## Delivery status

The engine already has the determinism, stable decision selectors and replay
checks needed by this design. `Marvel.Sim` records setup, prompts, decisions,
events and digests, then deals and resolves the game again to find divergence.

Schema 2 save, atomic generation commit, strict load, verified replay, the
information frontier, linear undo and redo, legal action reordering, and the
redacted structured operational-log boundary are implemented for hosted
sessions. The embedded host uses the same ledger and replay path with an
isolated memory store. Opt-in bounded metrics and correlated traces consume the
same redacted operational signals. A configured private diagnostics volume now
retains rotated copies, and a read-only incident manifest correlates those
records with hashes of the opaque save generations. Neither is replay or save
authority. No client snapshot, incident record or simulation record is a player
save. The desktop client presents visibility-safe completed units at their
server-advertised cursor boundaries and can undo the latest unit or choose an
eligible earlier point without learning the frontier signal that excluded any
other boundary.

## Boundary

The ledger will live in a Godot-free shared project above `Marvel.Rules` and
below its consumers. `Marvel.Server`, `Marvel.Sim` and the embedded local host
will use the same records and replay implementation. Rules, cards, content and
view projects must not reference the session layer.

```text
Marvel.Godot ─┐
Marvel.Server ├── Marvel.Session ── Marvel.Content / Marvel.Cards / Marvel.Rules
Marvel.Sim ───┘
```

The server owns one ledger and one reconstructed `Game` for each game label.
Clients submit decisions and session-edit commands. They never submit a `World`,
a descriptor, an event list, an RNG state or a replacement save.

`World.Digest()` remains a hidden-truth comparison format. It may verify a
server replay but must never cross the client boundary. A visibility-safe
`WorldDescriptor` remains the client snapshot.

## Terms

| Term | Meaning |
|---|---|
| Decision record | One durable answer to one prompt. |
| History unit | One root game operation and every dependent answer needed before the next root menu. It may be open or complete. |
| Active prefix | The units before the ledger cursor. They produce the live game. |
| Redo suffix | Previously committed units at or after the cursor. They are inactive but may be replayed. |
| Edit frontier | The earliest cursor position to which the current table may move. |
| Candidate | A new game reconstructed off to the side from a proposed trace. |
| Revision | The server concurrency token. It changes after every accepted gameplay or history command. |

## The save is a decision trace

A save is one strict UTF-8 JSON document. Schema 2 has these top-level members
in this order:

```json
{
  "format": "marvel-session",
  "schema": 2,
  "compatibility": {},
  "session": {},
  "setup": {},
  "initial": {},
  "revision": 0,
  "cursor": 0,
  "edit_frontier": 0,
  "current_prompt": {},
  "units": []
}
```

Schema 1 is the single readable predecessor. On startup the server parses it
with its original strict shape, replays the complete trace to derive every
information signal, and atomically commits a schema 2 generation before making
the session available. It never rewrites the active generation in place and
never publishes a partially migrated session. New saves and every later commit
write schema 2 only.

Unknown members fail loading. Missing members fail loading. A later schema uses
a new number and an explicit migration; a reader never guesses how to interpret
it.

`session` contains a server-generated storage id and the bounded display label
used to open the table. The storage id uses operating-system entropy outside
gameplay and never enters the seed, RNG stream, prompt, event list or digest.
Only that validated fixed-alphabet id names the save file. A user-supplied game
label is data inside the envelope and is never interpreted as a path.

It also records the durable lifecycle state, `active` or `retired`. A retired
session is a tombstone: startup does not publish it, and its display label may
be reused. Retention policy may later delete its save and protected metadata,
but deletion is not the operation that makes a close durable.

### Compatibility

`compatibility` records everything that can change replay meaning:

- the application version;
- the session schema;
- the engine replay-contract id;
- the RNG contract id;
- the state-digest version;
- the card dataset SHA-256;
- the setup dataset SHA-256; and
- the ability dataset SHA-256.

The application version records where the save came from; equality is not a
load requirement. The replay-contract id and dataset identities decide whether
the current application can attempt a replay. Protocol version is not replay
authority. It may be recorded for diagnosis, but a save is not invalid merely
because a newer client protocol can carry the same session commands.

Loading rejects an unsupported engine or dataset identity before dealing a
game. A migration may write a new save only after complete replay under the new
runtime verifies every prompt, decision, event and digest. “The final board
looks plausible” is not a migration rule.

### Setup

`setup` records the complete authored input to a game:

- scenario key and mode;
- ordered hero keys, which are seat order;
- null, empty or ordered modular-set selection without collapsing those cases;
- the unsigned 32-bit game seed; and
- any later setup choice that can affect dealing or rules behavior.

The ledger deals from this record. It does not deserialize card objects.

`initial` records setup semantic events, cumulative RNG words consumed and the
complete canonical initial state digest. Replay verifies all 3 before accepting
the first unit.

`current_prompt` is the stable server-side prompt at the active cursor, or null
for a terminal game. It is written on open and after every accepted gameplay or
history command. It pins the initial mulligan prompt in a zero-unit save and the
pending payment, target, interrupt, response or forced-effect prompt at the end
of an open unit. It is not a visibility-safe client menu: publication still
projects separate authorized views for each seat.

### Durable decisions

An affordance id is a live-session handle and never enters a save. A taken
decision records the stable selector already proven by `Marvel.Sim`:

```text
(anchor_id, anchor_player, verb, label, occurrence among exact matches)
```

It also records:

- the seat authorized to answer the prompt;
- ordered targets;
- ordered resource generators;
- defined numerical values;
- per-icon resource allocations;
- a stable prompt record before resolution;
- semantic events after resolution;
- the cumulative gameplay RNG words consumed after resolution; and
- the state fingerprint after resolution; and
- the engine result after resolution, which is null until the game is terminal
  and otherwise includes the outcome and terminal round.

Schema 2 defines the state fingerprint as `World.Digest()` plus the recorded
engine result. `World.Digest()` alone contains card state and cannot distinguish
a win from a loss on an otherwise identical terminal board. Replay verifies
both parts after every decision.

A decline records no selector. Replay resolves a taken selector against the
newly generated prompt and requires exactly one legal match. It then validates
targets, payment, values and allocations before calling `Game.Resolve` once.
There is no fallback to label matching, an old handle or the nearest legal
choice.

Object ids are durable only inside one deterministically dealt and replayed
game. They are not global ids and cannot identify a card across unrelated saves.

Unit ids are their zero-based positions in `units`; they are not random values.
A unit records its role, `open` or `complete` status, initiating seat, active
seat, round, phase, ordered decision records and derived frontier signals. The
serializer pins each nested record's exact member set and order with schema
tests before schema 2 ships.

## History units

One UI action can open several prompts. Playing a card may choose targets and
payment, then open interrupts, responses or forced effects. Reordering only the
first answer would split one operation into an invented history.

A history unit starts when the engine accepts a root menu decision. It ends
when the game reaches the next root menu, a phase boundary or a terminal state.
Every dependent answer between those points belongs to the same unit, including
answers from another seat.

The first accepted root decision creates an `open` unit and commits that save
before acknowledgement. Every later dependent answer appends to the same open
unit and is also committed before acknowledgement. Reaching the next root menu,
phase boundary or terminal state changes the unit to `complete` in the same
commit as the decision that reached it.

Every such commit also writes the prompt returned after the accepted decision
as `current_prompt` before acknowledgement.

An open unit is canonical and replayable, but not editable. Undo, redo and
reorder are unavailable until it completes. A server restart replays every
decision in the open unit and restores its exact pending payment, target,
interrupt, response, ordering or forced-effect prompt. The server never rolls
an open unit back merely because the process stopped between its decisions.

Units have one of these roles:

| Role | Examples | Reorderable |
|---|---|---|
| `turn_action` | play, printed Action, basic power | yes, when every other rule permits |
| `turn_control` | change form, end turn | no |
| `phase_step` | mulligan, discard to hand size, defense | no |
| `forced_resolution` | mandatory ability or ordering choice | no by itself |
| `terminal` | the decision that ends the game | no |

Dependent decisions move with their root unit. A unit may still be rejected in
a new position because its dependent selector no longer exists or is no longer
legal.

The active player, round, phase and turn identity are recorded for diagnosis and
verified from replay. They are not commands that can force the reconstructed
game into a phase.

## Multiplayer action authority

The Rules Reference defines the legal distinction:

- `rr:player-turn.2` permits ordinary card play during that player's turn.
- `rr:player-turn.3` and `.4` permit identity and ally basic powers during that
  player's turn.
- `rr:player-turn.5` permits printed Action abilities.
- `rr:player-turn.6` lets another player trigger an Action they could use on
  their own turn after a request, and lets that player offer the Action.
- `rr:action.1` and `rr:ownership-and-control` limit which cards and abilities a
  player controls.

The product collapses an accepted request or offer into the Action itself. It
does not add `Ask`, `Offer` or `Accept` decisions. During a player turn, every
non-eliminated seat may directly submit a currently legal printed Action for
that seat while the engine is at the root turn menu. An untimed Action is not
available while another action, payment, target choice, interrupt, response or
forced effect is being resolved.

This does not grant the other turn options. Off turn, a seat cannot:

- play an ally, upgrade, support or player side scheme through the ordinary
  play option;
- change form;
- end the active player's turn;
- use an identity's basic attack, thwart or recovery; or
- use an ally's basic attack or thwart.

An Action event is reached by triggering the Action printed on the event, as
`rr:player-turn.5.d` specifies. Interrupt and Response events remain governed by
their own timing windows.

Each seat receives a visibility-safe menu containing only commands it may
submit at the current revision. The selected affordance retains the acting seat
even when another seat is active. The server validates capability, actor,
selector and revision before replay. Two simultaneous menus cannot produce 2
commits at one revision: the first accepted command advances the revision and
the other becomes stale.

“Belongs to a player” is not a sufficient authorization rule. The Rules
Reference distinguishes ownership from current control. Ordinary play comes
from the active seat's hand. An in-play Action belongs to the seat that may
currently trigger it under ownership, control, card text and encounter-card
rules. The engine derives that seat; the client does not assign it.

## Transactional mutation

Resolve, undo, redo and reorder follow one gameplay transaction:

1. Authenticate the session and acting seat.
2. Require the expected live revision.
3. Build a proposed ledger at the next revision without changing the live
   session.
4. Deal a fresh game and replay the complete proposed active prefix.
5. Validate prompts, selectors, events, RNG consumption and state digests.
6. Write the complete proposed save to a sibling temporary file.
7. Flush it and atomically replace the prior save.
8. Swap the candidate ledger, game and next revision into the live session.
9. Return the authorized snapshot.

If validation, replay or persistence fails, the candidate is discarded and the
live session remains unchanged. A response never claims a mutation succeeded
before its canonical save is committed.

Session lifecycle commands have the same save-before-ack rule:

- `open` creates the initial ledger and protected owner authentication metadata
  as one durable persistence transaction, then publishes the live session and
  returns its credential. A crash cannot leave an acknowledged game without a
  recoverable save or leave an unpublished active save that startup later
  exposes without its owner authority.
- `attach` consumes the invitation and durably records the new resumable seat
  authority before returning it. One-time invitation state and resumable
  credentials remain protected metadata, never deterministic game data.
- non-owner `close` durably revokes only that credential before acknowledging;
  it does not retire the game.
- owner `close` atomically marks the ledger `retired`, revokes every associated
  credential and invitation, and removes the live session before acknowledging.
  Recovery completes either the old active bundle or the new retired bundle;
  it never republishes a successfully closed game.

The persistence abstraction treats a ledger and its protected authentication
metadata as one recoverable bundle even when the operating system stores them
in separate files. A small committed manifest or equivalent journal identifies
the complete generation. Startup ignores incomplete generations.

Candidate replay has 2 modes:

- verification mode compares every recorded prompt, event list, RNG count,
  frontier signal and digest with the reproduced value. Load, restore and redo
  use this mode, and compare the reconstructed prompt at the active cursor with
  `current_prompt` before publication;
- construction mode verifies the unchanged prefix, then treats decisions as
  inputs and regenerates every derived prompt, event list, RNG count, frontier
  signal and digest after the edit point. A new decision and an accepted reorder
  use this mode.

Construction mode is not permission to repair an input. Every durable selector,
actor, target, payment, value and allocation must still resolve exactly and be
legal. Reordering can change derived damage and healing, so comparing those
records with their old order would reject the feature by definition. The newly
derived records replace the old suffix only after the complete candidate is
The initial implementation favors this full replay over serializing internal
continuations or maintaining inverse mutations. Verified checkpoints may later
improve performance, but they are caches. Losing a checkpoint cannot lose or
change the canonical game.

The file operation must leave either the previous valid save or the proposed
valid save after interruption. Platform-specific replacement and directory
flush behavior belongs behind one persistence abstraction and receives crash
tests on every supported server platform.

## Cursor and branch behavior

`cursor` is the number of units in the active prefix. A normal game has
`cursor == units.length`.

The active prefix may end with one open unit. No other unit may be open, and a
redo suffix may contain only complete units. History editing requires every
unit in the active prefix to be complete.

Undo decrements the cursor to an allowed unit boundary and replays the shorter
prefix. Redo increments it and replays the retained suffix. Neither operation
reverses an event or mutates the live `World` backward.

Committing a new gameplay decision while a redo suffix exists removes that
suffix before appending the new unit. The save is a linear history, not a branch
graph. Reordering replaces the selected active range and also clears any redo
suffix.

The host revision never moves backward and is not the cursor. Resolve, undo,
redo and accepted reorder each advance it exactly once. Loading restores the
saved revision; issuing new runtime capabilities does not alter gameplay state.

Capabilities, invitations and plaintext bearer secrets never enter the game
save. Restart-safe authentication needs separate protected server metadata,
such as a verifier for a resumable credential. That metadata is operational
authority, not deterministic gameplay input.

## Information and randomness frontier

Players must not use history editing to act on information learned from a line
they then erase. The server therefore persists `edit_frontier` in unit space.
Undo and reorder cannot cross it. Replay also derives the frontier and requires
it to equal the saved value.

The shared ledger derives internal signals from authoritative rules-resolution
metadata, prompts, semantic events and gameplay RNG consumption when a
transition:

- draws a card;
- looks at or searches concealed cards;
- reveals a card or hidden face;
- makes a random selection;
- shuffles or otherwise consumes gameplay RNG for hidden state; or
- performs an equivalent operation added later.

Each signal records one of the bounded reasons `draw`, `search`, `reveal` or
`random`, plus a sorted set of seats for whom information became knowable. The
current cooperative product makes player-controlled cards readable to the
table, so every current signal names every seat. The explicit audience remains
part of the save so a later concealed-hand PvP policy can narrow it without
changing the frontier model. The server does not trust a client claim that
nobody looked, and disconnecting a client does not weaken the boundary.

RNG consumption advances the frontier even when its result remains concealed.
This is deliberately conservative. It prevents a trace edit from becoming a
way to reroll hidden state by changing which earlier operation consumed the
stream.

A unit is indivisible. Playing a card with no exposure remains on the editable
side of the frontier. If that play also draws, searches, reveals or randomly
selects, the complete unit advances the frontier and cannot be undone.

These internal signals are not public semantic events. Public events explain
board changes and pass through visibility filtering. Frontier signals protect
server history and may name information that must never reach a client. Rules
primitives record a bounded signal without concealed card identities when an
operation such as a no-result search or a transient deck-to-discard-to-hand
sequence cannot be recovered safely from the final board and public event
stream alone.

## Undo and redo

An undo request names the current revision and the desired earlier cursor. The
server accepts it only when:

- the requester has the configured table-edit authority;
- the cursor is on a unit boundary;
- it is not before the edit frontier; and
- complete replay of the prefix succeeds.

The first product policy lets a seat edit only units it initiated and whose
dependent decisions were all submitted by that same seat. A range containing a
unit initiated or answered by another seat requires a later explicit table-wide
authorization policy. This is a product permission, not a tabletop rule.

Redo uses the same checks against the retained suffix. If code or data changed
so a recorded unit no longer resolves legally, redo fails closed and reports a
bounded divergence without changing the session.

An undo or redo response is a history replacement, not a gameplay transition.
The client replaces its snapshot and rebuilt active chronology. It does not
animate guessed inverse events.

## Legal action reordering

Reordering accepts a permutation of contiguous `turn_action` units after the
edit frontier. The first product policy requires every selected root unit to
have the same initiating seat, every dependent decision in those units to have
been submitted by that seat, and every unit to belong to the same active-player
turn.

The server:

1. Verifies the range and permutation without reading hidden payloads from the
   client.
2. Replays the unchanged prefix before the range.
3. Replays each complete unit in the proposed order.
4. Replays the unchanged suffix after the range.
5. Commits only if every prompt, acting seat, selector, target, cost, limit,
   timing window and dependent answer remains legal.

The candidate may produce different public results inside the edited range.
That is the purpose of the feature. For example, a legal trace can move an
attack upgrade before an attack and leave an excess-damage healing Action after
it. Fresh replay determines the new attack, excess damage and healing. The
server does not patch those values.

The whole rewrite fails without mutation when any unit crosses the frontier,
an affordance disappears, a target or payment becomes illegal, another seat's
unit enters the range, or a dependent prompt changes incompatibly. Error detail
must not reveal concealed candidate state.

## Logs and telemetry are observers

Structured operational logs consume bounded session outcomes such as:

- request operation and correlation id;
- game label or a bounded non-secret derivative;
- revision and authorized seat;
- accepted, rejected, stale or uncertain disposition;
- save commit and replay result; and
- undo, redo or reorder refusal category.

They never record capabilities, invitations, hand contents, concealed card ids,
payment cards, save bodies or unbounded exception text. Wall-clock timestamps
and durations are allowed in operational records because they never enter the
ledger, RNG, digest, prompt, event stream or client game state.

Metrics and traces use the same redacted signals. Remote export is opt-in and a
no-op exporter is the default. Export failure cannot fail, delay, retry or
reorder a gameplay command.

The ledger may emit a deterministic commit sequence number for correlation.
The logger may add process, host and time fields outside that record. Neither
direction is allowed to feed observation data back into replay.

## Load and recovery

Server startup discovers saves only under its configured data root. For each
selected save it:

1. Parses the strict envelope and compatibility block.
2. Excludes a retired session from publication and completes any pending
   lifecycle recovery.
3. Deals from the recorded setup.
4. Verifies initial events, cumulative RNG word count and digest.
5. Replays and verifies every unit, including the inactive redo suffix.
6. Replays through the cursor to reconstruct the active game.
7. Verifies the saved frontier, revision and active state, including a terminal
   result when present, and requires the reconstructed pending prompt to equal
   `current_prompt` exactly.
8. Publishes the session and its recovered authorities only after complete
   success.

A corrupt, unsupported or divergent save is quarantined from play and reported
with a bounded diagnostic. The server does not skip a bad decision, discard a
redo suffix, substitute current defaults or publish the last plausible board.

Backup copies canonical saves and protected authentication metadata through
their respective interfaces. A client cache is not a backup.

## Verification

The subsystem requires executable examples for:

- byte-stable schema 2 records and strict parsing;
- atomic open with its initial save and owner authority, including crashes at
  each persistence boundary;
- durable attach, credential revocation and owner retirement, including restart
  after every close boundary and safe display-label reuse;
- complete replay of solo and multiplayer saves;
- rejection of a zero-unit save whose initial RNG count diverges;
- rejection of a zero-unit save whose initial pending prompt diverges;
- first-divergence prompt, event, RNG and digest checks;
- rejection of a terminal replay whose outcome or terminal round diverges;
- atomic commit failure before live-session replacement;
- restart from the last complete save after interrupted replacement;
- restart in the middle of an open action and restoration of its exact pending
  prompt, including rejection when only that post-decision prompt diverges;
- a reversible card play and an irreversible draw;
- search, reveal, shuffle and random-selection frontiers;
- redo and future truncation after a new decision;
- a legal 3-action reorder with changed damage and healing;
- rejection of illegal targets, payments, once-per-turn reuse and changed
  dependent prompts after reorder;
- direct off-turn printed Actions submitted by their controlling seat;
- rejection of off-turn ordinary play and identity or ally basic powers;
- rejection of single-seat undo, redo or reorder when an affected unit contains
  a dependent decision submitted by another seat;
- simultaneous seat commands at one revision accepting exactly one;
- structured-log and telemetry redaction; and
- no gameplay difference when every observer is disabled or failing.

The final native journey uses 2 clients and one persistent server. It plays,
saves, disconnects, restarts, restores, reconnects, edits reversible history
and reaches an engine-reported terminal result.
