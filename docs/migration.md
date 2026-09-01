# C# architecture

The migration from Python is complete. C# is the only engine, and no Python
implementation remains an authority or compatibility target.

This document records the architectural outcomes that still constrain the
repository. [AGENTS.md](../AGENTS.md) describes the working rules, and
[scope.md](scope.md) defines the supported product.

## Why the engine changed

The retired implementation loaded card scripts as arbitrary Python and blocked a
gameplay thread while waiting for player input. Those properties made downloaded
content unsafe, deterministic replay difficult, and rule resolution hard to test.

The C# design removes both:

- card behavior is inert, schema-checked data interpreted by trusted code; and
- player input is a typed continuation returned by the engine, never a blocking
  call inside gameplay.

## Resolve contract

The engine advances one decision at a time:

```text
(state, input) -> (state, prompt, events)
```

The prompt contains anchored affordances. The event list describes the semantic
changes made by that decision. Game-over state is part of the returned result.

Gameplay state is single-threaded and deterministic. Networking and asynchronous
input live above the engine boundary.

This shape gives replay, simulation and clients the same interface. A replay is
the same resolve operation over a recorded input sequence. Undo can rebuild a
prefix. Tests do not need timing coordination with a blocked engine thread.

## Cards are data

`datasets/abilities/abilities.json` contains executable card text. The parser
accepts values and named nodes, never source code, delegates or callbacks.

Only the Core Set has executable rows. See [card-dsl.md](card-dsl.md) for the
language and [scope.md](scope.md) for the product boundary and future-content
validation strategy.

General game rules stay compiled. Card-specific behavior stays in data. A rule
concept that several cards need belongs in the engine and DSL vocabulary. A
one-off card does not justify a general-purpose escape hatch.

## Deterministic state

One MT19937 stream is seeded once for each game. Bounded integers and shuffles
use the algorithms fixed by [rng-contract.md](rng-contract.md). Gameplay never
uses wall-clock time, ambient randomness or a second RNG.

`World.Digest()` serializes every card, area and gameplay field using the wire
format in [state-digest-v2.md](state-digest-v2.md). The digest records hidden
truth for testing and replay comparison. It never crosses the client wire.

Card id allocation, iteration order, JSON spelling and RNG consumption are
deterministic contracts. A change to any of them can change every game produced
from a seed.

## Authority replaces compatibility

The retired engine is not an oracle. Published sources decide behavior:

- Rules Reference v1.8 and audited modifications;
- joined, corrected printed card text;
- official rulings and FAQ entries; and
- authored setup facts taken from product instructions.

Tests cite the narrow rule they enforce. The executable Core behavioral corpus
is derived from those authorities and legal game scenes. Existing behavior is
evidence only when the same published authority supports it.

## Project boundaries

The dependency structure is:

```text
Marvel.Core
└── Marvel.Rules
    ├── Marvel.Cards
    ├── Marvel.Content
    └── Marvel.View

Marvel.Sim    -> Marvel.Cards + Marvel.Content
Marvel.Server -> Marvel.Cards + Marvel.Content + Marvel.View
```

An arrow points from a host to the engine projects it consumes. The enforced
responsibilities are:

| Project | Responsibility |
|---|---|
| `Marvel.Core` | RNG and canonical digest primitives |
| `Marvel.Rules` | State, phases, timing, prompts and semantic events |
| `Marvel.Cards` | Ability data types, validation and interpretation |
| `Marvel.Content` | Printed facts and supported setup readers |
| `Marvel.Sim` | Headless deterministic simulation and replay |
| `Marvel.View` | Visibility-safe descriptors of engine state |
| `Marvel.Server` | In-process and socket engine host |

Core engine projects contain no filesystem, network or UI-framework input. They
parse strings or streams supplied by a host.

## Presentation boundary

The client is a Godot .NET project for macOS and Windows. Its launchable shell
ships in this repository and opens, renders and plays a complete local Core
game. `Marvel.View` and `Marvel.Server` provide the client-safe projection and
transport boundary. The verified editor-run workflow is documented in
[godot-client.md](godot-client.md).

The client must use `IEngineTransport` in both deployments:

- `InProcessTransport` hosts a local game inside the client process; and
- `SocketTransport` speaks to the same `EngineHost` remotely.

The engine never references Godot. Build targets enforce that wall and keep the
solution on the .NET runtime Godot can host. See
[presentation-layer.md](presentation-layer.md).

## Security boundary

The server exposes game operations, not arbitrary host capabilities. It has no
path-bearing file-read operation, dynamic code loader or cheat console.

The server applies a visibility policy before serialization. Clients never
receive `World`, the state digest, hidden card identities or prompts they are not
authorized to answer. Session capabilities authorize mutations independently of
client-chosen game labels.

## Repository outcome

The repository uses one solution and one test tree. The retired Python engine,
web client, cross-engine fixtures and compatibility adapters are gone. Git keeps
their history; current documentation describes only the C# system that remains.
