# Affordances

An affordance is one thing a player can do now. It carries the domain action,
the board object it belongs to, legal targets and ways to pay.

The client renders affordances. It does not derive legal moves from printed card
text or duplicate engine rules.

## Wire shape

`Marvel.Rules.Prompts.Affordance` contains:

| Field | Meaning |
|---|---|
| `Id` | Opaque handle valid in the issuing session |
| `Verb` | Domain action such as `Play`, `Attack`, `Thwart`, `Change_Form` or `Ask` |
| `AnchorId` | Board object the player interacts with |
| `AnchorPlayer` | Seat whose board holds the anchor |
| `Label` | Printed or domain-level option label |
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
(AnchorId, AnchorPlayer, Verb, Label, occurrence among exact matches)
```

The occurrence index matters because repeated choice nodes can be identical on
every other public field. This tuple is an engine wire choice; the tabletop
rules define no persistent command identifier.

## Anchors

Every affordance has an anchor. A play action anchors to the card in hand. A
basic power anchors to the identity. A mid-resolution question anchors to the
card or game element whose ability is waiting.

Anchors let a client highlight the right object without inferring meaning from a
label. `AnchorPlayer` distinguishes multiplayer actions that share the same
domain shape.

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
  candidate when repeated entries are allowed.

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

The record can carry:

- a primary cost and required resource types;
- an alternative cost and its resource requirements;
- the generators available on the current board;
- values the player must define, such as X; and
- several simultaneous resource components that share one payment.

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

## Legality

An affordance may carry an `Illegal` reason so a client can show why a visible
card or action is unavailable. `IsLegal` is true only when that reason is absent.

The engine preflights form, timing, targets, limits, maxima and payment before it
offers an action. Taking an unchanged legal affordance must not reach a second,
stricter legality rule.

The engine validates every answer again. A client cannot create authority by
forging a target, generator, variable or handle that was not offered.

## Prompt context

`Prompt.Question` identifies why the engine is asking. It distinguishes a turn
menu from target selection, payment, defending, ordering and other suspended
resolution points.

The prompt carries the seat that may answer. `Marvel.Server` withholds it from
other visibility scopes and rejects an answer from a capability not authorized
for that seat.

## Persistence and replay

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
