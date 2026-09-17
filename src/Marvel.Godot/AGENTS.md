# Game interaction and information design

Apply the root and [shared source guidance](../AGENTS.md). Read
[presentation-layer.md](../../docs/presentation-layer.md) and
[godot-client.md](../../docs/godot-client.md) before implementation or launch.
This is the product standard for UI work, not a description of feature
completeness in the current client.

## The player's task is the organizing unit

The player expresses a game intention; the interface explains and helps carry
it out; the engine decides legality and results. Organize interaction around
playing a card, using an ability, responding to an encounter or completing a
setup choice, not around the engine's individual submission operations.

Before and throughout each decision, make the following understandable without
opening history or searching for an enabled button:

- What is happening, why, and which objects caused or are affected by it.
- Who must answer, and whether the decision is required, optional, or waiting
  for another player. Distinguish the answering, active and inspected seats.
- What can be done, how to initiate it, and what continuing or declining means.
- The proposed effect, known quantities, costs, uncertainty and selected choices.
- What remains to choose, the next commitment, and how to change course.

Keep current situation and action composition visible together. Never replace
the causal explanation with draft bookkeeping such as READY or TARGETS 1/1.
Essential information cannot require hover, color recognition or animation.

## Cards express intent

Dragging a source onto an object means "use this source in relation to this
object." Resolve that intention against current engine offers and the single
composer. Do not hardcode "character onto scheme means use THW" or infer an
event's purpose from its name, printed text, ownership or screen position.

Examples of the interaction grammar, when explicitly offered by the engine:

- Hero onto minion: begin the offered attack with that target selected.
- Ally onto side scheme: begin an offered effect on the scheme, choosing among
  methods if necessary; the engine supplies the applicable stat and effect.
- Event onto another character: begin its offered effect with that destination
  selected, then guide payment and remaining choices.

These examples are not legality rules or authorization to add expansion content.
If an offer explicitly identifies targeting as deferred, the gesture may open
its source-action composition with the destination unselected; explain that
target selection is still ahead. Do not promise that this destination will
become legal or silently answer a future prompt. A rejected destination in a
current target request changes nothing; it must not start the source action
anyway. Missing target data does not establish deferred targeting.

Match exact offer identities and target roles. Handle all three outcomes:

| Matches | Behavior |
|---|---|
| None | Return the card without changing the draft; explain the mismatch using available authorized information. |
| One | Open or update composition with the expressed choices selected; do not submit merely because the match is unique. |
| Several | Offer a contextual choice with meaningful action names, costs and effects; never choose by list order. |

A legal destination may complete only part of a grouped, ordered or allocated
selection. Let the composer assess that partial draft and explain what remains.
Do not require every gesture to constitute a complete legal engine answer.

## Make interaction discoverable and forgiving

Use restrained cues to show which objects offer interactions. At drag start,
show destinations authorized for the current interaction. Over a destination,
explain the proposed action or ambiguity before release. Use verbs and known
effect information, not unexplained generic ACTION markers.

The dragged card must lift, follow the pointer and remain visible above nearby
objects. An invalid or cancelled drag returns visibly and preserves the prior
draft. Release must not also click a control underneath. Offer useful feedback
from supplied unavailable reasons; absence of an offer does not justify an
invented reason such as insufficient resources.

Provide discoverable click and keyboard equivalents, including source actions,
target selection, payment, cancellation and confirmation. Card inspection is
distinct from choosing an action. Clicking a card body pins its inspector;
source actions need a clear contextual entry or keyboard equivalent rather
than making a body click both inspect and act.

## Compose near the intention

Use the smallest contextual surface that explains the choice: a source-local
chooser for ambiguity, a composition panel for targets and payment, or a dialog
for decisions needing more space or attention. A drag begins an understandable
flow; it must not merely reveal a scatter of Execute buttons on source and
target cards. Do not restore a permanent right-side action-selector dashboard.

Show the source, action, selected targets, known effect and costs together.
Highlight table objects as direct selectors while their relevant details remain
readable. Give one clear commitment control for the current composed answer,
labelled in game terms, such as "Attack Rhino" or "Play [card name]." Avoid
generic Submit/Execute/Resolve where the actual operation can be named.

Changing a selection, inspecting a card, opening a pile or expanding history
must preserve the draft and access to the current decision. Temporary surfaces
must not hide the hand or objects required for the current choice. Modal
surfaces contain focus and restore it on dismissal, without click-through.

