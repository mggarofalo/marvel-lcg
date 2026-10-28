# Ant-Man form destination foundation — evidence

Base: `3fa15589a1328f7d60bc5399f3db0ed8174f19f0`. This packet implements standalone
exact-face and other-hero destinations, without admitting Ant-Man or completing
MARVEL-862. Composite use of either destination raises before admission because
the existing form/trait projections do not yet represent an exact face.

The MC12 rulesheet permits each of the six directed changes and states that a
card-caused change does not consume the voluntary change. Tests use the actual
identity faces with synthetic local abilities. They exercise six changes and
both other-hero directions through `Game`, foreign and same-face destinations,
ambiguous generic destinations, two-face behavior, and rejection of composed
action, delayed-activation and scenario-window instructions. A valid scenario
window is the rejection control. A probed game and an unprobed control make the
same subsequent decision and compare state, random state, event kinds, pending
question and agenda.

Before the cohesive lowering and trigger-role extractions, the focused Release
test run passed 97 tests, covering the
new destination classes, existing form completion, timing and Game reviews,
three-face offers and ability lowering. Full log:
`C:/Users/mggar/Source/ant-form-final-test.log`. Before the final query-control
test was added, the same selection passed 96 tests. A compiled mutation changed
other-hero selection to alter ego: both Game tests failed on the wrong resulting
face. The restored source received a nonincremental Release build and the same
two tests passed. Logs are `ant-form-mutant.log`, `ant-form-restored-build.log`
and `ant-form-restored-test.log` under `C:/Users/mggar/Source/`.

This evidence does not establish projected active-face or trait semantics,
Resize, Helmet or Giant Strength, replay/publication compatibility, independent
review, or complete F1. Those remain separate work.

The form-change DSL parser lives in `AbilityFormChangeLowering`; occurrence
subject, actor, target and player matching live in `AbilityTriggerRoleMatching`.
The resolver separately validates an exact face and resolves a same-face
destination in `ResolveFace`. These cohesive extractions preserve the parsed
values, conditions and evaluation order. Post-extraction validation passed a nonincremental Release build of
`tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj` with no warnings or errors.
The broader focused command was:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --no-restore -m:1 -nr:false --filter 'FullyQualifiedName~Form|FullyQualifiedName~AbilityLowering'
```

Its result is recorded below; this broader selection is distinct from the historical
97-test selection. Logs are `ant-form-extraction-build.log` and
`ant-form-extraction-test.log` under `C:/Users/mggar/Source/`. The mutation evidence above was
collected before extraction and has not been rerun afterward.

Post-extraction focused result: `Passed!  - Failed:     0, Passed:   532, Skipped:     0, Total:   532, Duration: 22 s - Marvel.Content.Tests.dll (net8.0)`.
