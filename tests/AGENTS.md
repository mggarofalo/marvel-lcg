# Evidence for understandable play

Apply the root instructions. For presentation or interaction work, also read
[src/AGENTS.md](../src/AGENTS.md) and
[the UI guidance](../src/Marvel.Godot/AGENTS.md), even when only tests change.
Read [testing.md](../docs/testing.md) for the existing test-lane boundaries.

## Test the player's observable task

Use legal Core engine states for reachable gameplay. Synthetic contracts can
prove general shapes such as alternative effects without opening an expansion
product boundary; label them as synthetic and do not claim a played scenario.

For affected workflows, assert the explanation as well as the operation:

- Cause, answering seat, required/optional status and next choice remain visible
  throughout composition; selecting a target does not erase resolution context.
- Gestures distinguish zero, one and several offered matches, preserve identity
  and target-role constraints, and do not silently submit or choose by order.
- Known effects, costs and uncertainty are presented accurately; hidden
  information stays hidden through labels, previews, highlights and history.
- Payment, grouped/ordered choices and allocations report meaningful progress
  from the composer. Input alternatives use the same draft and commitment.
- Invalid/cancelled drags and reversible staging leave authoritative state
  unchanged. Mulligan permits staging, unstaging and explicit count-labelled
  completion. A later prompt cannot locally cancel already committed costs.
- Immediate previews, stable hand geometry, actual intermediate drag frames,
  target feedback, focus restoration, occlusion and dismissal behave correctly
  in the native UI. Focus, scale and reduced motion preserve usability.
- Repeated input submits once; stale input and uncertain transport outcomes do
  not repeat mutations or silently select replacement offers.
- Results remain understandable through nested choices, history and authorized
  undo; public seat inspection does not transfer decision authority.

Cover the changed behavior proportionately. Do not add assertion-free tests,
tests mirroring implementation details, or a full-game run for every copy edit.
Run the required existing repository gates. A successful complete-game smoke
remains regression evidence, not a substitute for the checks above.

## Independent comprehension review

For material changes to interaction, information hierarchy or table layout,
arrange an independent review of the real engine-backed client. Give the
reviewer game goals and relevant scenarios, not locations to click or a script
that reveals the intended interaction. Compare against any approved reference,
but evaluate understanding in the working app rather than screenshots alone.

Before acting, the reviewer should identify from the screen: what is happening
and why, who must act, whether the choice is optional, available actions, known
costs/effects and uncertainty, what remains to select, and what commits next.
After acting, they should explain the result and the next decision. Searching
for enabled buttons, opening history to discover the current cause, or receiving
coaching to proceed is a finding, not a successful review.

Select representative situations covering changed paths and boundary cases.
For a whole-surface redesign, include setup/mulligan, ordinary play, abilities,
target/payment ambiguity, incoming encounters and nested responses, defense,
results, inspection, dense boards, multiplayer and supported scale profiles.
Exercise drag and non-drag paths and attempts to cancel/change course. Mark
untested scenarios explicitly; do not extrapolate a narrow pass to the whole UI.

Record the reviewed commit/build and profiles, scenarios attempted, confusion
and missing-information findings, fixes and retest evidence, and an explicit
verdict for the claimed scope. Report automated verification separately from
product review. "Merge this increment" is not approval of a complete redesign.
Do not mark required behavior complete or relabel it an enhancement to obtain
approval. Guidance-only changes require consistency/link review, not a claim
that the prescribed UI has been implemented or playtested.
