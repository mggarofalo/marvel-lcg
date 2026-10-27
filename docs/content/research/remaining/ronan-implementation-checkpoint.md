# Ronan implementation checkpoint

MARVEL-567; phase prerequisite MARVEL-854. Integrated on 2026-10-27 into
`feat/ronan-completion`, based on complete 718-row Hulk candidate
`10af27098c40216c5340bd9fd3b9527d6e06af59`. The tested executable revision is
`2c68050f3c6a73c89dbc374d4f73044ca5847562`; this checkpoint and the exact source-pin
copy add documentation only.

## Accepted prerequisites

- Phase-start source `1f916ffd1c58d2545f8cf8fd21d1e05937d84720` integrates as
  `7145bbb5`; its [implementation record](ronan-phase-start-prerequisite.md)
  describes the real `WhenVillainPhaseBegins` occurrence before step-one threat,
  ordinary timing tiers, exactly-once suspension, and completion-observer lifetime.
- Independent review `edbaea8a09a775b46345024a142ceb9e76289ba3` integrates as
  `a796bcad`. The [review](ronan-phase-start-independent-review.md) found no
  blocking engine defect and adds departure, successive-frame, and forced-order
  lifetime evidence.
- Engagement proof `4086973d2384753f8cdb9d360f7b0c7e42f5fad3` integrates as
  `2c68050f`. Its [existing composition](ronan-selected-engagement.md) selects
  every tied lowest-HP hero with the first player, then scopes `you` through
  `eachPlayer` and uses `engagedWithYou`. Identity, in-play and hero-form guards
  preserve the selected physical identity across suspension. Existing minion
  transfer preserves state and incarnation; selecting the current seat is a
  no-op. This removes the proposed selected-engagement destination gap: rejection
  of the spelling `engagedWithChosenPlayer` does not imply missing capability.

All three packets applied without conflicts. The obsolete missing-support probe
is not included. The [source pin](ronan-source-pin.md) is copied exactly from
`origin/main` (Git blob `6a2cbe550d428ee3eac06b19a77c30a520b85055`).

## Integrated evidence

Logs are under `C:/Users/mggar/Source/evidence/`:

- `ronan-integration-focused.log`: Content Release run, **44 passed**, zero failed
  or skipped. Whole classes: `VillainPhaseBeginningTests` (9),
  `VillainPhaseBeginningLifetimeTests` (5), `RonanPhaseBeginningTests` (2),
  `RonanSelectedEngagementTests` (10), `ThorEngagementReviewTests` (4),
  `WreckingPhaseBoundaryTests` (6), `SetupAbilityTests` (6), and
  `RuleCitationTests` (2). The first five classes are the combined 30-case
  phase/lifetime/engagement gate. Command: `dotnet test
  tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter`
  with those eight class names joined by `FullyQualifiedName~...|...`.
- `ronan-integration-architecture.log`: `dotnet test
  tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release`,
  **129 passed**, zero failed or skipped.

Earlier full Content (7,792), Rules (1,187), and Session (84) evidence belongs to
the source prerequisite revision described in its record. Those full lanes were
not repeated on this integrated candidate. No integration finding required a
broader rerun. Normal commit-hook evidence is returned with the delivery handoff.

## Remaining scope and next packet

At this prerequisite checkpoint the production ability book contains **718 rows and zero Ronan rows**.
All five actual faces `90001–90005` remain pending, accounting for seven physical
copies. No production ability book, setup recipe or public admission changes are
part of this checkpoint. Test-local bodies establish prerequisites; they do not
establish complete modular-set gameplay, public setup, save/restart, or native
acceptance.

Next bounded packet: author only actual `90001` from `datasets/cards/cards.json`,
using the reviewed game-subject phase trigger and existing engagement body;
include its cannot-be-stunned text, printed Toughness and boost entry. Prove the
actual authored row against both first-player orders, ties/current-seat/no-hero
cases, preserved transfer state, stun rejection, and the boost recipient and
entry lifecycle. Replace test-local overrides deliberately where the actual row
belongs; retain documented synthetic lifecycle controls. Run ability-data and
affected phase/engagement classes, with distinguishing negative controls for new
decisions. Keep the incomplete modular set outside public admission. Later
packets own `90002–90005`, exact encounter-set composition and public
setup/save/restart evidence after complete admission.

## Subsequent actual-face packet

The [90001 authoring record](ronan-90001-authored.md) records the subsequent
719-row candidate, actual phase/entry/status evidence and remaining review.
The prerequisite evidence above remains tied to its stated revisions.
