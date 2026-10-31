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
  otherwise remove 3 threat from one scheme.
- **Rapid Growth** has two readings. It interrupts a basic attack or thwart, and
  it interrupts the hero-defense occurrence when the defense is basic. In both
  cases Wasp changes to her Giant face and gains +2 for that use.

## Evidence

The runtime tests use the production ability book on synthetic boards:
`WaspDividedBasicPowerTests`, `WaspStingTests`, `WaspResourceCardTests`,
`WaspIntoTheFrayTests`, `WaspMothersOrdersTests`, `WaspAllForOneTests`,
`WaspSupportCardTests`, `WaspFormCardTests` and `WaspEncounterCardTests`.
Lie in Wait runs through a public `wasp` game's villain phase.
`WaspAdmissionTests` closes the pack when a face is missing, a face changes
pack, or any recipe component changes. `WaspLoweringTests` refuses malformed
shapes of the new vocabulary.

Native Godot interaction and complete-game, save/restart and replay journeys
are not established here. The divided basic power uses the existing
repeated-target allocation contract that the client already renders.
