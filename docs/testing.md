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
CI runs this same hook script once in the routine managed lane and on both
Windows and Linux during explicit certification and release runs. Local execution
on one operating system does not establish cross-platform CI success.

## Fast merge preparation

Build once, then run the focused unit, contract and behavioral tests:

```bash
dotnet build Marvel.slnx -c Release
dotnet test tests/Marvel.UnitTests.slnx -c Release --no-build
```

`Marvel.Content.Tests` executes the admitted Core behavioral corpus once per test
assembly. Tests that exercise a particular transcript call the single-scenario
runner and do not execute the corpus as a side effect.

Pre-commit runs source-layout, complexity and file-length policy checks. It does
not rerun complete managed or native suites on every commit. Required PR CI owns
the managed and platform assurance below; bypassing a local hook cannot bypass
those required checks.

For an explicit broad local check, run:

```bash
dotnet husky run --group validation
```

That group builds the complete Release solution, runs every managed test project
with `--no-build`, then runs the representative native profile. The profile
includes focused probes, six complete game journeys, reduced motion and the three
two-player scale variants. Its local timeout is 25 minutes; the preceding full
suite log implies approximately 18 minutes for this membership on this machine.
The complete scale matrix remains available through the default
`tools/godot-smoke.ps1` or `tools/godot-smoke.sh` invocation. CI remains
authoritative for the second operating system and packaging.

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

Routine pull-request, main and default manual runs execute `Managed correctness`
once on Linux: complexity, unit, acceptance, behavioral-catalog,
executable-specification and deterministic dataset checks. `Native integration`
runs integration tests, architecture walls, representative Godot journeys,
hosted-client and invitation-layout checks on both Windows and Linux; Linux also
proves the supported server container. Windows desktop packaging remains an
independent job. Routine native jobs have a 30-minute deadline; this is a failure
bound, not a measured completion-time guarantee.

Exhaustive certification repeats the managed checks on both platforms, runs the
complete viewport and scale matrix, and adds rendered visual and contextual-table
acceptance. Request it explicitly with the manual `exhaustive` input; protected
release automation also requires it from the exact release tag. It has a
90-minute per-platform deadline. Do not dispatch certification alongside routine
PR CI by default. Routine success does not certify every scale or rendered state.

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
