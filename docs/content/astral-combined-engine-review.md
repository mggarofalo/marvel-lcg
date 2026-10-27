# Independent Astral offer and defeat-receipt engine review

Reviewed source: `fd8f86680087f2de34d970a4107f42be7f84c968`, including
the `9eb7d9e5` offer filter, public-game proof pickup `799b31fa`, and
the deferred defeat receipt prerequisite. This review owns tests and this
record. Production source remains byte-identical to the reviewed revision.

The offer description uses the same engine action admission as execution for
each pre-arrow scheme candidate. It preserves mandatory cardinality, cost
meaning, and confusion replacement. Unsupported projection is allowed to raise;
it is not converted into an absent target. Existing production-card tests
distinguish empty and crisis-blocked schemes, reject invalid answers before
costs, and compare queried execution with an unprobed control.

The receipt exception is limited to `RemoveThreat(chosen, Result)` after the
retained physical chosen card enters the projected departed set. The live
continuation owns its incarnation and immutable numeric result. Hydra Patrol's
actual When Defeated search, engagement and shuffle complete before the tail;
the tail neither retargets nor rereads the shuffled pile. Production-card tests
cover both independent-effect orders and one/two players; synthetic reentry
probes distinguish a new incarnation and another same-title copy.

Authorities checked offline are printed 09003 and 04154, RR 1.8 target,
confusion, crisis, independent effects and When Defeated timing, plus the
vendored MarvelCDB FAQ's queried-card coverage. The conservative projection
boundary is a repository choice rather than a new card rule.

## Guard survivor resolution

The earlier compiled mutant deleting only
`state.Discarded.Contains(card.ObjectId)` survived five enclosing-action probes.
That observation remains valid; those probes do not distinguish this guard.
For the admitted `chooseScheme` Astral shape, lowering fixes the initial
mutation and receipt tail to the same `chosen` binding. The supported
simultaneous inspection pair likewise fixes the mutation to `chosen`.
`AbilityPowerStateMutation.DefeatSideScheme` adds the defeated scheme to
`Discarded` when it marks the defeat window deferred. A live chosen target
after an unrelated defeat is therefore not distinguished by those Astral
enclosing shapes. This is a bounded structural argument, not a claim that
every possible DSL tree makes the guard redundant.

`DeferredDefeatProjectionReviewTests` now exercises the projection contract
directly, using the production ability book and a real Hydra Patrol prefix.
It supplies either the departed Patrol or the still-live main scheme as the
captured chosen binding. Both cases observe a deferred defeat and the exact
departed set; only the departed chosen recipient may pass continuation
validation. Complete card digest, RNG state/words and agenda remain unchanged.
This is explicitly a synthetic projection contract, not a publicly admitted
action or a complete game.

The same guard-deletion mutant compiles and fails the live-chosen case with
`Assert.Throws(): No exception was thrown`; its departed-chosen control passes.
Production source is restored exactly before a nonincremental rebuild and
regression tests. The mutant is killed at this projection boundary; the earlier
five enclosing-action probes are not relabeled as kills.

## Evidence and verdict

Evidence is retained outside the worktree under
`C:/Users/mggar/Source/evidence/astral-combined-review/`.
The guard mutation logs and TRX files are `guard-mutant-build.log`,
`guard-mutant.log` and `guard-mutant.trx`. The initial combined focused selection
passes all 17 cases in `focused.trx`.

The nonincremental restored build passes. Expanding the earlier 770-case
selection to every test in its 191 affected classes plus the new review class
runs 1,522 cases: 1,521 pass and
`StagedAttackEdgeTests.StunnedActorPaysThenCancelsBothSentencesWithoutAttackEndResponse`
fails `MissingOffer` at line 123. That fixture submits a target after stun has
already canceled the attack. The exact failure reproduces with all three
Astral runtime files restored to pre-change source (`9eb7d9e5^` for the offer
description; `3758d21f` for receipt projection and defeat-window validation).
The lead confirms this fixture is corrected separately in reviewed
`4b7d42cf`; this worktree leaves it untouched. Logs and TRX files are
`restored-affected.*`, `baseline-build.log` and `baseline-stunned.*`.

Architecture passes all 129 cases in `architecture.trx`. After the baseline
check, production source is restored exactly and rebuilt nonincrementally;
the final combined focused selection and citation checks pass all 19 cases in
`final-restored-focus.trx` and `final-restored-build.log`.

All eight unchanged public-game cases pass, together with their policy control
(nine tests, 4 minutes 5 seconds), in `public-games.trx` and `public-games.log`:
Rhino seed 1, Klaw 2, Ultron 3, Crossbones 4, Absorbing Man 5, Rhino Expert 6,
multiplayer Absorbing Man Expert 7 and multiplayer custom-deck Rhino 8.
Every accepted decision compares the independent public host's event JSON,
RNG words, state digest and result, then verifies strict durable replay of the
complete accepted trace. These are complete engine/host/replay games.

Verdict: **accept the combined Astral engine fix**. No new correctness finding
remains in this reviewed scope. The guard survivor is resolved by a compiled
distinguishing projection test, with the enclosing-shape limitation explicit.
The known stale fixture is not waived or changed; the lead's integration gate
must include its separate correction. Platform certification, full pack
admission and native/product acceptance remain outside this engine review.
