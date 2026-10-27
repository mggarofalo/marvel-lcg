# Hulk selected-character activation prerequisite

MARVEL-852 Q5/P4 probe, based on `fdd16b0f9f3f7c72631ed1d2033db2f6d06ea386`.
This packet changes ordinary selected-character attack ownership only. It adds no
ability-book row, setup recipe or public admission and does not implement Clash
of the Titans.

Current RR 1.8 `rr:attack-enemy-activation.1.1` permits an ability to attack an
ally controlled by the attacked player. Clauses `.1.2` and `.1.3` make another
defender and its controller the new targets. `rr:first-player.1` assigns tied
encounter targets to the first player. These current clauses govern the probe;
historical `ruling:4579a25303904b85` is not its deciding authority.

`AbilityStructuralPowerExecution.Activation` already resolves `Against` and
`ScheduleActivationsCommand` retains the exact object ID. Its activation seat
now uses the selected character's current controller. Existing trigger actor and
target bindings retain their occurrence snapshot controllers; they deliberately
do not use the character's later controller.

`SelectedAllyActivationTests` uses a synthetic ranked hero/ally choice with
distinct tied allies, one controlled by a player other than its owner. A unique
ranked minion attacks the exact selected ally. The first player differs from the
resolving player. Cases prove the selected controller owns the attack, full
undefended damage reaches that ally, a different player's ally defense changes
the target and recipient, and a departed target answer preserves digest, RNG,
effects and pending work before a valid retry. Every activation drains fully.
The synthetic rows use printed catalog facts but do not establish played pack
or public product coverage.

Verification logs are outside the repository at
`C:/Users/mggar/Source/marvel-evidence/hulk-clash-target/`:

- `before.log`: original fallback compiled; selecting the other player's ally
  failed with attacked seat expected 1, actual 0; same-player control passed.
- `mutant-owner.log`: current-controller lookup replaced by `against.Owner`;
  compiled mutant fails the cross-player selected-ally cases.
- `restored-build.log`: forced non-incremental Release rebuild after restoration.
- `restored.log`: selected-ally tests and complete typed-activation class pass
  (20 cases), including preserved trigger snapshot ownership.
- `commit.log`: normal commit hooks, without bypasses.

The full two-pool composition remains unresolved. This is a traced limitation,
not an executable pair probe: `AbilityEffect.ChooseCard` has no named captured
selection; `AbilityStructuralQueries.AnswerCardChoice` returns a `RunChoice`
with `BindsPlayerSelection: true`; `AbilityResolutionBindings.ChooseSelection`
replaces both chosen and player-selection bindings. A second nested `chooseCard`
therefore does not provide a distinct binding for the first selected enemy.
`that` reads `AbilityQueryContext.Altered`, not an outer card choice. No binding
extension or state-changing workaround is included. Explicit ranked enemy/target
pair selection, enemy ties, unique maxima through full encounter resolution,
stun cancellation and actual Clash surge remain follow-up work.

## Lost-target continuation repair

The independent reviewer probe `1c6f36fa3f2363df641a7fb4adaae046997aacb4`
was applied as `c84caaa4`. `review-before.log` reproduces its compiled failure:
one lost selected incarnation scheduled two attacks against resolving player 0
with the default character sentinel; 22 other focused cases passed.

An omitted `Against` remains the engine's request for the attacked player's
identity. An explicit `Against` now requires its resolved character to remain in
play. The existing captured reference rejects a reentered incarnation; the
in-play check separately rejects a departed character whose incarnation has not
yet changed. No identity or new incarnation substitutes for that selection.

The existing order-description path validates this target before answer
assessment and `TrackResolution`. Engine choice: a stale explicit target makes
that continuation unsupported, raising `RulesNotImplementedException` before
the order answer schedules or commits anything. The pending continuation remains
unchanged; this is not a claim of rollback for arbitrary effect execution after
committed costs. The test proves both departure and departure/reentry preserve
digest, RNG state and consumed-word count, effects, current occurrence/frame,
pending work, and final physical-card incarnation. It also proves no character
damage, live activation or attack is introduced. Between-prompt departure is an
adversarial synthetic state change rather than a claimed complete played card.

Final focused evidence in the same external directory:

- `stale-mutant-null.log`: allow a missing explicit reference through the guard;
  compiled mutant fails the reentered-target case (1 failed, 3 passed).
- `stale-mutant-out-of-play.log`: omit the in-play check; compiled mutant fails
  the departed-target case (1 failed, 3 passed).
- `stale-mutant-omitted.log`: apply the guard to omitted targets instead of
  explicit ones; compiled mutant fails the default-target and stale-target
  controls (3 failed, 1 passed).
- `stale-restored-build.log`: restored Release source built with
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`.
- `stale-restored.log`: 24 cases pass with `dotnet test` on that project,
  `-c Release --no-build --filter 'FullyQualifiedName~SelectedCharacterActivationBoundaryTests|FullyQualifiedName~SelectedAllyActivationTests|FullyQualifiedName~ActionAbilityTypedActivations'`.
  This includes omitted-target identity damage, scenario-controller rejection,
  current-controller ownership, defense replacement and trigger snapshot policy.
- `stale-commit.log`: normal commit hooks, without bypasses.

No ranked-pair binding, full Clash encounter procedure, stun/surge or public
admission is added by this repair. Independent review closure is separate.
