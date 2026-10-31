# Wasp candidate public admission

The Wasp hero pack (`wsp`, `pack:mc13:wasp-hero-pack`) has 36 printed faces:
the three-face identity 13001a/b/c, 15 signature cards, 16 Aggression and 9
Basic starter cards, obligation 13026, the Beetle nemesis set (13027, 13028,
13029, two 13030) and extras 13031–13034. The setup record pins the vendored
MarvelSDB quantities (`datasets/marvelsdb/pack/wsp.json`,
`wsp_encounter.json`); no printed divider was inspected, so those quantities
are a repository choice from that snapshot. Admission requires every face to be
authored and the exact recipe (`WaspRecipe`).

## Interpretations

Each choice below is recorded in the row note or the code comment that owns it.

- **Giant Wasp divides basic attacks and thwarts** (`basic_attack_divided`,
  `basic_thwart_divided`). The pack's rules clarification says each enemy dealt
  damage is attacked and each scheme given threat removal is thwarted.
  Rulings f4cbf2c7 and 0bf3bc9d: the hero declares one target, then divides
  the final amount. Ruling 116e678d: an enemy given no share is not attacked,
  and Guard keeps the villain out of the pool. Rulings 1b420f29 and 44010c0f:
  each recipient retaliates. Rulings aefac89a and f216f635: the damage is
  simultaneous, and crisis and patrol bound the candidates.
- **Interrupts to a divided attack resolve against the declared target**, even
  when the division later gives it no share. Ruling 78f96d5c: she "will
  initially choose one target", and an interrupt such as Befuddle resolves
  before the damage is split. rr:interrupt resolves an interrupt before its
  triggering condition, so no share exists yet. Ruling 116e678d governs only
  the "attacked" status after damage, so responses and Retaliate follow the
  shares (rr:attack-player-ability-type.5). In
  `TheDeclaredTargetsAttackInterruptsResolveEvenWhenItIsGivenNoShare`,
  Taskmaster's Photographic Reflexes is discarded although every point goes to
  a minion.
- **Wasp Sting** divides a total of 4 in Giant form and deals 5 to one enemy in
  Tiny form. Each version is a separate form-gated Action.
- **Into the Fray** removes threat equal to the excess damage the attack
  *dealt* (rr:excess-damage). Ruling dd353e34: this counts even when the target
  does not take the damage.
- **Mother's Orders** adds a cost of one resource of any type to each hero's
  basic attack. It is paid at the same time as exhaustion (rr:cost.10).
  Responses to the spent cards, such as Pym Particles, resolve before the
  attack.
- **Janet Van Dyne** counts only overpaid energy icons. An overpaid wild icon
  counts as declared energy (rr:wild-resource.1). Ruling 5e6ac90d: Power Of
  cards do not double resources while overpaying.
- **Nadia** shuffles in only cards with a printed mental icon. Outside payment,
  a wild icon is not mental (rr:wild-resource.3).
- **All for One** exhausts any number of ready Avengers immediately before the
  attack that counts them. The engine does not yet support stopping for a
  choice inside a labelled attack.
- **Giant Help** is two form-gated Actions: divide 4 threat while Giant,
  otherwise remove 3 threat from one scheme. Both carry the thwart label, so a
  hero who cannot thwart, such as one engaged with Baron Zemo (03028), is
  offered neither (rr:cannot). Every thwart-group effect (`distributeThreat`,
  `thwartSchemes`, `thwartDifferentSchemes`, `legalPractice`) is offered only
  when its performer can thwart. That covers the unlabelled Inconspicuous
  (04038) and the Core thwart-group cards too. Resolving such an offer raises
  `cannot thwart`, so no saved session can hold one; withholding it changes no
  admitted outcome or replay.
  Inconspicuous stays unlabelled, so confusion still cancels its whole thwart
  as before (`ThwartGroupProhibitionTests`).
- **Rapid Growth** has two readings. It interrupts a basic attack or thwart, and
  it interrupts the hero-defense occurrence when the defense is basic. In both
  cases Wasp changes to her Giant face and gains +2 for that use.

## Evidence

The runtime tests use the production ability book on synthetic boards:
`WaspDividedBasicPowerTests`, `WaspStingTests`, `WaspResourceCardTests`,
`WaspIntoTheFrayTests`, `WaspMothersOrdersTests`, `WaspAllForOneTests`,
`WaspSupportCardTests`, `WaspFormCardTests`, `WaspEncounterCardTests`,
`WaspSmallButMightyTests` and `ThwartGroupProhibitionTests`.
Lie in Wait runs through a public `wasp` game's villain phase.
`WaspAdmissionTests` closes the pack when a face is missing, a face changes
pack, or any recipe component changes. `WaspLoweringTests` refuses malformed
shapes of the new vocabulary.

## Public journeys

`WaspPublicRestartTests` opens the published starter through `EngineHost` with
seed 851, keeps the dealt hand, changes Nadia to Giant Wasp and suspends at the
division allocation. Against Ultron the basic attack is divided between Ultron
and the Drone that Ultron's setup engages; against the Wrecking Crew the basic
thwart is divided between two side schemes. Each case checks the prompt copy
and per-target consequences, copies the strict save and authority metadata into
a fresh host, rejects a stale and an incomplete allocation without changing the
save or state, then compares events, save, prompt, digest, RNG state and
pending work against the uninterrupted host. The thwart case then undoes and
redoes the allocation. In the attack case the Drone's share defeats it and
reveals its facedown card, so the host refuses that undo with
`history_frontier` and keeps the session unchanged. A following decision matches
on both hosts.

`WaspPublicGameTests` plays the unarranged starter with the same scenario, seed
and policy matrix as Ant-Man, beside an exact same-seed replay of every
decision's prompt, events, digest and RNG. All seven games reach a terminal
policy loss; they establish complete games and replay, not victory. The policy
never changes to Giant, so these seven games do not exercise the division.

