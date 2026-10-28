# Ant-Man standalone destination foundation — independent review

Reviewed source: `b6a86c0bd07826d52ca60bfb3546d55ac7e6e362`.
Baseline: `3fa15589a1328f7d60bc5399f3db0ed8174f19f0`.

Verdict: **rejected for the claimed standalone-only admission boundary**.
Independent compiled adversarial tests establish incomplete enforcement. This review
does not accept Ant-Man, projected exact-face alternatives, or complete F1.

The destination resolver validates membership in the physical identity before
treating a same-face destination as a no-op. Other-hero uses the supplied current
face and requires that face to be a hero. Generic hero remains ambiguous when an
alter ego has two possible hero destinations. Live admission and execution share
the resolver, and execution schedules the existing exact identity/incarnation
`IdentityFormChangeOperation`. The operation's existing live revalidation remains
the owner of departure, permission and destination changes during interrupts.

The new structural guard traverses all declared effect children, including
`afterActivation`, and ordinary action admission calls it before payment. Legacy
binary projection readers explicitly reject new destination values. The remaining
concern is entry coverage:

- `AbilityBatchExecution.RunOrdered` starts setup, reveal, boost and activation
  abilities through `Run` without whole-effect admission. A one-child `and`
  wrapper enters its child directly. Setup, reveal and boost probes each schedule
  an exact `12001c` flip without reaching the composite guard. Activation entry
  uses the same batch owner but was not independently executed in this review.
- `AbilityWindowResolution.CanInitiateAbility` returns immediately when the
  resolving player is the scenario. With the engine's villain-phase-beginning
  occurrence shape (no player), `PrepareWindow` accepts a synthetic scenario-owned
  composite that `Waiting` correctly rejects. The fixed `firstPlayer` selector
  avoids treating a missing resolving seat as a form-change recipient. This is
  a direct preparation contract inconsistency, not a claim that discovery offers
  the rejected composition through the ordinary game loop.
- A top-level `afterActivation` entering through setup can register future work
  before the new guard runs. A live activation makes this distinguishable from
  rejection merely because there is no activation.

`AbilityFormDestinationEntryReviewTests` adds five focused negative cases for
these entry boundaries, plus five independently executed positive controls:
standalone setup/reveal/boost, standalone scenario preparation and supported
delayed draw registration during a live activation.
The probes capture state before the call and check pending flip work or delayed
runtime registration as well as exception type and reason. They are synthetic
contract tests, not printed Ant-Man card compositions.

Independent execution against the reviewed source completed on Windows in Release:

```powershell
$env:MSBUILDDISABLENODEREUSE='1'
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1 -nr:false
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --no-restore -m:1 -nr:false --filter 'FullyQualifiedName~AbilityFormDestinationEntryReviewTests'
```

The build succeeded with zero warnings/errors. The focused result was **5 failed,
5 passed, 0 skipped, 10 total**. The three ordered-entry failures show an unexpected
pending `IdentityFormChangeOperation`; delayed setup fails because `AbilityRuntime`
is populated; direct scenario preparation fails because no exception is raised.
These failures remain enabled as regression evidence. Build/test logs are
`C:/Users/mggar/Source/ant-form-independent-entry-build.log` and
`C:/Users/mggar/Source/ant-form-independent-entry-review.log`.

The implementation author's prior 532-test focused run and pre-extraction mutation
evidence are provenance, not independent results. This reviewer changed no source
or production ability-book rows and did not rerun that wider selection after the
five distinguishing failures were established.

Resize, Helmet, Giant Strength, exact-face projections, production ability-book
rows, public admission, native interaction and complete F1 remain outside this
review. No source fixes are owned by this reviewer.

## Independent entry-guard repair closure

Reviewed repair: `18214aaa99d6312a48b868105b41413bd4986971`, over the rejecting
review `69b59089` and foundation `b6a86c0b`. The rejection above remains the
historical finding against the foundation source. **The repaired standalone-only
entry boundary is accepted within this review's scope.** This does not accept
Ant-Man or complete F1.

Source inspection confirms that `RunOrdered` materializes and checks the whole
selected ability set before creating or running its first resolution state.
Setup, reveal, boost, card-entry and completed-activation callers share that
owner. Reveal checks its selected set after cancellation and before beginning a
treachery receipt; its existing blanking and form filters still determine the
selected set. Direct scenario-window preparation checks the whole effect before
the scenario-player initiation shortcut. The compiled-ability interpreter also
checks before labels or child effects can start work. Exact and other-hero form
destinations inside compositions remain explicitly unsupported.

The independent Windows Release run rebuilt the repaired source with zero
warnings/errors and passed **89 tests, 0 failed, 0 skipped**. The selection
included the original 12 entry regression/control cases, seven new independent
closure cases, destination component/Game cases, and existing activation,
blanking, reveal cancellation, resolution-status and nested-reveal regressions.
The new synthetic cases establish:

- A made activation rejects composition without changing cards, RNG, pending
  agenda or delayed runtime; an activation not made skips the same body, while a
  standalone destination schedules the exact flip.
- A blanked in-play minion supplies no reveal or activation body, and a
  form-mismatched reveal does not enter the unsupported composition.
- A canceled reveal returns its cancellation event without entering the body.
  Once that one-use cancellation is consumed, the same composite rejects with
  the serialized occurrence unchanged, proving rejection precedes the treachery
  receipt as well as flip scheduling.

Commands used `MSBUILDDISABLENODEREUSE=1`, `-m:1`, `-nr:false`, Release, and
`--no-build --no-restore` for the test execution. The exact filter is retained in
the command below:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --no-restore -m:1 -nr:false --filter 'FullyQualifiedName~AbilityFormDestinationEntry|FullyQualifiedName~AbilityFormDestinationTests|FullyQualifiedName~AbilityFormDestinationGameTests|FullyQualifiedName~CoreActivationAbilityTests|FullyQualifiedName~PrintedTextBlankingLifecycleTests|FullyQualifiedName~MediaCoverageRevealTests|FullyQualifiedName~ResolutionStatusTests|FullyQualifiedName~NestedRevealCompletionTests'
```

Independent final logs are
`C:/Users/mggar/Source/evidence/ant-entry-closure-build.log` and
`C:/Users/mggar/Source/evidence/ant-entry-closure-regression.log`.
The repair author's wider run and combined guard-omission mutation are provenance,
not independent measurements in this closure. No new mutation experiment, full
managed lane, native run, source fix or production ability-book change was made
by this reviewer. Projected active-face/trait semantics, Resize, Helmet, Giant
Strength, public admission and complete F1 remain unaccepted here.
