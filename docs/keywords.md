# Keywords, and which of them the engine has

`rr:keywords` lists 28, and the Rules Reference writes almost every one out as
the ability it is equivalent to. That sentence is what the engine implements —
not the reminder text on the card.

Icons are the same shape and are here too: they have their own entries and their
own "equivalent to the following constant ability" line.

## Implemented

| keyword | the ability it is equivalent to | where |
|---|---|---|
| **Assault** | a basic thwart against this scheme uses ATK instead of THW | `BasicPowers` |
| **Form** | grants an identity a unique form | [forms.md](forms.md) |
| **Guard** | the engaged player cannot attack any villain | `BasicPowers.Attackable` |
| **Hinder X** | enters play with X threat **on the card** | `Reveal.EnterPlay` |
| **Incite X** | *When Revealed: place X threat on the main scheme* | `Reveal.Keywords` |
| **Overkill** | excess damage from a defeated ally goes to its controller's identity; from a minion, to the villain | `Damage.Attack` |
| **Patrol** | the engaged player cannot thwart the **main** scheme | `BasicPowers.Thwartable` |
| **Piercing** | discard **each** tough status card before dealing damage | `Damage.Attack` |
| **Quickstrike** | *Forced Response (Hero): after this minion engages a player, it attacks that player* | `Reveal.Quickstrike` |
| **Ranged** | this attack ignores retaliate | `Damage.Attack` |
| **Restricted** | a third one forces a choice, after it is in play | `CardPlay` |
| **Retaliate X** | *Forced Response: after this character is attacked, deal X damage to the attacker* | `Damage.Retaliate` |
| **Stalwart** | cannot have confused or stunned status cards | `Statuses.Limit` |
| **Steady** | one additional card of each; not afflicted until two | `Statuses.Limit` |
| **Surge** | *When Revealed: deal yourself 1 facedown encounter card* | `Reveal.Keywords` |
| **Temporary** | *Forced Interrupt: when the round ends, discard this card from play* | `PhaseEnd` |
| **Toughness** | *Forced Response: after this character enters play, give it a tough status card* | `Reveal.EnterPlay` |
| **Uses (X "type")** | enters play with X counters of that type | `Reveal.EnterPlay` |
| **Victory X** | *When Defeated: add this card to the victory display* | `Defeat` |
| **Villainous** | *Forced Interrupt: when this character uses a basic power, give it a boost card* | `Keywords.IsBoosted` |
| **Vulnerable** | *Forced Interrupt: when this character becomes confused or stunned, discard it* | `Statuses.Vulnerable` |

| icon | | where |
|---|---|---|
| **Acceleration** | 1 additional threat during Place Threat | `MainScheme.Acceleration` |
| **Amplify** | each boost card gains one boost icon | `MainScheme.Amplify` |
| **Boost** | adds to the activating enemy's ATK or SCH | `Attack`, `VillainPhase` |
| **Consequential damage** | an ally takes it after attacking or thwarting | `BasicPowers.AllyPower` |
| **Crisis** | player cards cannot remove threat from the main scheme | `MainScheme.Crisis` |
| **Hazard** | one additional encounter card during Deal Encounter Cards | `Deal.HazardIcons` |
| **Per-player (`*`)** | multiply by the player count | `CardCatalog.PrintedValue` |

The three status cards — **tough**, **stunned**, **confused** — are in
`Statuses` and `Damage`. They are cards, not flags: the recorded board shows a
Tough on Rhino with its own object id, and Rhino's own `toughness` field at
zero.

## Not implemented

| keyword | cards | what it needs |
|---|---|---|
| **Permanent** | 86 | "except by card abilities **in the same set**" needs the effect's set, not just the card's |
| **Setup** | 39 | a setup step that puts cards into play before step 1 |
| **Team-Up** | 28 | a play restriction naming two characters in play |
| **Linked** | 14 | set-aside cards brought in by the card that names them |
| **Alliance** | 13 | other players helping pay a cost |
| **Requirement** | 13 | specific resources that must be *spent*, not merely generated |
| **Peril** | 12 | table talk, and other players not acting |
| **Teamwork** | 31 | a minion activating on another minion entering play |

**Uses is half done and says so**: the counters are placed, and "when the last
all-purpose counter is removed, discard that card" waits for an ability that can
remove one — no node in [the card DSL](card-dsl.md) does yet.

## Two things the engine decides that a player should

Both are `rr:forced.5` — "if two or more forced abilities would initiate at the
same moment, **the first player determines the order**" — and both are
MARVEL-187:

- a card carrying **surge** and its own When Revealed text has two abilities
  initiating at once, and the engine runs the keyword first;
- **engaged minions** activate "in the order of that player's choice"
  (`rr:villain-phase.step.2.b`), and the engine takes them in play-area order.

A third is `rr:restricted`: **which** restricted card leaves is the player's
choice, and the engine keeps the one just played.

All three are deterministic and stated in comments, so a replay reproduces.
They become visible the moment two of the abilities interact.
