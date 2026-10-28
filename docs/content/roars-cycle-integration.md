# Rise of Red Skull cycle integration

The canonical delivery records are MARVEL-568 (Kang) and MARVEL-569 (Ant-Man),
within MARVEL-703. This candidate combines reviewed engine prerequisites; neither
pack has complete executable content or public admission. The production book
contains 748 rows, including 25 of Ant-Man's 35 faces and no Kang faces.

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

Muster Courage and updated main have passed the combined focused checks above.
Test-only probe
`364660e1` establishes two Giant Stomp
boundaries under MARVEL-875: nested live choice inside one attack is refused,
and a splash followed by fixed villain damage omits that villain's per-enemy
attack-end response. A damage/completion control is not full attack correctness.
Recipient facts and the live-choice continuation have since been repaired and
independently reviewed below, followed by the actual-card packet.
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
records code-review and behavioral evidence limits. The subsequently reviewed
live-choice continuation is recorded below, and MARVEL-870 includes the changed
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

Shared simultaneous defeat source `62269caa`, independently accepted at
`c9515472`, groups attack recipients through would-defeat, when-defeated and
departure windows. Production Shield Toss against two Hydra Soldiers establishes
the original missing order prompt without relying on a draft card. Independent
verification passes all 1,193 Rules tests and the 1,015 existing affected Content
cases. An added reentry fixture required an assertion correction; its corrected
case passes and kills a compiled stale-incarnation mutant after restoration.
The broader Content filter was not repeated after that test-only correction.
[The review](simultaneous-attack-defeat-review.md) records exact limits and logs.
This closure unblocks Giant Stomp's continuation work, not its production row or
pack admission. MARVEL-870 must account for the new order prompt and potentially
changed encounter-card assignments before main merge or publication. Internal
candidate integration proceeds with that gate explicitly open.
The integrated staged tree `15d345cc31856e30caa5deca9ec84e005aa23f3a` passes
48 simultaneous-attack and ability-data checks in
`roars-sim-defeat-merged-focused.log`. This focused result supplements the
source review; a final complete combined run remains required.

Giant Stomp's bounded live-choice capability is implemented at `e1f5f267`, with
cohesive test-layout closure `bae99bb0` and independent review `38ce4bca`.
Its unchanged two-Hydra defeat-order test passes on the shared Rules repair.
The reviewer adds stale-incarnation rejection and retry evidence, and accepts
the targetless attack description correction `2fa6b3d4`. Restored independent
verification passes 57 affected Content and 66 View checks; normal hooks pass.
See [the exact review and limits](giant-stomp-live-choice-review.md).
The actual `12003` row follows in the content packet below. Swarm's separate
form/ready and Team-Up prerequisites are recorded below. These engine packets
retain the 23-face inventory preceding that content packet and add no public admission.

The Giant Stomp integration `78b62715` passes 90 selected Giant Stomp, Melee,
counted-attack and ability-data checks and normal commit hooks. The focused log is
`roars-giant-integration-focused.log`; the staged source tree is retained in
`roars-giant-integration-tested-tree.txt` under the external evidence directory.

Swarm form/ready source `a3aad554` is independently accepted at `a7973b01`.
The review adds prohibited-form-change/independent-ready controls and compares
extra offer queries against same-seed unprobed execution, including complete
events, RNG consumption, pending work and a subsequent nonempty offer. All 84
focused checks pass; no production source changes were needed in review.
[Its record](swarm-form-ready-evidence.md) preserves mutations and limits.
Team-Up event source `b4726685` is independently accepted at `1c21233a`, including
stored-event action/interrupt eligibility. Restored review passes 50 affected
Content and 69 normal card-play checks, with a distinguishing compiled stored-zone
mutant and normal hooks. [The record](team-up-event-admission-evidence.md) covers
the captured-offer rejection and in-play-trigger boundaries. These sources are
combined for full Content and Rules verification before the actual `12020` row.

