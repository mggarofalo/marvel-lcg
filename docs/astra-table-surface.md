# Astra desktop table surface

The supported 1920×1080 route uses a spatial table scene graph. It does not use
the former rail renderer, lane renderer, or a persistent action selector.
Engine-projected affordances remain the only source of legal operations;
Godot places those operations on their represented cards. A system operation
without a readable card source is a bounded object in the contextual decision
area. The existing ordered decision panel remains the responsive and
accessibility fallback, but it is not duplicated on the desktop table.

## Reference checklist

The implementation and redesign gate compare the real engine-backed client
with the corresponding `table-geometry` scenes.

| Scene | Desktop result and check |
|---|---|
| Opening mulligan | Uses the same far-side/near-side table grammar as play. The opening hand is fanned at the near edge and each discard choice is attached to its card. |
| Ordinary player phase | Encounter piles, schemes, villain, engagement axis, identity, allies, assets, player piles, and private hand retain stable regions. |
| Action selection | Legal actions are controls on the represented card. Multiple actions open a local chooser without using prompt order as a legality rule. |
| Target and payment | Engine-offered targets and generators are selected on their table objects; temporary target and payment relationships are projected from structured descriptors. |
| Defense | The defending identity and enemy remain on the confrontation axis while the prompt uses the same local control grammar. |
| Encounter reveal and attachment | Revealed cards use the context region. Hosted cards tuck beside their host and carry an `ATTACHED` relationship tab. |
| Result presentation | Event motion and local result cues do not replace or reflow the table. History records the authoritative result in its drawer. |
| Player switching | One player tableau is expanded. Other players remain compact public summaries, and a remote prompt source is represented as a bounded decision-source card. |
| Card inspection | Hover opens after a delay, crossing into the preview does not flicker, click pins the inspector, and Escape, backdrop, and Close restore focus. |
| 100% and 150% | Both profiles use the same 1320×930 reference grammar, with scaled controls and bounded overflow rather than a dashboard reflow. |

## Physical interaction invariants

- Hand cards overlap, fan, and preserve their authored resting rotation after
  hover. Hover lifts a card forward; drag lifts it above the hover layer and
  follows the pointer.
- A legal offered hand-card drag activates only the exact engine affordance.
  Invalid drops animate back to the recorded pose without mutating the draft.
- Exhausted characters rotate 90 degrees. Their upright exhaustion caption and
  contextual action object remain readable and operable.
- Status, counters, threat, health, selection, and attachment state stay local
  to the affected object. Ambient ownership lines are not rendered.
- Relationship routing avoids unrelated card rectangles. Source, target, and
  payment relationships are transient and come only from visibility-safe
  prompt descriptors.
- Piles and dense/unknown areas open bounded local inspection surfaces. The
  private hand and active decision remain present.
- History is collapsed by default, expands as a dedicated drawer, and does not
  become a second action surface.

## Automated matrix

`tools/godot-redesign-gate.ps1` runs the native smoke at 1920×1080 for one and
two players at 100% and 150%. Its checkpoints cover setup, opening table,
inspection, player phase, villain/defense progression, player switching, and
terminal presentation. The smoke additionally asserts fan overlap, hover and
drag z-order, an intermediate pointer-following drag frame, valid and invalid
drops, quarter-turn exhaustion, local host layering, preview timing, pinned
focus, relationship routing, overflow reachability, repeated input, stale
render fencing, and the absence of the retired desktop choice sheet.
