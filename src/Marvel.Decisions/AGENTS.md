# One draft per prompt

Apply the root and [shared source guidance](../AGENTS.md). Read
[affordances.md](../../docs/affordances.md) and
[presentation-layer.md](../../docs/presentation-layer.md).

All input methods edit the same `DecisionComposer` draft. Dragging, contextual
choices, keyboard selection and a complete ordered alternative must not maintain
independent answers or produce duplicate submissions.

## Interpret intent through explicit offers

- Match exact offered sources, targets and roles, not labels or geometric
  conventions. Multiple matches remain a player choice, never a list-order rule.
- A single matching offer may be selected as a reversible draft; matching it
  is not consent to submit. Do not silently select other strategic choices or
  resource generators. Engine-authored preferred declarations remain visible,
  editable suggestions. Preserve defaults for declarations the engine contract
  establishes as observationally equivalent; do not burden the player with a
  meaningless question. Meaningful declaration/allocation choices remain
  explicit, and the UI must never infer equivalence itself.
- A dropped destination can be an authorized partial target selection without
  completing the answer. Preserve grouped, ordered and repeated selections;
  obtain validity and readiness from engine-owned assessment.
- If targeting belongs to a later prompt, do not synthesize or pre-answer it.
  The current draft cannot authorize a future handle or target.

## Make progress explainable

Expose what is selected, which choices remain, and why submission is not ready
using authorized facts and engine assessment. Keep target details, alternative
costs, variable amounts, generator effects and resource declarations available
to consumers; an option number or selected-item count is not their meaning.

Draft cancellation clears only uncommitted choices. Declining an optional
window submits an engine decision; undo is a separate server-authorized history
operation. Preserve these distinctions even when one visible intention spans
several prompts. Never reuse an accepted or replaced prompt's draft to make the
next decision silently.

Bind input to its current prompt and render/request lifetime. Repeated input
must not submit twice, and stale events must not select a new offer with a
reused handle. After replacement, surface the change rather than silently
retargeting a draft.

Test equivalent behavior across input paths, partial selection and ambiguity,
progress, cancellation, authoritative rejection and stale/repeated input. Use
the evidence rules in [tests/AGENTS.md](../../tests/AGENTS.md).
