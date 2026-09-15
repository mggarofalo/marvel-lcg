# Test lanes

The test suite has three lanes. All block a merge in CI, but they answer different
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

This lane is also the local pre-commit gate. The hook builds the complete
solution in Release before running the focused tests with `--no-build`, so a
project omitted from the fast test graph cannot escape warnings-as-errors.
CI remains authoritative across Windows, Linux and the native boundaries.

## Integration

Architecture build probes, server transports, managed Godot behavior and release
packaging have their own lane:

```bash
dotnet test tests/Marvel.IntegrationTests.slnx -c Release --no-build
```

These tests cross process, project or presentation boundaries, so their runtime
does not make the focused unit-test result harder to see.

## Acceptance and regression

Whole-game seed sweeps and the simulation harness live in one dedicated project:

```bash
dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release
```

These tests trade speed for breadth. They still block a merge, but their separate
CI step makes their cost and failures visible instead of charging them to the
unit-test lane.

## CI assurance profiles

Pull requests split assurance by what can vary. `Managed correctness` runs the
unit, acceptance, behavioral-catalog, executable-specification and deterministic
dataset checks once on Linux. `Native integration` builds and runs integration,
architecture-wall, native Godot, hosted-client and rendered-state checks on both
Windows and Linux; Linux also proves the supported server container. Windows
desktop packaging remains an independent job.

The pull-request native game uses one representative viewport at the default
scale with both motion preferences. Pushes to `master`, manual runs and protected
release calls replace it with the complete viewport and scale matrix and repeat
the platform-neutral checks on both operating systems. A merged change therefore
gets exhaustive cross-platform evidence, while a pull request gets an earlier
answer from the same platform boundaries. Release automation explicitly requests
the exhaustive profile from the exact protected tag.

## Behavioral evidence for refactoring

[Architectural behavior contracts](architecture-behavior-contracts.md) identify
finite rule and card scenarios at the public game-loop and component boundaries,
with executed mutation checks. Reuse these contracts when decomposing the
interpreter, agenda, and phase orchestration. Whole-game completion and digest
recordings are supplementary evidence; they do not replace a distinguishing
observation of the rule being changed.

To run every test project in one command:

```bash
dotnet test Marvel.slnx -c Release
```
