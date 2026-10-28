# Giant Strength turn period independent review — MARVEL-874

Reviewed source: `7e40f6b03df0417250ee73848f5f5332a9f3e4bc`, based on
`9a60c9eeee400f86831411761069c078f377c1c3`. This is a nonpublic engine
prerequisite review; it does not admit Ant-Man.

## Authority and review

Generated card `12009` gives Ant-Man +1 ATK until the end of this turn after
changing to Giant hero form. Current RR 1.8 `rr:lasting-effects.6` restricts
initiation to the specified period, and `.5` expires the effect at its ending
point. The older official `ruling:b777f490f7a0b34d` agrees that no turn is
taken during the villain phase, so Giant Strength has no effect there.

The shared guard covers admission and reached execution. Opening at turn start
and closing in the turn-ending occurrence separates ending interrupts from
responses, and the source tests cover a normal turn, another player's turn,
turn-end response, player-phase end, villain phase and elimination flag. Attack
and activation guard branches are unchanged. The extracted agenda propagation
methods preserve their original loops, conditions and updates; their access to
the agenda's internal frame list and public extension visibility support the
existing call sites. No state digest, schema or production ability book changed.
The flag is reconstructed by normal game progression, but no independent save or
replay round trip was executed in this review.

## Finding: expiration on active-player elimination

`GameTurnEnd.FinishTurn` bypasses the turn-ending occurrence when the active
player is eliminated and calls `AdvanceTurn` directly. The latter closes the
new flag but does not call `PhaseEnd.EndPlayerTurn`, which expires registered
`EndOfTurn` effects. A surviving player's Giant Strength initiated off-turn can
therefore remain active into their own turn. The source elimination test checks
only the flag and phase, so it does not distinguish that outcome.

`EliminatedActivePlayersTurnExpiresSurvivorsStrength` exercises the real game
boundary with the actual Giant Strength face and a test-local interpretation:
seat 1 changes form and resolves Giant Strength during seat 0's turn; seat 0 is
then eliminated, and seat 1's ATK must revert before their turn. The existing
effect's expiration, not merely admission of a new effect, is the observation.
On the reviewed source, the focused `GiantStrengthPeriodTests` run compiled and
reported **8 passed, 1 failed**: the new test reached the next active seat and
observed ATK 4 where 3 was expected. Raw output:
`C:/Users/mggar/Source/evidence/ant-turn-independent-elimination.log`.

Verdict: **reject pending correction and independent retest** of this boundary.
The source packet's own reported evidence is in
`C:/Users/mggar/Source/evidence/ant-turn-period-packet.txt`, including 44 focused
content tests, 34 rules tests and five compiled mutants. The larger 50/52 run
contains two separately handled sparse-deck fixture failures. Those are not
attributed to this change.

## Repair review

Reviewed repair `c4fa7b9d52296c4a4d1ac2d325f599f0defe09b0` on the above
review commit. It removes the active-elimination shortcut, so the existing
turn-ending occurrence handles interrupts, expiration, delayed work and
responses before advancing to another surviving seat. RR 1.8
`rr:player-elimination.step.5` says the remaining players continue; combined
with `rr:lasting-effects.5` this supports ending the elapsed turn and expiring
its effects. The explicit normal ending windows are the engine's existing
sequencing applied to that boundary; the elimination clause does not itself
spell out their scheduling.

The original actual-card regression is unchanged. Additional controls cover
elimination of a non-active player without ending the turn; elimination of the
last seat to take a turn; and a survivor's forced interrupt and response to the
eliminated seat's turn ending. The implementer's compiled original-shortcut
mutant failed four cases and a direct-expiration-without-windows mutant failed
both window controls. The packet reports restored Content 52/52, Rules 44/44
and passing normal hooks; see
`C:/Users/mggar/Source/evidence/ant-turn-elimination-repair.txt` and its logs.

The new occurrence also consumes a scheduler occurrence ID on a path that did
not previously consume one. Subsequent event causes and ability receipts can
expose those IDs, and session replay verifies serialized events exactly. The
integrator must resolve replay identity and compatibility on the final combined
candidate under the MARVEL-870 publication gate, alongside its form, hand-size
and ally-completion changes. Include a genuine predecessor trace with a
survivor's action after elimination; the behavioral repair alone does not
establish unchanged replay.

Independent rerun on `c4fa7b9d`: GiantStrengthPeriodTests, PlayerTurnTests,
PlayerPhaseTests and PhaseAbilityLimitTests **52/52 passed**; PhaseEndTests,
PlayerPhaseEndTests and EliminationTests **44/44 passed**. Logs:
`C:/Users/mggar/Source/evidence/ant-turn-closure-independent-content.log` and
`C:/Users/mggar/Source/evidence/ant-turn-closure-independent-rules.log`.

Verdict: **accept the bounded engine behavior** after the repair. The original
expiration finding is closed; the MARVEL-870 replay publication gate remains
open for the integrated candidate.
