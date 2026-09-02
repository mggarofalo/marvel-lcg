# src/ — C# engine

The engine and its hosts are implemented as plain .NET 8 projects. See
[migration.md](../docs/migration.md) for the architecture and
[presentation-layer.md](../docs/presentation-layer.md) for the build-enforced
Godot wall and server topology.

Current projects:

```
Directory.Build.props / Directory.Packages.props   central package management
Marvel.slnx / global.json

src/Marvel.Core          seeded MT19937 and canonical digest primitives
src/Marvel.Rules         state, phases, timing, prompts, events, and the fold
src/Marvel.Cards         authored ability DSL and interpreter
src/Marvel.Content       printed cards and scenario setup readers
src/Marvel.Session       deterministic save records and verified replay
src/Marvel.Sim           non-Godot headless driver and replay harness
src/Marvel.View          engine-agnostic visible-state projection
src/Marvel.Server        engine host; embedded or a standalone socket process
src/Marvel.Godot         macOS and Windows client; the only Godot reference

tests/*.Tests            behavioral xUnit suites for the corresponding project
```

The Godot project opens a local Core game, renders its visibility-safe board,
composes every admitted prompt shape and reconciles each authoritative response.
See [godot-client.md](../docs/godot-client.md) for the verified editor launch and
complete native local-game smoke.

`Marvel.Server` is one assembly with two entry points. A bundled client uses
`InProcessTransport`; a hosted client uses `SocketTransport` against the same
`EngineHost` running from the executable or its Linux Dockerfile. Client code
depends only on `IEngineTransport`, never directly on `Game`.
