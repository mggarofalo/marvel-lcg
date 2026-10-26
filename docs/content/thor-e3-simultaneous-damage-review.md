# Thor E3 simultaneous damage review

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
