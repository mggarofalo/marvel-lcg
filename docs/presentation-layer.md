# Presentation layer

The presentation architecture has 5 layers:

1. `Marvel.View` projects engine truth into visibility-safe descriptors and
   transport-neutral board and event presentations.
2. `Marvel.Decisions` composes prompt-bound answers and assesses their legality
   through engine-owned prompt functions.
3. `Marvel.Server` hosts games behind one transport-neutral protocol.
4. `Marvel.Client` owns transport selection, response validation and reusable
   client progress state.
5. `Marvel.Godot` implements the desktop controls on macOS and Windows.

Godot is an implementation concern, not the whole presentation layer. Reusable
presentation code must not reference Godot assemblies. Decision composition
straddles the boundary deliberately: a client needs to assess a draft before it
can enable submission, while the engine remains authoritative and validates the
submitted answer again.

The boundary is checked in both directions. The Godot wall prevents every
shared project from resolving Godot assemblies. `PresentationBoundaryTests`
inspect the compiled Godot assembly and reject authoritative engine assemblies
or rule types outside the prompt, event and outcome contracts it renders.

The Godot project reads the authored Core Set setup surface through
`IEngineTransport`. Its Start flow opens one or 2 ordered hero seats under an
explicit game label. Its Join flow attaches once with a masked seat invitation.
The embedded `InProcessTransport` remains the default. An explicit
`tcp://host:port` endpoint selects `SocketTransport` at the same composition
boundary. Each complete response replaces the prior visibility-safe snapshot,
while its semantic events are appended in engine order to a diagnostic
chronology. The client renders the board, waiting state and every admitted
prompt shape from those contracts. Tests run a complete deterministic journey
through both compositions and compare every authoritative response.

The planned deterministic session layer sits behind both transports. It makes
the server-owned setup and accepted decision trace authoritative for save,
restore, undo, redo and legal action reordering. Clients continue to submit
decisions rather than state. See [session-ledger.md](session-ledger.md).

## Build boundary

Godot is the only UI framework planned for the desktop client. No engine, card,
content, view, decision or server project may reference Godot assemblies.

`Directory.Build.targets` enforces that rule. The projects under
`tests/godot-wall/` intentionally violate individual constraints, and
`tools/godot-wall.sh` proves each violation fails the build.

The complete solution targets .NET 8. One target framework keeps runtime
behavior, JSON serialization and digest tests on the same floor the client will
host. Adding a project or changing a target framework requires updating the wall
tests as well as the solution.

## Engine result

One engine decision returns:

- the next prompt, when input is required;
- semantic events describing what changed;
- a visibility-filtered world descriptor; and
- terminal game state when the game has ended.

The internal `World` and `World.Digest()` never cross this boundary. The digest
contains hidden truth by design and is unsuitable as a client bootstrap format.

## Affordances

A prompt contains affordances rather than display strings. Each affordance has a
stable id, domain label, board anchor, target requests, cost options and legality
context.

The client renders this data. It does not rediscover legal moves from card text
or duplicate rules logic. See [affordances.md](affordances.md).

## Semantic events

Events describe game meaning such as creating, moving, flipping or changing a
card. They are not animation commands. A client may animate them, log them or
apply accessibility behavior without changing the engine contract.

The current descriptor remains authoritative after every decision. Events explain
the transition; they do not replace the snapshot. See
[event-stream.md](event-stream.md).

## Visible world

`Marvel.View.WorldProjection` converts a `World` into a normalized
`WorldDescriptor`. The projection walks the runtime area graph rather than a
hard-coded list of zones, so areas created during play enter the descriptor
automatically.

Readable cards include printed identity and live public fields. Concealed cards
retain only what a viewer may know, such as pile size and an appropriate card
back. Hidden object ids and mutable fields are normalized so a client cannot
track a card through a shuffle.

Prompts and events pass through the same visibility scope as the snapshot. A
hidden event cannot restore a face or object id removed from the descriptor.

## Visibility policies

The server supports 2 explicit policies:

- `cooperative` shows every player's cooperative private area, regardless of a
  client viewer claim; and
- `restricted` binds private information to a server-authorized seat.

A client claim is validated input, never authority or a hide-cards setting. The
server policy decides the scope and binds it to the session capability.

Restricted multiplayer uses one-time seat invitations. Attaching consumes an
invitation and returns a new capability bound to that seat. Another seat cannot
answer the pending prompt, even if it can guess the game label or request body.

