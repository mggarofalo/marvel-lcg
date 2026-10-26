# Remaining content implementation program

**Tracking:** Content Expansion module and [MARVEL-703](https://plane.wallingford.me/dev/projects/cd677658-41d9-4576-b055-5f2a726b3c85/issues/b26d754d-4e0e-486c-8a04-f0e9b968cdb2). **Planning baseline:** `b16183e2`, 2026-10-25. This is an execution queue, not evidence that a researched pack runs. The canonical product records are MARVEL-559–622. Captain America (560) and Wrecking Crew (562) are implemented and agent-accepted in [PR #439](https://github.com/mggarofalo/marvel-lcg/pull/439); the original remaining scope was **62**. Green Goblin (559) and Ms. Marvel (561) are now also merged and agent-accepted in [PR #451](https://github.com/mggarofalo/marvel-lcg/pull/451), leaving **60** canonical packs. Core and Rise of Red Skull are the accepted regression baseline. MARVEL-630 is Done and additive-content compatibility has been integrated; MARVEL-696's all-pack research is Done. Do not reopen either based on historical notes. The independent Wrecking Crew UI review MARVEL-694 and layout fix MARVEL-695 stay separate from this content queue.

The [research coverage ledger](research/remaining/README.md), its ten paired narrative/face-inventory packets, [source review](research/remaining/source-review.md), and [independent review](research/remaining/independent-review.md) supply MARVEL-563–622 handoffs. [Green Goblin](research/green-goblin-deep-research.md) and [Ms. Marvel](research/ms-marvel-deep-research.md) have their own deeper packets. Their examples are first slices; the canonical parent acceptance criteria still require every playable face and mode. The current source gaps are recorded below, without turning them into invented card behavior.

## Complete pack-to-wave ledger

The existing Content Expansion module defines these **11 planning waves**. Ranges are inclusive, and a box owns its included heroes, scenarios, modular sets and campaign, even where those components also appear in another deck. This mapping covers each of the 64 canonical parents exactly once; the four accepted parents are shown only to make the subtraction auditable. A wave is a delivery grouping, not proof of a published release or a command to admit its packs together. Each parent keeps its own implementation, evidence and acceptance state.

| Existing wave | Canonical pack parents | Remaining |
|---|---|---:|
| Core-era expansions | **559 Green Goblin accepted**; **560 Captain America accepted**; **561 Ms. Marvel accepted**; **562 Wrecking Crew accepted**; 563 Thor; 564 Black Widow; 565 Doctor Strange; 566 Hulk | 4 |
| Rise of Red Skull cycle | 567 Ronan Modular Set; 568 The Once and Future Kang; 569 Ant-Man; 570 Wasp; 571 Quicksilver; 572 Scarlet Witch | 6 |
| Galaxy's Most Wanted cycle | 573 The Galaxy's Most Wanted; 574 Star-Lord; 575 Gamora; 576 Drax; 577 Venom | 5 |
| Mad Titan's Shadow cycle | 578 The Mad Titan's Shadow; 579 Nebula; 580 War Machine; 581 The Hood; 582 Valkyrie; 583 Vision | 6 |
| Sinister Motives cycle | 584 Sinister Motives; 585 Nova; 586 Ironheart; 587 Spider-Ham; 588 SP//dr | 5 |
| Mutant Genesis cycle | 589 Mutant Genesis; 590 Cyclops; 591 Phoenix; 592 Wolverine; 593 Storm; 594 Gambit; 595 Rogue; 596 Mojo Mania | 8 |
| NeXt Evolution cycle | 597 NeXt Evolution; 598 Psylocke; 599 Angel; 600 X-23; 601 Deadpool | 5 |
| Age of Apocalypse cycle | 602 Age of Apocalypse; 603 Iceman; 604 Jubilee; 605 Nightcrawler; 606 Magneto | 5 |
| Agents of S.H.I.E.L.D. cycle | 607 Agents of S.H.I.E.L.D.; 608 Black Panther (Shuri); 609 Silk; 610 Falcon; 611 Winter Soldier; 612 Trickster Takeover | 6 |
| Civil War cycle | 613 Civil War; 614 Synthezoid Smackdown; 615 Wonder Man; 616 Hercules | 4 |
| Fear No Evil cycle | 617 Fear No Evil; 618 Jessica Jones; 619 Luke Cage; 620 Elektra; 621 Iron Fist; 622 Shadowland | 6 |
| **Total** | **64 parents, including 4 accepted** | **60** |

## Current integration checkpoint

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

Thor is the active integration candidate at `707a0ed1` on `feat/thor-completion`.
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
search. Rendered 100%/150% payment journeys pass. Combined
managed/replay/platform certification and the Thor PR remain open. The
[checkpoint](thor-delivery-checkpoint.md) records exact evidence boundaries.

Black Widow's 34 faces and bounded prerequisites are mapped in its
[executable handoff](research/remaining/black-widow-implementation-handoff.md).
The first nonpublic Preparation resolution receipt (MARVEL-762) is being
implemented separately; it does not expand Thor's candidate or admit Black
Widow. Independent review precedes dependent production card authoring.

The intermittent Linux drag assertion from an earlier CI run remains recorded
in MARVEL-752. Its strict assertion now captures gesture geometry; focused
Windows/Linux checks and all four final CI native shards pass. No speculative
product fix or relaxed assertion was substituted for evidence.

## Delivery order and shared capability queue

Work through one integrated pack candidate at a time, starting with Green Goblin plus Ms. Marvel as the first delivery. A source acquisition or narrowly owned review may run beside it when paths and integration capacity are independent. Within later waves, choose the first source-ready pack whose characteristic capability yields reusable proof; the table order is the default until evidence changes the dependency. Do not bulk author all cards or admit an incomplete pack to manufacture an early public journey. Before whole-pack admission, use nonpublic engine tests for characteristic mechanics. When complete, use the normal `DatasetGameFactory` and session/client path for setup, action, save and restart.

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

1. Certify and merge Thor PR #452, exact candidate `4c6f5af9`, then update
   MARVEL-563 and its reviewed prerequisite children. All 35 faces and the
   verified starter are implemented; see [Thor checkpoint](thor-delivery-checkpoint.md).
2. Integrate Black Widow's independently reviewed engine and face packets on the
   Thor squash merge. The [Black Widow checkpoint](black-widow-delivery-checkpoint.md)
   identifies nine integrated faces, three pending review, and explicit remaining
   capabilities. Complete all 34 faces before opening public admission.
3. Prove complete Black Widow setup/custom decks, characteristic nested response
   persistence, nemesis/obligation and representative complete games; select
   native checks by changed interaction, certify the exact candidate, then merge.
4. Continue Doctor Strange and Hulk using existing source-qualified starters and
   research packets. First prove Invocation deck/identity-specific mechanisms;
   serialize shared interpreter and ability-book changes and independently review.
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

