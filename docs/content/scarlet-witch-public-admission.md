# Scarlet Witch candidate public admission

The Scarlet Witch hero pack (`scw`, `pack:mc15`) has 32 printed faces: the
identity 15001a/b, 8 signature card titles (15 copies), 8 Justice and 5 Basic
starter titles (25 copies), obligation 15023 (two copies), the Luminous nemesis
set (15024–15027) and extras 15028–15031. The setup record pins the vendored
MarvelSDB quantities (`datasets/marvelsdb/pack/scw.json`,
`scw_encounter.json`); no printed divider was inspected, so those quantities
are a repository choice from that snapshot. Ruling b29d66914e946389 confirms the
two obligations are intentional. Admission requires every face to be authored
and the exact recipe (`ScarletWitchRecipe`).

## Boost-icon counts

Ruling 6962a015e8597900: "Boost icons are counted whenever a game step or card
ability determines the exact number of boost icons on an encounter card." The
engine makes each count an occurrence, `WhenBoostIconsCounted`, with an
interrupt window that Chaos Control and Scarlet Witch's Crest answer.

- An attack boost card turned faceup is counted in its faceup occurrence, so the
  Crest changes it before Preemptive Strike cancels icons (ruling
  937d6ff4f970a1da).
- An authored ability counts the encounter cards a node reads immediately
  before that node resolves (`boostIconsDiscarded`,
  `topEncounterDiscardBoostPlusOne`, `boostIcons: <card>`). A "2 or more" test
  is a count (ruling 310c4caa404d0f5a). Another player's count qualifies
  (ruling 4e1ff06e6c235760).
- Engine choice: a count no interrupt could observe completes without a window,
  so admitted games without Scarlet Witch keep their outcomes. A boost card's
  faceup occurrence now carries the count condition, so the label of that
  interrupt window names `WhenBoostIconsCounted` in every game.
- Chaos Control is a `would` interrupt. rr:would gives it priority over the
  Crest on the same count, so optional `would` interrupts now get their own
  offer stage.
- The Next Evolution adds 1 to every scenario-owned encounter card while it is
  in play (rulings 1ea208998735ef4b, 805b3f9b199ca68b). A count is never
  negative, and a decrease needs a positive count.

## Interpretations

Each choice is recorded in the row note or the code comment that owns it.

- **Hex Bolt** discards three cards, then resolves each in the order the player
  chooses, counting each as it resolves (rulings 123a79b5c8900e0c,
  a5028ed150b39433, 805b3f9b199ca68b, 0870bc11f295fbb0). The discard stops
  when the encounter deck empties (rr:encounter-deck.2), so fewer than three
  cards may resolve.
- **Molecular Decay**, **Swift Retribution** and **Bait and Switch**: as for All
  for One (13032), the discard or the enemy activation resolves immediately
  before the labelled power begins, because the engine does not suspend inside
  one. Swift Retribution's attack and Bait and Switch's thwart are initiated
  with the card (`initiated`), so a guard or patrol minion, or a stun or
  confusion, gained during the activation does not stop them (rulings
  6fd4652a06512d03, ca713a04a3ff5491). A hero already stunned or confused at
  initiation discards the status instead, and the villain does not activate.
- **Hex Bolt** counts every encounter card it discarded, including one a player
  owns, such as a nemesis-set card; ownership does not make a card a player card.
- **Crisis Averted** under a crisis icon removes threat only when paid with a
  mental resource, so its offer requires a mental declaration then
  (rr:initiating-abilities step 5, rr:event.3). The requirement is projected
  only when the declared resources decide whether the event has an effect.
- **Superpowered Siblings** draws three while a card titled or subtitled
  Pietro Maximoff is in play (rr:team-up.2, ruling 82607e2d70cbf9dd).
