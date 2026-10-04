# A quieter Marvel Champions table

This is a synthetic interface study, not game logic or an accepted product implementation. Open `index.html` locally. Three layouts, four scenes and 100%/150% text profiles are selectable. The committed study consists of this README and standalone HTML; capture outputs and their local harness are external to the repository.

The chosen direction, implemented contracts and verification boundary are in
[implementation.md](implementation.md). Independent played observations are in
[native-review-followup.md](native-review-followup.md).

## Principles first

The table is the player's memory. Stable ownership, zones and local live values make a later decision intelligible without reconstructing a dashboard. A task begins at an object, but a task is not a row of engine submission controls. Inspecting a card and initiating an action are separate intentions.

The current situation, the editable answer and the authoritative result are three different kinds of information. They need separate visual roles: causal explanation in the confrontation seam, a temporary answer rail below the hand, and a durable latest-result sentence outside History. The next draft must not overwrite why the choice exists. One intention has one commitment.

Small typography does not require tiny targets. Base body text is 14px, card titles 15px, full-card selection targets are 140–146px wide, primary commitment is padded independently. At the 150% profile, reading text grows and decorative art yields space. Names, local live values and current choice remain visible; card paragraphs are summaries, with full inspection available.

Only the selected source, target and offered payment candidates get decision cues. A table full of permanently labeled actions competes with the game. Color is reinforced by words, outlines and selected ticks.

## Reasoning to Marvel Champions

A player may perform turn options in any order and repeatedly while able, with form change limited. This favors a stable table with contextual entries over a prescribed three-step wizard. `datasets/rules-reference/entries/player-turn.md:16` lists the options; `docs/player-phase.md:9` explains the engine's offered menu.

Payment is an actual trade: discarded cards are lost from the hand, while a resource ability can exhaust or consume an asset. The study stages Energy's two resources plus Web-Shooter's wild resource and names both costs. This is based on `datasets/rules-reference/entries/cost.md:26` and card records `01088` / `01008` in `datasets/cards/cards.json:1845` / `:171`. Demo arithmetic is scaffolding and must never be copied into production.

Swinging Web Kick costs 3 and its printed effect deals 8 damage to an enemy (`datasets/cards/cards.json:113`, card `01005`). The future destination is not selected during payment; the enemy remains visible, the printed effect is labeled as printed rather than promised net damage, and the synthetic result independently reports Rhino's actual HP change. A real result must come from authorized events, accounting for prevention/statuses/windows.

Rhino I has ATK 2, SCH 1, HP 14 per player (`datasets/cards/cards.json:1953`, card `01094`). The study is solo. The Break-In!'s 1B threshold is 7 per player and completion loses the game (`datasets/cards/cards.json:2031`, card `01097b`). Local counters pair the quantities with the objects they explain.

The defender is chosen before boost cards are revealed. The incoming attack therefore says ATK 2 + unknown boost and avoids a predicted final hit total. Hero defense exhausts and applies DEF; ally defense exhausts and changes the damage target. Sources: `datasets/rules-reference/entries/attack-enemy-activation.md:22` through `:37`, `docs/enemy-attacks.md:22`. Backflip's later damage interrupt is not presented as a defense declaration choice; its printed trigger is `datasets/cards/cards.json:76`.

Spider-Man's Spider-Sense is an initiation interrupt, distinct from defense selection; `datasets/cards/cards.json:14` and `docs/enemy-attacks.md:9`. The current choice cannot silently swallow optional windows for the sake of a faster flow.

## Three alternatives

**Quiet physical table — recommended.** Far-side opposition, confrontation seam, near-side characters/persistent assets and fanned hand. The selected task has one short dock below the hand. This best preserves rule-relevant relationships and familiar tabletop memory while removing button scatter. Source-body clicks inspect; inspection offers a separate Play entry. Candidate body selection in the study is intentionally a POC shortcut and needs a distinct selection versus inspection path in production.

**Confrontation lanes.** Identity anchors a left column; enemy state, hand and composer align to its right. Compact and scannable, with no full-height sidebar. Its tradeoff is that physical confrontation and ownership become less immediate, and changing object density could disrupt alignment. Useful as a compact accessibility/fallback direction, weaker as the primary table.

