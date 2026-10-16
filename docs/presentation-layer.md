# Presentation layer

Ownership follows the decision each assembly makes:

| Assembly | Responsibility |
|---|---|
| `Marvel.View` | Visibility-safe descriptions of authorized engine results. |
| `Marvel.Decisions` | Prompt-bound drafts assessed by engine-owned legality. |
| `Marvel.Protocol` | Versioned requests, responses and their strict serialization. |
| `Marvel.Transport` | Embedded value boundaries and bounded socket framing. |
| `Marvel.Observability` | Privacy-bounded operational records and sinks. |
| `Marvel.Server` | Authoritative session lifecycle, persistence, replay and scope. |
| `Marvel.Server.Host` | Standalone process configuration and lifetime. |
| `Marvel.Client` | Explicit session identities, response validation and recovery. |
| `Marvel.Desktop` | Embedded/remote desktop composition and owned host lifetime. |
| `Marvel.Godot` | Native controls, gestures, layout and animation. |

A remote client has no dependency on the server, content loader or session replay.
The desktop composition root explicitly owns its embedded host. Protocol, transport
and observability cannot resolve server, content, session or product assemblies;
negative build probes exercise that boundary.

Godot is an implementation concern, not the whole presentation layer. Reusable
presentation code must not reference Godot assemblies. Decision composition
straddles the boundary deliberately: a client needs to assess a draft before it
can enable submission, while the engine remains authoritative and validates the
submitted answer again.

Two separate policies protect this boundary. The Godot wall prevents every
shared project from resolving Godot assemblies. The presentation wall limits
the project references available to each presentation assembly and disables
transitive project references there. Compiled-assembly tests then reject
unreviewed Marvel assemblies and types that enter emitted code. A semantic
source test reviews every engine member used by `Marvel.View`, including
constants that do not survive compilation and methods on its live `World`.

These checks prevent a presentation project from reaching an authoritative
type through a direct, transitive, package or file reference. No dependency
check can recognize a duplicated rule written with only strings and integers.
The ownership rules below and adversarial review cover that semantic mistake.

## Choose a project

Place code according to the question it answers:

| Question | Project |
|---|---|
| What may happen, and how does it change the game? | `Marvel.Rules` or another engine project. |
| Does this prompt accept the answer currently being drafted? | An engine-owned function called by `Marvel.Decisions`. |
| How should an authorized result be described without exposing hidden state? | `Marvel.View`. |
| Which request is sent, and how does the client recover from failure? | `Marvel.Client`. |
| Which control, gesture, animation or style presents that result? | `Marvel.Godot`. |

For example, Godot may disable Submit from `DecisionComposer.Progress`. It may
not count targets, inspect resources or parse card text to reach its own answer.
`Marvel.Decisions` may call `TargetRequest.Allows` because that function remains
engine-owned. The server validates the submitted answer again before mutation.

Draft editing uses that same authority. `TargetRequest.AllowsPartial` assesses
an unfinished selection, including combination exclusions, allocation capacities
and prefixes of complete grouped answers. `ResourceCost.Declarations` supplies
the resource types an icon may declare for that cost, including printed-resource
restrictions. Decisions exposes these assessments to controls; renderers do not
maintain parallel count or wildcard rules.

`ClientGameLifecycle` owns admission of setup, entry and session requests, one
in-flight operation, and generation checks that discard superseded completions.
It accepts the complete validated response, revision and history together from
`LocalGameClient`, and retains operational progress and recovery locks across
surface changes. Its result instructs the sole prompt-bound draft to preserve,
retry, replace or clear. Godot applies that instruction and renders the supplied
progress; it does not maintain another request or recovery state machine.
Godot's previous-response cache serves only presentation comparison and is
invalidated with deferred rendering and animation when the session changes.
Session capabilities remain outside renderable progress.

The Godot project reads the authored Core Set setup surface through
`IEngineTransport`. Its Start flow opens one or 2 ordered hero seats under an
explicit game label. Its Join flow attaches once with a masked seat invitation.
The embedded `QueuedInProcessTransport` remains the desktop default. An explicit
`tcp://host:port` endpoint selects `SocketTransport` at the same composition
boundary. Each complete response replaces the prior visibility-safe snapshot,
while its semantic events are appended in engine order to a diagnostic
chronology. The client renders the board, waiting state and every admitted
prompt shape from those contracts. Tests run a complete deterministic journey
through both compositions and compare every authoritative response.

The deterministic session layer sits behind both transports. It makes
the server-owned setup and accepted decision trace authoritative for save,
restore, undo, redo and legal action reordering. Clients continue to submit
decisions rather than state. See [session-ledger.md](session-ledger.md).

Protocol 32 uses the engine's `Rules.Play.Decision` as the one submitted answer type.
`DecisionAssessment` owns complete target, variable and resource legality for both
composition and acceptance. The protocol codec emits only its five answer fields.