- **Chaos Magic** plays a card ignoring its resource cost (rr:ignore.1): no
  increase or reduction applies, and a paid-resource condition reads the empty
  payment (ruling 77085e3b31a219a4). A card with a resource requirement is not
  offered (rr:requirement-resources.2). It then discards encounter cards equal
  to the printed cost.
- **Agatha Harkness** adds one looked-at card to hand; two or more remaining
  cards are ordered onto the deck bottom in one answer.
- **Turn the Tide** answers a thwart whose removal took a scheme's last threat
  while the identity was a hero (rulings 2f5ce668fd00dbb2, 0e978ae1cc77dc8c).
- **Last Stand** grants +3 ATK for the attack and delays the discard to the
  attack's end, before consequential damage (ruling 01d17bfed5e5dad8,
  rr:delayed-effect.1). A player attack's end now ends attack-bounded effects
  and delays when no enemy attack is in progress; Last Stand raises before any
  change if an ally attack happens inside an enemy attack.
- **Magic Shield** is a "would take" interrupt at rr:damage step 3. A tough
  status card applies first (step 2), so Tough prevents the damage and Magic
  Shield is not offered (`ScarletWitchMagicShieldTests`).
- **Order and Chaos** cancels the treachery's When Revealed effects, then deals
  2 damage to the villain, in sequence as the reprint 14018 does: the
  cancellation always resolves against the treachery that triggered it. No
  player restriction is printed, so another player's reveal qualifies.
- **Chaos Manipulation** searches, discards, and shuffles the searched deck at
  the end (rr:search.3). **Slipping Sanity** counts star icons, which are not
  boost icons (rr:boost-boost-icon.1).

## Evidence

`ScarletWitchBoostCountTests`, `ScarletWitchPlayerCardTests` and
`ScarletWitchSignatureTests` run the production rows on synthetic boards.
`ScarletWitchPublicGameTests` plays the public starter to an ending against six
scenarios, two of them multiplayer, with exact decision replay, and three games
that take every free optional ability her own cards offer.
`ScarletWitchPublicRestartTests` reaches Chaos Control's would-count window in a
public Rhino game, restarts on a fresh host, rejects the stale answer, and
continues both hosts to identical events and state. History refuses to move
there (`history_open`), because the villain's activation still has decisions
pending.
`ScarletWitchAdmissionTests` closes the pack when a face is missing, a face
changes pack, or any recipe component changes, including a single obligation.

A zero-raise scan played the public starter against every admitted scenario
(Rhino, Klaw, Ultron, Crossbones, Absorbing Man, Taskmaster, Zola, Red Skull,
Wrecking Crew, Risky Business, Mutagen Formula), seeds 1–40, twice: once with
the research policy, which declines optional abilities, and once also taking
every free optional ability her own cards offer. None of the 880 games raised
`RulesNotImplementedException`. Fifteen games (seven and eight) ended before
the policy-coverage checks were met: the hero was defeated before playing a
card, attacking or acting in every seat. Those are complete games, not engine
failures.

## Replay identity

The replay identity moves to `engine-replay-v35`. Projecting a payment
declaration onto an offer changes the prompt Tackle (05015) and Concussive Blow
(05031) produce against a stalwart or already-statused enemy. Prompt labels
change too: the boost faceup interrupt window names `WhenBoostIconsCounted`
(for example the prompts that offer Preemptive Strike, Attacrobatics, Target
Acquired or Foiled!), and a hero's thwart that removes a scheme's last threat
adds `WhenHeroThwartRemovesAllThreat` to that occurrence, which a response
window's label then names. Boost-icon counts complete inline when no interrupt
could observe them, the player-attack end expires only effects bounded by that
attack, and the other new vocabulary is read only by Scarlet Witch rows, so no
other admitted outcome changes.

## Native evidence

No interaction is materially new. A count opens an ordinary interrupt window;
Agatha's order uses the ordered card-look prompt Thor's inspection introduced;
Chaos Magic uses the scoped hand-play prompt. No native journey was recorded for
this pack.
