# Rocket Raccoon candidate public admission

Rocket Raccoon is the second admitted part of The Galaxy's Most Wanted (`gmw`,
rules insert `mc16`), beside Groot. His 30 printed faces are the identity
16029a/b, 15 signature cards (16030–16039 with their copies), 17 Aggression and
8 Basic starter cards (16040–16052), obligation Crisis on Halfworld (16053) and
his nemesis set (`rocket_nemesis`): Vendetta (16054), Blackjack O'Hare (16055),
Blackjack's Bazooka (16056) and two Planetary Invasion (16057). The setup record
pins the vendored MarvelSDB quantities (`datasets/marvelsdb/pack/gmw.json`,
`gmw_encounter.json`); no printed divider was inspected, so those quantities are
a repository choice from that snapshot. Admission requires every one of the 30
faces to be authored, to belong to `gmw`, and the exact recipe
(`RocketRaccoonRecipe`).

The rest of the box (Ship Command, the Brotherhood of Badoon, the modular sets,
Drang, Collector 1 and 2, Nebula, Ronan and the campaign) is outside the
boundary. Groot's and Rocket Raccoon's faces are playable; every other `gmw`
face stays closed, and a setup recipe naming one is withheld
(`SetupCatalog.WithoutFaces`). Drang (16058) is the first closed face. Each hero
closes alone: a missing face or a changed recipe closes him and leaves the
other open. The packet plan is [gmw-wave-plan.md](gmw-wave-plan.md)
(MARVEL-904).

## Interpretations

Each choice below is recorded in the row note or the code comment that owns it.
MarvelCDB has no FAQ entry for a `gmw` card; the rulings are Hall of Heroes
entries in `datasets/rulings/`.

- **"Murdered You!"** answers excess damage *dealt*. rr:excess-damage measures
  damage dealt beyond the enemy's remaining hit points; prevention reduces what
  is taken, not what is dealt (rr:prevent.1.1). Rulings dd353e3498f6b923 and
  70423690d446327f: a minion with a tough status card can be dealt excess damage
  and Rocket draws. "You" is Rocket's identity with his events, upgrades and
  resources (rr:you-your.12–.14); an ally's damage is not his (rr:you-your.15).
  Each enemy dealt excess damage is its own instance (rr:triggering-condition.1):
  Relentless Assault's overkill against a 1-hit-point minion that carries excess
  into a 1-hit-point villain draws 2, because Rocket dealt excess damage to both
  (ruling 3f57fe6e7dc99dd2), so Rocket Launcher dealing excess to two minions
  draws 2 as well, and overkill damage counts as damage the attacker dealt
  (ruling 4648617b8449406e). An attack whose damage Temporal Shield (11014) or
  Photographic Reflexes (04104) prevents entirely still deals it
  (rr:prevent.1.3), so its excess draws too.
- **When "Murdered You!" responds.** The response window opens when the attack
  or ability that dealt the damage finishes (ruling ed234608b14481f5, a
  one-sentence attack event). Ruling 80e8d8516006df27 resolves a two-sentence
  event's responses after each sentence. The engine opens response windows when
  an ability's occurrence finishes, for every card, so a multi-sentence ability
  draws the same number of cards after its last sentence instead of between
  sentences. No Rocket Raccoon card has a second sentence that reads the hand or
  deck, but Melee (05030), the ruling's own example, is reachable in a custom
  deck: Rocket ends with the right count and the right board, and draws after
  the second sentence rather than after each.
- **Follow Through** interrupts at rr:damage step 1, when an attack by Rocket's
  identity, event or upgrade is about to deal damage beyond an enemy's remaining
  hit points. It adds 1 to the damage dealt beyond them, so overkill and every
  ability that counts the excess see the larger amount (rr:overkill.3). The
  interrupt is optional and is offered to the first player with one; an
  attack's excess can be increased once per copy. It is offered against an
  attack Temporal Shield prevents entirely, since that attack still deals its
  damage, and the increased amount is prevented with the rest.
- **Schadenfreude** registers a lasting effect until the end of the turn
  (rr:lasting-effects). It heals 2 each time Rocket deals damage to an enemy:
  once per enemy dealt damage, so one attack with overkill into the villain
  heals twice (ruling 4648617b8449406e). Damage dealt counts even when Tough or
  Temporal Shield prevents what the enemy takes (rr:prevent.1.3). Damage an enemy
  cannot take is not dealt (Ultron, 01136), damage to a friendly character is not
  damage to an enemy, and a card played under another player's control, such as
  Electrostatic Armor (10031), deals its damage for its controller.
