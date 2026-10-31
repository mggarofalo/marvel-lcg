# Quicksilver candidate public admission

The Quicksilver hero pack (`qsv`) has 33 printed faces: the identity
14001a/b, 15 signature cards (14002–14011 with their copies), 15 Protection
and 10 Basic starter cards (14012–14023), obligation 14024, the Avalanche
nemesis set (14025, 14026, 14027, two 14028) and extras 14029–14032. The
setup record pins the vendored MarvelSDB quantities
(`datasets/marvelsdb/pack/qsv.json`, `qsv_encounter.json`); no printed
divider was inspected, so those quantities are a repository choice from that
snapshot. Admission requires every face to be authored and the exact recipe
(`QuicksilverRecipe`).

## Interpretations

Each choice below is recorded in the row note or the code comment that owns it.

- **Super Speed** readies Quicksilver once per phase after a basic thwart,
  attack or defense. Ruling 9cfb9119e5c7f0d7: using a basic power includes
  its action, so the response follows the completed thwart, the completed
  attack, or the defended attack after its damage. Ruling 983ae8870c226cd4: a
  stunned attack or confused thwart is not a use, so it does not trigger.
  Ruling 71b0ff212c394895: after a basic defense, this response shares one
  window with Defense Specialist and Electrostatic Armor. The printed limit
  covers all three triggering conditions, so the three authored responses
  share one limit (`limitGroup`, rr:limit).
- **Superpowered Siblings** draws 3 when a faceup in-play card is titled or
  subtitled Wanda Maximoff (`namedInPlay`). The Scarlet Witch ally carries that
  subtitle (rr:subtitle); a copy in hand does not count (rr:identity.4).
- **Scarlet Witch** interrupts her own basic thwart or attack, discards the top
  encounter card and adds its printed boost icons to that use. A star is not a
  boost icon.
- **Double Time** offers each ordered pair of options, each needing a valid
  target only for itself (ruling 5a46f4ebeb92ff9d). The pair resolves as
  separate sentences, each choosing its target when it resolves
  (`seqEachTargeted`). Ruling fc2d762cb30f31fa: "remove 2 threat" may remove
  the 1 a scheme holds.
- **Speed Cyclone** costs X resources of any type, X chosen while paying
  (rr:initiating-abilities.step.3, rr:non-numerical-variable.1), and stuns X
  different enemies (`spendX: printed`). The engine offers X from 1, as for
  every existing X cost; X = 0 would stun nothing. Ruling 5636cf3904ac9370
  reads its cost in hand as 0 for other abilities. rr:non-numerical-variable.1
  lets effects modify the amount paid without changing X; the engine does not
  yet reduce the X component, so a cost reduction applied to Speed Cyclone
  raises `RulesNotImplementedException` before anything is paid.
- **Serval Industries** shuffles in only Quicksilver identity-specific cards
  and is not offered without one in the discard pile
  (rr:initiating-abilities.step.2). The same gate now applies to every
  `chooseDiscardToShuffle` with a minimum, such as Ancestral Knowledge (01042),
  which was previously offered with an empty discard pile and then asked a
  question nobody could answer.
- **Friction Resistance** responds to a ready instruction that readied your
  hero. The readying occurrence now opens a response window (`WhenCardReadied`);
  no admitted card listened to it before. Ruling 47cf0084407ee2a4: its
  generated resource is a printed physical resource.
- **Never Back Down** gives +2 DEF for the attack and, if the defending hero
  took no damage from it, stuns the attacker when the attack ends. Ruling
  da8c5c381de666bb: it cannot be played before an ally becomes the defender.
- **Side Step** prevents 3 and, when paid with energy, deals 1 to the enemy
  whose damage it prevented.
- **Order and Chaos** cancels a revealed treachery's When Revealed effects and
  deals 2 to the villain; the Scarlet Witch ally satisfies its team-up by title.
- **United We Stand** heals 1 from each of up to X damaged friendly
  characters, X the villain's stage, at most 3. Ruling 31d886a41646771b: an
  undefined letter is 0, so against a villain with no stage the event is not
  offered; nor is it offered when no friendly character is damaged
  (rr:heal, rr:initiating-abilities.step.2).
- **Need for Speed** offers the flip, then the options; "exhaust Pietro
  Maximoff" has no valid target in hero form, and the second option's
  prohibition holds until the end of the player's next turn (rr:cannot).