Each open or attach retains an `EntryClaim` before transmission. An uncertain
reply returns `ClientEntryAttempt`; retry sends the exact same claim and intent.
The host atomically persists an encrypted receipt with the authority. The client
secret decrypts that receipt after a host restart, and a changed intent is rejected.
Recovery returns the current scoped view and only still-unused invitations. Revoked
and retired claims remain unusable. Godot offers Recover table or Recover seat;
it never manufactures a second entry when recovering an uncertain result.

Failure disposition is typed independently of diagnostic codes. A rejected candidate
preserves the prior authoritative session; a lost response requires synchronization;
only explicit unavailable authority removes the capability. Committed history
inspection is cached with its hosted session and projected separately for each scope.
Network framing never holds a game gate. Session requests serialize per game, so an
incomplete peer or another game's durable commit cannot stall every table.

Protocol 25 adds completed deck-shuffle receipts containing only destination
seat, count and previously public titles. No returned card's hidden identity or
position crosses the wire. Replay v9 records this additional semantic event;
the RNG and state-digest contracts remain unchanged.

Protocol 24 adds the passive `SpecialAbilityNext` and `VisibleCardSelection`
purposes. Visible target requests carry `exclusive_sets`: engine-authored sets
of card ids from which at most one distinct card may be selected. The composer
asks the engine assessment about combinations; card galleries do not compare
printed titles. Complete `groups` remain exact answers, never candidate pools.
Minion and Special sequences offer only the next participant and
re-read current eligibility after its complete resolution. The existing
`MinionActivationOrder` purpose and `Question.Order` family retain their names
and numeric values; each offered card now commits only its next activation.
A sole remaining participant advances in the engine without a redundant choice.
Required targets, payments and optional timing opportunities remain separate
prompts. An unresolved Special with no possible effect still occupies its
sequence step; it does not move the final-step bonus to an earlier upgrade. Protocol 23 endpoints
are rejected before gameplay. Replay contract v8 rejects prior traces because
the choices and continuation boundaries differ; the save schema, RNG and state
digest formats are unchanged.

Protocol 23 distinguishes printed stat annotations from effective values and
their authorized explanations. It retains protocol 21's authorized card search
and look choices and protocol 22's latest-action undo status. Internal
concealed-candidate exposure metadata stays out of the wire contract. These
presentation facts do not change the saved prompt record or replay identity.
Quantity-dependent typed payments retain
the protocol 20 resource contract. The engine resolves the
required resource slots from the offered component and chosen variable; the
composer uses that same function for readiness and icon declarations. Saves use schema 6 and replay contract v17. Other schema and replay identities
are rejected; there is no migration adapter.

Readable faces carry `printed_values` separately from `effective_values`.
Printed values preserve the numeral or symbol, the special-rule star, the
per-player mark and the consequential-damage count as independent facts. Zero,
X, dash and a standalone star remain distinct; a standalone star is drawn once.
Effective quantities come from the engine's evaluation. HP means maximum HP;
remaining health and damage retain their existing state contracts. A renderer
must not add attachment values or reconstruct effective quantities itself.

Effective-value source rows contain only explanations authorized for the viewer.
They are not a complete arithmetic equation: private contributions are omitted
without placeholders, counts or cumulative totals. Historical sources have no
live card link, even if the same physical card has since returned to play.
Client admission rejects incomplete nested facts before replacing usable state.
Both transports preserve these facts with the same visibility boundary.

The generated printed annotations change the card dataset fingerprint. Existing
sessions pinned to another dataset identity remain incompatible even though the
save schema, replay contract and state-digest format are unchanged.
The typed base definition for Titania also changes the ability-book fingerprint;
the existing ability compatibility check rejects saves pinned to the other book.

Live persistent sources also carry explicit relationships, already-applied
contributions and complete supported ability descriptions. These descriptions
are not executable offers. Source and acting-identity costs remain distinct;
unsupported abilities retain inspection access rather than a partial guessed
summary. The client validates nested collections and required fields while
retaining signed numeric values supplied by the engine.

## Build boundary

Godot is the only UI framework planned for the desktop client. No engine, card,
content, view, decision, client or server project may reference Godot assemblies.

`Directory.Build.targets` enforces that rule. The projects under
`tests/godot-wall/` intentionally violate individual constraints, and
`tools/godot-wall.ps1` proves each violation fails the build.

The same targets give each presentation project an exact project-reference
allowlist. They also disable transitive project references, so `Marvel.Client`
cannot compile against `Marvel.Core`, `Marvel.Cards`, `Marvel.Content` or
`Marvel.Session` merely because the desktop composition uses them. The projects under
`tests/presentation-wall/` prove that every role rejects a forbidden reference.
`PresentationAssemblyPolicyTests` check the assemblies that the compiler emits,
including references introduced through a package or a DLL.
`PresentationSourcePolicyTests` make a new View call to an engine mutator or
constant an explicit review decision rather than an invisible dependency.

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