- **Charge counters.** Battery Pack, Particle Cannon, Rocket Launcher and
  Rocket's Pistol enter play with counters but do not print Uses, so they stay
  in play empty (ruling c0c1782ca297d006). Hand Cannon prints Uses and is
  discarded with its last counter (rr:uses-x-type). Battery Pack cannot move a
  counter onto itself (rr:target.3).
- **Tinkering** chooses and discards a [tech] upgrade as its first instruction,
  then draws 2. The discard pays even if the upgrade does not reach a discard
  pile (ruling 9abcb89111e37613). It is limited once per round.
- **Rocket Launcher** is not an attack. The player is chosen through their
  identity and the villain is dealt damage before the minions, as for Explosive
  Arrow (04006).
- **Salvage** responds after the completed cost it paid, as Pym Particles does,
  and puts a [tech] upgrade from its owner's discard pile on top of the deck.
- **I've Got a Plan** answers Rocket's basic thwart; a confused thwart is not
  one (ruling 983ae8870c226cd4). The +1 THW applies in hero form until the end
  of the phase.
- **The Groot ally** reads the completed attack's defender. A defense event
  used outside an attack is not a defense against an attack (ruling
  f6b7f9804304980b).
- **Booster Boots** uses Backflip's window: Rocket taking damage from an attack
  after pending prevention (rr:damage step 3).
- **Looking for Trouble** discards and engages its minion as the cost, as
  Defender of the Nine Realms (06003) does. Rulings 1fc1a3ff38a01231 and
  a1f4982cfebcd768 govern an emptied encounter deck and legality after payment.
- **Flora and Fauna** (16020 in Groot's deck, 16048 in Rocket's) needs Groot and
  Rocket Raccoon in play, in either form or as an ally (rr:team-up; ruling
  9e3a063b5c91ec99). A Rocket Raccoon upgrade is an upgrade printed in his
  identity set, in any player's play area. With no such upgrade in play the
  second option has no valid target (rr:choose-option.2.2), and the Groot
  option resolves without a choice, as it does in a game without Rocket.
- **Crisis on Halfworld** follows Wilt (16025). "The highest cost upgrade you
  control" ranks only upgrades that can be discarded (rr:permanent.4.1); the
  first player breaks a tie (rr:first-player.1), as for Oversized Hands (07042).
- **Planetary Invasion** reveals the first minion it discards, then gives it a
  tough status card in the same sentence. Ruling ef63f3668700ef73: a quickstrike
  minion's attack is initiated by the reveal and the tough status card is given
  before that attack's steps resolve. When the encounter deck empties first, the
  discard is fulfilled (rr:encounter-deck.2).
- **Blackjack's Bazooka** attaches to Blackjack O'Hare if able, otherwise to the
  villain (rr:attach-to), and is discarded for three mental resources, as for
  Laser Rifle (04072). Vendetta's amplify does not reach a quickstrike attack
  resolved before it enters play (ruling 3678ade5dd527884).

## Shared engine changes

Every change is new vocabulary used only by Rocket Raccoon rows and Flora and
Fauna:

- **Excess damage dealt.** Damage that a player's identity deals to an enemy
  (through its events, upgrades and resources, rr:you-your.12–.14) is recorded
  on its occurrence with the excess beyond the enemy's remaining hit points.
  Excess adds the `WhenExcessDamageDealt` condition; each enemy dealt excess is
  one trigger instance whose actor is that identity. Only excess-damage
  abilities read the record or the condition.
- **The excess-damage interrupt.** An attack by an identity that would deal
  excess damage adds `WhenAttackExcessDamageWouldBeDealt` to its rr:damage
  step 1 occurrence. Step 1 offers optional interrupts on that condition after
  its forced replacements; a step without one finishes exactly as before.
  `increaseExcessDamage` is refused outside that interrupt.
- **Per-damage healing.** `healEachTimeYouDamageAnEnemy` registers a lasting
  effect, until the end of the turn, that heals the resolving identity each
  time it deals damage to an enemy.
- **Counters, deck top, play areas, defenders and reveal status.**
  `moveCounters`, `placeOnTopOfDeck`, the `playerPlayAreas` search area, the
  `defendedBy` condition and `discardUntil` `revealStatus` are new words.
- **Counter placement inside an option.** An option that places counters can
  partially resolve when it has a card and places at least one counter
  (rr:choose-option.2).
- **Effect families.** Counter placement, removal and movement share
  `AbilityCounterEffect`; the excess and per-damage effects share
  `AbilityEnemyDamageEffect`.

