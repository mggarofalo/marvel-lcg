# Test lanes

The test suite has three lanes. All block a merge in CI, but they answer different
questions and run as separate steps.

## Quality-tool setup

The pre-commit and CI complexity gate uses the same Lizard 1.24.0 analyzer for
C# and GDScript, with a maximum cyclomatic complexity of 10. Install its pinned
runtime dependencies in an active Python 3.13 environment, with that environment's
scripts directory on `PATH`:

```bash
python -m pip install --requirement tools/requirements-quality.txt
dotnet tool restore
dotnet husky exec .husky/csx/complexity.csx
```

The package and command-line options are documented by
[Lizard on PyPI](https://pypi.org/project/lizard/1.24.0/). The checker analyzes all
tracked and non-ignored source files; it does not limit inspection to staged edits.
CI runs this same hook script once in the managed pull-request lane and on both
Windows and Linux during exhaustive main/manual/release runs. Local execution on
one operating system does not establish cross-platform CI success.

## Fast merge preparation

Build once, then run the focused unit, contract and behavioral tests:

```bash
dotnet build Marvel.slnx -c Release
dotnet test tests/Marvel.UnitTests.slnx -c Release --no-build
```

`Marvel.Content.Tests` executes the admitted Core behavioral corpus once per test
assembly. Tests that exercise a particular transcript call the single-scenario
runner and do not execute the corpus as a side effect.

The local pre-commit gate is deliberately broader than this fast lane. It builds
the complete solution in Release before running every managed test project with
`--no-build`, so neither a project nor a slower managed test lane can be omitted
before Git accepts a commit. It then runs the native Godot journey at every
supported UI scale, with the reduced-motion checkpoint and
two-player desktop scale variants. CI remains authoritative for the second
operating system and packaging.

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
complexity, unit, acceptance, behavioral-catalog, executable-specification and deterministic
dataset checks once on Linux. `Native integration` builds and runs integration,
architecture-wall, native Godot, hosted-client and rendered-state checks on both
Windows and Linux; Linux also proves the supported server container. Windows
desktop packaging remains an independent job.

The pull-request native game uses one representative viewport at the default
scale with both motion preferences. Pushes to `main`, manual runs and protected
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
