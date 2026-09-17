# Game intent and semantic contracts

This guidance applies throughout `src/`, in addition to the root instructions.
Read [presentation-layer.md](../docs/presentation-layer.md) and
[affordances.md](../docs/affordances.md) before changing offers or their consumers.
The player-facing interaction requirements live in
[Marvel.Godot/AGENTS.md](Marvel.Godot/AGENTS.md).

## Supply meaning at the owning layer

An affordance is an engine offer. A visual affordance is how a player discovers
and understands that offer. Both must exist: opaque executable handles do not
by themselves provide a usable game.

- Engine projects (`Marvel.Rules`, `Marvel.Cards`, and other rule owners) decide
  legality, timing, targets, costs, quantities, modifiers, effects and state
  changes. Expose their semantic results without UI layout or gesture rules.
- `Marvel.View` filters those results for the authorized viewer and describes
  them without independently calculating legality or simulating consequences.
- `Marvel.Decisions` owns the single draft for one prompt and obtains validity
  and progress through engine-owned functions.
- `Marvel.Server` owns authorization, protocol compatibility and authoritative
  validation/publication. `Marvel.Client` owns request and recovery lifecycle.
- `Marvel.Godot` owns interaction, layout, focus and readable presentation. It
  matches gestures to explicit offers; it does not invent their meaning.

Do not interpret labels, descriptions, card names, keywords, counters or screen
positions as a substitute for structured action, relationship or target data.
Formatting supplied values is presentation; calculating which stat applies or
which effect is legal remains an engine responsibility.

## Obligations for engine offers and results

When adding or changing a reachable decision, provide enough semantic context
for a client to explain why it exists and what accepting or declining means.
Review the following information according to the decision's needs:

- Causal source and current resolution, answering seat, and required versus
  optional choice. Distinguish the answering seat from the active turn seat.
- Exact offered action identity, source and anchor namespace, eligible targets
  and their roles, and constraints for groups, ordering and repeated allocation.
- Costs, alternatives, generators, declarations, variables and exhaustion or
  discard commitments. A generator is not interchangeable with a hand discard.
- Relevant current quantities and modifiers, target-specific consequences, and
  which results are known, conditional, or unresolved.
- Available reasons an offered action is unavailable, and semantic events that
  explain the result after acceptance.

This is a checklist of responsibilities, not a requirement to flatten every
decision into one universal record. Preserve distinctions in the engine's
existing concepts. Extend a contract deliberately where information is missing.
Do not patch a missing semantic fact with card-specific UI conditions.

An effect preview must not mutate live state, consume RNG, reveal hidden future
outcomes, or replace engine validation. State uncertainty explicitly when a
result depends on later choices, responses, boosts or other unresolved effects.
Do not advertise a current stat as guaranteed final damage or threat removal.

Keep the supported [Core Set boundary](../docs/scope.md). Later stat-replacement
or card patterns can motivate general contracts; they do not authorize adding
isolated expansion content or assuming its rules from an informal example.

## Preserve decisions and commitment boundaries

A player's intention may span several prompts. Preserve its visible causal
context, but do not turn it into a draft that silently answers future prompts.
Only current explicit offers authorize selection. If an offer explicitly
identifies targeting as deferred, a gesture may initiate that offered source
action with the destination unselected and explain that a later target choice
is required. It must not promise or pre-submit that destination. An invalid
destination in a current target request is a rejected drop, not permission to
start the source action anyway. Do not infer deferred targeting from prose.

Keep draft edits, accepted decisions, optional declines and authorized history
undo distinct. Every submitted answer may commit state, including during
resource generation or a larger unresolved action. A UI Cancel operation cannot
undo those commitments. Do not auto-pass an optional window or auto-submit a
sole option merely to streamline the UI. Presentation-only transitions may
advance without input; never silently answer or skip any engine prompt. If a
prompt is needless bookkeeping, correct the engine flow under rule authority
rather than having the client auto-click it.

## Authorization, transport and recovery

Bind decisions to the current prompt, revision and authorized seat. Reject stale
input rather than silently applying it to a replacement offer or target.
Changing the inspected seat is not a change of authority.

All interaction hints are information-bearing outputs: previews, destinations,
choice counts, disabled reasons, connectors, history and accessibility text must
respect the server's cooperative or restricted visibility policy. Never expose
hidden identity or order through an explanation of an otherwise hidden action.

Preserve uncertainty after transport failure. Once transmission may have begun,
do not retry a mutation or imply cancellation rolled it back; use the existing
authoritative synchronization/recovery contract. Explain waiting, rejection,
stale-state replacement and recovery without disguising them as game effects.

For wire changes, make an explicit version/compatibility decision, update the
protocol documentation, and test both in-process and socket paths. Retain the
same information and authorization boundaries in each deployment.
