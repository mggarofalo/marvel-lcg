# MARVEL-746: candidate self play-cost pricing

Based on `125e99a83f052900b6204df4880adcc83c82222e` (E2 event-price composition).
The commit containing this record is the review candidate; no actual Thor
ability row, setup, product admission, or replay/protocol version changes are
included.

Printed authority: `datasets/cards/cards.json`, 06011: "Reduce the cost to play
Hercules by 1 for each minion engaged with you." The E4 handoff requires live
candidate pricing rather than a continuous effect activated in hand.

`ICardPlayAbilities.SelfPlayCostReduction` is an engine-owned read-only port.
The Cards implementation reads the specific physical candidate, its playing
seat's current engagement, and a bounded checked Constant instruction. The
existing payment calculation applies the reduction, existing discounts and the
zero floor. Existing play restrictions and accepted payment snapshots retain
ownership of uniqueness, ally-limit choices and suspended resource payment.
There is no renderer rule, effect registration, RNG use or runtime mutation.

`SelfPlayCostTests` uses a synthetic book over printed card facts. It proves
zero/two/seven minions; second-seat and foreign-seat separation; changing
engagement; several physical candidates; no continuous effect from hand;
stacked/consumed discounts; uniqueness and the ordinary ally-limit choice;
underpayment atomicity after engagement changes; query purity across digest,
RNG words/index/consumption, registered effects, agenda and prompt; equivalent
subsequent committed events and state versus an unprobed control; and a resource
generator whose cost suspends after the candidate is exposed outside the hand.
Invalid timing, nesting and nonpositive scalar forms fail before gameplay.

Focused command:

```text
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~SelfPlayCostTests|FullyQualifiedName~FirstAllyCostTests|FullyQualifiedName~VariableEventPaymentTests"
```

27 tests passed. This includes the existing first-ally mechanism and E2's variable
energy / printed event price composition. A compiled mutant replaced the new
subtraction in `CardPayment.CostOf` with zero: all three seat/count theory cases
failed (expected price versus actual printed price). The source was restored,
then rebuilt with `dotnet build ... -c Release --no-incremental`; that rebuild and
the same 27-test run passed. Final fixture cleanup uses three distinct admitted
allies for the ally-limit state, with focused verification repeated afterward.

Logs are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/hercules-{focused,mutant,restored-build,restored,architecture}.log`.
Full wave/public/native/replay acceptance and actual 06011 authoring remain the
integrator's separate work. No product-readiness claim is made here.

Architecture command: dotnet test tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release; 129 passed. Normal pre-commit architecture, complexity and file-length hooks remained enabled.

The first normal commit attempt found CCN 11 in the existing unsupported-constant
switch after adding a policy case. Candidate-pricing instructions now exit the
constant query before continuous-binding construction; the unsupported-constant
refusal switch is unchanged. No complexity baseline was weakened. Focused checks
and normal hooks were rerun after that correction.
