# Selected-character controller review

Reviewed production revision `fd23bf88ab51d12bda7f67d2301dd0214fe02904`
independently against current RR 1.8 `attack-enemy-activation.md`,
`first-player.md`, `ownership-and-control.md` and `leaves-play.md`.

The changed non-null fallback correctly obtains the selected character's current
controller. RR `rr:attack-enemy-activation.1.1` permits an attack against an ally
that the attacked player controls; physical ownership is insufficient. The
special trigger actor/target cases retain their occurrence controllers, and the
submitted tests distinguish those snapshots from later control changes.

## Closed finding: lost explicit target substitution

The finding below is closed by source repair
`b001e377de3af27c2c77119145949a571868a6db`, independently reviewed as
`20c524bab05d3bf914323e4f71748796aede4afb` in this worktree. The repair checks
whether `Against` was explicit, requires its resolved target to remain in play,
and uses the existing captured reference to reject a different incarnation.
An omitted `Against` retains its default identity target. Current-controller
lookup and the special trigger actor/target snapshot-controller branches remain
intact.

`AbilityResolutionChoices.Chose` reconstructs the order description before
answer assessment and `TrackResolution`. That description calls `Activation`
with the current enemy list, reaches the guard and raises before answer state
is committed. The documented engine choice is an unsupported stale continuation,
reported through `RulesNotImplementedException`; no general rollback guarantee
for committed effect execution is implied.

The original finding at `fd23bf88ab51d12bda7f67d2301dd0214fe02904` was:

`AbilityStructuralPowerExecution.Activation` resolves an explicit `Against` to
null when the selected incarnation is gone. `ActivationSeat` then returns the
ability player, and scheduling records `Character = -1`. `Attack.Prepare` resolves
that sentinel to the ability player's identity. These operations silently change
both attacked player and character. This null fallback predates the reviewed
repair; it is outside the changed non-null controller lookup.

The focused regression uses the same synthetic ranked ally choice as the source
packet, followed by ordering two minion attacks. A player-1 ally owned by player
0 is accepted; the attack order correctly belongs to player 1. While the order is
pending, that ally leaves play and reenters, making a new incarnation under
`rr:leaves-play.1`. Answering the original order schedules both attacks with
`Character = -1`, `Seat = 0`, attacking the resolving player's identity instead.
The test's no-attack assertion fails. This is an accepted-choice continuation,
not an unbound selector in a malformed program. The between-prompt departure is
an adversarial synthetic state change, not a claim of a complete played card.

Repair acceptance must distinguish omitted `Against` from a lost explicit
binding; preserve the original selected incarnation; never follow a reentered
copy or substitute an identity; and preserve state, RNG, effects and pending
work when rejecting the stale order answer. The exact supported rejection or
explicit unsupported-execution contract must be documented and tested. No
production repair was included in the initial reviewer packet.

## Evidence

Logs: `C:/Users/mggar/Source/marvel-evidence/hulk-selected-owner-review/`.
`selected-order-probe.log` records a successful Release compilation followed by
one failing regression and 22 passing cases: the source's complete selected-ally
and typed-activation classes, plus two positive boundary controls. Omitting
`Against` schedules and fully drains an attack against resolving player 1,
placing 3 damage on that identity and none on player 0. A scenario-controlled
explicit target raises `RulesNotImplementedException` with `no attacked player`
before scheduling any attack. `boundary-probe.log` records earlier exploratory
null-selector probes; those probes are not retained as regression scope.

Independent closure command, run against
`20c524bab05d3bf914323e4f71748796aede4afb`:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~SelectedCharacterActivationBoundaryTests|FullyQualifiedName~SelectedAllyActivationTests|FullyQualifiedName~ActionAbilityTypedActivations'
```

`closure.log` records a successful Release compilation and **24 passed, zero
failed**. The original regression now covers departure with and without reentry,
checking digest, RNG state and consumed words, effects, current frame and
occurrence, pending work, final incarnation, no damage and no scheduled attack.
The same run covers the omitted-target and scenario-controlled-target controls,
selected-ally controller and defense cases, and the complete typed-activation
class including trigger snapshots. No additional production finding remains
within this bounded repair. The source packet's compiled guard mutants are
recorded separately in its evidence document; they were not rerun for closure.

This evidence neither implements nor verifies Clash of the Titans. Ranked-pair
binding, full ties, stun replacement, actual-card surge and public admission
remain outside this review. Normal closure commit hooks are recorded separately
in `closure-commit.log`.
