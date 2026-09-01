# Presentation layer

The presentation architecture has 3 layers:

1. `Marvel.View` projects engine truth into visibility-safe descriptors.
2. `Marvel.Server` hosts games behind one transport-neutral protocol.
3. The Godot client renders those descriptors on macOS and Windows.

The Godot project reads the authored Core Set setup surface through
`IEngineTransport`, lets the player select an offered assignment, and opens it
through `InProcessTransport`. Each complete response replaces the prior
visibility-safe snapshot, while its semantic events are appended in engine order
to a diagnostic chronology. The client renders the board and every admitted
prompt shape from those contracts. The same app-facing setup and open path is
exercised over the socket transport in tests.

## Build boundary

Godot is the only UI framework planned for the client. No engine, card, content,
view or server project may reference Godot assemblies.

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

- `cooperative` allows the configured seat, hot-seat or watcher view expected by
  a cooperative game; and
- `restricted` binds private information to a server-authorized seat.

A client claim is input to the policy, not authority. The server decides the
scope and binds it to the session capability.

Restricted multiplayer uses one-time seat invitations. Attaching consumes an
invitation and returns a new capability bound to that seat. Another seat cannot
answer the pending prompt, even if it can guess the game label or request body.

Face-up cards in public areas remain public. Cards in a player hand remain
private to that seat even if an engine effect left their physical `FaceUp` field
set. Authorized searches expose only their current target set.

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

## Wire protocol

The socket protocol uses source-generated JSON inside a 4-byte big-endian length
frame. Frames are bounded. Unknown operations, unsupported protocol versions and
unknown JSON members fail before they reach the engine. Protocol 5 adds
per-target maximum occurrences to repeated target allocations, allowing clients
to render indirect-damage capacities without deriving remaining hit points.

The protocol supports discovering setup choices, opening, attaching,
synchronizing, resolving and closing a game. Setup discovery is a read-only,
session-free query over the same transport as play. Its scenario records carry
their authored keys and mode, and its modular-set list is classified by the
same content rule that validates an open request. The client groups and renders
those records; it does not infer valid products from keys or card text.

Game ids and correlation ids are bounded client labels. A random session
capability authorizes game operations; it never enters gameplay state or the
seeded RNG stream.

Once a complete mutation request has been sent, cancelling the response read
cannot imply that the mutation was rolled back. The client must consume the
authoritative response unless a future protocol adds idempotent retries.

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

`Marvel.View` does not yet contain card layout or art-pack loading. Those are
client tasks, not engine or DSL fields. Presentation hints must not enter
`datasets/abilities/abilities.json`.

## Client delivery

The Godot project remains deliberately above the engine wall. It:

- depends on transport and descriptor contracts rather than `Game`;
- renders areas, cards, prompts and events;
- supports local in-process play first;
- can switch to the socket transport without changing client behavior; and
- keeps game rules and legality in the engine.

`Marvel.Sim` remains the non-Godot driver. It proves the engine is playable and
diagnosable without opening the graphical client.
