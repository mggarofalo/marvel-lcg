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
