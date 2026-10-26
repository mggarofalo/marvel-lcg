# Affordances

An affordance is one thing a player can do now. It carries the domain action,
the board object it belongs to, legal targets and ways to pay.

The client renders affordances. It does not derive legal moves from printed card
text or duplicate engine rules.

## Answer authority

`DecisionAssessment` in `Marvel.Rules` validates a complete answer against one
prompt. It checks the answering seat, the exact offered handle, legality, ordered
targets, declared variables and resource allocation. A payment must satisfy one
complete applicable cost option; sources and variables cannot be combined across
alternative prices. A draft can require its selected cost option explicitly.

`Game.Resolve` runs this assessment before changing the world, clearing information
signals or advancing timing. `Marvel.Decisions` uses the same assessment to compose
a draft, and session replay uses it after identifying the recorded offer. Hosts
still authorize the connection's seat; prompt legality does not grant a capability.
Malformed answers raise `ArgumentException` and leave the current decision intact.
An unimplemented game rule raises `RulesNotImplementedException` separately.

A cancellable prompt admits an empty decline. Keeping a mulligan hand is an
explicit offered action with no selected cards. Simulation and transcript policies
can use `ResourcePayment.Allocate` to choose an allocation; that helper is an
explicit policy choice, not a missing-answer default in the engine.

## Wire shape

`Marvel.Rules.Prompts.Affordance` contains:

| Field | Meaning |
|---|---|
| `Id` | Opaque handle valid in the issuing session |
| `Verb` | Domain action such as `Play`, `Attack`, `Thwart`, `Change_Form` or `Ask` |
| `AnchorId` | Board object the player interacts with |
| `AnchorKind` | Whether `AnchorId` is a card, area, or intentionally unspecified object |
| `AnchorPlayer` | Seat whose board holds the anchor |
| `Label` | Printed or domain-level option label |
| `CommitLabel` | Optional engine-authored action and affected object for accepting this exact choice |
| `Description` | Optional readable action text authored by the engine/card DSL |
| `Targets` | What still has to be selected |
| `Costs` | Legal resource-generation plans and variables |
| `Illegal` | Reason the option cannot currently be taken |

The fields are values, not references into live engine state. The same record
works in-process and over the socket protocol without exposing hidden state.

## Handles and identity

`Id` is a short-lived handle, not a persistent command name. A saved id from one
session may name something else in another session.

A consumer that must re-identify an affordance records the stable public tuple:

```text
(AnchorKind, AnchorId, AnchorPlayer, Verb, Label, occurrence among exact matches)
```

The occurrence index matters because repeated choice nodes can be identical on
every other public field. This tuple is an engine wire choice; the tabletop
rules define no persistent command identifier.

## Anchors

Every affordance has an anchor. A play action anchors to the card in hand. A
basic power anchors to the identity. A mid-resolution question anchors to the
card or game element whose ability is waiting.

Anchors let a client highlight the right object without inferring meaning from a
label. `AnchorKind` is required because card ids and area ids are independent
namespaces; a consumer must not probe cards and then areas to infer which one
the engine meant. `AnchorPlayer` distinguishes multiplayer actions that share
the same domain shape.

## Target requests

`TargetRequest` contains:

- `Legal`, the current candidate object ids;
- `Min` and `Max`, the ordinary selection bounds;
- `Groups`, complete legal grouped selections when a flat count is insufficient;
- `MustIncludeTraits`, traits the final selection must contain;
- `Rule`, a named extra selection rule;
- `IsSearch`, which marks a choice through hidden information; and
- `AllowRepeated`, used for allocations such as indirect damage; and
- `MaximumOccurrences`, the maximum allocation entries permitted for each
  candidate when repeated entries are allowed; and
- `Details`, engine-authored consequence text for each legal target.

When `Groups` is non-empty, it is authoritative. `Min` and `Max` then describe
the pooled candidates and must not be applied to a selected group. A selection
must exactly equal one complete group in its listed order; a subset or reordered
copy is not the offered answer. Clients and tests should use
`TargetRequest.Allows` rather than rebuilding this distinction.

Duplicate targets are rejected unless `AllowRepeated` is true. Indirect damage
uses repeated entries because each entry allocates one point; it is not choosing
the same target repeatedly for one effect. Its `MaximumOccurrences` entry is
the character's current remaining hit points, so a client can render a bounded
allocation control without deriving damage rules from the board. The engine
still validates the submitted allocation.

Search requests also inform visibility. The authorized player may see the legal
hidden targets while the prompt is active. Other viewers may not.