Two of these changes reach already admitted cards, so the replay identity is
`engine-replay-v34`. Damage an identity deals to an enemy beyond its remaining
hit points adds the `WhenExcessDamageDealt` condition to that damage's
occurrence, and a response window's persisted label names its occurrence's
conditions: a v33 trace with a response prompt after excess damage, such as
Wasp's games against Klaw or Ultron, records a label v34 no longer produces. An
attack whose damage Temporal Shield (11014) or Photographic Reflexes (04104)
prevents entirely still deals it (rr:prevent.1.3; rulings 70423690d446327f and
05670207b5acc4d9), so Into the Fray (13013, 16042) counts that excess and
removes threat v33 did not remove (`RocketPreventedAttackTests`). Flora and
Fauna offers its choice only while a Rocket Raccoon upgrade is in play, which no
earlier game can contain; without one it resolves the Groot option as before.

## Evidence

The runtime tests use the production ability book on synthetic boards:
`RocketIdentityTests`, `RocketSignatureTests`, `RocketPlayerCardTests` and
`RocketEncounterTests`, with `RocketPreventedAttackTests` and
`RocketDamageCreditTests` for prevented attacks and for which damage counts as
Rocket's. The Battery Pack target choice names the counter it moves, and the
Crisis on Halfworld choice tells the first player it is breaking a tie for the
highest cost (rr:first-player.1). `RocketRaccoonAdmissionTests` closes Rocket Raccoon when
a face is missing, a face changes pack, or any recipe component changes, while
Groot stays open; `GrootAdmissionTests` shows that only the two heroes' faces of
the box open. `RocketLoweringTests` refuses malformed shapes of the new
vocabulary.

## Public journeys

`RocketPublicRestartTests` opens the published starter through `EngineHost`
against Rhino with seed 1637 and keeps the dealt hand. Rocket begins in
alter-ego form and plays Battery Pack (2 charge counters) and Rocket's Pistol
(3). At the Battery Pack target choice it copies the strict save and authority
metadata into a fresh host, rejects the stale answer, then moves the counter on
both hosts: the pistol holds 4 and the exhausted Battery Pack 1. It undoes and
redoes the move and compares events, save, prompt, digest, RNG state and pending
work against the uninterrupted host.

`RocketPublicGameTests` plays the unarranged starter against Rhino, Klaw,
Ultron, Crossbones, Absorbing Man and the Wrecking Crew, beside an exact
same-seed replay of every decision's prompt, events, digest and RNG. Every game
reaches an ending. These games establish complete games and replay, not
victory. The research policy declines optional abilities.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 1 | VillainWins | 4 | 46 |
| Klaw | 1 | 2 | PlayersLose | 4 | 28 |
| Ultron | 1 | 3 | PlayersLose | 2 | 19 |
| Crossbones | 1 | 4 | PlayersLose | 4 | 34 |
| Absorbing Man | 1 | 5 | PlayersLose | 3 | 36 |
| Wrecking Crew | 1 | 6 | PlayersLose | 3 | 24 |
| Rhino | 2 | 7 | VillainWins | 7 | 102 |
| Absorbing Man | 2 | 8 | PlayersLose | 7 | 88 |

`RocketAbilitiesResolveThroughACompleteReplayedGame` plays three more
unarranged games with the same replay check. Outside the hand, it takes every
free, untargeted optional ability that a Rocket Raccoon card in play offers,
such as "Murdered You!", Follow Through, Hand Cannon and Bug.

| Scenario | Players | Seed | Outcome | Round | Decisions | Taken |
|---|---:|---:|---|---:|---:|---:|
| Rhino | 1 | 9 | VillainWins | 7 | 42 | 21 |
| Klaw | 1 | 10 | PlayersLose | 4 | 22 | 9 |
| Crossbones | 1 | 11 | PlayersLose | 4 | 26 | 14 |

`FloraAndFaunaChargesARocketUpgradeAtAGrootAndRocketTable` seats Rocket Raccoon
beside Groot, both on their published starters, with the same replay check.
Whenever Flora and Fauna offers its Rocket option it takes it.

| Scenario | Players | Seed | Outcome | Round | Decisions | Taken |
|---|---:|---:|---|---:|---:|---:|
| Rhino | 2 | 12 | PlayersLose | 5 | 92 | 2 |
| Absorbing Man | 2 | 21 | PlayersLose | 6 | 85 | 1 |

A scan of the public starter against every admitted scenario mode (39 modes,
seeds 1–40, 1,560 games per policy) ends every game without a raise or
failure, both with the research policy and with the policy that also takes
Rocket Raccoon's free optional abilities: 3,120 games in all.

## Native client

No native journey was run. Every Rocket Raccoon decision reuses an existing
interaction: charge counters are card counters, Tinkering, Battery Pack, Salvage
and Flora and Fauna choose cards or options through existing selections, Crisis
on Halfworld presents option choices, and the interrupts and responses use the
existing windows. Follow Through's opportunity reuses the interrupt opportunity
prompt at damage step 1. These are not materially new UI interactions, so no
owner review item is needed. Windows/Linux native runs, other scales and package
journeys are not established here.
