# Kang setup recipes, complete games and admission

MARVEL-568 packet P6, branch `feat/kang-recipe` from `75c52fdc`. The Once and
Future Kang has engine- and server-level evidence through its real setup path.
Public admission is withheld until the client can show separated game areas;
[the client specification](#p8-client-specification) below defines that packet.

## Recipes

`datasets/setup/setup.json` declares `kang` and `kang_expert` from printed 11007a
(contents and setup), `pack:mc11:setup` and `pack:mc11:adjustable-difficulty`.
[setup-dataset.md](../setup-dataset.md) records the shape:

- Kang (I) is the villain deck (11001; Expert 11034).
- Stages 1, 2 and 4 are the main scheme deck in that order (MARVEL-894 Q13).
- Every Kang (II), Kang (III), the four stage-3 schemes and four Kang's Dominion
  copies are set aside. The set-aside stage-3 cards are the pool 2A draws from.
- The other 26 Kang-set cards are the scenario's encounter cards, at the pinned
  MarvelSDB quantities. Standard, plus Expert in `kang_expert`, and the
  recommended Temporal set complete the encounter deck.
- `remove_hero_obligations: true` leaves every identity's obligation undealt
  ("remove each player's obligation cards from the game"); nemesis sets stay
  set aside for 4B. Fear of Kang (11049) is a Master of Time encounter card, not
  a player's obligation, so it stays in that set (Q13).

The recommended combinations are both modes, each of Temporal, Master of Time
and Anachronauts, and one to four players: 24. `KangRecipeSetupTests` deals all
24 through `ScenarioDealer.Prepare` and `WorldSetup`, then checks the villain,
the main scheme deck under 1B, the set-aside pool, the absence of the other
mode's villains, one dealt encounter card per player, the complete encounter
composition, removed obligations with nemesis sets aside, and seeded
determinism. `KangRecipeAdmissionTests` checks that all 58 printed faces are
authored and that admission excludes both recipes even when every face is
authored. The two records are adjudicated `outside-core`, and the behavior
catalog is regenerated.

## Complete games through the real setup

`KangRecipeFactory` (tests only) implements the host's `IDurableGameFactory` seam
with the same three steps `DatasetGameFactory.Create` takes for a standalone
game, over the complete declared catalog, and refuses any other scenario. That
is the only difference from the production factory: it bypasses admission
alone, never the dealer or setup.

`KangRecipeGameTests` plays each game beside a same-seed replay and compares
state digest, RNG state, phase, round, prompt, events and information after
every decision. The research `CoreGamePolicy` answers, after
`KangInsertAdvice`. While the table is separated, that advice follows the
insert ("If a player is in danger of being defeated by Kang (II), that player
should change to alter-ego form and recover hit points"); half printed hit
points is our threshold.

| Recipe | Modular | Players | Seed | Outcome | Round | Decisions | Split | Reunion and Kang (III) |
|---|---|---:|---:|---|---:|---:|---|---|
| Standard | Anachronauts | 1 | 1 | all players eliminated | 5 | 48 | yes | yes |
| Standard | Master of Time | 2 | 1 | all players eliminated | 8 | 133 | yes | yes |
| Standard | Master of Time | 3 | 15 | all players eliminated | 6 | 143 | yes | yes |
| Standard | Temporal | 4 | 4 | all players eliminated | 5 | 152 | yes | yes, through an eliminated player's area |
| Expert | Anachronauts | 1 | 36 | all players eliminated | 4 | 34 | yes | yes |
| Expert | Master of Time | 2 | 11 | all players eliminated | 5 | 95 | yes | yes |
| Expert | Temporal | 3 | 1 | 1B completed | 3 | 93 | no | no |
| Expert | Temporal | 4 | 1 | 1B completed | 4 | 121 | no | no |

No game reaches a player victory. Under this policy, Expert at three and four
players never defeats Kang (I) in the scan, so its split and reunion at those
counts rest on the existing fixture-board tests (`KangPlayerCount*`,
`KangCoincidentDepartureTests`), not on a game dealt from the recipe.

`KangRecipeRestartTests` opens Standard, Temporal, two players, seed 1 on
`EngineHost` with a `MemorySessionStore`. It restarts on a fresh host once the
table is separated, and again while the first player's join destination is
pending. Every later answer goes to both hosts until the reunion reveals 4A and
Kang (III). After every answer the state digest, RNG, phase, prompt, agenda and
effects match. After the first answer and at each checkpoint, the strict save
bytes, revision, world and history projections also match. The unrestarted
game (`KangRecipeGames`, same seed) ends with all players eliminated in round 6. The shared `PublicRestartJourney` now takes a factory, so the Hulk-based
callers are unchanged.

## Engine changes

The scan reached these paths. Each one raised or threw before, and each now has
a focused test and a killed mutant. Only Kang content separates the table or
grants the hero-specific prohibition, so admitted content and saved prompts are
unaffected. The replay identity stays `engine-replay-v32`.

| Path | Rule | Test |
|---|---|---|
| A requested Action event (rr:player-turn.6) is judged by the acting seat's hero-specific prohibition, as its offer already was | rr:player-turn.6, rr:cannot | `KangDepoweredRequestedActionTests` |
| An eliminated seat's activation step advances before any villain lookup | rr:player-elimination.6 | `KangEliminatedActivationTests` |
| An eliminated player's local area remains until its own phase-end departure, so a co-due last group joins it instead of going central | 11008b, rr:player-elimination.step.5, rr:first-player.3 | `KangRetainedReunionTests` |
| A departing group with no players combines into an area with players (our choice) | 11009b–11012b | `KangRetainedReunionTests` |
| Sir Raston's boost, after its damage eliminates the player, still gives its enemy a boost card, which waits facedown | rr:player-elimination.5, rr:boost-boost-icon.6 | `KangSirRastonEliminationTests` |
| An ability resumes after the local stage completion that eliminating an area's last player schedules ahead of it | rr:player-elimination.5 | `KangEliminatingAbilityTests` |
| Time-Travel Hijinks' storage does nothing when its obligation left play with the eliminated player | rr:then.1 | `KangTimeTravelHijinksEliminationTests` |

## Interpretation choices

- No ruling covers Sir Raston after an eliminating boost. "Give this enemy
  another boost card" names the enemy the card boosts, which the ended
  activation does not change. rr:boost-boost-icon.6 says what happens to a
  boost card dealt outside an activation.
- No ruling covers a stage-3 area whose only player was eliminated. Its Kang
  and completed stage stay until the printed phase-end removal, so the area is
  still a game area that 2B's "unless there are no other game areas remaining"
  counts. Either departure order ends in one reunion.
- A group with no players joins only an area with players. Joining two
  playerless groups first would leave the same components in the same final
  area, chosen by the same first player.
- The Hall of Heroes rulings, the MarvelCDB FAQ and the mc11 insert were
  searched for each of these; none addresses them.

## Mutants

Each was compiled after its commit, run against the focused tests, then
restored with `git checkout` and a touch.

| Mutant | Result |
|---|---|
| Hero-specific check reads the prompt's player | killed (`KangDepoweredRequestedActionTests`, pre-fix run) |
| Villain lookup before the eliminated-seat check | killed (`KangEliminatedActivationTests`) |
| `Remains` ignores retained components | killed, 4/4 |
| Join destinations ignore retained areas | killed, survivor-first cases |
| Playerless groups may join retained areas | killed, 3-player case |
| `Holds` drops the live-retention check | survived; equivalent. Every retained group departs, clearing its entries, before any of its cards can leave another way, because no player remains to affect that area |
| Boosted enemy fallback returns nothing | killed, 2/2 |
| Resume only when the ability frame is current | killed, 2/2 |
| Storage guard removed | killed |
| Obligation flag ignored | killed, 26 cases |
| Nemesis sets also removed | killed |
| Flag accepts only `true` without rejecting other values | killed |

## Zero-raise scan

A temporary test, not committed, played every recipe mode × modular set × one
to four players × seeds 1–40 (960 games) through `KangRecipeFactory`, once with
the default `CoreGamePolicy` and once with `KangInsertAdvice`.

| Policy | Games | Raises | Failures | Split | Reunion and Kang (III) | Endings |
|---|---:|---:|---:|---:|---:|---|
| default | 960 | 0 | 0 | 79 | 0 | 655 scheme, 305 eliminated |
| insert advice | 960 | 0 | 0 | 79 | 51 | 656 scheme, 304 eliminated |

The environment boundary (Q8) stays a raise. No environment is in either
recipe, its modular sets, the Standard and Expert sets or any declared nemesis
set, so no Kang game reaches it.

## Admission decision

Kang stays out of the public catalog. Its table splits into per-player game
areas, each with its own Kang and main scheme, beside a central 2B. The Godot
client renders one villain area and one main scheme, and has no presentation
for game areas. Playing Kang there would hide which Kang threatens whom,
which cards a player can target, and why. The skill reserves owner review for
"multiple villains ... sharing a location the heroes must protect", which
this is.

## P8 client specification

### Facts that already exist (View, protocol 44)

- `WorldDescriptor.GameAreas`: each game area's id and the seats in it. While
  the table is separated, central has no entry; an eliminated player's area can
  remain with no seats until its departure.
- `PlayerDescriptor.GameArea`: each seat's game area id, or null when
  eliminated.
- `CardLocationDescriptor.GameArea`: each addressable card's game area, or null
  for central 2B and other cards in none. It also covers an eliminated
  player's retained Kang, stage and side schemes, whose area's seat has left.
- `AreaDescriptor.Owner`: per-seat `VillainArea`, `MainSchemesArea` and
  `SideSchemesArea` hold each area's Kang (II), stage 3B and local side
  schemes. Owner −1 holds central 2B and, after the reunion, 4A/4B and Kang (III).
- Events: `PlayAreaJoined` and `PlayAreaDetached` (presented as "… joined game
  area N"), `CardsMoved` with the verbs Advance, Join and Store_Beneath.
- Prompts: `ResolveKangPhaseJoin` ("Choose a game area to join", then
  "Choose a matching unique card to discard"), `OrderKangPhaseDepartures`
  ("Choose the next Kang departure to resolve") and `OrderKangPhaseEnd`
  ("Choose which phase-end effect resolves first"). Each carries
  `DisplayQuestion`, `Description`, per-offer `DisplayLabel`/`CommitLabel` and
  `ContextCardIds`.
- Engine offers already filter attack, thwart, defence and card targets to what
  the source can affect.

### What the client must show

1. Each game area as its own region: its seats, its Kang with health and
   statuses, its stage 3B with threat, target and acceleration, its side
   schemes and its encounter cards. Central 2B stays visible to everyone, with
   its acceleration tokens and the note that they wait for 4A.
2. Which game area the viewing seat is in, and that its cards cannot affect or
   be affected by another area. 2B is the only exception.
3. An eliminated player's retained area until it departs: its Kang and
   completed stage, marked as leaving at the end of the phase.
4. The stage-2A reveals in turn order, with each new area forming, and the
   reunion: areas combining, side schemes and engaged minions moving with
   their group, 4A revealing Kang (III) and the stored Dominions.
5. The join, departure-order and phase-end-order prompts, with seat names and
   each area's Kang named in every option.
6. Event text that names the seats in a game area rather than its number.

### What is missing

- **Pending departures.** No View fact says that a Kang (II) defeat or a
  completed 3B will move a group at the end of the phase, or that a Dominion
  has been stored. Add an engine-owned, visibility-safe descriptor of pending
  departures: source area, group, cause (defeat or completion) and boundary.
- **A semantic summary per area.** Clients would have to assemble the local
  Kang, scheme and threat from zones and owners. Add a `GameAreaDescriptor`
  summary: local villain id, local main scheme id, seats, whether the area is
  retained, and whether it is central.
- **Stored Dominions.** The concealed pile under 4A sits in the main scheme
  deck. Its count, the stored-Dominion total, needs an explicit public fact.
- **Prompt and event labels.** They use numeric area ids ("game area 3").
  They need seat names and the local Kang's name from the View contract, not
  from the renderer.
- **Reach explanations.** A target that is not offered because it is in
  another area has no reason the client can show. If the UI must explain the
  absence, the engine has to publish it.

A separate UI packet (P8 View/Protocol, then P9 Godot) implements these, with
native evidence and an owner review item for the new separated-table
interaction.
