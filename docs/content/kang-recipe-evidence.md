# Kang setup recipes, complete games and admission

MARVEL-568 packet P6, branch `feat/kang-recipe` from `75c52fdc`. The Once and
Future Kang has engine- and server-level evidence through its real setup path.
The Godot client renders the separated table
([P9](#p9-godot-split-table)). Public admission is withheld until the owner
approves that interaction in MARVEL-905.

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
- `remove_hero_obligations: true` deals every identity's obligation into the
  removed-from-game area ("remove each player's obligation cards from the
  game"), where 27135 Loose Ends can still find it (ruling 78ee6578c6efb661);
  nemesis sets stay set aside for 4B. Fear of Kang (11049) is a Master of Time encounter card, not
  a player's obligation, so it stays in that set (Q13).

The recommended combinations are both modes, each of Temporal, Master of Time
and Anachronauts, and one to four players: 24. `KangRecipeSetupTests` deals all
24 through `ScenarioDealer.Prepare` and `WorldSetup`, then checks the villain,
the main scheme deck under 1B, the set-aside pool, the absence of the other
mode's villains, one dealt encounter card per player, the complete encounter
composition, the obligations in the removed-from-game area with nemesis sets
aside, and seeded
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
| Standard | Anachronauts | 1 | 1 | all players eliminated | 5 | 46 | yes | yes |
| Standard | Master of Time | 2 | 1 | all players eliminated | 8 | 124 | yes | yes |
| Standard | Master of Time | 3 | 15 | all players eliminated | 6 | 136 | yes | yes |
| Standard | Temporal | 4 | 4 | all players eliminated | 5 | 148 | yes | yes, through an eliminated player's area |
| Expert | Anachronauts | 1 | 36 | all players eliminated | 4 | 32 | yes | yes |
| Expert | Master of Time | 2 | 11 | all players eliminated | 5 | 91 | yes | yes |
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
game (`KangRecipeGames`, same seed) ends with all players eliminated in round 6.
`PublicRestartJourney` takes a factory, shared with the Hulk-based callers.

## Engine changes

The scan and review reached these paths. Each has a focused test and a killed
mutant. The replay identity stays `engine-replay-v32`, because no admitted game
or saved prompt changes:

- Only Kang content separates the table, retains an eliminated player's area,
  completes a stage by elimination, removes hero obligations or grants the
  hero-specific prohibition, so those paths never run for admitted content.
- Three changes are generic, and each leaves admitted content unchanged.
  - The boost-ability fallback for "the activating enemy" runs only when no
    enemy is activating. Only 01164, 04027 and 90005 name the activating enemy,
    and none can eliminate a player, so an admitted activation never ends
    under them.
  - Placing a card under a host that has left play does nothing (rr:then.1).
    Every admitted host is in play when its ability resolves: 04003, 04122,
    04028, 04107, 05007 and 09029.
  - Work that elimination requires waits behind the running ability. Only the
    Kang stage completion schedules work that way.

| Path | Rule | Test |
|---|---|---|
| A requested Action event (rr:player-turn.6) is judged by the acting seat's hero-specific prohibition, as its offer already was | rr:player-turn.6, rr:cannot | `KangDepoweredRequestedActionTests` |
| An eliminated seat's activation step advances before any villain lookup | rr:player-elimination.6 | `KangEliminatedActivationTests` |
| An eliminated player's local area remains until its own phase-end departure, so a co-due last group joins it instead of going central | 11008b, rr:player-elimination.step.5, rr:first-player.3 | `KangRetainedReunionTests` |
| A departing group with no players combines into an area with players (our choice) | 11009b–11012b | `KangRetainedReunionTests` |
| Sir Raston's boost, after its damage eliminates the player, still gives its enemy a boost card, which waits facedown | rr:player-elimination.5, rr:boost-boost-icon.6 | `KangSirRastonEliminationTests` |
| Eliminating an area's last player completes its stage after the eliminating ability resolves entirely: Sir Raston's extra boost card is dealt before the completion stores a Dominion | rr:player-elimination.5, 11009b–11012b | `KangSirRastonEliminationTests`, `KangEliminatingAbilityTests`, `AfterRunningAbilityTests` |
| The removed hero obligations are dealt into the removed-from-game area and still validated against their identity | 11007a, rr:obligation.4 | `KangRecipeSetupTests`, `DeckConstructionRulesTests` |
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
| `Holds` drops the live-retention check | survived; equivalent, remarked on `KangElimination.Holds` |
| Boosted enemy fallback returns nothing | killed, 4/4 |
| Stage completion goes ahead of the eliminating ability | killed, 4/4 |
| Completion follows the innermost invocation rather than the outermost | killed (`AfterRunningAbilityTests`; `KangEliminatingAbilityTests` 2/2) |
| Completion follows the first owned frame rather than the last | killed (`AfterRunningAbilityTests`) |
| Completion registered as the ability's own work | killed, 2/2 |
| Storage guard removed | killed |
| Obligation flag ignored | killed, 26 cases |
| Removed obligations dealt to the set-aside area | killed, 26 cases |
| Removed obligations skip identity validation | killed (`DeckConstructionRulesTests`) |
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
areas, each with its own Kang and main scheme, beside a central 2B. The client
must show which Kang threatens whom, which cards a player can target, and why.
The skill reserves owner review for "multiple villains or alternating villains
sharing a location the heroes must protect", which this is. The Godot
separated table ([P9](#p9-godot-split-table)) awaits that review in MARVEL-905.

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
7. That completing a stage 3B is not a loss. It ends that area's stage and
   stores a Kang's Dominion under 4A. Show the insert's advice beside the
   threat: "It is better to let Kang (II) complete his main scheme than to let
   Kang (II) defeat your hero" (`pack:mc11:game-areas`).
8. Which area is acting. While the table is separated, each villain-phase step
   runs for every area in turn order before the next step begins (ruling
   97d62cdb9ce436a7): threat on each main scheme, then each Kang's
   activation, then each player's encounter card.
9. What "each player" means. On an encounter card it means each player in the
   same game area (ruling 4799b99ecc0e5dea), but a per-player value still
   counts every player who started the scenario (ruling 0c9221b6940b4007).
10. That all areas share one encounter deck and one encounter discard pile
    (`pack:mc11:game-areas`).
11. Progress toward winning: Kang (I), then a Kang (II) in each area, then
    Kang (III) after the reunion.

### Facts the View carries for the split table (protocol 46)

Each fact is derived in the engine and copied by View without sentences; the client
validates that every id it names is one the same response lists
(`ClientGameAreaValidation`) and words each fact through `TableWording`.

| Need | Engine owner | View and wire | Tests |
|---|---|---|---|
| Per-area summary: local Kang, local scheme, central, retained, former seats | `GameAreaFacts` | `GameAreaDescriptor.Villains`, `MainSchemes`, `Central`, `Retained`, `FormerSeats` | `KangTableFactsTests`, `SplitTableProjectionTests` |
| Pending departures: who, from where, why, where to | `KangTable` over `KangPhaseTransitions.Pending` and `KangJoinProcedure.Destinations` | `KangTableDescriptor.Departures` (`Seats`, `SourceGameArea`, `CompletedScheme`, `Destinations`, `Reunion`) | `KangTableFactsTests`, `SplitTableProjectionTests` |
| A pending split after Kang (I)'s defeat | `KangFirstSplit.Pending` | `KangTableDescriptor.PendingSplit` | `KangTableFactsTests` |
| Stored Dominions under 4A | `KangTable` (cards beneath 11013a) | `KangTableDescriptor.StoredDominions` | `KangTableFactsTests` |
| Win progress | `KangTable` (Kang (I), (II), (III) in play); `KangFirstSplit.LocalKangsDealt` | `Stage`, `RemainingLocalKangs`, `LocalKangsDealt`, `StoredDominions`, `PendingSplit` | `KangTableFactsTests`, `KangSplitTableViewTests` |
| Which area is acting in the villain phase | `VillainPhaseFocus` | `KangTableDescriptor.Acting` | `VillainPhaseFocusTests`, `KangTableFactsTests` |
| Why another area's cards are not offered | `Places.CanAffect` | `PlayerDescriptor.OutOfReach`, `OutOfReachReason` | `SplitTableProjectionTests`, `KangSplitTableViewTests` |
| Player-named prompt and event labels | `GameAreaFacts` labels, `KangDepartureNames` | prompt `DisplayLabel`/`CommitLabel`/`Description`; `GameAreaEventPresentation` | `KangTableFactsTests`, `SplitTableProjectionTests` |
| "Each player" scope, shared encounter deck and discard, 3B completion is not a loss, turn-order steps, 2B tokens wait for 4A | `KangTable` notes (`KangNoteKind`, anchored to 2B and each uncompleted 3B), `World.Players`, `World.AreaOf` | `Notes` (kind, card), `StartingPlayers`, `EncounterDeck`, `EncounterDiscard` | `KangTableFactsTests`, `SplitTableProjectionTests` |
| Topology events named as they happened | `PlayAreaJoined`/`PlayAreaDetached` `Members` (journal omits) | `GameAreaEventPresentation` | `KangTableFactsTests`, `SplitTableProjectionTests`, `TopologyEventJournalTests` |

`KangSplitTableViewTests` drives Standard, Temporal, two players, seed 1 through
`KangRecipeFactory` behind `EngineHost` with a restricted capability per seat, to
the split, a pending departure and the reunion. After every answer both seats'
responses cross the strict codec and pass `ClientResponseValidation.HasCompleteBoard`;
their game-area and Kang facts are equal, each seat reads only its own hand, and no
fact names a card in another seat's hand.

## P9 Godot split table

The Godot client renders these facts with the table's building blocks
(`Tracker`, `ColumnHeader`, `Chip`, `StatusBadge`, `TablePanel`, `OverflowCue`)
on `TableGrid`, the twelve-column grid over the table's actual width.
`GameAreaTableProjection` arranges the facts for the inspected seat. It reads
typed fields only and never card text, and every word comes from
`TableWording`. `GameAreaTableRenderer` draws the result. Kang stays out of the
public catalog until owner review MARVEL-905 approves the interaction.

The board states, the hover explains: no rule sentence sits on the table. A
column says what it is with a one-line header and icons; its tooltip carries
the explanation; the rules live behind the rules toggle.

| Shown | From |
|---|---|
| A tracker line: the Kang faced now, one pip per Kang (II) the split dealt (filled while standing, hollow once leaving or gone) and the stored Dominions; the full progress on hover | `KangTableDescriptor.Stage`, `RemainingLocalKangs`, `LocalKangsDealt`, `StoredDominions`, `PendingSplit` |
| One column per game area, in engine order, titled by its players | `WorldDescriptor.GameAreas` (non-central), `PlayAreas` or `FormerSeats`, `PlayerDescriptor.Name` |
| A central column between the game areas holding stage 2B and the shared encounter deck and discard, with the shared icon | `KangTableDescriptor.CentralSchemes`, `EncounterDeck`, `EncounterDiscard`; the `SharedEncounterDeck` and `CentralTokensWaitFor4A` notes on hover |
| Each column's Kang (II), stage 3B and side schemes | `GameAreaDescriptor.Villains`, `MainSchemes`; `BoardCardPresentation.GameArea` for side schemes |
| Your area: the person icon and the legal edge | `GameAreaDescriptor.PlayAreas` contains the inspected seat |
| Acting: a heavy danger edge and the villain phase's three step pips | `KangTableDescriptor.Acting.GameArea`, `Acting.Step` |
| Retained: the hourglass icon, titled by the former players; what retained means on hover | `GameAreaDescriptor.Retained`, `FormerSeats` |
| Departing: the leaving arrow; each candidate destination lit with the accent edge and the joining arrow while the departure is pending, which spans the join prompt | `KangDepartureDescriptor.SourceGameArea`, `Destinations` |
| Out of reach: the lock icon, the dimmed title and dimmed cards (with their attachments), the reason on hover | `PlayerDescriptor.OutOfReach`, `OutOfReachReason` |
| "Completing this stage does not lose the game" | the `CompletionStoresDominion` note anchored to that area's scheme, in its tooltip and the rules toggle |
| The rules toggle: each rule the engine says applies, once | every note, table-wide rules first |

A column's tooltip reads, in order: its title, why the area is out of reach,
which step is acting and for whom, who departs and why, that it can be
joined, then the rules anchored to its schemes. Godot words no rule
consequence beyond `TableWording`, which words the clause each fact cites.

The grid regions, icons, edges, weights and dimming are our choices. The far
side of a separated table takes grid columns 0–1 for the seat rail, 2–9 for
the tracker line and the columns, and 10–11 for engaged enemies; a game area
weighs a fifth more than the central column. `GameAreaColumnLayout` places the
one-line header, a badge margin, the cards and a strip reserved for
attachments, and stands the shared piles side by side under 2B. A Kang table
that is not separated (stage 1, or stage 3 after the reunion) keeps the single
villain mat and adds only the tracker, as a caption line above the main
scheme. A game without Kang facts or a split
has no game-area presentation.

The tabletop seat rail shows each seat as a `SeatTile`: a portrait (the hero's
initials), the identity card's published title, an HP bar against the
identity's published maximum, and a wrapping row of roles and badges for form,
statuses (with their count) and engaged enemies; the tile grows rather than
clip a row. The seat that must answer shows a labelled "Answering" chip on the
accent edge, beside the portrait's accent edge; the other roles are icons, each
named in its own tooltip. An `OverflowCue` counts the tiles the rail does not
wholly show, from their laid-out edges, and recounts as the rail scrolls. The
seat rail and the decision sheet's head appear in every game, not only Kang.

The decision sheet's head states the question, what it concerns as one short
line, the answering seat, whether it is required, its causes and source as
chips, and the engine's whole instruction once, wrapped.

The native contracts `kang-2`, `kang-3` and `kang-4` deal each recipe through
`KangRecipeFactory` behind the native contract host. `KangNativeScene` opens
the game once every player has their own game area. It replays a policy
prelude inside the factory, so the game is a prepared component, not a
transcript.

In `kang-2` (Standard, Temporal, seed 1), pressing Synchronize advances the
game. Before serving each request, the host answers prompts with the
insert-advised policy (`KangNativeCheckpoints`) until the next checkpoint: the
acting step, a pending departure, and the join prompt. The client answers the
join prompt itself, by choosing the named destination and its commitment. The
host then advances to the reunion against Kang (III).

`kang-3` (Master of Time, seed 15) checks the three-area split. `kang-4`
(Temporal, seed 4, at 150% reading scale) checks the four-area split and a
retained area. Each split asserts the rendered columns, labels, markers,
dimming, central stage, shared piles and progress text, and opens and closes
the rules toggle; the later `kang-2` steps assert their own markers and
facts. Every capture (`native_kang_layout.gd`) asserts that the tracker and
every header show whole and on one line, that no column carries an
explanation paragraph, that no placed card, encounter stack, attachment strip
or pile covers a header, the tracker or the rules toggle, and that no two
columns overlap. Every other native contract asserts that no game-area
presentation appears.
