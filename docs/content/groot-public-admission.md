# Groot candidate public admission

Groot is the first admitted part of The Galaxy's Most Wanted (`gmw`, rules
insert `mc16`). His 29 printed faces are the identity 16001a/b, 15 signature
cards (16002–16011 with their copies), 17 Protection and 8 Basic starter cards
(16012–16024), obligation Wilt (16025) and his nemesis set (`groot_nemesis`): Blazing
Inferno (16026), Furnax (16027) and three Fan the Flames (16028). The setup
record pins the vendored MarvelSDB quantities
(`datasets/marvelsdb/pack/gmw.json`, `gmw_encounter.json`); no printed divider
was inspected, so those quantities are a repository choice from that snapshot.
Admission requires every one of the 29 faces to be authored, to belong to
`gmw`, and the exact recipe (`GrootRecipe`).

The rest of the box (Rocket Raccoon, Ship Command, the Brotherhood of Badoon,
the modular sets, Drang, Collector 1 and 2, Nebula, Ronan and the campaign) is
outside the boundary. Runtime admission opens a pack's faces part by part:
Groot's faces become playable while every other `gmw` face stays closed, and a
setup recipe naming one of those closed faces is withheld
(`SetupCatalog.WithoutFaces`). The packet plan is
[gmw-wave-plan.md](gmw-wave-plan.md) (MARVEL-904).

## Interpretations

Each choice below is recorded in the row note or the code comment that owns it.
MarvelCDB has no FAQ entry for a `gmw` card; the rulings are Hall of Heroes
entries in `datasets/rulings/`.