## Costs

`CostOption` describes one printed cost and the sources that can generate
resources toward it. Generation and payment remain separate decisions.

Each offered `ResourceSource` can carry the authored `AbilityName` and an
engine-described `CostDescription` for its non-resource commitments. View exposes
those descriptions only with the authorized source face. Payment distinguishes
discarding a hand card from using the named ability and displays its exhaustion,
counter or other costs before commitment. These facts do not change eligibility.

The record can carry:

- a primary cost and required resource types;
- an alternative cost and its resource requirements;
- the generators available on the current board;
- values the player must define, such as X; and
- several simultaneous resource components that share one payment; and
- engine-projected resource types whose declaration has a proven numeric benefit.

The engine must not choose generators for the player. The selected subset is
part of the answer and is validated against the offered cost. When a generated
icon can be declared as more than one type, or simultaneous components share a
payment, the answer also carries the player's explicit per-icon declaration and
component assignment. A client may suggest an allocation but cannot silently
substitute a deterministic policy for that choice.

An alternative is not flattened into one number. “One mental resource or 2 of
any type” has 2 legal readings, and their resource restrictions differ.

Printed-resource requirements remain distinct from generated resource types. A
wild icon can pay a typed cost but cannot be declared as a physical icon printed
on a card.

`PreferredResourceTypes` is empty unless the checked ability proves that one
declared type improves a numeric result. A decision composer may pre-fill that
single useful declaration as a reversible draft. Several preferences, or an
observable effect the engine cannot rank, remain an explicit player choice.

A resource component may carry `RepeatedResource`: every unit of its `Cost`
requires that resource type. For a variable cost X, the engine resolves X typed
slots, not one typed slot followed by generic slots. It is mutually exclusive
with fixed `Rule` requirements. `ResourcePayment.RequiredResources` supplies
the resolved requirement to draft composition; the renderer does not expand it.
Protocol 20 carries this contract and rejects older endpoints.

## Legality

An affordance may carry an `Illegal` reason so a client can show why a visible
card or action is unavailable. `IsLegal` is true only when that reason is absent.

The engine preflights form, timing, targets, limits, maxima and payment before it
offers an action. Taking an unchanged legal affordance must not reach a second,
stricter legality rule.

The engine validates every answer again. A client cannot create authority by
forging a target, generator, variable or handle that was not offered.

## Prompt context

`Prompt.Asking` identifies why the engine is asking. It distinguishes a turn
menu from target selection, payment, defending, ordering and other suspended
resolution points. `Prompt.DisplayQuestion`, when supplied, is the
engine-authored display question; it is rendered directly rather than recovered
from `Prompt.Label` prose.

The prompt carries the seat that may answer. `Marvel.Server` withholds it from
other visibility scopes and rejects an answer from a capability not authorized
for that seat.

`Prompt.Description` carries readable engine-authored context that applies to
the whole decision, such as the attacker, calculated damage, target health and
relevant attack modifiers during a damage interrupt. Clients display it and do
not recalculate combat state.

`Prompt.PublicKind` supplies a typed, passive purpose for public waiting context.
The table projection keeps the primary pending owner independent of a seat's
authorized off-turn Action menu. It filters `ContextCardIds` to public, faceup
cards already readable to the audience and does not publish private prompt text
or choices. Minion activation ordering has its own public kind, engine-authored
cause and commitment, and an engaged-minion area anchor. The command affordance
identity remains stable.

`Affordance.CostDescription` names mandatory arrow costs beyond the resource
amounts and symbols already in `CostOptions`, including exhaustion, discarding,
counter spending and damage. Cards derives it from the compiled typed ability
cost. View preserves it alongside the exact offered source copy and current
authorized source state. Clients display these facts without inferring payment,
legality or an outcome from the board.

## Persistence and replay

Protocol 27 carries `DirectRecipientCommit` for an engine-admitted support
activation whose only choice is one currently legal recipient. Its target details
state the mandatory exhaustion cost and complete effect. A deliberately named
recipient button may select exactly one target and submit that same prompt-bound
draft. Opening or dismissing the source-local chooser does not pay the cost.

The admitted structural boundary is an in-play support Action with only its own
exhaustion cost, an immediate identities choice, and a draw or constant next-card
discount leaf. It excludes resource costs, labeled abilities, conditional or
nested choices, and other targets. The interpreter validates the recipient before
payment and again when the effect resolves through the existing structural choice
execution; cost events and the Action occurrence retain their order.

