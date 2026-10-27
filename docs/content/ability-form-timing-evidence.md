# Ability-driven form-change timing — MARVEL-862

Base `9a3339827661c12b1ee0be183c17df04cfd3822b`; branch `fix/ability-form-timing`.
This packet repairs the supported Split Personality / Taskmaster path. It does
not admit Ant-Man or Wasp, author abilities, extend the DSL destination vocabulary,
or change the public Forms selection API.

## Authority and observable sequence

Generated printed rows `01025`, `01019a/b`, `04093`, and boost `04064` are used
unchanged. The vendored MarvelCDB FAQ queried `01025` and `04093` and contains no
entry for either. RR 1.8 `rr:initiating-abilities.step.5` pays costs first;
`rr:interrupt.3` places an interrupt before its condition resolves;
`rr:initiating-abilities.2` places responses immediately after their condition;
`rr:ability.step.4.a` puts forced responses before optional responses.
`rr:then.2` requires full resolution before a dependent tail, and
`rr:player-elimination.5` requires finishing an initiated ability after elimination.

The interpreter schedules `IdentityFormChangeOperation`, retaining the unique
matching destination, and uses its existing effect-procedure continuation.
The old face remains showing during the interrupt window. The operation applies
the flip and records the existing `CardsFlipped` event. Its response window runs
before the saved ability suffix. The existing `EffectCompletion` receipt records
one actual flip, or zero when the occurrence is canceled; dependent text reads
that receipt. Ability changes preserve `Seat.FormChangedInRound`.

Actual-row regression: Jennifer Walters plays Split Personality with three
`01087` payments. Payment is discarded while Jennifer is still showing and all
eight player-deck cards remain. After the flip, Taskmaster consumes `04064` and
deals three damage. She-Hulk's optional response is offered before any Split
Personality draw. Declining it then draws four cards. A synthetic interrupt
observer sees Jennifer, zero damage, and the unconsumed boost. Synthetic positive
and cancellation controls distinguish a completed flip from a canceled one.
An actual Taskmaster response eliminates a synthetic action's owner; its already
initiated suffix still damages the surviving villain.

## Evidence

Logs are outside the repository at `C:/Users/mggar/Source/ability-form-*.log`.

- `restored.log`: 105 focused Content tests passed, including six new timing and
  completion cases, two adapted direct-run continuation fixtures, ordinary
  voluntary changes, prohibition/failed-then and no-op controls.
- `architecture.log`: 129 tests passed, with no baseline changes.
- `rules.log`: 19 focused Forms/lifecycle/citation tests passed.
- `mutant-suffix.log`: removing suspension for form changes compiled and failed
  four of six new tests.
- `mutant-cancel.log`: falsely recording a canceled flip as applied compiled and
  failed the cancellation dependent-tail control.
- `final.log`: six new cases passed after restoring the cancellation mutant.

The broad `content.log` run was deliberately stopped and superseded while the
candidate was being repaired. It is not verification evidence for this packet.
It revealed direct-run fixture assumptions, addressed in the focused continuation
fixtures. A separately reproduced pre-existing fixture failure is
`OwnerDeckShuffleReviewTests.ADepartingForeignControlledCardUsesItsOwnersParticipationState(eliminated: true)`,
line 39: expected `AlliesArea`, actual `RemovedArea`.
Reproducer: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter OwnerDeckShuffleReviewTests`;
`receipt-next.log` includes its focused failure. Root owns that separate
elimination-fixture reconciliation and broad integration.

## Replay and remaining boundary

There is no command schema, protocol field, digest encoding or RNG algorithm
change. Gameplay replay semantics change: ability flips now have interrupt and
response windows before their suffix, so optional choices, Taskmaster's encounter
discard/damage, event ordering, and later RNG consumption can differ from old
ledgers. Previously recorded decisions may no longer match the next prompt or
state checksums. Root owns the aggregate replay-compatibility/version decision;
this packet does not claim old archives remain compatible.

The binary DSL still cannot express Tiny versus Giant, and its face-specific
projection/admission work remains a separate packet before three-face authorship.
A unique matching form destination does not authorize a new product.

A separate voluntary-path timing defect remains: on a Jennifer board with the
same synthetic `WhenFormChanged` Interrupt observer as `AbilityFormTimingTests`,
call `Forms.ChangeAndSchedule(world, seat, facts, 1)`. The card is already
She-Hulk immediately after that call; `Sequence.Work` then offers the interrupt
with She-Hulk showing. RR `interrupt.3` requires the old face at that window.
`GameActions.ChangeFormNow` uses this same eager helper. This is a concrete
follow-up, not accepted timing behavior. Changing that public API/voluntary path
is outside this packet and needs coordination with the Forms selection review.