Terminal results distinguish final villain defeat, final main-scheme completion,
and player loss. `WorldDescriptor.LossReason` distinguishes elimination of all
players from simultaneous exhaustion of the encounter deck and discard pile.
The rule owner supplies that public reason; View describes it without inferring
the ending from the visible board. Replay derives it along with the outcome.

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

The table's passive pending situation names the primary answering seat and a
typed public decision purpose. Its causal references are limited to already
readable public, faceup cards. Private questions, options, costs, searched cards
and candidate membership remain inside the separately authorized prompt. A seat
with off-turn Actions may have its own prompt while another seat remains the
primary turn owner. Public waiting metadata never supplies an answer contract.

View names an offered source instance and describes its current authorized HP,
readiness and counters. Mandatory nonresource costs come from the engine's
typed cost description. These facts let the player assess an offer; View and
Godot do not forecast defeat, reconstruct costs or infer legal choices from them.

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

`EngineHost` is the synchronous protocol dispatcher for live game sessions.
`SessionAuthorityRegistry` owns capabilities, invitations and lifecycle
publication; `SessionTransaction` owns durable candidate publication;
`AuthorizedSessionProjector` owns visibility-safe response and history shaping;
and `RequestExecution` owns request-local operational outcomes. Dataset loading
and the public host, transport and factory contracts live independently of that
application composition.

`IEngineTransport.ExchangeAsync` is the one interface a client uses in either
deployment:

- `InProcessTransport` synchronously calls the host in the same process for headless callers;
- `QueuedInProcessTransport` gives the desktop's embedded host one dedicated FIFO
  owner, returning control to the UI while each synchronous request runs; and
- `SocketTransport` sends the same request to `SocketEngineServer`.

The in-process path does not create a second gameplay API. The socket path may
use asynchronous network input, but game-state mutation remains synchronous and
single-threaded inside the host.

Embedded transports detach requests and responses through the same
source-generated JSON contract as socket messages. A queued request is captured
before admission; a response is captured on the host owner before publication.
Nested collections therefore cannot carry client mutations into an authoritative
prompt or change an accepted request while it waits. Snapshot failures before
dispatch report that the request did not commit; failures after dispatch preserve
uncertainty. This is transport isolation and does not change the wire version.

The queued transport exclusively owns endpoint exchange. Cancellation while a
request is queued proves it was not sent; after dispatch begins, cancellation
does not interrupt replay, persistence or consumption of the authoritative
response. An endpoint exception preserves uncertainty and permits read-only
synchronization on the same owner. Shutdown stops new admission and drains all
accepted requests before the owner exits. Client detach requests shutdown without
waiting on the UI thread; temporary setup connections release their owners too.
Normal window close asynchronously drains every application-owned embedded host
before quitting Godot, including setup and detached requests still finishing.
No scheduling choice enters the seed, digest, ledger or protocol version.

The current socket framing and bearer capabilities are plaintext. A remote
endpoint is therefore for development and trusted private networks only; it is
not an Internet-safe deployment boundary.

The [standalone server guide](server.md) documents process and container launch,
visibility configuration, durable sessions and shutdown behavior.

## Wire protocol

The socket protocol uses source-generated JSON inside a 4-byte big-endian length
frame. Frames are bounded. Unknown operations, unsupported protocol versions and
unknown JSON members fail before they reach the engine. Both endpoints must use
protocol 32. Setup and decisions replay under engine-replay-v18, with session
schema 6 retaining the exact ordered decks. Requests carry engine-authored
commitments and explicit payment allocations; authorized responses include
readable choices, active attack participants and attack completion events.
Neither hidden state nor interpreter continuation payloads cross this boundary.

Protocol 29 adds exact optional-window choices,
forced-ability receipts and authorized event-face references; it requires replay v14.
Protocol 28 adds explicit activity operation receipts,
batch membership, round numbers and phase boundaries; it requires replay v13.
The engine can also offer an early exact recipient for a single public event
choice, revalidating that recipient after payment and preserving a genuine
choice if it is no longer legal. Protocol 19 kept replay and digest formats
unchanged. Protocol 13 adds
display-only card markup and a separate art-safe face id so full card frames
can preserve printed emphasis and symbols without requesting concealed art.
Protocol 12 adds
visibility-safe completed-action summaries at the cursor immediately before each
action, their visibility-safe discard-result details, and whether an action is
still open, so a client can offer an authoritative history target without
receiving journal decisions or information-frontier signals. Protocol 11 adds the
product, replay, save-schema and dataset identities to the
setup discovery response so a client can diagnose compatibility before opening a
game. Protocol 10 added declaration-sensitive wild-resource costs.
Protocol 9 added legal trace rewriting for a permutation of contiguous committed
action-unit positions. The client sends no decisions or derived state in that
command. Protocol 8 added
replay-verified undo and redo commands that name an expected revision and a
retained history cursor. Each authorized game response carries only the cursor
boundaries that capability may currently request and the scoped summaries needed
to name them; it does not expose journal decisions, concealed card identities,
information signals, or state digests. Protocol 7 added the host revision that binds a
decision to the prompt it answers. Protocol 6 added
the printed and live face facts used by procedural cards. Protocol 5 added per-target
maximum occurrences to repeated target allocations, allowing clients to render
indirect-damage capacities without deriving remaining hit points.

