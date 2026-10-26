# Black Widow B1 continuation independent review

Reviewed source: `9a961b65bf1f9e937020633248a5b6bddee1c0e5`, with Thor
`707a0ed1`, Preparation receipt `a804ad86`, and initial review `6584332d`.
Plane: MARVEL-763 (`4cd7bec9-69e2-4586-8286-da68a3207054`).

Verdict: **request changes**. The lethal Bite path succeeds, but the shared
projection change loses necessary validation and nested abstract state.

## Finding

`AbilityPowerSuspension.PowerSequenceChildren` now skips predecessor projection
when the remaining local suffix is independent. That method also supplies the
children used by `AbilityPowerTrace.ChildPowerStateAfter` for a complete sequence
projection. The latter computes each child from `child.Reachability` and replaces
its `ordered` result. An independent suffix can consequently replace an earlier
damage result or unsupported result with the earlier baseline. An enclosing
sequence then consumes that inaccurate state. Local suffix independence is not
proof that the complete sequence has no later projected consumer.

The same optimization also omits preflight validation for permanent attachment
departure and for later recipients in one damage instruction after opaque stage
work. Four existing negative controls fail with no exception:

- `IndependentDrawCannotAdmitAnUnsupportedMinionDeparture`
- `OneDamageEffectCannotReadAnotherRecipientAfterAnUnprojectedStageConstant`
- `NestedDrawSuffixDoesNotEraseOuterBoardDependentReads`, both variants

The next production packet must separate suspension-only traversal from complete
projection and required predecessor validation. Preserve all four negative
assertions. Recheck nested sequences/conditionals, dependent effects and repeated
work; none may lose an unsupported result because a local final child is a draw.
No production edits were made during this review.

## Independent evidence

The added Content tests exercise both false `isInPlay(chosen)` branches in a
chained synthetic tail after a suspended defeat interrupt. The original recipient
departs, optionally reenters, and the source reenters under another controller.
Both draws belong to the original resolving player; no stun reaches the departed
or new recipient copy. The receipt retains the original source incarnation and
controller. Declining the initial optional Preparation response creates no receipt.

The added Server tests use the existing `IDurableGameFactory` injection seam.
`PreparationReplayReviewFactory` supplies a synthetic Preparation Action and a
synthetic completion response on the supported Spider-Man identity. This is a
bounded persistence probe, not public Black Widow admission or a printed-card
playthrough. It persists after the discard cost and suspended defeat choice,
checks strict JSON replay and host restart, resumes to the completion receipt,
restarts there again, and tests both optional decline and completing both
responses. It compares full replay events, exact prompt records, card digest,
RNG words/index/consumption, phase and round. Host events are visibility filtered;
strict session replay validates their underlying recorded event trace.

Commands, run serially in Release:

```text
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~Preparation|FullyQualifiedName~PowerContinuationDependency"
dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter FullyQualifiedName~PreparationReplayReview
```

Content: 22 passed, 4 failed (the existing controls above); all three added cases
passed. Server: both replay cases passed. External logs:
`C:/Users/mggar/AppData/Local/Temp/widow-independent-content-final.log` and
`C:/Users/mggar/AppData/Local/Temp/widow-independent-replay-final.log`.
No full Content lane or new mutation run was performed; the existing controls
already reject the submitted production revision. Normal commit hooks remain on.

## Remaining B1 scope

The generic suspended Preparation completion and optional-decline replay shape
now has supported test-local durable evidence. Source remediation and independent
re-review remain required, followed by combined Black Widow validation. A public
setup-to-Bite-to-restart journey remains deferred until complete pack admission;
this synthetic factory does not establish that journey. Resource Preparation
completion remains unimplemented, but no printed Black Widow Preparation has a
Resource ability. It is not an outstanding printed B1 requirement and must not be
silently claimed as general Resource support.
