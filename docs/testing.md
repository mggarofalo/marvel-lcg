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

Pre-commit runs architecture-policy, complexity and file-length checks. Architecture
policy includes source layout, the reviewed-debt ratchet, engine determinism and
static-data boundaries, and narrow test-quality controls. It does
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
baseline suite log implies approximately 18 minutes before the fourteen added contracts;
the expanded membership requires its own measurement.
The complete scale matrix remains available through the default
`tools/godot-smoke.ps1` or `tools/godot-smoke.sh` invocation. CI remains
authoritative for the second operating system and packaging.

Native case membership comes from `tools/native-cases.json` on both platforms.
Inspect it without building or launching Godot, then select a focused case:

```powershell
pwsh tools/native-cases.ps1 -Profile representative -List
pwsh tools/native-cases.ps1 -Profile representative -ShardCount 2 -ShardIndex 0 -List
pwsh tools/native-cases.ps1 -GodotBin $env:GODOT_BIN -Case card-face-sample-100-motion
```

Every selected case must exit successfully within its deadline and emit all its
required completion markers. An unknown selector fails. Evidence is written
outside the repository: individual logs and `results.jsonl` retain the exact case
inputs, elapsed time, exit code, timeout status and missing markers. Use
`-ResultsDirectory` to choose that external directory and `-NoBuild` only after
building the corresponding final sources. The independent manifest tests retain
each reviewed baseline case by identity and inputs, not just a total count.
Fixture generation removes any prior exact output leaf before running its provider;
a successful filter with no matching test cannot reuse stale fixture evidence. A directory
already containing `results.jsonl` is rejected before any log, build or fixture
is changed; choose a fresh directory for each run. This applies equally to
`-ResultsDirectory` and `MARVEL_NATIVE_RESULTS_DIRECTORY`.

## Native contract evidence

The manifest preserves the reviewed baseline union of 125 exhaustive and 115
representative cases, including each helper's fixture and environment settings.
Seventeen additional contracts bring those profiles to 142 and 132 cases. Seven
optional inspection cases are separate from both profiles. The manifest is the
membership authority; shell wrappers only select it.

Thirteen additions use legal prepared Core scenes through the ordinary
`EngineHost`, socket transport, `LocalGameClient` and native controls. They are
component journeys, not transcripts from an opening deal to the prepared state.
`NativeSceneGameFactory` uses the canonical scene operations and validates the
initial prompt; the repeated-target case records its phase prelude through the
host before exposing the pending choice. The grouped case is explicitly
synthetic because the executable Core catalog has no grouped-target producer.

| Contract suffix | Canonical example / kind | Distinguishing observation |
|---|---|---|
| `victory` | Spider-Man's final blow against Rhino II; prepared Core | One resolve publishes `PlayersWin`; no active mutation controls; final table and history survive synchronization. |
| `end-phase` | Spider-Man discards Black Cat at end of phase; prepared Core | Stage, clear and restage the offered discard; only the explicit commit moves the card, after the separate end-turn decision. |
| `lost-reply` | Spider-Man attacks Rhino; prepared Core with socket fault | Host commits before dropping the reply; inspection works while mutation controls are locked; stale callbacks before and after recovery produce exactly one resolve. |
| `not-sent` | Same attack with listener unavailable before send; prepared Core | Authority stays at revision zero; restoring the listener permits one explicit retry and one resolve. |
| `wakanda-daggers-first` | Energy Daggers then Panther Claws; prepared Core | Pay, choose order, commit the player and enemy choices; sole final special needs no extra ordering prompt; four resolves deal five damage. |
| `wakanda-claws-first` | Panther Claws then Energy Daggers; prepared Core | Opposite order retains source/final-step context; four resolves deal four damage and discard Wakanda Forever. |
| `keyboard-payment` | Black Cat paid with Energy; prepared Core | Actual Tab/Shift-Tab traversal, Enter and Escape inspect/dismiss, restore focus, select payment and commit once without pointer staging. |
| `keyboard-target` | Spider-Man chooses Rhino with Shocker also available; prepared Core | Actual keyboard traversal selects source, action and target, then explicitly commits the winning attack. |
| `variable-payment` | Energy Channel; prepared Core | Edit X from two to one and back, select Energy, preserve the draft through scaling, then receive exactly two upgrade counters from one resolve. |
| `repeated-targets` | Bomb Scare's Explosion; prepared Core plus recorded prelude | Add/remove/re-add repeated hero allocation, fill the ally allocation, reject incomplete/over-capacity staging and apply exactly the selected damage once. |
| `dense-table` | Iron Man/Rhino seed 5 after 48 policy decisions; prepared Core | Five installed sources and five hand cards remain accessible and separate; source meaning or its complete excerpt/disclosure stays readable without resolving anything. |
| `card-look` | Tony Stark's Futurist; prepared Core | Inspect and change selection among exactly three authorized top cards; named commitment adds Mark V Armor, discards the other two and leaves the remaining deck concealed. |
| `resource-declaration` | For Justice paid with The Power of Justice; prepared Core | Change individual resource declarations with native menus; the submitted `BY` allocation reduces scheme threat from five to one. |
| `synthetic-grouped-contract` | Authored ordered groups `[12,11]` and `[11,13]`; synthetic | Replacing a group retains no earlier member; its authored order survives scaling and a single explicit submission, independently of pooled min/max. |

