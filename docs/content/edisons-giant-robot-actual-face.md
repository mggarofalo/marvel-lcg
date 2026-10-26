# Edison’s Giant Robot actual face (MARVEL-734)

Base: `619992df` (reviewed blanking prerequisite integrated from `25c9b453`).
Actual row `05028` is appended to the book: 573 unique authored faces, 31 of 34
Ms. Marvel faces. The complete prior book prefix remains byte-identical through
its final row; only its closing list delimiter is extended. Explicit authored-face
inventory adds only `05028`. No generated facts, engine or public admission change.

Printed authority is `datasets/cards/cards.json`: damage cannot be taken; Hero
Action spends one mental resource to blank the printed text box until phase end.
Vendored `ruling:d1087a7e030d2fd9` confirms blanking applies to all players.
`preventDamageWhile` expresses the unconditional prohibition; the independently
reviewed standalone `blankPrintedTextUntilEndOfPhase` expresses its action.
The interpreter’s true condition and opcode names are engine representation choices.

`PrintedTextBlankingTests` reads the unmodified full ability book. Both seats can
pay for the Robot engaged with seat zero. Damage is actually prevented before
payment, accepted from the other seat after payment, then prevented after expiry.
Both seats lose the action while blanked. ROBOT, ATK 2, SCH 1 and HP 8 remain.
The blank survives turn end; phase end restores immunity and action offers.
A separate `Game.Resolve` path commits the actual action, ends the turn, resolves
the phase discard decision and proves scheduler restoration with a live next prompt.
Alter-ego cannot use the action. Wrong energy payment preserves both payment cards,
card digest, pending agenda, registered and active effects, and RNG after repeated
offer queries. Actual-row reentry tests retain the existing independent lasting-effect
control. Synthetic variant bodies in lifecycle and preflight review tests remain
synthetic and retain their separate controls; none append a duplicate actual row.

Verification, Windows Release:

- Initial Cards + blanking + citation filter: 179 pass, before the added scheduler case.
- Actual-face class with scheduler case: 7 pass.
- Data mutant changing only actual `05028` cost from mental B to energy Y: 4 fail,
  3 pass. Both seat payment paths reject the wrong mutant cost; the energy-negative
  test fails because energy becomes accepted. The scheduler payment also fails.
- The original data is restored byte-for-byte, then the Content project is rebuilt
  nonincrementally. Final Cards + all blanking + citation filter: 180 pass, zero fail.
- Full Content and combined integration lanes remain lead-owned and were not run here.

External logs under `C:/Users/mggar/Source/`: `robot-actual-focused.log`,
`robot-actual-scheduler.log`, `robot-actual-mutant.log`,
`robot-actual-restored-build.log` and `robot-actual-restored.log`.
Independent actual-row content review remains required. Ms. Marvel public admission
stays closed; the remaining unauthored faces are 05014, 05018 and 05029.
