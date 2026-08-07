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

The Python reference engine lives in [`../py_src/`](../py_src/) and is the behavioral source of truth this code is validated against.