Scale checkpoints cover all eleven settings from 50% to 150% in ten-point
steps, at 1920×1080 and 2560×1440, with motion enabled and disabled. The states are
setup, mulligan, action ambiguity, selected search, nested ability window,
end-phase discard, variable payment, repeated allocation, resource declarations,
Wakanda payment/order, dense tableau and victory. Each checkpoint preserves the
composer, revision, visible choice surface and selections, exercises hit geometry
and real Tab focus, and checks the complete commitment label. Dense-table checks
also verify source/hand separation and source meaning. Required completion
markers identify the measured states; merely changing a scale setter does not
count. Synthetic groups separately exercise all eleven scales and exact order.

Baseline exclusions remain explicit. Two-player journeys exclude the
single-player form-change/undo sequence rather than premarking it as passed.
End-phase coverage requires an actual `EndPhaseDiscards` prompt and accepted
revision. Existing pointer and programmatic-focus checks do not establish real
keyboard traversal; the two added keyboard contracts do. Prepared victory and
synthetic groups do not establish a complete opening-to-victory transcript or a
playable Core grouped ability.

### Measured focused checks and negative controls

The local representative profile has passing evidence for all 129 case IDs across
staged Windows runs. Affected scheduler and UI journeys were rerun after their
fixes; the final eighteen-case run passed against the coherent Release/Debug
build. This aggregate is not a single final-build run. Required Windows and
Linux CI execute the complete representative profile on the PR commit.

On Windows with Godot 4.7.1, all fourteen additions passed focused native runs.
After rebuilding the engine meaning fixes, the final four-case run passed
victory (13.107 s), Daggers-first (17.823 s), Claws-first (19.984 s) and Futurist
(4.581 s). These are focused observations, not a claim that the final complete
profile or Linux certification has finished. Per-run logs remain outside the
repository; the executor records inputs and results in `results.jsonl`.

The expanded baseline checkpoints also exposed two presentation lifecycle defects:
live table reconstruction could duplicate the ability sheet's situation text, and
scale changes could discard an unresolved source-action choice. The repaired
nested-window case passed in 12.164 s. After both source-choice mutations were
restored, the complete seeded `local-game-100-motion` journey passed in 85.867 s,
including all 44 combinations for setup, mulligan and ambiguity, unchanged draft
and revision, Escape focus restoration, stale callback rejection and terminal
controls. These local checks span several coherent build checkpoints; they are
not a single uninterrupted final-build profile. Final combined membership and
both-platform certification must be reported separately from these focused
results.

