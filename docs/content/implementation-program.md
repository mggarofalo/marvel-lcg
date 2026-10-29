# Remaining content implementation program

**Tracking:** Content Expansion module and [MARVEL-703](https://plane.wallingford.me/dev/projects/cd677658-41d9-4576-b055-5f2a726b3c85/issues/b26d754d-4e0e-486c-8a04-f0e9b968cdb2). **Planning baseline:** `b16183e2`, 2026-10-25. **Current:** nine of 64 canonical packs are merged and agent-accepted; 55 remain. Captain America/Wrecking Crew are in PR #439, Green Goblin/Ms. Marvel in #451, Thor in #452, Black Widow in #453, Doctor Strange in #454, Hulk in #458 and Ronan Kree Fanatic in #459. Hulk merged as `f256e30b` after all eleven CI jobs passed on `0edf7c9a` in run `40474550732`. Core and Rise of Red Skull are the accepted regression baseline outside this expansion queue. MARVEL-630 additive-content compatibility and MARVEL-696 all-pack research remain Done. Separate UI review items do not reopen accepted content.

The [research coverage ledger](research/remaining/README.md), its ten paired narrative/face-inventory packets, [source review](research/remaining/source-review.md), and [independent review](research/remaining/independent-review.md) supply MARVEL-563–622 handoffs. [Green Goblin](research/green-goblin-deep-research.md) and [Ms. Marvel](research/ms-marvel-deep-research.md) have their own deeper packets. Their examples are first slices; the canonical parent acceptance criteria still require every playable face and mode. The current source gaps are recorded below, without turning them into invented card behavior.

## Complete pack-to-wave ledger

The existing Content Expansion module defines these **11 planning waves**. Ranges are inclusive, and a box owns its included heroes, scenarios, modular sets and campaign, even where those components also appear in another deck. This mapping covers each of the 64 canonical parents exactly once; the nine accepted parents are shown only to make the subtraction auditable. A wave is a delivery grouping, not proof of a published release or a command to admit its packs together. Each parent keeps its own implementation, evidence and acceptance state.

| Existing wave | Canonical pack parents | Remaining |
|---|---|---:|
| Core-era expansions | **559 Green Goblin accepted**; **560 Captain America accepted**; **561 Ms. Marvel accepted**; **562 Wrecking Crew accepted**; **563 Thor accepted**; **564 Black Widow accepted**; **565 Doctor Strange accepted**; **566 Hulk accepted** | 0 |
| Rise of Red Skull cycle | **567 Ronan Modular Set accepted**; 568 The Once and Future Kang; 569 Ant-Man; 570 Wasp; 571 Quicksilver; 572 Scarlet Witch | 5 |
| Galaxy's Most Wanted cycle | 573 The Galaxy's Most Wanted; 574 Star-Lord; 575 Gamora; 576 Drax; 577 Venom | 5 |
| Mad Titan's Shadow cycle | 578 The Mad Titan's Shadow; 579 Nebula; 580 War Machine; 581 The Hood; 582 Valkyrie; 583 Vision | 6 |
| Sinister Motives cycle | 584 Sinister Motives; 585 Nova; 586 Ironheart; 587 Spider-Ham; 588 SP//dr | 5 |
| Mutant Genesis cycle | 589 Mutant Genesis; 590 Cyclops; 591 Phoenix; 592 Wolverine; 593 Storm; 594 Gambit; 595 Rogue; 596 Mojo Mania | 8 |
| NeXt Evolution cycle | 597 NeXt Evolution; 598 Psylocke; 599 Angel; 600 X-23; 601 Deadpool | 5 |
| Age of Apocalypse cycle | 602 Age of Apocalypse; 603 Iceman; 604 Jubilee; 605 Nightcrawler; 606 Magneto | 5 |
| Agents of S.H.I.E.L.D. cycle | 607 Agents of S.H.I.E.L.D.; 608 Black Panther (Shuri); 609 Silk; 610 Falcon; 611 Winter Soldier; 612 Trickster Takeover | 6 |
| Civil War cycle | 613 Civil War; 614 Synthezoid Smackdown; 615 Wonder Man; 616 Hercules | 4 |
| Fear No Evil cycle | 617 Fear No Evil; 618 Jessica Jones; 619 Luke Cage; 620 Elektra; 621 Iron Fist; 622 Shadowland | 6 |
| **Total** | **64 parents, including 9 accepted** | **55** |

## Current integration checkpoint

The active Ant-Man/Kang candidate is recorded in
[the cycle checkpoint](roars-cycle-integration.md). It has all 35 authored
Ant-Man faces and 758 total rows, including Hank's reviewed entry
cases, plus reviewed Kang foundations. The owner resolved MARVEL-885 in favor of
zero-HP defeat before ally-limit enforcement. Source `338cf0fb` and independent
review `9598e36d` implement and verify that ordering. Neither pack is publicly admitted. Giant Stomp's live-choice
capability is independently accepted at `38ce4bca` and integrated at `78b62715`,
including the shared simultaneous attack-defeat repair and corrected View wording.
Swarm Tactics' form-change/ready prerequisite is independently accepted at
`a7973b01`; the separate event Team-Up check is accepted at `1c21233a`.
Actual Giant Stomp and Swarm Tactics rows are independently accepted at `62eac154`.
MARVEL-878's Power resource generation source is independently reviewed at
`cadf296e`, with the regression repair independently accepted at `8836ebba`.
MARVEL-882's spent-card response mechanism is accepted at `1ba9161d`; its actual
Pym row is independently accepted at `fee19693`. Nemesis rows are accepted at `6f508f74`. Hank
entry/overpayment is independently reviewed, with the MARVEL-885 defeat-first
adjudication implemented and independently verified. MARVEL-870 owns the
final replay publication boundary, and final combined verification remains open.
The earlier 29-face integration passes 96 affected Content/data, 1,202 full Rules
and 129 full Architecture checks. Its full Content run passed 8,752 cases with
one obsolete Captain America quote assertion; the corrected assertion passes in
the focused run. Final complete-pack verification remains required. MARVEL-883 encounter-set
discard is independently accepted at `14b2a06e`; actual Yellowjacket's Plan is
accepted at `872b6d0a`. MARVEL-884's next-turn prohibition is accepted at
`836db363`; actual Care for Cassie is accepted at `1910cf57`. Hank entry-payment
provenance is accepted at `1b4eed16`; the separate entry interrupt/counter
operation is independently accepted at `77f4559a`, and entry health at
`5b7e2b48`. The excess-resource expression and its admission repair are independently
accepted at `b86db252`; the combined Rules suite passes 1,210 checks and the
selected Server replay suite passes 22. The earlier combined full Content run at `0d5703bc` with
Hank passes all 8,871 tests after the counter-admission and fixture repairs. Hank's actual row is independently reviewed at `ca6693d2`; its source
passes 70 focused checks and its review passes 45, with cap/health mutations
distinguished. The newer defeat-first and Tech blanking integration passes 134 focused Content/data,
1,210 Rules and 22 selected Server replay checks. The subsequent34-face full Content run passes8,898 and finds one stale face inventory list; adding12026 to that expected list is followed by103 passing affected checks. Final
pack admission, full verification, native interaction and publication checks remain open.
Team-Building Exercise source `92e708fb` is independently reviewed at `627cef15`, completing all 35 authored faces. The reviewed gameplay tree passes 127 affected checks, including below-cap Hank overpayment with a distinguishing extra-counter mutant. Printed starter and public admission source `c526e9d7` is independently reviewed at `08ff9b12` with 130 Content, 29 Server and 25 authority checks. The three-face setup prerequisite is independently reviewed at `39a0ae35` with 1,230 Rules tests. Public Tiny/Giant choices preserve one physical identity; characteristic response resolution, fresh-host replay, native and final whole-pack verification remain. Its permanent-play prerequisite source `73363f70` is independently reviewed at `bff0af9c` and combined with90 focused Content/data and1,217 Rules checks passing. Event dispatch source `8db24131` is independently reviewed at `5d19b7ee`; the combined candidate passes 144 affected Content and 1,217 Rules checks. Actual Team-Building Exercise and Hank Pym overpayment integration are verified in the card packet; complete starter and public admission are verified in the admission packet. Actual Tech Theft source `891a1be2` is independently reviewed at `d0e7234a`, including full Plan reveal and scheme-defeat restoration. MARVEL-888 owns live TECH-player-card blanking and
MARVEL-889 owns trait-matching discounted nested play. Their researched
implementation and evidence requirements are recorded under the canonical
Ant-Man parent. The combined Cassie/payment revision passes 121 focused Content/data and all 1,210 Rules checks; final pack/native/replay gates remain open.

The attack-excess prerequisite is independently accepted at `3b3f3558` and
integrated by an identical-tree fast-forward. Conditional-resource contract
repair `aaedbb07` is independently accepted at `01f9ebca`; its protocol 43
boundary rejects protocol 42 peers before gameplay. The combined candidate
passes 201 selected checks across Content, Rules, Server and Decisions. Actual
Power-card generation is integrated with 866 affected Content and 69 Rules
checks passing. The ally-limit prerequisite is
independently accepted at `9e7290e2` and integrated with 211 selected Content
and Rules checks passing. Stinger and Moment of Triumph's actual rows are
independently accepted at `ef9ff6f8` and integrated by an identical-tree
fast-forward, including the separately reviewed response explanation repair.

Fear No Evil's printed catalog is published in PR #465, merged as `0dbd5824`
after all eleven checks passed on `b3e457ff`; source and squash trees match.
Acquisition MARVEL-697 is complete. The 171 added records bring that catalog to
239 IDs; this is printed authority for implementation, not executable content or
another accepted pack. All 64 canonical parents are present. Nine have accepted
content/engine behavior; eight are marked Done because Wrecking Crew retains its
separate UI review. No owner-content-acceptance gate is added.

Main push run `40752980930` also passes all eleven jobs on its second attempt.
The first attempt's Windows server-startup timeout did not recur on the single
unchanged failed-jobs retry; its cause remains unproven and no timeout was changed.

## Accepted baseline evidence

[PR #451](https://github.com/mggarofalo/marvel-lcg/pull/451) is merged at
`71ac4975a3d6899715c0b5e29cf29bb84231a9db`. All eleven CI jobs passed on exact
candidate `d7993ebf` in run `40300681863`, including managed correctness,
Windows/Linux platform and native gates, and the Windows package check.

Ms. Marvel is agent-accepted: all 34 playable faces, verified 40-card starter,
public setup/custom decks, seven complete public games, and exact fresh-host
continuation for Morphogenetics, Bruno and Melee. The local acceptance lane
passed all 150 tests; CI certified the final candidate. Protocol 39 and replay
v25 carry the reviewed semantic and ally-entry changes. Original historical
journal bytes remain unchanged with explicit rejection and separate current
legal replay evidence where corrected behavior differs.

Green Goblin is also agent-accepted. PR #450 admitted all 57 faces, four modes
and four modular sets; PR #451 completes the scenario victory evidence. Risky
Business Standard wins with the printed Captain America starter, seed 1, round
13, 139 decisions. Mutagen Formula Standard wins with a legal Captain America
Leadership deck, seed 3, round 13, 130 decisions. Both have independent review
and exact replay. Existing defeats cover both scenarios in Standard and Expert;
multiplayer, restart and native proofs remain recorded. Environment-layout
review MARVEL-732 remains a separate UI decision, not a content gate.

Thor is merged and agent-accepted in PR #452 at `c14a045c`.
All 35 faces (611 total) have independent review. Complete public admission,
the verified 40-card starter, custom-deck ownership and fresh-host continuation
are integrated with independent setup review. Reviewed engine prerequisites
include engagement, replay v26, typed variable payment, original ally-play
receipts, private inspection, basic-use costs, simultaneous direct-damage
barriers and exact causal-recipient meaning (protocol 40). Current RR 1.8
requires actual defeat for overkill; the historical Loki/overkill proposal was
rejected and current-rule actual-book evidence is integrated.

Hammer Throw now survives admitted villain-stage reveals and its exact upgrade
return. Independent review adds actual two-player Absorbing Man stage effects,
health settlement/elimination and nested continuation refusal; 53 restored
Hammer Throw checks and all ten unchanged public replay games pass. These games
are losses and satisfy the canonical hero-pack representative-game requirement;
the explicit victory/defeat matrix applies to scenario packs. Heimdall's native ordered return and
150% footer fix pass final affected checks with inspected captures. Lightning
Strike's actual combined payment explanation (MARVEL-761) is independently
reviewed and integrated through `63937f6d`, including bounded partial-coverage
search. Rendered 100%/150% payment journeys pass. All eleven final managed/replay/platform CI jobs passed on `286d0d44`;
the PR is merged. The
[checkpoint](thor-delivery-checkpoint.md) records exact evidence boundaries.

Black Widow's 34 faces and bounded prerequisites are mapped in its
[executable handoff](research/remaining/black-widow-implementation-handoff.md).
Black Widow is merged and agent-accepted with all 34 playable faces and
645 total ability rows. All card packets and engine prerequisites have independent
review. The verified starter and complete public admission are integrated; focused
combined admission checks pass 11 Content and 11 Server cases. Eight complete
public games have independent review, including natural Counterattack use.
Reviewed native cost/consequence repairs pass combined Safe House and Preparation
journeys; the final Godot managed suite passes 981 tests (the conditional native
entry point is separately exercised). Historical replay audit exposed lost forced
ability metadata after optional damage prevention; implementation 3c74dfd6 and
independent review e088693d now preserve the owning receipt through damage and
following effects. Strict historical rejection and compatible controls pass.
PR #453 merged at `ffb26f66` after all eleven jobs passed on exact head
`cf175e56` in run `40404335331`, including the independently reviewed setup
viewport repair. Main carries Doctor Strange protocol 42. Its complete 40-face pack and pinned Invocation rules are merged and accepted.

The intermittent Linux drag assertion from an earlier CI run remains recorded
in MARVEL-752. Its strict assertion now captures gesture geometry; focused
Windows/Linux checks and all four final CI native shards pass. No speculative
product fix or relaxed assertion was substituted for evidence.

## Delivery order and shared capability queue

Work through one integrated pack candidate at a time. Green Goblin, Ms. Marvel
Thor, Black Widow, Doctor Strange, Hulk and Ronan are accepted; Kang and Ant-Man prerequisites are in bounded parallel work. A source acquisition or narrowly owned review may run beside it when paths and integration capacity are independent. Within later waves, choose the first source-ready pack whose characteristic capability yields reusable proof; the table order is the default until evidence changes the dependency. Do not bulk author all cards or admit an incomplete pack to manufacture an early public journey. Before whole-pack admission, use nonpublic engine tests for characteristic mechanics. When complete, use the normal `DatasetGameFactory` and session/client path for setup, action, save and restart.

| Order | Shared capability and first proving pack | Required boundary/proof; later reuse |
|---:|---|---|
| 1 | Activation replacement, villain side/phase transitions and exact damage/counter replacement: Green Goblin 559 (682, 689) | Separate would-activate from completed activation; prove stun/confuse priority, one damage instance, same physical villain and attached state, stage continuation. Reuse for later double-sided villains and Kingpin. Engine reviewer required before scenario authoring. |
| 2 | Exact post-resolution event occurrence/physical card binding and ordered player-deck search: Ms. Marvel 561 (684, 685) | Recover the played copy, not an equal title; normal/off-turn window, decline, exhausted and out-of-game controls; Teen Spirit search/deck exhaustion without re-search after reshuffle. Then conditional event modifiers, persona state, obligation/nemesis (686–688). Engine reviewer required. |
| 3 | Complete scenario and hero admission, reusable encounter modules, source-qualified starter recipes: 559/561, then 563–567 | All included faces and modes before public admission; missing face/wrong recipe rejection; legal custom decks and unsupported-product rejection. MARVEL-702 supplies verified starter lists one pack at a time, not a global stop for unrelated engine slices. |
| 4 | Typed separate decks, payment provenance and form/turn receipts: Doctor Strange 565, Ant-Man/Wasp 569–570, Quicksilver/Scarlet Witch 571–572 | Invocation lifecycle and visible top; multi-form state, spent-resource/boost facts, exact timing across save/restart. Use already implemented game-area topology rather than re-implementing it for Kang 568; add Kang stage orchestration and local/central client meaning. |
| 5 | Campaign continuation, market/reward state and player-owned counters: Galaxy's Most Wanted 573 and Mad Titan's Shadow 578 | Begin with standalone scenario production setup, then one campaign transition; later complete Standard/Expert progression and restarts. Preserve seeded choices, cross-session rewards, prior campaign compatibility and normal client management. Reuse with Sinister Motives 584, Mutant Genesis 589, NeXt Evolution 597, Age of Apocalypse 602, Agents 607, Fear No Evil 617. |
| 6 | Deferred encounter pools, reusable attachments, persistent form/version replacement and global conditions: Hood 581, Valkyrie 582, Vision 583, Ironheart 586, Storm 593 | Implement one characteristic receipt/choice before each card book. Prove source/card identity, expiration, visibility and replay; avoid treating existing primitive names as pack support. |
| 7 | Team/area topology, cross-owner decks, side decks and special setup: Kang 568, Sinister Motives 584, Mutant Genesis 589, Mojo 596, Trickster 612 | Engine owns legal targets and turn/area scope. View/Decisions/Godot explain acting seat, local versus central threat and consequences. Materially new split-tableau/epic UI requires a specific owner review item with captures after agent proof. |
| 8 | Later named resource/status/keyword and identity mechanics: 597–616 | Apply each packet's first characteristic case, ruling check and negative control before bulk abilities. Share general engine fixes; keep per-product setup, recipes and complete-face ledgers with their canonical parents. Civil War 613 derives competitive modes and team victory from its insert, not a standard campaign assumption. |
| 9 | Fear No Evil and announced products 617–622 | MARVEL-697 acquires the missing MC60 scenario corpus; 618–619 need printed starter verification; 620–622 remain source incomplete under 699–701. Research on printed mechanics may proceed, but no inferred or preview-only face enters executable admission. |

The ordering is a dependency graph, not a claim that every earlier hero must finish before any later engine probe. Existing RoRS/core mechanisms are reusable only after the new characteristic case proves their behavior. `datasets/abilities/abilities.json`, `datasets/cards/**`, `datasets/marvelsdb/**`, rules references, `Agenda.cs`, and `AbilityRunner.cs` are collision-sensitive under `.parallel-sensitive`; serialize overlapping edits, including apparently different cards. Reviewers work from raw printed authority, rulings, diff and evidence; new engine capabilities and complete-pack admission require an independent reviewer. Do not leave permanent per-pack workers running.

## Active delivery queue

Green Goblin and Ms. Marvel are accepted in PR #451; their closed prerequisites
and public proof do not reenter the queue. The next delivery sequence is:

1. Thor PR #452 is merged; MARVEL-563 and its twenty prerequisite children are
   Done. The reviewed 35 faces, verified starter and full CI acceptance are the
   regression baseline; see [Thor checkpoint](thor-delivery-checkpoint.md).
2. Black Widow PR #453 contains all 34 faces, verified starter, public admission,
   reviewed complete games and native evidence. The independently reviewed setup
   viewport repair at `cf175e56` passed all eleven jobs in CI run `40404335331`.
   PR #453 is merged at `ffb26f66`. See the
   [Black Widow checkpoint](black-widow-delivery-checkpoint.md).
3. Doctor Strange PR #454 is merged with all eleven checks green. Its 40 faces, verified starter, public games and restart evidence are accepted; MARVEL-839 remains separate UI review.
4. Hulk PR #458 is merged and accepted. Complete Ronan final certification; all five faces, public admission, four complete games, fresh-host proof and both discovered engine repairs are independently accepted. See [Ronan checkpoint](ronan-delivery-checkpoint.md).
5. Advance the dependency groups and eleven-wave ledger above through Fear No
   Evil. Source-incomplete products retain their explicit acquisition gates;
   each completed product closes its own canonical parent with evidence.

Original starter dividers for Thor, Black Widow, Doctor Strange and Hulk are
pinned and reviewed under `datasets/rules-packs/mc06` through `mc09`. Each has
a verified 40-card player starter; Doctor Strange's five Invocation cards form
a separate deck. These resolve those portions of 702 without closing its global
queue or establishing playable hero support.

Wrecking Crew layout work 695 is merged in PR #445; focused UI review 694 is
separate and does not block these content parents. Content acceptance is
agent-owned. Any genuine human UI concern gets its own review item with
inspected screenshots/video and precise questions.

## Source and readiness gates

| Plane work | What it unblocks | Completion condition and current limit |
|---|---|---|
| 697 Fear No Evil encounter authority | 617 scenario/campaign setup and all MC60 encounter abilities | Verify every numbered printed face and quantity, pin source, repair vendored/supplement input, regenerate offline and pass card extract `-- check`. The 62 inspected scan filenames of 201 and existing hero-side cards do not certify the absent 60065–60210 scenario corpus. |
| 698 Phoenix Burning Hunger text | 591 obligation/nemesis and full Phoenix admission | Source image pinned, supplement repaired and generated 34028 text restored in the prerequisite candidate; extract check and distinguishing text regression passed. Final integration/merge evidence remains required; Phoenix abilities are not implemented by this source repair. |
| 699 Elektra; 700 Iron Fist; 701 Shadowland | 620, 621, 622 respectively | Acquire complete published English faces, physical quantities and rules/side decks/campaign, pin offline authority and gates. Current previews are hypotheses for mechanic research only. Each parent remains source incomplete until its own gate passes. |
| 702 Printed hero starter lists | Per-pack authored setup for position-derived starters, beginning 563–566 and proceeding through 618–619 | Record original printed list, URL/hash, exact names/quantities and joined IDs. Complete per pack; a verified pack proceeds without waiting for 702's entire queue. Existing verified Ms. Marvel and campaign-box lists are not repeated. Permanent components and non-Permanent set-asides affect legal deck size differently. |

Source work is agent-owned. Follow `docs/rules-provenance.md`, `docs/card-dataset.md` and `docs/setup-dataset.md`; never hand-edit generated cards or use a network source at build/game time. A source gap blocks its dependent authored text/setup/admission, not an unrelated nonpublic engine probe. Other bounded source checks named in each packet (for example the Doctor Strange Invocation section and Ronan print-and-play PDF) join 702 or the relevant parent before setup. Recheck actual source state before declaring a gate absent or newly resolved.

## Pack proof, integration and acceptance contract

For every parent, maintain a per-face and per-mode obligation ledger in its existing pack evidence: identity sides, signatures, starter and extras/reprints, obligation, nemesis, modular sets, scenario stages/schemes and campaign cards as applicable. For a new engine decision, cite the narrow printed/RR/product/ruling authority and include a plausible wrong-result mutant or negative control. A reviewer challenges timing, cost atomicity, exact objects/owners, no-op legality, canceled/replaced effects, nested decisions, multiplayer and restart where relevant. Any reachable unsupported behavior raises rather than silently skips.

Whole-pack admission requires complete ability/setup rows and public factory/session tests. Run a legal setup → characteristic action → save/restart path, a same-seed uninterrupted comparison of events/digest/RNG, rejected input with unchanged state, and negative admission controls. Scenario packs need complete playable Standard/Expert/mode matrices, victories and defeats and campaign progression/retry where printed; hero packs need representative complete games and focused obligation/nemesis/extra-pool branches. Select native interaction cases by **new interaction class**, using viewport input plus inspected captures. Do not infer usability from a test pass or a coached click script.

Integrate reviewed packets before broader checks. Record exact revision, focused tests, mutation outcome, production path, native capture and any failure. At final candidate run the repository's required Release build, unit/integration/acceptance lanes, full solution and relevant walls, and use CI's Windows/Linux/platform/native gates at that same revision. Preserve compatible previous-release deck, session and ongoing-campaign fixtures when content is added; if a wire/replay change is needed, follow release policy and prove migration or explicit safe rejection. Reuse existing packaging gates for package claims. A green earlier revision does not certify later combined code.

Agents may accept ordinary content and engine expansion after authority review, independent reviewer findings are resolved, and required evidence passes. Mark researched, implemented, verified, merged and accepted separately in the canonical parent; do not close a parent for a first slice. Human acceptance is reserved for a **materially new UI interaction** (for example Kang split tableau, epic multi-group or Civil War competitive team display) or a concrete unresolved judgment. Before asking, finish agent-native review, fix known defects, attach accessible screenshot/video and build/revision to a specific Plane review item with focused questions. There is no blanket owner exploration gate for each pack. Wrecking Crew's existing 694/695 review is an example, not a dependency for 559/561.

## Dispatch and checkpoint cadence

The program record and canonical pack parents are the status authority; this file is the durable queue/dependency map. Keep one integrated delivery active. Each bounded assignment owns one reviewable result, named paths, exact base revision, relevant authority, acceptance checks, evidence and a stop condition. Default soft checkpoint: 25 tool calls or 15 minutes of active work and 1 million observed cumulative input tokens including cache; these are **not** enforced hard caps. A lead reviews the saved diff/evidence/spend and either integrates, narrows the question or deliberately starts a fresh packet. No self-renewal, recursive delegation or unattended “finish all packs” loop. Aggregate reviewer and replacement usage; do not weaken correctness gates at a checkpoint. Stop workers before yielding and leave their exact dirty state and process IDs recorded.

After each merge or source repair, update the program record with exact revision, the one active parent, newly unblocked shared capability, reviewer/CI evidence, blocked source gates and the next executable packet. Update only the affected canonical parent/wave links and priority; do not create 62 generic duplicates or reparent the 64 existing records. Replan if a reviewed source/ruling changes a mechanic, a shared engine primitive fails its negative control, or final integrated checks reveal a cross-pack regression.


## Certification and next-pack checkpoint — 2026-10-27

Ronan Kree Fanatic PR #459 is merged as `1677ebcdf26bc44b1f092b91af4b973689c1f92f`.
All eleven CI jobs passed on `a2439588384a10c503cebe8ec1f5b0578dce0e8c` in
[run 40501220687](https://github.com/mggarofalo/marvel-lcg/actions/runs/40501220687).
The certified and merged trees are identical. Nine canonical packs are accepted;
55 remain. This is the print-and-play modular set, not the Galaxy's Most Wanted
Ronan scenario. All five faces, seven physical cards, 80 public setup combinations,
four complete games and four fresh-host checkpoints are accepted. The
[delivery checkpoint](ronan-delivery-checkpoint.md) records the independent reviews,
resolved failures and published replay-v28 boundary.

Kang integration `e7e3e745` includes the accepted nonpublic two-seat first split,
Stage-II reunion, physical nemesis association and central 2B-to-4B continuation.
Independent review caught and closed a survivor being skipped when the first
nemesis's quickstrike eliminated a player. Stored Dominion support (MARVEL-868),
printed Stage-III victory (MARVEL-869), wider player counts, complete card authoring,
public admission and UI proof remain. The accepted central slice has no stored
Dominions; it is not full-pack acceptance.

Ant-Man integration `cdb611d3` includes reviewed explicit three-face selection,
ability-driven and voluntary form timing, six of 35 authored faces and the Ronin
upgrade selector. Iron Man's global hand-size maximum repair is independently accepted after
closing an early zero-floor in projected calculations; integration verification remains. The actual
Ronin and Assess the Situation rows remain unauthored. Face-specific DSL, remaining
cards, public admission and the post-Ronan replay boundary remain separate work.

Fear No Evil has 201 pinned original scans. Bullseye, Electro, Hammerhead, Typhoid
Mary, Purple Man, Tombstone, Cops and Disasters are independently proofread: 74 faces,
87 physical cards and 91 original scans. The Cops review identified Wanted's hazard
icon and independently accepted the regenerated candidate. The six Tombstone/Cops/Disasters
research documents are included with this checkpoint. Additional families remain.
Annotation importer PR #460 is independently reviewed but unmerged; one Windows
server-readiness timeout in its first CI run is being investigated on current main.
No new Fear No Evil card dataset or runtime admission is claimed.
