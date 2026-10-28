# Independent review: ally-limit exclusion

Candidate: `c4ac17bf780c8f6aeec70298a40eb0090e2d8077`. This review uses synthetic ability rows on printed cards and prepared boards; it does not admit Stinger content. Current RR 1.8 `rr:ally-limit` requires the controller to choose allies until their count is legal, before entry abilities. An excluded ally remains a controlled ally and may be chosen; that choice alone does not lower the counted total.

## Findings and probes

- **Stale mandatory choice (confirmed synthetic contract failure):** `ContinuousEffectRegistry.RegisterCore` schedules a check on a limit reduction; ending the reduction calls the same check but does not cancel a previously scheduled check. `PlayerLimitProcedure.ChooseAlly` throws when the count is already legal. `RestoredLimitDoesNotLeaveAMandatoryDiscard` registers and disposes a synthetic reduction before the agenda processes it. This is a public engine API contract test; a supported game sequence reaching both transitions before the queued choice is not yet established. The appropriate result is no forced discard and normal continuation. Do not treat this as a demonstrated reachable gameplay failure without that evidence.
- `SyntheticExpirationDuringSuspendedEntrySettlesBeforeResponse` forces an increase to expire after an over-limit entry schedules a choice and before processing it. It expects two counted discards before the synthetic entry response. The explicit disposal is synthetic: phase and attack expirations happen in separate agenda procedures, and no supported route that interposes expiration at this exact point has been established. It tests the continuation contract, not a played scenario.
- `RepeatedQueriesPreserveStateAndTheNextLimitChoice` snapshots digest, RNG, agenda and registered effects before repeated constant and price queries, then compares the resulting choice with an unprobed identical control.

Existing candidate tests cover exclusion versus ordinary ally, transfer, blanking with a synthetic lasting marker, reentry, and registration/expiration rechecks. Public blanking currently targets minions; this review does not claim actual ally blanking gameplay. Set-value ally-limit modifiers and an actual Stinger row are outside this prerequisite. No UI or pack acceptance is asserted.

## Verification

On the candidate, the focused run compiled and produced two passes and one failure: the restored-limit probe throws `InvalidOperationException: player 0 no longer exceeds their ally limit`. The affected Content classes (independent probes, candidate exclusion, Ant ally support, ally entry payment) produced 26 passes and the same one failure. The query-purity probe also answered the same next choice in both boards and compared resulting digest, RNG and pending agenda. Logs: `C:/Users/mggar/Source/ally-exclusion-independent.log` and `C:/Users/mggar/Source/ally-exclusion-independent-affected.log`. The red test is intentionally retained for the subsequent fix. No independent mutant was run because the candidate failed a control; the author previously reported three compiled mutants.

## Stale-choice repair verification

The queued plan now returns no prompt when the live counted total is legal;
the existing agenda applies that plan and proceeds normally. Synthetic controls
cover continuation to the suspended entry response and a limit restored and
then reduced again before the queued plan executes. Neither establishes a
supported gameplay route to the synthetic interposition.

The unchanged independent probe and affected Content selection passed 104/104;
the affected Rules selection passed 76/76. A compiled mutant restoring the
stale-choice exception failed both the original probe and continuation control
while the restored-then-reduced control passed. The restored source was rebuilt
with `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c
Release -m:1 --no-incremental`; the same Content selection then passed 104/104
with `dotnet test ... --no-build`. All runs set `MSBUILDDISABLENODEREUSE=1`.
Logs are at `C:/Users/mggar/stinger-stale-*.log`, outside the source tree.
Independent closure review and integration gates remain.