| Mutation / negative control | Distinguishing result |
|---|---|
| Reverse the renderer's selected group before passing it to the composer | Compiled native probe fails `first group's authored order was lost`; restored probe passes. |
| Remove submitting suppression when publishing the board draft | Compiled lost-reply journey fails because a second commitment remains enabled after the host has committed; restored journey passes. |
| Register situation ownership after attaching the live prose node | Compiled native mutation fails at the first nested-window scale with `owners=1`; subscribing before attachment passes all 44 scale/viewport/motion combinations. This distinguishes a missed Godot `Ready` event from lost ownership state. |
| Disable prompt-bound source-choice restoration | Compiled native mutation loses the ambiguity surface at 50% scale. The restored source choice survives all 44 combinations with unchanged draft/revision and restores its action-entry focus on Escape. |
| Remove active-popup identity from choice callbacks | Compiled native mutation lets a dismissed same-generation callback change the draft. The restored callback fence rejects both replaced and dismissed choices; resizing after dismissal does not resurrect the chooser. |
| Remove the engine's named CardLook commitment | Existing Futurist outcome test fails on the missing `Add Enhanced Spider-Sense to hand` label. |
| Remove final-special context from the prompt summary | Existing final/nonfinal Daggers theory rejects the final prompt; nonfinal control still passes. Both meaning mutations were restored and all three focused cases pass. |
| Permit substring completion markers | Executable negative control rejects treating `NOT_JOURNEY_OK` as successful completion. |
| Expand a singleton process argument as characters | Executable process control rejects the corrupted invocation. |
| Force plain arguments through fixture expansion | The non-fixture argument control rejects the null fixture lookup; fixture-path-with-spaces control preserves exact argument boundaries. |
| Reuse a previous fixture file without producing a new one | The committed freshness control requires the stale leaf to be absent before accepting fresh provider output. |
| Thirteen malformed manifest variants | Each fails its owning diagnostic, including script traversal, invalid profile/input shapes, fixture paths escaping evidence storage and placeholders without providers; the real manifest passes. |
| Root exits while an owned child retains output pipes | Executable process control requires the shared deadline, bounded cleanup and child termination; normal-exit and isolated-environment controls also pass. Windows is measured; Linux requires its CI execution. |

Retaining the previous composer in `DecisionPanelLifecycle.Render(null)` survived
the native victory probe. On this path `ClearDecision` already clears the
composer and world, so that mutation does not recreate an active terminal
control. It is not counted as a killed terminal-control mutant. The functional
victory assertions still check terminal lock, absent active controls, history and
synchronization; their sensitivity to an independently introduced active terminal
control is not established by that experiment.

Exact duplicate-dispatch observations also remain in the existing managed
`ClientGameLifecycleTests.InFlightSubmissionExcludesRepeatedInputAndSynchronization`
and `UncertainMutationAndFailedSynchronizationNeverUnlockOrResend` tests. The
native lost-reply case adds real-socket commit loss and stale callbacks with an
exact one-resolve assertion; it does not replace those managed admission checks
or claim a separately measured duplicate-dispatch mutation.

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
executable-specification and deterministic dataset checks. `Platform gates`
runs integration tests, architecture walls, hosted-client and invitation-layout
checks on both Windows and Linux; Linux also proves the supported server container.
`Native journeys` runs the representative manifest in two isolated shards per OS.
The required `Native integration (windows-latest)` and
`Native integration (ubuntu-latest)` checks succeed only when every platform gate
and every journey shard succeeds. Failure, cancellation, skipped work or missing
results cannot satisfy either check. Windows desktop packaging remains independent.

Routine platform-gate and native-journey jobs retain a 40-minute deadline. CI run 38869380422 completed all 129
representative cases on Windows in 31.7 minutes and Linux in 21.3 minutes; Windows
then reached the whole-job deadline during hosted checks. Sorting selected case IDs
ordinally and alternating them between two shards preserves all cases and their
inputs. That Windows measurement divides into 65 cases taking 14.2 minutes and
64 taking 17.5 minutes, before each shard's independent setup. This is measured
partition evidence, not a guarantee of future runner speed. Each shard builds its
own binaries, regenerates its required fixtures and uploads separate evidence.
Executable controls prove disjoint, exact unions for every profile and filtered
case selection. Unsharded hooks retain manifest order; empty execution shards fail.

Exhaustive certification repeats the managed checks on both platforms, runs the
complete viewport and scale matrix, and adds rendered visual and contextual-table
acceptance. Request it explicitly with the manual `exhaustive` input; protected
release automation also requires it from the exact release tag. It has a
90-minute deadline per job, with the exhaustive manifest partitioned by the same
two-shard rule and all platform gates retained. Do not dispatch certification alongside routine
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