Every supported engine choice needs an understandable path, including ordered
groups, allocations, searches and variables that do not fit a simple drop.
Offer a complete ordered alternative through the same composer and lifetime;
do not show duplicate commitment controls or independently executable copies
of one operation across table and alternative paths.

## Costs, staging and commitment

Payment shows what is required, what each selected source contributes, the
associated exhaustion/discard or other costs, and what remains unresolved.
Distinguish playing a hand card from discarding it for resources and from using
a resource-generating ability. If a gesture has multiple meanings, ask which
offered use is intended. Name alternative costs rather than numbering them.
Keep variable amounts and meaningful resource declaration/allocation choices
explicit and editable. Preserve defaults where the engine contract establishes
declarations as observationally equivalent, and editable engine-authored
preferred suggestions; do not ask meaningless questions or infer equivalence
from card text.

During mulligan, accept hand cards at the discard pile or a clearly labelled
temporary discard-selection area. Stage the selection without performing a
discard; allow cards to be moved back. Keep a visible overlay with the purpose,
selected count and "Replace N cards" or "Keep hand" completion control. State
that replacement follows confirmation, and identify whose mulligan is pending.
Use the same reversible staging language for other multi-object draft choices.

Separate local draft cancellation, declining a game choice, and authorized
history undo. One intention can span several submitted answers; an accepted
cost or effect is not undone by closing the next composition surface. If an
engine choice is required, dismissal must leave an obvious way to resume it.

## Continuity and feedback

Announce incoming situations before presenting their response options. For an
attack, identify the attacker, affected character, relevant known strength and
unresolved boost information; explain the current interrupt or defense choice.
Apply the same standard to encounter reveals, abilities, responses, ordering,
searches and other decisions. Attacks are an example, not the entire standard.

Preserve the originating intention and already accepted steps through nested
engine prompts. A new prompt gets its own draft; the surrounding explanation
continues. Do not silently decline optional windows or choose sole options.
Advance only presentation transitions automatically; never silently answer or
skip an engine prompt. Do not add presentation acknowledgments for bookkeeping
that offers no player choice. If the engine itself emits a needless prompt,
correct its flow under rule authority rather than auto-clicking it in Godot.

After commitment, show the authoritative result on affected objects and in
readable history: who acted, what happened, and what changed. Group the narrative
without losing event order or meaningful intermediate decisions. History is an
expandable account, not a prerequisite for knowing the current situation.

Show sending, waiting and recovery as distinct from game resolution. Prevent
repeated submission; if offers change, cancel/revalidate the stale interaction
visibly and require any new meaningful choice. Never silently retarget or replay
input after synchronization. Inspection remains available while another seat
is answering, subject to authorized visibility.

## Inspection and spatial readability

Show hover details immediately, with no intentional activation delay. Keyboard
focus must provide equivalent detail without stealing focus. Use a dismissal
grace period when moving between the source and preview. Pinning is explicit;
Escape, Close and backdrop dismiss the inspector and restore useful focus.

Previews must not shift the source under the pointer, obstruct a drop target,
consume a drag, or flicker between overlapping hand cards. Keep essential state
and the active decision readable without requiring a pinned inspector.

Retain a stable physical-table grammar through mulligan and play: encounter
objects on the far side, confrontation through characters, persistent upgrades
and supports, distinct piles and a fanned hand near the player. Ready characters
are upright; exhaustion is a quarter-turn with an upright readable caption.
Attachments, statuses, counters and selections stay local to their hosts.
Temporary source/target/payment cues must be distinct and must not imply
relationships through unrelated cards. Do not draw ambient ownership lines.

Keep one player tableau expanded with meaningful public summaries for others.
Use bounded pile/region drawers for dense or unfamiliar areas, preserving hand
and decision access. Test readability and hit targets at supported scales;
being technically inside the viewport is insufficient. Provide non-color cues,
visible keyboard focus and reduced-motion behavior.

## Acceptance

Follow [tests/AGENTS.md](../../tests/AGENTS.md). Automated traversal demonstrates
operability, not comprehension. For material interaction changes, require an
independent reviewer to use the real client without a click-by-click script and
explain the situation, options, commitment and result from the screen alone.
Record confusion and missing information as product failures, even if the game
can be completed and all mechanical tests pass.