- **Flora Colossus** is a forced interrupt at rr:damage step 3 ("when
  [character] would take any amount of damage"). It removes up to the incoming
  damage in growth counters and prevents 1 damage per counter removed
  (`preventDamageWithCounters`). Ruling a1a372ea9b59ba4e: with 3 counters
  against 5 damage it removes 3 and Groot takes 2. A tough status card applies
  first (rr:damage step 2; rulings 7cf2a0ea8a6b2a1f and c8268c2138f6c809), so
  Tough prevents the damage and no counters are spent. Indirect damage assigned
  to Groot is dealt to him and also triggers it. It is printed on the hero face
  only (rr:identity.4).
- **"To a maximum of 10"** on Growth Spurt, Fruition, Root Stomp, Fertile
  Ground and Flora and Fauna caps what that one placement can leave on Groot
  (rr:max-maximum.6; ruling 39c26b6b8d77edfd). The placement is
  `min(n, 10 − counters)`. An ability that would then change nothing is not
  offered; Fertile Ground stays available at 10 because it also draws.
- **"We Are Groot"** offers four options, remove 1–4 counters. Its "up to"
  cost has a minimum of 1 (rr:cost.9). An option needing more counters than
  Groot has is not offered (rr:choose-option.2). The removal is the first
  instruction of the chosen option rather than a separate cost step. That is
  an engine choice: no admitted card responds to spending a cost, so the order
  is not observable.
- **Entangling Vines, Vine Spikes and Rocket Raccoon** interrupt one basic use.
  The engine adds two views of the basic-use interrupt:
  `WhenHeroBasicThwartInitiated` (hero thwart only) and
  `WhenAllyBasicAttackInitiated` (ally attack only). The bonus belongs to that
  use (rr:basic-power.1). Rocket's overkill applies only to his attack on a
  minion.
- **Lashing Vines** has three response triggers: after Groot's basic thwart,
  basic attack or defense. A stunned attack or confused thwart is not a use
  (ruling 983ae8870c226cd4). The three share no limit; the 2-counter cost
  bounds repetition.
- **Starhawk** returns to hand when the damage he would take after prevention
  exactly equals his remaining hit points (`incomingDamage`). Ruling
  dc116cb456de74af: a card can take damage beyond its remaining hit points, so
  more damage does not match. Ruling 7e6dab4d8d2f1fc9: overkill does not lower
  what he takes.
- **Starting hit points** on Fighting Fit and Dauntless are the printed value
  (rr:hit-points.1). Ruling 7cb6a0fe6e2d6039 predates RR 1.5 and reads the
  value after setup; Rules Reference 1.8 governs.
- **Hard to Ignore** follows Unflappable (09020): a defense event used outside
  an attack is not a defense against an attack (ruling f6b7f9804304980b).
- **No-effect offers.** Flora and Fauna is not offered at 10 growth counters
  with Groot ready: no part of it can resolve, and an ability with no effect
  cannot be initiated (rulings 342e7b62e13a6806, e015aa6cd98e8ef2). Wilt's
  optional flip, like every obligation's, offers only "Remain in your current
  form" to a player already in alter-ego form, because the flip has no target
  (rr:choose-option.1); the Core corpus pins that one-option decision.
- **Flora and Fauna** needs Groot and Rocket Raccoon in play, in either form or
  as an ally (rr:team-up; ruling 9e3a063b5c91ec99). Rocket Raccoon's upgrades
  are not authored, so none can be in play, and the row authors only the Groot
  option. `FloraAndFaunaAuthorsTheRocketOptionWhenARocketUpgradeIsAuthored`
  fails when an upgrade is authored before the second option is.
- **Deft Focus** reduces its controller's next SUPERPOWER card by 1 until the
  end of the current turn (ruling 01084513ea5772f5). An unused reduction
  expires then.
- **[guardian]** in "Play only if your identity has the [guardian] trait" is
  the icon spelling of the GUARDIAN trait (Rocket Raccoon and the other
  Guardian cards).
- **Wilt** may flip Groot to alter-ego form, then offers its two choices. The
  removal does as much as it can (3, 2 or 1 counters); with none it gains
  surge. The structure follows Need for Speed (14024).
- **Blazing Inferno, Furnax and Fan the Flames** deal indirect damage that each
  player assigns (rr:indirect-damage). For "each player" the first player
  orders the players (rr:each-player.1). Furnax's activation is an attack or a
  scheme (rr:activation).

## Shared engine changes

Every change is new vocabulary used only by Groot rows, except the following:

- **Tough before "would take" interrupts.** rr:damage applies tough status
  cards (step 2) before abilities that trigger when a character would take
  damage (step 3). A target whose tough status card will prevent the damage now
  has no damage left for such an interrupt
  (`DamageWindowEligibility.RemainingDamage`). Backflip (01003) and the other
  admitted "would take" interrupts are no longer offered to a character
  holding a tough status card unless the damage ignores Tough. A piercing enemy
  attack discards the tough status card before its damage window opens
  (rr:piercing.1), so those interrupts still see its damage. Crossbones' Armor
  (04065) and Norman Osborn's infamy replacement (02001a–02003a) are "would take"
  abilities too: a tough status card prevents the damage first and nothing
  reaches them (ruling c8268c2138f6c809), while damage that ignores Tough still
  does. A v32 trace can record a prompt or outcome that v33 does not produce, so
  the replay identity is `engine-replay-v33`
  (`AToughStatusCardLeavesNoDamageForAWouldTakeInterrupt`,
  `APiercingAttackDiscardsToughBeforeAWouldTakeInterruptIsOffered`,
  `WouldTakeAfterToughTests`).
- **Basic-use bonus descriptions.** A bonus offered on a thwart-only interrupt
  reads "Add 2 THW for this basic thwart only" (Entangling Vines); any other
  fixed bonus keeps its ATK wording.
- **Prevention spent against the remaining damage.** `preventDamageWithCounters`
  removes only as many counters as there is damage left to prevent.
- **Basic-use interrupt views.** The two new step conditions fire on existing
  basic-use occurrences and add no step or event. Only Groot rows listen to
  them, so a game without those cards opens no new prompt. Overkill may now be
  granted by an ally basic-attack interrupt as well as a hero one.
- **Typed, turn-long cost reductions.** `reduceNextCardCost` accepts a trait or
  kind filter and `until: EndOfTurn` for a single player. The existing phase
  reduction is unchanged.
- **The bracketed trait token.** A play restriction naming `[guardian]` reads
  the GUARDIAN trait. No other admitted restriction uses brackets.

## Evidence

The runtime tests use the production ability book on synthetic boards:
`GrootIdentityTests`, `GrootSignatureTests`, `GrootPlayerCardTests` and
`GrootEncounterTests`. `GrootAdmissionTests` closes Groot when a face is
missing, a face changes pack, or any recipe component changes, and shows that
only his faces of the box open. `GrootLoweringTests` and
`BasicAttackOverkillTests` refuse malformed shapes of the new vocabulary.

## Public journeys

`GrootPublicRestartTests` opens the published starter through `EngineHost`
against Rhino with seed 1601 and keeps the dealt hand. Groot begins in
alter-ego form. At the Growth Spurt offer it copies the strict save and
authority metadata into a fresh host, rejects the stale answer, then takes the
action on both hosts: Groot has 2 growth counters. It undoes and redoes the
action and compares events, save, prompt, digest, RNG state and pending work
against the uninterrupted host. The once-per-round limit then withholds Growth
Spurt.

`GrootPublicGameTests` plays the unarranged starter against Rhino, Klaw,
Ultron, Crossbones, Absorbing Man and the Wrecking Crew, beside an exact
same-seed replay of every decision's prompt, events, digest and RNG. Every game
reaches an ending. These games establish complete games and replay, not
victory. The research policy declines optional abilities.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 1 | VillainWins | 4 | 26 |
| Klaw | 1 | 2 | PlayersLose | 3 | 19 |
| Ultron | 1 | 3 | PlayersLose | 2 | 18 |
| Crossbones | 1 | 4 | PlayersLose | 5 | 38 |
| Absorbing Man | 1 | 5 | PlayersLose | 4 | 31 |
| Wrecking Crew | 1 | 6 | PlayersLose | 3 | 23 |
| Rhino | 2 | 7 | VillainWins | 7 | 108 |
| Absorbing Man | 2 | 8 | PlayersLose | 4 | 54 |

`GrootAbilitiesResolveThroughACompleteReplayedGame` plays three more
unarranged games with the same replay check. Outside the hand, it takes every
free, untargeted optional ability that a Groot card in play offers, including
Growth Spurt from the turn menu, so Flora Colossus spends real counters.

| Scenario | Players | Seed | Outcome | Round | Decisions | Taken |
|---|---:|---:|---|---:|---:|---:|
| Rhino | 1 | 9 | PlayersWin | 8 | 50 | 25 |
| Klaw | 1 | 10 | PlayersLose | 5 | 30 | 10 |
| Crossbones | 1 | 11 | PlayersLose | 6 | 35 | 19 |

A scan of the public starter against every admitted scenario mode (39 modes,
seeds 1–40, 1,560 games per policy) ends every game without a raise or
failure, both with the research policy and with the policy that also takes
Groot's free optional abilities: 3,120 games in all.

## Native client

No native journey was run. Every Groot decision reuses an existing
interaction: growth counters are card counters, "We Are Groot" and Wilt present
option choices, "We Are Groot" chooses tough recipients through the existing
multi-target selection, indirect damage uses the existing assignment, and the
interrupts and responses use the existing windows. These are not materially new
UI interactions, so no owner review item is needed. Windows/Linux native runs,
other scales and package journeys are not established here.