Face-up cards in public areas remain public. Under restricted policy, cards in
a player hand remain private to that seat even if an engine effect left their
physical `FaceUp` field set. Cooperative policy authorizes every player hand by
default. Player decks, encounter decks and other concealed piles still hide
their identity and order. Authorized searches expose only their current target
set.

These are product and wire choices. The tabletop rules do not define remote
viewers or network authorization.

## Engine host

`EngineHost` owns live game sessions. `IEngineTransport.ExchangeAsync` is the
one interface a client uses in either deployment:

- `InProcessTransport` calls the host in the same process; and
- `SocketTransport` sends the same request to `SocketEngineServer`.

The in-process path does not create a second gameplay API. The socket path may
use asynchronous network input, but game-state mutation remains synchronous and
single-threaded inside the host.

The current socket framing and bearer capabilities are plaintext. A remote
endpoint is therefore for development and trusted private networks only; it is
not an Internet-safe deployment boundary.

The [standalone server guide](server.md) documents process and container launch,
visibility configuration, durable sessions and shutdown behavior.

## Wire protocol

The socket protocol uses source-generated JSON inside a 4-byte big-endian length
frame. Frames are bounded. Unknown operations, unsupported protocol versions and
unknown JSON members fail before they reach the engine. Protocol 9 adds legal
trace rewriting for a permutation of contiguous committed action-unit positions.
The client sends no decisions or derived state in that command. Protocol 8 added
replay-verified undo and redo commands that name an expected revision and a
retained history cursor. Each authorized game response carries only the cursor
boundaries that capability may currently request; it does not expose journal
decisions, information signals, or state digests. Protocol 7 added the host revision that binds a
decision to the prompt it answers. Protocol 6 added
the printed and live face facts used by procedural cards. Protocol 5 added per-target
maximum occurrences to repeated target allocations, allowing clients to render
indirect-damage capacities without deriving remaining hit points.

The protocol supports discovering setup choices, opening, attaching,
synchronizing, resolving and closing a game. Setup discovery is a read-only,
session-free query over the same transport as play. Its scenario records carry
their authored keys and mode, and its modular-set list is classified by the
same content rule that validates an open request. The client groups and renders
those records; it does not infer valid products from keys or card text.

Game ids and correlation ids are bounded client labels. A random session
capability authorizes game operations; it never enters gameplay state or the
seeded RNG stream.

The client keeps its game id and session capability together in memory. It
removes capabilities and invitations from responses before those responses
reach rendering code. A host may copy one returned invitation once; an attaching
client masks and clears the invitation field before sending the request.

Once a complete mutation request has been sent, cancelling the response read
cannot imply that the mutation was rolled back. The client must consume the
authoritative response unless a future protocol adds idempotent retries.

The socket transport reports whether request transmission began. Only a failure
before writing begins is safe to retry. Once writing begins the result is
uncertain, even if the client did not observe the write completing, so the
client never repeats the decision. It issues one read-only
`sync` request instead. A sync response replaces the current descriptor and
prompt, carries an empty event boundary and never replays the event chronology.
An invalid, expired or closed capability returns the client to connection setup.
Every successful game response carries the current host revision. Resolve
echoes that value, and a mismatch returns `stale_decision` before the engine can
apply the answer.

The response types are stable wire records. Adding a new affordance, event or
descriptor variant requires a protocol-version decision because older clients
cannot infer an unknown union member.

## Server safety

The standalone process exposes only game protocol operations. It has no arbitrary
file read, dynamic card code, process execution or cheat-console endpoint.

Dataset loading happens at the host boundary. Engine and card projects accept
already-opened content and never gain network or filesystem authority.

## Card rendering

The planned client will draw card frames and live values procedurally. The
illustration remains an image asset inside the art box.

Procedural rendering lets the client show current cost, stats, traits, keywords
and rules text from the visibility-safe descriptor. It avoids treating a scan of
printed state as the current state and then layering separate rules badges over
it.

`Marvel.View` does not contain card layout or art-pack loading. Those are client
tasks, not engine or DSL fields. The descriptor supplies the already-authorized
visible face id; presentation hints must not enter
`datasets/abilities/abilities.json`.

## Client delivery

The Godot project remains deliberately above the engine wall. It:

- depends on transport and descriptor contracts rather than `Game`;
- renders areas, cards, prompts and events;
- supports local in-process play first;
- can switch to the socket transport without changing client behavior; and
- keeps game rules and legality in the engine.

The editor launch and native complete-game smoke are documented in
[godot-client.md](godot-client.md).

`Marvel.Sim` remains the non-Godot driver. It proves the engine is playable and
diagnosable without opening the graphical client.