Protocol 16 adds engine-authored preferred resource declarations when the
typed ability can prove that one declared type improves a numeric result. The
hint pre-fills a reversible draft; it does not weaken authoritative payment
validation or choose among several beneficial outcomes.

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

Once a complete gameplay mutation request has been sent, cancelling the response
read cannot imply rollback. The client synchronizes the current state without
resubmitting that decision. Open and attach instead retry their retained entry
claim, which durably identifies the authority already issued. Each socket exchange
uses a 30-second deadline, including recovery synchronization. Caller cancellation
stops a response read promptly while preserving the same uncertainty and recovery
contract.

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

Table snapshots carry explicit seat roles (prompt owner, viewed private seat,
active player, first player, and public focus), compact public summaries, and a
small visibility-reviewed relationship vocabulary. A renderer uses those fields
for attachment, engagement, offered target, generator, and result connections;
it never extracts an attacker, payment anchor, focus, or threshold from readable
copy or board counters. The fields were added with protocol version 15.

## Server safety

The standalone process exposes only game protocol operations. It has no arbitrary
file read, dynamic card code, process execution or cheat-console endpoint.

Dataset loading happens at the host boundary. Engine and card projects accept
already-opened content and never gain network or filesystem authority.

`EngineAssemblyPolicyTests` checks the compiled dependency direction of Core,
Rules, Cards, Content and Session, and rejects ambient filesystem, network,
clock, randomness and threading capabilities there. Supplied streams, in-memory
parsing and deterministic hashing remain allowed. These checks protect a hosting
boundary; they do not replace review of iteration order or gameplay semantics.

Parsed card facts and setup recipes are immutable snapshots shared across games.
Duplicate identities within a catalog namespace fail ingestion rather than
selecting whichever definition appeared last. Content admission is an engine
choice, independent of where a host obtains the dataset bytes.

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

Printed card annotations retain their engine-owned meaning: an ally's power
stars indicate consequential damage, while per-player marks remain separate
from already evaluated live values. Board presentations describe these marks
after visibility filtering. These facts use the wire's existing
printed-attribute dictionary, including uniqueness and scheme icons;
no new wire member or gameplay decision is introduced. Older renderers may
ignore these additional dictionary entries. Card illustrations appear only in
enlarged inspection; table faces reserve their space for readable rules and
current values.

The physical table keeps card faces between 80% and 100%, with the upper bound
also constrained by available table height. This preserves legible values and
keeps the fanned hand clear of the decision area. Interface controls retain
their independent 50–150% scale. Hover previews show a complete enlarged face
without capturing table pointer input; explicit inspection pins the face and
owns focus until dismissal.

## Client delivery

The Godot project remains deliberately outside the engine. It:

- depends on transport and descriptor contracts rather than `Game`;
- renders areas, cards, prompts and events;
- supports local in-process play first;
- can switch to the socket transport without changing client behavior; and
- keeps game rules and legality in the engine.

The editor launch and native complete-game smoke are documented in
[godot-client.md](godot-client.md).

`Marvel.Sim` remains the non-Godot driver. It proves the engine is playable and
diagnosable without opening the graphical client.

Protocol 22 adds `history.undo_status`, a bounded latest-action edit status:
`0` unavailable, `1` available, `2` no history, `3` action in progress,
`4` protected history, or `5` another player's authority. Protected history does not name the private draw,
search, reveal or randomness signal, its objects, or any concealed result.
Another player's operation is classified by authority before protection.
Replay and save identities remain unchanged.

Protocol 32 carries engine-authored attack identities and lifetime boundaries,
exact scheme/thwart occurrence causes, resource-ability costs and player-loss
reasons. Attack identities distinguish enemy activation ids from character
occurrence ids. Replay v18 owns their nested completion order; its compatibility
contract is defined in [release-policy.md](release-policy.md). The RNG,
card-state digest and save envelope retain their existing formats. Supporting
travel, reveal and installation receipts may share one presentation beat; every
raw event remains available in diagnostics.