- **Avalanche** offers indirect damage or exhausting the identity, the latter
  only while the identity is ready (rr:choose-option.1, rr:exhausted.1).
- **Vibration Resistance** reduces the damage its host takes from each attack
  by 1, once per attack, after Tough and before interrupt prevention (rulings
  3dfec18909dc931e and c8268c2138f6c809).
- **Earthquake's boost** offers spending two physical resources only when they
  can be paid; otherwise it exhausts the identity.

## Shared engine changes

Every change is new vocabulary used only by Quicksilver rows, except the
following, each of which changes no admitted outcome:

- The `chooseDiscardToShuffle` target gate described above withholds an
  offer that could not be completed. No saved session can hold its
  unanswerable prompt.
- The ready instruction raises a `WhenCardReadied` response window, and an
  ally's basic thwart or attack raises `WhenAllyBasicUseInitiated`. Before this
  pack no ability listened to either, so an admitted game opens no new prompt.
- A card whose printed cost is X refuses a cost reduction and refuses an
  ability without `spendX`. Speed Cyclone is the first admitted player card
  with a printed X cost.

Replay identity stays at `engine-replay-v31`.

## Evidence

The runtime tests use the production ability book on synthetic boards:
`QuicksilverIdentityTests`, `QuicksilverSignatureTests`,
`QuicksilverPlayerCardTests` and `QuicksilverEncounterTests`.
`QuicksilverAdmissionTests` closes the pack when a face is missing, a face
changes pack, or any recipe component changes. `QuicksilverLoweringTests`
refuses malformed shapes of the new vocabulary.

## Public journeys

`QuicksilverPublicRestartTests` opens the published starter through
`EngineHost` against Rhino with seed 851, keeps the dealt hand, changes Pietro
Maximoff to Quicksilver and makes a basic attack. At the Super Speed offer it
copies the strict save and authority metadata into a fresh host, rejects the
stale answer, then takes the response on both hosts: Quicksilver readies. It
undoes and redoes the response and compares events, save, prompt, digest, RNG
state and pending work against the uninterrupted host. A second basic attack
in the same phase is not offered Super Speed.

`QuicksilverPublicGameTests` plays the unarranged starter against Rhino, Klaw,
Ultron, Crossbones, Absorbing Man and the Wrecking Crew, beside an exact
same-seed replay of every decision's prompt, events, digest and RNG. Every game reaches a terminal
policy loss; they establish complete games and replay, not victory. The
research policy declines optional responses.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 1 | PlayersLose | 5 | 38 |
| Klaw | 1 | 2 | PlayersLose | 3 | 27 |
| Ultron | 1 | 3 | PlayersLose | 2 | 30 |
| Crossbones | 1 | 4 | PlayersLose | 3 | 36 |
| Absorbing Man | 1 | 5 | PlayersLose | 5 | 39 |
| Wrecking Crew | 1 | 6 | PlayersLose | 3 | 25 |
| Rhino | 2 | 7 | PlayersLose | 7 | 103 |
| Absorbing Man | 2 | 8 | PlayersLose | 5 | 61 |

`QuicksilverResponsesResolveThroughACompleteReplayedGame` plays three more
unarranged games with the same replay check. Outside the turn menu, the test
takes every free, untargeted optional ability that a Quicksilver card offers,
such as Super Speed after a basic power and Friction Resistance after he
readies. Each game takes at least one and reaches a policy loss.

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 9 | PlayersLose | 4 | 32 |
| Klaw | 1 | 10 | PlayersLose | 4 | 34 |
| Crossbones | 1 | 11 | PlayersLose | 4 | 37 |

A scan of the public starter against every admitted scenario mode (39 modes,
seeds 1–40, 1,560 games) ends every game without a raise or failure, both
with the research policy and with the policy that also takes Quicksilver's
free optional abilities.

## Native client

No native journey was run. Every Quicksilver decision reuses an existing
interaction: Speed Cyclone's X is the variable-payment editor
(`native_variable_contract.gd`), United We Stand and Speed Cyclone choose
recipients through the existing multi-target selection, Double Time and
Avalanche present option choices, and the responses use the existing response
window. These are not materially new UI interactions, so no owner review item
is needed. Windows/Linux native runs, other scales and package journeys are
not established here.
