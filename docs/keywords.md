# Keywords, icons, and status cards

The engine implements a keyword from its Rules Reference definition, not from
reminder text. `CardCatalog` reads printed keyword lines from
`datasets/cards/`, and the rules layer supplies the behavior. A keyword on an
unsupported card does not make that card executable.

## Core Set behavior

The Core product boundary exercises these keyword families directly:

| Keyword | Engine behavior |
|---|---|
| Guard | the engaged player cannot attack a villain while that minion is in play |
| Overkill | excess attack damage crosses from a defeated minion to the villain, or from a defeated ally to its controller's identity |
| Retaliate X | after an eligible character is attacked, it deals X damage to the attacker |
| Surge | When Revealed, deal the resolving player another facedown encounter card |
| Toughness | after the character enters play, give it a tough status card |
| Uses (X type) | enter play with the printed number and kind of counters |
| When Defeated | resolve the card's ability before it leaves play |

Core cards and scenario rules also use the following icon behavior:

| Icon | Engine behavior |
|---|---|
| Acceleration | add one threat during the villain phase's threat step |
| Boost | add to the activating enemy's ATK or SCH |
| Consequential damage | damage an ally after it attacks or thwarts |
| Crisis | prevent player cards from removing threat from the main scheme |
| Hazard | deal one additional encounter card |
| Per-player (`*`) | scale the printed value by the starting player count |

The tables summarize runtime behavior, not the full Rules Reference vocabulary.
General implementations for other keywords are exercised by focused rules tests
and are available for coherent future product work; they do not admit expansion
cards to the executable catalog. See [scope.md](scope.md).

## Status cards

Tough, stunned, and confused are card objects rather than boolean fields. Each
has its own object id and location.

- Tough prevents the applicable damage instance and is discarded. Damage
  reduced to zero before that point does not spend it.
- Stunned replaces the next qualifying attack.
- Confused replaces the next qualifying thwart or scheme.

Steady and stalwart are implemented as status limits: steady requires an
additional matching status card before the character is afflicted, while
stalwart prevents confused and stunned status cards entirely.

## Parsing printed keywords

`rr:keywords.1` places keywords at the start of the text box as distinct
sentences. `Keywords.Line` reads that initial run and stops at the first
ordinary sentence or timing trigger. Reminder text does not create behavior.

This prevents prose such as an ability that says “Surge” or refers to another
form from being mistaken for a printed keyword. Keyword arguments retain their
printed type: a count is parsed as a count, while a trait or title remains a
string.

## Ordering and choices

Keyword-provided abilities enter the same occurrence and resolution ledger as
printed ability rows. Forced effects resolve before optional effects. When two
simultaneous forced effects require an order, the first player chooses; the
engine does not substitute object-id or collection order.

Restrictions and payments are likewise checked before an affordance is offered.
The runtime never presents a keyword action that is known to fail when taken,
and an unimplemented reachable operation raises `RulesNotImplementedException`
instead of producing a plausible partial result.
