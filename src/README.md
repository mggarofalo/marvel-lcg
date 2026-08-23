# src/ — C# engine

Empty until the Engine Core phase begins. See [../docs/migration.md](../docs/migration.md) for the target architecture and [../docs/plane.md](../docs/plane.md) for how work is tracked.

Planned layout:

```
Directory.Build.props / Directory.Packages.props   central package management
Marvel.slnx

src/Marvel.Engine        core rules; no I/O, no RNG state
src/Marvel.Cards         card DSL and card data
src/Marvel.Server        ASP.NET Core; serves the web client

tests/Marvel.Engine.Tests    xUnit
tests/Marvel.Specs           Reqnroll behavioral specs
```

**The layout above is under review.** [presentation-layer.md](../docs/presentation-layer.md)
(MARVEL-159) proposes replacing `Marvel.Server` and the TypeScript client with a
Godot client, splitting `Marvel.Engine` into `Marvel.Core` / `Marvel.Rules` /
`Marvel.Cards.*`, and adding an engine-agnostic `Marvel.View` above a build-enforced
wall. Read that before creating any project here.

The Python reference engine lives in [`../py_src/`](../py_src/) and is the behavioral source of truth this code is validated against.
