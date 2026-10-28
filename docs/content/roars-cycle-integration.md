# Rise of Red Skull cycle integration

The canonical delivery records are MARVEL-568 (Kang) and MARVEL-569 (Ant-Man),
within MARVEL-703. This candidate combines reviewed engine prerequisites; neither
pack has complete executable content or public admission. The production book
contains 746 rows, including 23 of Ant-Man's 35 faces and no Kang faces.

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
Power Gloves and all five new faces.

Signature-action source `9e51ef2e`, independently reviewed at `cd1763139`, adds
Wasp, Hive Mind, Army of Ants and Wrist Gauntlets. Source verification passes
121 affected checks and distinguishing mutations; independent review passes
22 focused cases, including declined Wasp responses, wrong-target retry and
invalid wild-resource declarations through `Game.Resolve`. The four rows leave
the previous book entries unchanged. See [source and independent review](ant-man-signature-actions.md).
Merged revision `9f93ee9d` passes all 138 selected Ant-Man, ability-data,
form-destination and Giant Strength period checks; normal commit hooks pass.
The result is recorded in `roars-signature-merged-focused.log`.

Form-response source `8650fb4a`, independently accepted at `575a462c`, adds
Giant Strength, Moxie and Lay Down the Law. All 13 turn-period cases now use the
production Giant Strength row. The review observes Moxie's three bonuses during
a real villain phase and their expiry in the next live player turn, and verifies
Lay Down the Law's selected scheme. Six source behavior mutations and the
reviewer's omitted-round-expiry mutation are distinguished; restored independent
checks pass 83/83. See [the review](ant-man-form-response-independent-review.md).

Muster Courage source `d812dc88`, independently reviewed at `3c027ee9`, adds
`12032` using existing stage-based conditional branches. No dynamic-selection
engine extension is needed. Review covers current versus prior and active versus
first villain, all-tough versus one eligible recipient, and incomplete-pack
exclusion over both transports: 44 Content and nine catalog transport tests pass.
See [the source and review](research/remaining/ant-man-team-stage-composition.md).
The candidate also incorporates main `0dbd5824`, including the independently
certified Fear No Evil printed catalog; those research faces add no runtime
admission.

The combined main/Muster tree `77c0a51c69c3ae2663f5bfa417eb95e2d62f2a98`
passes all 68 selected Muster, ability-data, supplement, printed-correction and
setup-dataset checks. Offline card regeneration also matches both generated
files. Logs are `roars-muster-main-focused.log` and
`roars-muster-main-generator.log`; the final complete suite remains pending.

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
production row is included in the later form-response packet. The changed post-elimination action occurrence
sequence remains part of publication/replay gate MARVEL-870; this narrow engine
acceptance does not settle replay compatibility.

## Next delivery

Verify Muster Courage and updated main on the combined candidate. Test-only probe
`364660e1` establishes two Giant Stomp
boundaries under MARVEL-875: nested live choice inside one attack is refused,
and a splash followed by fixed villain damage omits that villain's per-enemy
attack-end response. A damage/completion control is not full attack correctness.
Repair and independently review recipient facts, then the live-choice continuation.
Complete
remaining Ant-Man and Kang content, setup,
publication/replay boundary MARVEL-870, public journeys and relevant native/UI
evidence before claiming either pack complete.

Recipient source `deac79b5`, independently reviewed at `89702db2`, repairs the
bounded fixed-target attack case: every recipient retains an attack-end response
and retaliation eligibility, while repeated damage accumulates for the same
physical incarnation. Independent verification passes 34 affected Content tests;
Tough-prevention probes and a compiled omitted-retaliation mutation distinguish
zero damage from not attacking. [The review](research/remaining/attack-recipient-review.md)
records code-review and behavioral evidence limits. The live-choice continuation
and its Guard interaction remain unsupported, and MARVEL-870 includes the changed
responses/retaliation in its final compatibility assessment.

The combined 22-face/recipient-repair tree
`3f4d306e3d7a9a4f12c4838b400e91ea9d30e4c1` completes full Content with
8,564 passes and one failure, and full Rules with 1,193 passes, no skips.
The sole failure is the explicit authored-face inventory omitting `12009`,
`12016` and `12031`. The earlier packet's `~AbilityDataTests` filter did not
select that split class. Its independent expected set now includes those three
reviewed rows; no gameplay or completeness assertion is weakened. Logs are
`roars-22faces-full-content.log` and `roars-22faces-full-rules.log`.
This run is not an uninterrupted green suite. After the inventory correction,
all 72 selected ability-data, form-response, turn-period and Giant Stomp
composition tests pass (`roars-22faces-inventory-corrected.log`). The next full
run remains part of final combined verification. The pack-delivery skill now
names the filter covering every split ability-data class; its validator passes.