An empty target list retains the ordinary separately prompted choice. New traces
can use either form and replay strictly. A suspended exhaustion cost retains the
explicit recipient in the existing continuation results map and revalidates that
recipient when the effect resumes. No future client answer is queued.

The optional target changes recorded prompt shape, so the replay identity is
`engine-replay-v12`. Earlier replay contracts are incompatible; their saved files
remain preserved. Save schema 5, RNG and state-digest formats remain unchanged.
Protocol 27 rejects older endpoints that cannot interpret the commitment marker.

A simulation record stores the chosen affordance’s stable public identity and
the selected targets, resources and variables. Replay resolves that identity
against the newly produced prompt before submitting the answer.

Persisting only `Id` would tie a record to incidental allocation order. Persisting
engine object references would be impossible over the wire and unsafe for hidden
state.

## Product boundary

Affordances expose only actions the supported Core Set can reach. The types are
general enough for later card patterns already considered during DSL design, but
that does not make later products playable. See [scope.md](scope.md).

## Card-play initiation

`Affordance.PlaysCard` is an engine-owned presentation marker. It identifies
ordinary plays and event abilities that initiate a card play, independently
of their action or timing-window verb. Clients use it to stage the source and
open payment, while all targets, costs and legality remain in the offered
contract. Later effect choices do not inherit this marker. Protocol 18 carries
this distinction; labels and printed text are not a substitute for it.

Protocol 19 also carries `Affordance.DeferredTargetSelection`. The engine sets
it when an admitted effect begins with a separate card-target choice, including
a transparent single-step sequence. The current action commits its costs;
the later prompt owns its own answer and legal candidates. Payment is not
reversible from that later choice. Cancellation or an intervening change can
prevent the choice from being reached. A false value makes no promise about
conditional or later branches. Clients must not derive this marker from event
card kind, a missing current `Targets`, or readable text. View preserves the
marker in `AffordancePresentation`; it never projects a future legal target set.

`Affordance.CommitLabel` names the engine-established operation and affected
object for accepting one exact offered choice, such as `Attack Rhino` or
`Discard Avengers Mansion`. It is separate from the candidate's display name
and opaque command label. View copies it only in an authorized prompt; clients
render it without interpreting the effect or changing answer identity.

These additive fields share the increment's unreleased protocol 19 bump.
Readers reject unknown members, so both endpoints must run that version;
the marker itself changes no replay, save or state-digest format. The current
encounter-reveal timing correction is separately versioned as engine replay v3.

## Decline meaning and action consequences

Protocol 19 carries `Prompt.DeclineLabel`, an engine-authored name for declining.
A player turn says `End turn`; ordinary opportunities say `Pass this opportunity`;
defender selection says `Leave attack undefended`. Clients render the supplied
commitment without interpreting a trace label. Both endpoints must use protocol
19 because older readers reject unknown members. Replay/state digest formats
are unchanged.

`Affordance.DisplayLabel` supplies a readable name for a card choice, independently
of the command label. A Rhino target displays `Rhino` while its command label
remains `01094`. The engine supplies this name only after admitting the candidate;
clients use the authorized prompt rather than looking up hidden card identities.

Basic-power descriptions include exhaustion and current conditional ally
consequential damage. Defender offers include exhaustion, hero DEF reduction or
ally damage redirection, while boosts remain unresolved. Phase-end hand requests
name discard commitments and explain the subsequent draw and ready sequence.
These are engine-authored descriptions, preserved by the existing authorized
prompt projection; they do not alter legality or simulate future windows.

`CauseCardIds` separately names ability sources whose suspended continuations await
the current activation. It is passive provenance, not an action or legality input.
The view filters it to readable faces for the authorized prompt and public faces
for the public pending situation. The view names those causes from the filtered
snapshot; engine prose does not embed their titles.

## Simple payment shortfall

`ResourcePaymentProgress` assesses the additional icons for one unrestricted
numeric cost. Decisions exposes the result as `PaymentProgress.RemainingRequired`;
Godot displays it while the draft is incomplete. Typed, printed, alternative,
variable and simultaneous costs return no simple count. Zero does not authorize
submission: normal allocation, declarations and answer validation still apply.
The progress is calculated locally from the authorized offer, with no new wire field.

`Affordance.ContextCardIds` (protocol 40) names the cards causing that exact offer. A shared imminent-defeat window binds each repeated source offer to its own physical recipient. These prompt-local facts do not change its source anchor, effect targets, costs, opaque handle or semantic command address. The existing incarnation eligibility guards and prompt revision bind them to the current occurrence. View copies and filters context identities and supplies readable copy/location descriptions.
