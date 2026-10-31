# Kang main scheme stages and the split table

MARVEL-568 packet, branch `feat/kang-stages` from `79410d45`. Nonpublic: Kang
is not admitted (`RuntimeProductAdmission`), and no setup recipe exists yet.

## What is authored

`datasets/abilities/abilities.json` holds all fourteen faces 11007a/b–11013a/b,
printed text from `datasets/cards/cards.json`:

| Face | Runtime | Authority |
|---|---|---|
| 11007a | none: contents and setup are composition | `pack:mc11:setup`, `rr:appendix-ii-setup.step.12.b` |
| 11007b | When Revealed `dealEncounterCards`; completion loses (`KangFirstSplit.LosesWhenCompleted`) | printed text, `rr:winning-the-game.1` |
| 11008a | When Revealed `splitKangStageTwo` | `pack:mc11:areas` |
| 11008b | none: token placement and the reunion are engine-owned | `pack:mc11:areas`, `rr:acceleration-token.2.1` |
| 11009a–11012a | When Revealed `createKangGameArea` | `pack:mc11:areas` |
| 11009b–11012b | Forced Response `captureKangLocalCompletion`; all-defeated completion is engine-owned | `pack:mc11:game-areas` |
| 11013a | When Revealed `revealKangStageThree` | printed text |
| 11013b | When Revealed `putKangNemesesIntoPlay`; generic final-stage loss | `rr:main-scheme-main-scheme-deck.2.1` |

Stages 1, 2 and 4 form the main scheme deck, 2 on top (MARVEL-894 Q13).
Stage 4A therefore waits in that deck, and Dominion storage, central reunion and
local completion read it there.

## Separated table

While the villain's play area is in no game area (`SeparateGameAreas.Active`):

- step 1 places each area's own scheme threat in turn order of its earliest
  player (ruling 97d62cdb9ce436a7), counting only that area's acceleration icons
  and tokens; 2B's tokens add none and carry into 4A (Q1);
- hazard icons deal to their own area's players;
- a revealed side scheme enters the revealer's play area (Q15, MARVEL-898); an
  environment raises;
- defence candidates (MARVEL-899), basic attack and thwart targets and crisis
  icons are narrowed by `Places.CanAffect`.
- "each player" means the players in the source's game area (ruling
  4799b99ecc0e5dea) everywhere the card runner resolves it;
- "the villain", "the main scheme" and Incite name the card in the source's own
  game area; side scheme, scheme and villain queries search every play area, and
  every card query drops what its source cannot affect;
- amplify counts only icons that reach the activating enemy (Q14);
- the first player orders Kang's phase-end departure against other due delayed
  effects (Q6), and an eliminated player takes no stage 3A.

MARVEL-900: a piercing attack discards Tough on a villain whose damage Temporal
Shield prevents. Ruling bafde43c1a76c982 (RR 1.7–1.8): "Effects that 'prevent
damage' prevent damage taken, not dealt", and piercing removes Tough before a
prevention applies. `rr:piercing.2`'s exception needs an attack that would deal
no damage. The engine already behaved so; the test and comments pin it.

## Evidence

`KangStageFlowTests`, `KangSeparateAreaTests`, `KangGameAreaDefenseTests`,
`KangGameAreaReferenceTests`, `KangTemporalShieldTests`,
`GameAreaProjectionTests`, `GameAreaClientValidationTests` and
`GameAreaTransportTests` deal the full authored book. They run stage 1 through
the split, Kang II defeat, joining, local completion with a stored Dominion,
4A, Kang III and victory, and the 1B threat loss.

`KangSplitAreaIsolationTests` reveals each Kang, Standard and Expert encounter
card for each side of a live two-player split, through the revealer's own
revealing area, and holds the other player's play area unchanged. It also pins
the review probes: Corrupted Timestream, Advance, Past Machinations' Incite,
Masterplan, Time-Travel Tactics, Gang-Up, amplify and overkill.

Thirteen compiled mutants, each restored with `git checkout` and a touch,
were all killed by the focused tests: defence, attack, thwart and crisis
partitions; 1B loss; local step 1; turn order; central tokens; local hazard;
local side-scheme placement; the card and seat game-area projection; and
client id validation.

## Remaining

- P6 recipe: the setup dataset entry, with excluded hero obligations, set-aside
  II/III/Dominions, the stage-3 pool and Expert replacements.
- An environment entering play, or left central, while the table is separated
  raises (Q8 not implemented; no environment is in the Kang pool).
- Admission, public journey, save/restart evidence and the Godot split-table UI.
