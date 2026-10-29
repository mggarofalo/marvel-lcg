# Rise of Red Skull cycle integration

The canonical delivery records are MARVEL-568 (Kang) and MARVEL-569 (Ant-Man),
within MARVEL-703. This candidate combines reviewed engine prerequisites and all
35 authored Ant-Man faces. Ant-Man now has reviewed public setup and complete
product admission in this candidate; Kang does not. The production book contains
758 rows and no Kang faces.
Hank's reviewed entry cases are supported. The owner resolved MARVEL-885:
defeat the zero-HP entering ally before enforcing the ally limit. Source `338cf0fb` and independent review `9598e36d` implement and verify that
adjudication, including the mandatory limit check if an interrupt saves the ally.
See [the independent ordering review](hank-entry-defeat-first-independent-review.md).

Hank Pym source `b10fc2fd`, independently reviewed at `ca6693d2`, preserves
the preceding 755 objects and their raw prefix. Source actual/inventory checks
pass 70 cases; independent checks pass 45 after a nonincremental rebuild.
The cap and health-grant mutations fail their intended controls. Actual entry
with a wholly overpaid Power card yields one counter, while a printed double
resource yields two, consistent with the current cost rule and ruling. See
[the actual-card review](hank-pym-independent-review.md). The earlier candidate `0d5703bc` passes all 8,871 Content tests with no failures or
skips (`roars-33-combined-content.log`, 10 minutes 15 seconds). The entry
production code also has the 1,210 Rules and 22 selected replay results below.
Final pack admission, native interaction and publication checks remain open.

Care for Cassie source `11ec5c48`, independently accepted at `1910cf57`,
preserves all 754 preceding objects and their raw prefix. Source verification
passes 86 Content/data and 12 catalog/admission checks; independent restored
verification passes 56 checks. The reviewer corrected two source citations and
added a physical-card choice control that catches a wrong-first-card mutant
which the six original actual-card cases did not catch. See [actual-card review](care-for-cassie-independent-review.md).

Hank's entry-payment foundation source `f00c8676`, independently accepted at
`1b4eed16`, retains generated and paid resources on the exact entry occurrence,
including Make the Call's separately paid ally. Independent receipt controls
pass 20 cases. The Make the Call path now forwards the accepted resource
allocation. See [payment review](hank-entry-payment-independent-review.md).
The separate MARVEL-887 entry operation is now independently accepted at
`77f4559a` from source `39927976`; health settlement is independently accepted
at `5b7e2b48` from `f0198165`. Both are combined with the excess-resource expression and its reviewed
admission repair in the working candidate. Own-source interrupts precede entry and
the ally-limit choice precedes responses; a lethal entering ally follows the
existing defeat lifecycle. The zero-health/excess-limit collision is now governed
by the owner's defeat-first adjudication in MARVEL-885, implemented in this
candidate after independent review. See [entry timing review](ally-entry-interrupt-independent-review.md)
and [health review](ally-entry-health-independent-review.md).
The combined Cassie/receipt candidate passes 121 focused Content/data checks
and all 1,210 Rules tests. Logs are `roars-32-combined-{content,rules}.log`
under the external evidence directory; the tested tree is recorded in
`roars-32-combined-tested-tree.txt`. Final pack/native/replay checks remain open.

The entry excess expression is independently reviewed at `fc300a97`. A full
combined Content run then reports 8,814 passes, 31 failures and no skips
(`roars-entry-combined-content.log`). Sixteen failures concern counter-target
admission before a future healing result or unstable area selection. Repair
`48a0d2f9` was rejected by a paired undamaged/damaged healing control; refinement
`056559cb`, independently accepted at `b86db252`, passes 93 affected source
checks and 51 independent checks, with compiled mutations distinguished. See
[repair review](entry-resource-excess.md).

The other fifteen failures concern entry fixtures asserting state before queued
entry completes. Their timing corrections retain placement, pricing, control
and limit assertions. One invalid event in an ally fixture is replaced by an
actual ally. Two low-level Make the Call tests now use the complete sequence
answer path instead of leaving consumed continuations queued. The initial
90-case focused run passes 86; after those two fixture corrections all nine
Core lasting-effect cases pass. The final combined focused run passes all
157 cases (`roars-entry-combined-final-focused.log`), including the repaired
counter paths and entry fixtures. The subsequent full Content run including
Hank passes all 8,871 tests, closing these regressions.

Full Rules verification initially passes 1,195 and fails fifteen. Synthetic
allies now have explicit positive health and tests advance queued entry before
asserting its outcome; the restored full Rules suite passes all 1,210 cases
(`roars-entry-rules-fixtures-run1.log`). The lead reviewed this test-only diff
and retained negative controls. Twenty-two selected Server entry/payment,
variable-resource and live-effect replay checks pass
(`roars-entry-combined-replay.log`). These do not close MARVEL-870 publication.

