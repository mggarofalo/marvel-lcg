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
src/Marvel.Sim           non-Godot headless driver and replay harness
src/Marvel.View          engine-agnostic visible-state projection
src/Marvel.Server        engine host; embedded or a standalone socket process

tests/*.Tests            behavioral xUnit suites for the corresponding project
```

The Godot client and procedural card renderer remain planned:

```
Directory.Build.props / Directory.Packages.props   central package management
Marvel.slnx

src/Marvel.Godot         macOS and Windows client; the only Godot reference
```

`Marvel.Server` is one assembly with two entry points. A bundled client uses
`InProcessTransport`; a hosted client uses `SocketTransport` against the same
`EngineHost` running from the executable or its Linux Dockerfile. Client code
depends only on `IEngineTransport`, never directly on `Game`.