**Focus theater.** The current situation dominates the center while background objects recede. Most readable for unfamiliar mandatory prompts, least effective for comparing several threats and persistent tools. Useful as a temporary treatment for search/order/allocation choices, not an entire-game layout.

Recommendation combines the physical table with task-sized expansion from focus theater only when the real prompt demands it. Full ordered choice fallback must remain available for unsupported spatial shapes without duplicating the active composer or commit.

## Rendered-study critique and implementation choice

The 150% payment capture made the quiet table the strongest primary layout: opposition, hand and staged payment remained identifiable together, and one named commitment carried the action. The first pass exposed a clipped Web-Shooter counter and a result line falling below the 1080px viewport. Reducing decorative art and vertical gaps restored the counter and receipt without shrinking reading text.

Reviewers selected the quiet physical table and rejected confrontation lanes and focus theater as primary layouts. Lanes made the geometry compact but weakened the physical villain/identity relationship; theater emphasized the current question but dimmed board facts needed to compare threats and tools. Theater contributes a temporary expanded choice sheet, and lanes remain a comparison study rather than an accepted fallback implementation.

The native first pass then exposed limitations that the synthetic captures could not judge: scattered source controls, irrelevant payment/confirmation narration, forgotten exhaustion costs, a receipt cleared at draft start, and cause/progress separated by a wide blank region. The implementation keeps the table landmarks but adapts the dock to contiguous cause and selected action, real engine-authored costs/effects, one named commitment and a durable latest result. It removes eyebrow labels and generic free-cost/confirmation sentences. The HTML is a design comparison, not an exact native blueprint or proof of product acceptance.

## Lessons retained from rejected studies

`docs/ui-prototypes/README.md` rejects its earlier alternatives as literal implementation baselines: duplicated controls, label/selection inversions, incoherent seat switches, synthetic timing fixtures, hidden controls and result overlays. `docs/table-geometry/README.md:22` favors far-to-near landmarks but rejects its implementation specification: focus loss, missing explicit identifiers, misleading flattening of affordances/candidates/generators, hit occlusion, unsolved dense board and synthetic fixture limitations.

This study does not introduce a second seat, invent action groups, create a permanent right action dashboard, hide the latest result behind History, or place commitment controls on every card. It deliberately stays synthetic and sparse. Dense real engine fixtures, complete candidate types, native focus continuity and every supported prompt remain implementation/review requirements.

## External observations supplied by research

A frame reviewed from [Nelson All Over Cards first-game tutorial](https://www.youtube.com/watch?v=YJsR30Vgj_E) around 10:05: far-side villain/scheme/encounter deck, near-side identities, player piles beside their identities and hand cards temporarily gathered in the center. The lesson is stable ownership landmarks with a temporary action focus, not more permanent labeled regions.

Research also included the [official Slay the Spire press kit](https://www.megacrit.com/press-kits/slay-the-spire/) and [Midnight Suns gameplay stream coverage](https://www.pcgamer.com/midnight-suns-gameplay-stream/): fanned hand, selected-action/target explanation and local HP/intent. The presentation lessons transfer; exact future intent does not transfer to Marvel Champions' hidden boost.

## Verification and limitations

An external local harness at `/tmp/marvel-ui-design/render.cjs` captured all 24 concept × scene × scale combinations at 1920×1080. Its external `/tmp/marvel-ui-design/geometry-report.json` reported no JavaScript errors or enabled stage controls outside the viewport or obscured at their center. Neither harness nor PNG/report output is committed. Review a fresh checkout by opening `index.html`, selecting each layout/scene/text profile and inspecting the canvas and available interactions; no build or server is needed. This is a narrow geometry check, not a gameplay, arbitrary density, accessibility, all-pixel occlusion or comprehension claim. Card/resource glyphs are study stand-ins; production must use canonical resource icons.

Known study limitations: source action support is only Swinging Web Kick, later targeting is explained without preselecting Rhino, payment candidates are authored, candidate body click doubles as selection without an independent inspection gesture, inspector keyboard focus is incomplete, incoming defense does not invent a hidden boost result, generic other operations are absent, and End turn merely selects the independent attack study. Those shortcuts cannot enter production.