Tech blanking source `95021926` has independent review `c8e1bcb5`. The source passes 68 focused Content checks and 1,210 Rules checks; the independent protection review passes 43 checks after a compiled cross-set Permanent mutation and exact restoration. This mechanism is combined with the defeat-first implementation. The prerequisite candidate `d5df33de` passes 134 focused Content/data checks, all 1,210 Rules checks and 22 selected Server replay checks (`roars-defeat-tech-combined-{content,rules,replay}.log`).

Actual `12026` source `891a1be2` and independent review `d0e7234a` are now combined. The source passes 68 focused checks and distinguishes a no-op row mutation; the review passes 36 checks and independently verifies all 756 preceding objects and their 767,344-byte prefix. Its additional control removes threat through scheme defeat and verifies blanking ends only on defeat. See [actual Tech Theft evidence](tech-theft-actual.md). The combined full Content run passes 8,898 cases and fails one stale Ant-Man reprint inventory list that omits the new12026 row, with no skips (9m55s, `roars-34-combined-content.log`). Adding only12026 to that strict expected list leaves gameplay assertions unchanged; all103 affected inventory, Hank, Tech and Plan checks then pass (`roars-34-inventory-corrected.log`). This is not a claim of a subsequent all-green full run; final complete-pack verification remains. See [the protection review](tech-blanking-independent-review.md).

Team-Building Exercise's scoped permanent-play prerequisite source `73363f70` is independently reviewed at `bff0af9c`. The reviewed source passes 1,216 Rules and 66 focused Content checks; the final independent scoped-cost class passes seven checks, including ordinary subsequent full-cost play and one-use modifier consumption. The combined candidate passes90 focused Content/data checks and all1,217 Rules checks (`roars-team-permanent-{content,rules}.log`). Action-event dispatch source `8db24131` is independently reviewed at `5d19b7ee`; the combined candidate passes 144 affected Content checks and all 1,217 Rules checks (`roars-team-event-{content,rules}.log`). The current reachable event inventory is covered by the bounded implementation. Actual `12024` source `92e708fb` and independent review `627cef15` are now combined. The reviewed gameplay tree passes 127 affected checks, including below-cap actual Hank overpayment and an extra-counter mutant. Root integration changes only documentation relative to that tested tree. See [actual-card evidence](team-building-actual.md). See [event review](team-building-event-invoke-checkpoint.md#independent-event-review). See [the permanent-path review](team-building-permanent-independent-review.md).

All 35 Ant-Man faces are authored and independently reviewed. MARVEL-885 has a
verified defeat-first implementation; no further human decision is pending on
that ordering. Actual Plan-to-Tech Theft reveal is verified in the card packet;
starter and public admission source `c526e9d7` is independently reviewed at
`08ff9b12`. Its reviewed gameplay tree passes 130 Content, 29 Server and 25
authority checks; root integration changes only documentation relative to that
tested tree. The three-face setup prerequisite `55d41d26` is independently
reviewed at `39a0ae35`, including all 1,230 Rules tests and an actual catalog
blueprint check. See [admission evidence](ant-man-public-admission.md).
Public journey source `767691e3` and independent review `a602f39a` now prove the
pending Giant response through a fresh host, invalid/stale rejection, authorized
undo/redo and subsequent attack against an uninterrupted control. Seven complete
public games replay identically; all are defeats, not victory evidence. Source
passes eight cases and the strengthened independent restart control passes.
See [public journey evidence](ant-man-public-journey.md). Native `add0dfdf`
passes headless and inspected rendered public form/response play; its engine
copy fix removes raw face IDs from destination explanations. See
[native evidence](ant-man-native.md). The public native case is registered in
both CI profiles; manifest checks pass three cases, with platform execution still
pending. Team-Building/Hank payment interaction evidence, final integrated
managed/platform checks and MARVEL-870 replay publication remain open.

Earlier integration checkpoints below describe the revision tested at that
point; their older inventory counts and pending tasks are historical.

Actual Yellowjacket's Plan source `76758101`, independently accepted at
`872b6d0a`, preserves all 753 previous objects and their byte prefix. Source
verification passes 98 focused checks and a wrong-set runtime row mutation;
independent actual-card verification passes seven cases including cancellation
and cleanup. The actual Plan-to-Tech Theft path remains a complete-pack
requirement once `12026` is authored. See [the content review](yellowjacket-plan-independent-review.md).

MARVEL-884 next-own-turn prohibition source `5a64ab1d`, independently accepted
at `836db363`, uses the existing beginning/end occurrence bodies, keeps each
registration tied to its seat, and combines expiry with existing settlement.
Source verification passes 38 affected Content and 43 Rules cases; independent
staggered-registration/reentry checks pass two and distinguish a compiled
premature-arming mutation. The beginning-interrupt boundary is derived from the
current general timing rules, not a card-specific ruling. Independent review
explicitly accepts that derivation; it is not a human approval requirement.
See [the review](marvel-884-duration-review.md). No actual `12025` row is added
by this prerequisite. The combined candidate passes 113 focused Content/data
checks and all 1,210 Rules tests in `roars-31-combined-{content,rules}.log`.
The tested tree is recorded in `roars-31-combined-tested-tree.txt`.

