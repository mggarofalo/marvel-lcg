# Thor E3 simultaneous damage review

Current scope clarification: the overkill-specific ruling quoted in this
historical review describes pre-1.8 overkill. It is not current authority for
overkill. The retained direct-damage barrier applies the current damage stages
to a simultaneous effect; see [timing.md](../timing.md) and the separate
[RR 1.8 adjudication](overkill-rr18-adjudication.md). No overkill timing change
was integrated from the rejected MARVEL-756 candidates.

Candidate: `10684748978c342817c78f575254f6a8bd315c68`.
Verdict: **rejected**. The Tough bypass prerequisite has a concrete simultaneous
damage timing defect. Lightning Strike (`06006`) remains unauthored and unadmitted.

This independent review adds synthetic contracts in
`SimultaneousDamageBarrierReviewTests`; their abilities are test inventions, not
claims about the printed Hydra Mercenary or Sandman abilities. The existing E3
and historical Shocker/Whirlwind assertions are unchanged. No engine or ability
book changes are part of this review.

## Authority and findings

Vendored `ruling:304a4798e0b453b4` says simultaneous overkill damage follows the
damage process and "each step is resolved simultaneously for both targets."
Applying that step barrier to multi-recipient direct damage is supported by
`rr:and` and `ruling:3eb442853617fe14`, which expressly treats Ground Stomp
damage as simultaneous. `rr:damage.step.6`, `.step.7`, and `.step.8` separate
would-be-defeated interrupts, when-defeated interrupts, and departure.
`rr:forced.5` gives the first player the order of simultaneous forced abilities.

`SimultaneousDirectDamageResolution.Continue` invokes each recipient's `Settle`
in declaration order. `SimultaneousDirectRecipient.Settle` delegates to
`DamageResolution.Settle`, whose `DamagePlacement.FinishPlaced` runs the
recipient's steps 6, 7, and 8. Suspending that recipient delays its own completion,
but does not create a shared defeat-stage barrier.

The eight review cases distinguish these failures:

- A dying global forced observer answers only one of two step-6 occurrences
  when declared first. Declaring it last produces both answers.
- The same loss occurs for a global step-7 observer. The reversed-order control
  again passes.
- One recipient's step-7 threat placement precedes the other's step-6 threat
  placement. The resulting threat history is `[2, 3]`, where the rules require
  `[1, 3]`. Reversing recipient order passes. Delaying departures alone therefore
  cannot repair the full defect.
- Accepting a suspended optional step-6 interrupt lets the observing recipient
  depart before the second step-6 window. The second opportunity is absent.
- Separate mandatory self When Defeated abilities resolve without the required
  first-player order prompt. The test uses seat 1 as first player to distinguish
  ordering authority from seat 0 and declaration order.

## Evidence

On Windows, from this review checkout:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~SimultaneousDamageBarrierReviewTests|FullyQualifiedName~IgnoreToughDamageTests|FullyQualifiedName~RuleCitationTests' --logger 'console;verbosity=normal'
```

Result: **22 cases, 17 passed, 5 failed**. The review's eight cases have three
passing reversed-order controls and five semantic failures; the existing E3
flag/projection tests and citation validation pass. The failing assertions are
preserved deliberately for the engine repair; none are skipped or weakened.

Logs are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/marvel-thor-damage-review-evidence/`:
`final-focused.log` is the final test evidence. `barrier-probes.log` contains an
earlier fixture-only failure (missing main-scheme face suffix and an unsupported
acceleration-token amount), corrected before the semantic run;
`barrier-probes-corrected.log` is an intermediate six-case semantic run.

## Remaining acceptance scope

Repair shared step-6 and step-7 boundaries and their mandatory ordering while
retaining the replacement/prevention/placement barriers. Recheck source and
recipient incarnations, causal defeat attribution, and completion through nested
choices. These probes do not establish whether eligibility must be frozen across
every within-tier board-changing interrupt; normal live eligibility rules still
apply when ordered abilities resolve.

This packet does not supply an actual printed-card cross-recipient defeat case,
new mutation evidence, adversarial projection-purity comparisons against an
unprobed control, save/replay or frozen-route evidence, a full suite, or public
Lightning Strike integration. Existing passing flag/projection evidence does not
accept those untested scopes or the simultaneous defeat procedure.

## Repair candidate: shared defeat stages

Branch `fix/simultaneous-defeat-stages`, based on review revision
`6c068c3c7c0c96575357f1da55726270e5d74215`, introduces a Rules-owned staged
coordinator and exact per-recipient step-6 occurrence binding. Narrow Cards
integration defers individual authored-window scheduling only for marked shared
occurrences; per-target damage-ability entry hooks remain active.

The observer probes now assert and answer the required two-instance mandatory
order prompt. The suspended optional probe asserts both available occurrences in
its first opportunity, then the remaining occurrence. These changes strengthen
the review's timing requirements: no automatic declaration-order choice is
assumed. The threat history, two observer effects, first-player authority,
completion, and departure assertions remain in place.

Five additional synthetic continuation cases cover step-6 forced healing in both
declaration orders with seat 1 as first player; selecting the second optional
recipient first; re-entry during step 6; and a re-entered own When Defeated source
during step 7. The re-entry receipt test uses an explicit causal occurrence to
check the actual defeat list rather than the root execution wrapper.

Measured focused evidence in `%TEMP%/simultaneous-defeat-*.log`:

- Content review and new continuation cases: 13/13 passed.
- Expanded restored Content focused set including Tough, singleton defeat,
  Shocker and Whirlwind controls: 54/54 passed before the final added step-7
  incarnation case; that case is included in the 13/13 run.
- Rules focused controls: 22/22 passed, including existing synthetic per-target
  observer and suspended-defeat hooks.
- A compiled mutant removing the step-7 completion barrier fails
  `FirstPlayerOrdersMandatoryDefeatAbilitiesAcrossRecipients`: the required
  ordering prompt is absent. A non-incremental restored build passes and the
  restored focused suite passes. An initial `if (false)` mutation did not compile
  because warnings are errors; it is not counted as mutation evidence.

This candidate still needs independent review and integration compatibility
proof. It does not claim to repair simultaneous attack damage, establish genuine
predecessor save/replay compatibility, or admit Lightning Strike. One early broad
Content run was interrupted before completion and is not passing evidence.

The complete Content run exposed the admitted Squirrel Girl / two Weapons Runner
case's immediate-completion assumption. Its test now asserts the first player's
two-ability order prompt and both soldiers remaining in play, explicitly chooses
the second soldier, and retains all original departure, each owner's dealt card,
villain damage, and Squirrel Girl damage assertions. This is the printed-card
cross-recipient defeat case missing from the initial synthetic review.

The same broad run found two direct-entry When Defeated probes whose synthetic
`Killed(card)` fact omitted its incarnation. Their fixture now captures the
current incarnation; the original effect and missing-resolving-player assertions
remain intact. These corrections postdate the broad binary, so that run is not
final-source full-suite evidence.

Final closeout evidence: non-incremental Release build passed with zero warnings
and errors; the final-source focused Content set passed 68/68, including all
review probes, all five continuation cases, Squirrel Girl's actual two-soldier
order, corrected direct-entry When Defeated probes, singleton replacements,
Tough, Shocker, Whirlwind, and rule citations. Full Rules passed 1112/1112.

The completed earlier full Content binary passed 5553 and failed exactly three
of 5556 cases: Squirrel Girl's immediate-completion assertion and the two
missing-incarnation direct-entry fixtures described above. Their corrected
sources passed the final focused run; full Content has not been rerun on those
final sources. This is a partial integration result, not a final full green.
