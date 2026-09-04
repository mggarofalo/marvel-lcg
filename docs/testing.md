# Test lanes

The test suite has two lanes. Both block a merge in CI, but they answer different
questions and run as separate steps.

## Fast merge preparation

Build once, then run the focused unit, contract and behavioral tests:

```bash
dotnet build Marvel.slnx -c Release
dotnet test tests/Marvel.UnitTests.slnx -c Release --no-build
```

`Marvel.Content.Tests` executes the admitted Core behavioral corpus once per test
assembly. Tests that exercise a particular transcript call the single-scenario
runner and do not execute the corpus as a side effect.

This lane is the useful local pre-push check. It is intentionally not installed
as a Git hook: hooks are local, bypassable and awkward to keep identical on
Windows and Linux. A developer who wants a hook can have `pre-push` invoke the
second command after a Release build, but CI remains authoritative.

## Acceptance and regression

Whole-game seed sweeps and the simulation harness live in one dedicated project:

```bash
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release
```

These tests trade speed for breadth. They still block a merge, but their separate
CI step makes their cost and failures visible instead of charging them to the
unit-test lane.

To run every test project in one command:

```bash
dotnet test Marvel.slnx -c Release
```