Yellowjacket and Size Increase source `8f4053ba`, independently reviewed at
`6f508f74`, add two actual nemesis faces. Review includes both physical Size
Increase copies on one host and passes 61 focused checks. The spent-resource
response mechanism `8c6c04df`, independently reviewed at `1ba9161d`, is integrated
for Pym-style spent-source responses; its 15 independent checks cover nested
payment, multiplayer priority and incarnation rejection. The actual Pym row is now independently accepted at `fee19693`. See [nemesis evidence](ant-man-nemesis-cards.md) and
[spent-resource review](resource-card-spend-independent-review.md).

The full Content run on the spent-resource source exposed 50 inherited Power
failures: Make the Call affordability, dependent Core transcripts, and an obsolete
For Justice quote assertion. Baseline comparison reproduced the affected cases
before this mechanism and passed them before Power activation. Repair `07e6b32f`,
independently accepted at `8836ebba`, uses allocation-based affordability and
restores the explicit resource-port contract. Independent verification passes
18 affected Content and 129 Architecture tests, with a compiled old-default
mutation rejected by the architecture gate. See [repair review](power-regression-repair-independent-review.md).
Combined full Content verification reports 8,752 passed, one failed and no skips
in `roars-29-combined-content.log`. The sole failure is another obsolete Power
quote assertion in Captain America's reprint test, also present in the earlier
full-run log. Its corrected assertion checks the base icon, possible output and
exact conditional bonus/mask for matching and nonmatching cards. Restored focused verification passes 96 affected Content/data tests; the
combined full Rules and Architecture suites pass 1,202 and 129 respectively.
Logs are `roars-29-combined-{focused,rules,architecture}.log` under the external
evidence directory. The full Content run preceded the assertion-only correction;
these results do not claim a second complete Content run. The earlier
50 failures comprise 46 dependent transcripts, two Make the Call cases and two
obsolete quote assertions (For Justice and Captain America).

At that earlier checkpoint, four Ant-Man faces remained: `12011`, `12024`, `12025`, and `12026`.

Actual Pym Particles source `3d16f976` is independently accepted at `fee19693`.
All 752 prior objects and their raw prefix are unchanged. Source verification
passes 61 Content/data and 12 Server catalog/admission tests; independent review
passes nine actual-card cases including non-first-player effects. Valid runtime
row mutations distinguish healing, drawing and recipient ownership, followed by
restored non-incremental builds. See [the content review](pym-particles-actual-independent-review.md).

Encounter-set discard source `c8e19d94`, independently reviewed at `14b2a06e`,
adds MARVEL-883's prerequisite for Yellowjacket's Plan. Source 29 Content/nine
Rules and independent 25 Content/11 Rules checks pass. The review distinguishes
a compiled wrong-deck routing mutation with paired-deck controls. These are
synthetic prerequisite probes; no actual `12029` row or played Kang scenario is
claimed. See [the review](encounter-set-discard-independent-review.md).
The combined candidate passes 98 focused Content/data checks and all 1,205
Rules tests in `roars-30-combined-{content,rules}.log`. The tested tree is
recorded in `roars-30-combined-tested-tree.txt`; full Content/native/publication
checks remain scheduled at the complete-pack boundary.

Stinger and Moment of Triumph are independently accepted at `ef9ff6f8`, after
the response explanation repair `1eccb891` and its independent review `c868fa51`.
The integrated fast-forward has exactly the reviewed tree
`460713e7c6ce03648e7abdbf36f845b1f2fbae50`. Independent content/data checks pass
58 cases and catalog/admission checks pass 20; the original 748 objects and raw
book prefix remain unchanged. See [the content review](ant-man-stinger-triumph-independent-review.md).

Actual Power generation source `c1363aea` is independently reviewed at
`cadf296e` and combined here without textual conflicts. Its source checks pass
57 Rules and 818 Content cases; independent payment checks pass 29 and catch a
compiled one-paid/one-excess mutation. The [review](power-resource-activation-independent-review.md)
records the synthetic ten-resource search measurement and remaining limits.
Combined Windows Release verification passes 866 affected Content checks and
69 Rules checks in `roars-power-content-integration.log` and
`roars-power-rules-integration.log` under the external evidence directory.
Native conditional-resource interaction, final whole-candidate verification and
MARVEL-870 replay publication remain open. These rows do not admit the pack.

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

Ally-limit source `c4ac17bf` was independently challenged at `eaeebd0f` by a
queued choice whose limit was restored before it ran. Repair `6ac0b2f`, accepted
at `9e7290e2`, completes that obsolete plan without losing its entry continuation.
Excluded allies do not count, but remain eligible discard choices; discarding
one can leave another discard required. The integrated prerequisite passes 101
selected Content and 110 Rules checks in `roars-stinger-integration-*.log`.
The tested tree is recorded in `roars-stinger-tested-tree.txt`.
[Independent closure](ally-limit-exclusion-independent-review.md) distinguishes
the synthetic interposed timing and blanking probes from demonstrated gameplay.
The actual Stinger row is the next content task; the shared prerequisite does
not itself change the 25-face count or admit the pack.