The combined staged tree `33bba469c53c6944f75c1a46e7edfac22d135085` completes
Content with 8,640 passes and one failure, and Rules with all 1,193 passing,
without skips. The sole failure is the earlier composition probe expecting
Swarm's now-supported form/ready sequence to raise. It is updated to assert the
actual form change, readying and paid-card discards while retaining every original
query-purity check and its same-cost draw control. Logs are
`roars-three-prerequisites-full-content.log` and
`roars-three-prerequisites-full-rules.log`. This is not claimed as an
uninterrupted green full run; the corrected probe and affected paths require their
focused result, with final full verification still due before publication.
After the fixture correction, all 168 selected composition, Swarm, Team-Up,
form/projection, Giant Stomp and ability-data tests pass in
`roars-three-prerequisites-corrected-focused.log`. The independent Swarm reviewer
also inspected the fixture-only change and found no weakened assertion or
production defect: the former refusal no longer applies to this supported shape,
and the retained draw control distinguishes both final face and readiness.

Actual-card source `6a10f07a`, independently accepted at `62eac154`, adds Giant
Stomp `12003` and Swarm Tactics `12020`. The previous 746 ability objects and
their 755,307-byte prefix are unchanged; the book now has 748 rows and 25 Ant-Man
faces. Tests use the actual rows for live-choice, form/response and Team-Up action
behavior; unrelated synthetic variants remain explicitly replaced and labeled.
Source restored verification passes 192 selected checks after distinguishing both
row mutations. Independent review passes 104 Content/data and 12 server catalog
and admission cases, including both transports' exclusion of these incomplete-pack
cards. Compiled catalog-leak controls distinguish that exclusion.
See [the content review](ant-man-giant-swarm-independent-review.md).
The remaining Ant-Man faces are `12006`, `12011`, `12014`, `12024`, and
`12025` through `12030`. Resource generation prerequisite MARVEL-878 is confirmed
by a separate diagnostic; spent-card responses and Hank's entry receipt still
need their own implementation and proof. No public pack admission is added here.
The merged two-row candidate passes all 192 selected Content/data checks in
`roars-25-faces-focused.log`; its tested staged tree is recorded in
`roars-25-faces-tested-tree.txt`. Final complete verification remains scheduled
after the remaining content and shared engine work is integrated.

Event allocation source `983ae1f0`, independently accepted at `f75ca018`, now
preserves resolved costs and source allocations through shared payment and
validates actual generated resources before publishing successful paid facts.
The independent control preserves resources paid for an event's arrow cost as
resources paid for that card. The merged candidate passes all 190 selected
payment and continuation checks in `roars-event-allocation-integration.log`.
See [the source record](event-allocation-commit.md) and
[independent review](event-allocation-independent-review.md). The deliberately
inconsistent generator probe does not establish a reachable authored failure or
rollback after costs commit. Conditional Power generation remains MARVEL-878;
this prerequisite adds no Ant-Man faces or public admission.

Attack-excess source `672ac1ad`, independently accepted at `3b3f3558`, retains
settled excess for each exact attack recipient through basic, authored and
simultaneous damage. The integration branch fast-forwarded to the reviewed tree;
`roars-attack-excess-identical-tree.txt` records the tree comparison. Independent
review passes 52 affected tests, including prevention and repeated placements,
and distinguishes a compiled requested-damage mutation. Replacement and further
causality cases remain explicit limits in
[the review](attack-excess-independent-review.md). No actual Moment of Triumph
row is admitted by this prerequisite.

Conditional-resource source `19795428` was rejected at `c00af293` for malformed
bonus admission and its missing protocol boundary. Repair `aaedbb07`, accepted
at `01f9ebca`, requires and validates the bonus fields and establishes protocol
43 with protocol 42 rejected before gameplay through both transports. The
ordinary resource JSON spelling is preserved. Suspended capture, composer parity
and malformed-field controls pass; see
[the closure](conditional-resource-independent-review.md). Actual Power-card
generation and native evidence for its reachable interaction remain outstanding.

The combined excess/conditional candidate passes 101 selected Content, 54 Rules,
45 Server and one Decisions test, with no failures or skips. Logs are
`roars-conditional-excess-{content,rules,server,decisions}.log`; the tested tree
is in `roars-conditional-excess-tested-tree.txt`. The book remains at 748 rows
and 25 Ant-Man faces. New attack-recipient metadata and conditional payment
behavior enter the final MARVEL-870 replay assessment; no engine replay version
change or historical archive compatibility is claimed here.
