# Shared domain calculations

Rules-owned calculations supply both live procedures and bounded ability analysis.
They do not execute a game, clone `World`, choose player answers or consume RNG.

## Implemented boundaries

| Calculation | Inputs | Consumers |
|---|---|---|
| `EliminationLayout` | Ordered placements, eliminated seats and whether a departure requires permanent attachment resolution | Live `Elimination`, power analysis and repeated-effect traces |
| `DamageAssignment` | Replaced damage, tough presence and the amount returned by prevention | Live damage, damage descriptions and both ability trace paths |

`WorldEliminationLayout` reads the board without creating areas.
`AbilityEliminationLayout` overlays known departures and engagement changes.
The calculation selects the next surviving clockwise seat and retains each engaged
minion's hosted tree. It identifies departure membership and refuses unsupported
permanent attachment resolution before writes occur.

Live elimination performs the ordered moves and rereads departures as its steps
change the board. It owns discard destinations, attack termination, events and
continuous-effect settlement. A departure from the eliminated play area does not
necessarily remove a card from the game: another owner's card can survive in that
owner's discard pile.

`DamageAssignment` applies tough only to positive replaced damage and spends one
status card. It preserves damage dealt when prevention changes damage taken.
Live procedures own replacement callbacks, prevention consumption, placement and
defeat windows. Sharing arithmetic does not make those procedures speculative.

## Projection results

`RuleProjection<T>` has three explicit shapes:

- `Known` holds one calculated result under the supplied read assumptions.
- `Possible` preserves supported alternatives without selecting an answer.
- `Unsupported` names a reached calculation that would require guessing.

Power analysis keeps concrete overlays in `AbilityPowerState`, separately from
these result shapes. It preserves each alternative through later calculations.
It neither merges incompatible damage/status facts nor reads an aggregate as its
first branch. Existing form and readiness assumptions remain explicit within each
overlay; `Known` does not promise knowledge of every future game decision.

An initiation check that needs unsupported facts raises
`RulesNotImplementedException` before payment. A damage description instead names
the unavailable calculation and omits a predicted health transition. Neither uses
unsupported as zero damage or as an empty set of legal actions.

## Behavioral evidence

`EliminationTests.Layout` and `DamageSourceTests.Assignment` pair calculation
expectations with live observations derived from the rules. They distinguish
clockwise skipping, hosted movement order, ownership, last-player elimination,
zero damage, tough consumption, and dealt/taken amounts.

`ActionAbilityTests.DomainProjection` checks public initiation and committed
paths. Both choices remain available after payment discards the source; tough
and healing produce their independently expected damage totals. Another scenario
eliminates the initiating player, relocates the minion's upgrade and status, and
finishes the paid ability. Reads preserve the digest and RNG position.

Mutation checks challenge retained membership, ordering, status consumption,
alternative collapse and unsupported-result handling. Whole-game regression
tests supplement these finite scenarios rather than defining their expected rules.

## Remaining extraction candidates

These boundaries still need distinct calculations before their live and projected
implementations can share more behavior:

| Domain | Current dependency | Required boundary for further extraction |
|---|---|---|
| Continuous values | `TraceHealth`, trace predicates and live continuous effects | Explicit projected source membership and modifier inputs; preserve settlement timing |
| Villain advancement | `AdvancePowerVillain`, repeated traces and live defeat | Stage succession and retained attachments, separate from entry effects and windows |
| Threat and side-scheme defeat | Trace threat maps and live threat procedures | Numeric assignments and defeat eligibility, separate from replacement and completion windows |
| Prevention and other statuses | Live prevention effects, trace status counts and power toughness | Ordered effect-use inputs and status limits, without consuming a future effect |

These are bounded follow-on candidates, not a request for general simulation.
Stage-entry constants, defeat-triggered work and other unsupported projected
dependencies retain their fail-closed boundaries. Further extraction needs a
distinguishing rule-derived scenario before widening those boundaries.
