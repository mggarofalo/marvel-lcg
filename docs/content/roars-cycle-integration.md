# Rise of Red Skull cycle integration

The canonical delivery records are MARVEL-568 (Kang) and MARVEL-569 (Ant-Man),
within MARVEL-703. This candidate combines reviewed engine prerequisites; neither
pack has complete executable content or public admission. The production book
contains 738 rows, including 15 of Ant-Man's 35 faces and no Kang faces.

The integrated sources are Kang `07fd360d`, Ant-Man `f6357d29`, the independently
strengthened baseline fixture repair `3a7bb2ec`, and the Resize projection closure
`def6a7ed`. Foundation merge `1b8acd57` preserves both stage-transition suspension
and form-change completion in their shared interpreter paths. Fixture integration
is `69fa1a2e`; the subsequent projection merge has no textual conflicts.

The five-face content packet `40bc0d6a`, independently reviewed at `5902b564`,
adds Ant-Man's three identity faces, Resize and Helmet. Its 62 focused review
checks cover target legality, response and draw ordering, all six form changes,
and multiplayer ownership. Synthetic blank-reading controls do not establish a
reachable effect that blanks these card kinds. The merged exact inventory retains
Power Gloves and all five new faces. Giant Strength remains unauthored.

## Combined verification

The tested staged tree is `b0d57f65b7e1c36420edfb4cdcb73f9e03120bb6`, before the
five-face content packet and this evidence document were added. Windows Release tests used
`MSBUILDDISABLENODEREUSE=1` and `-m:1`:

- Full Content: 8,483 passed, 10 failed, none skipped; 8 minutes 25 seconds.
- Full Rules: 1,192 passed, no failures or skips; 17 seconds.

External logs are `C:/Users/mggar/Source/evidence/roars-integrated-content.log`
and `roars-integrated-rules.log`. This is **not a green integrated candidate**.
The independently passing source packets do not replace this combined result.

Failures concern Superhuman Strength and Indomitable (`CoreCombatResponseTests`),
two Energy Channel cases (`CorePlayerSelectionTests`), repeated Assess
(`IronManHandSizeMaximumTests`), Legal Practice (`CoreFinalCardBatchTests`),
stunned-target Press the Advantage (`StageEntryContinuationTests`), Lead from the
Front (`CoreLastingEffectCardsTests`), stacked Cosmic Flights
(`CoreDamagePreventionTests`), and the pinned Core winning-transcript digest.

Source `54a8869f` proves that all nine sparse fixtures terminate through
player-deck reset and an empty encounter deck. Minimal reserve cards preserve
their original hands, maximum-X values and behavioral assertions. Independent
review `e8e49d10` passes 81 relevant tests, including the complete fixture and
defeat-transcript classes and terminal cleanup controls. The genuine winning
transcript has one reviewed state difference: final Uppercut remains in the
resolving area because victory stops its cleanup discard. Its pin and explicit
event checks now reflect that authority-derived result; the transcript and
historical artifacts are unchanged. See [the repair and review](roars-terminal-fixture-repair.md).
At integrated revision `b13de4fa`, 1,654 affected Content tests pass, with no
failures or skips (`roars-reviewed-combined-focused.log`). The final full combined
suite still needs to run after the remaining reviewed packets are integrated.

The end-turn period repair `c4fa7b9d`, independently accepted at `34395423`,
closes MARVEL-874's engine prerequisite. Eliminating the active player now runs
the ordinary end-turn windows and expiry before advancing. Independent checks
pass 52 Content and 44 Rules tests; the merged Resize and Giant Strength fixture
classes pass all 18 tests (`roars-end-turn-merged-focused.log`). Giant Strength's
production row remains unauthored. The changed post-elimination action occurrence
sequence remains part of publication/replay gate MARVEL-870; this narrow engine
acceptance does not settle replay compatibility.

## Next delivery

Integrate the independently reviewed signature-action packet, then author Giant
Strength against the accepted turn-period engine. Verify Giant Stomp's sequential
targeting composition before assigning any engine expansion. Complete
remaining Ant-Man and Kang content, setup,
publication/replay boundary MARVEL-870, public journeys and relevant native/UI
evidence before claiming either pack complete.