`AGiantStarterDividesItsBasicPowersThroughACompleteReplayedGame` plays two more
unarranged games with the same replay check. Before the policy answers, the
test changes Wasp to Giant whenever the turn offers it, and spreads each
division one point per recipient. Each game divides a basic power at least
once and reaches a policy loss.

| Scenario | Players | Seed | Outcome | Round | Decisions | Divisions |
|---|---:|---:|---|---:|---:|---:|
| Ultron | 1 | 3 | PlayersLose | 6 | 46 | 1 |
| Wrecking Crew | 1 | 8 | PlayersLose | 2 | 14 | 1 |

`MothersOrdersResourceAttackCostIsPaidThroughACompleteReplayedGame` plays two
unarranged games in which Shadow of the Past puts Mother's Orders into play.
The policy pays the offered one-resource attack cost through the public prompt
at least once in each, and the replay matches.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 16 | PlayersLose | 7 | 42 |
| Crossbones | 1 | 26 | PlayersLose | 8 | 61 |

No native journey reaches Mother's Orders, so its prompt has no native
evidence. The scripted client cannot arrange the nemesis side scheme, and no
native script yet plays an unarranged game as far as Shadow of the Past.

### Unarranged games that raise

A scan of the public starter against six scenarios, seeds 1–40 (240 games),
found games that end in `RulesNotImplementedException`. In every Wasp-caused
case, Into the Fray (13013) defeats a minion whose defeat window the
labelled-power projection cannot predict. The continuation then reads the
main scheme:

- Advanced Ultron Drone (01143, When Defeated) and Beetle (13028, Forced
  Interrupt). Example seeds: Ultron 27, 36, 37; Rhino 10, 17; Absorbing Man
  10, 13, 16, 28, 40. These still raise.
- Hydra Soldier (01182, When Defeated encounter deal). Example seeds:
  Crossbones 1, 18, 30; Absorbing Man 1, 35. These now complete, because a
  facedown dealt card changes no threat
  (`IntoTheFrayRemovesExcessThreatAfterTheMinionsWhenDefeatedEncounterDeal`).

One further raise, Wrecking Crew seed 32, comes from the Wrecking Crew boost
card 07045, which has no authored boost ability. It is not Wasp content.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 1 | VillainWins | 5 | 33 |
| Klaw | 1 | 2 | PlayersLose | 5 | 38 |
| Ultron | 1 | 3 | PlayersLose | 4 | 34 |
| Crossbones | 1 | 4 | PlayersLose | 3 | 20 |
| Absorbing Man | 1 | 5 | PlayersLose | 4 | 29 |
| Rhino | 2 | 6 | PlayersLose | 7 | 81 |
| Absorbing Man | 2 | 7 | PlayersLose | 5 | 73 |

The complete-game and restart helpers are shared with Ant-Man
(`PublicStarterGames`, `PublicRestartJourney`); Ant-Man's seven games reproduce
its recorded table unchanged.

## Native divided powers

Three scripted journeys use the public Wasp starter with seed 1. Setup
selections, seed entry, menus and decisions use viewport input, and no gameplay
fixture is injected:

- `wasp_public_divided_attack.gd` (Ultron): Giant Wasp declares an attack on
  Ultron and divides 2 between Ultron and the Drone.
- `wasp_public_divided_retaliate.gd` (Wrecking Crew): she declares an attack
  on Wrecker and divides 2 with Piledriver, whose Retaliate 1 hits her.
- `wasp_public_divided_thwart.gd` (Wrecking Crew): she declares a thwart on
  Pile It On! and divides 2 with Clear the Road.

Each journey checks the following:

- The identity menu says the amount is divided after the declaration, and the
  declared target's preview applies "if all of it is assigned here".
- The existing repeated-target editor shows the total ("Divide 2 damage among
  enemies"), states that each recipient is attacked and its Retaliate applies
  (or that each scheme is thwarted), and gives a consequence for each
  recipient. Piledriver's row reads "Attacked if given any damage · first
  point: 11/11 → 10/11 HP · Retaliate 1 will hit Wasp".
- One point cannot be committed. A full allocation disables further
  additions, and nothing commits until the explicit commit.
- One commitment applies every share. Ultron goes from 17 to 16 and the Drone
  leaves the table. Wrecker goes from 14 to 13, Piledriver from 11 to 10 and
  Wasp from 11 to 10. Each scheme loses one threat.

The rendered setup, destination, target menu, allocation and result images were
inspected. The playback caption for the retaliation names the whole divided
attack ("Wasp → Piledriver, Wrecker"), rather than only the declared target.

The interaction reuses the repeated-target allocation that indirect damage
already uses (`contract-repeated-targets-*`). The engine supplies the
consequence copy, and the client renders the per-target detail it already
carries for ordinary targets. These are not materially new UI interactions, so
no owner review item is needed.

On macOS with Godot 4.7.1 at 1920×1080, 100% scale and reduced motion, the
manifest cases `wasp-divided-attack-100-reduced`,
`wasp-divided-retaliate-100-reduced` and `wasp-divided-thwart-100-reduced`
passed headless. The scripts also passed rendered through
`tools/godot-acceptance.ps1 -Rendered`. The existing
`contract-repeated-targets-100-motion` and `ant-man-public-form-100-reduced`
cases still pass. The final evidence is under `~/Source/evidence/wasp-manifest2-*`
(headless) and `~/Source/evidence/wasp-native-final-*` (rendered).

Windows/Linux native runs, other scales and package journeys are not
established here. The repeated "Attack resumes." line for each announced
recipient is the existing attack-boundary copy for an attack that is already
open.
