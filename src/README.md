# src/ — C# engine

**Contracts first.** `Marvel.Core` holds the cross-engine RNG (MARVEL-8) and
the state-digest serialiser (MARVEL-9/44). The engine resolve, the card DSL and the corpus replay harness
are migration phases 4–6 and are not started. See [../docs/migration.md](../docs/migration.md) for the target architecture and [../docs/plane.md](../docs/plane.md) for how work is tracked.

Current:

```
Directory.Build.props / Directory.Packages.props   central package management
Marvel.slnx / global.json

src/Marvel.Core          the RNG and digest contracts; no I/O, no UI, no state
tests/Marvel.Core.Tests  xUnit; the cross-language vectors are the acceptance
```

`Marvel.Core` is the one project name common to both layouts under review in
MARVEL-159, so what lands there survives that decision. Nothing here depends on
the presentation-layer question, which is why this could start before it was
answered.

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
