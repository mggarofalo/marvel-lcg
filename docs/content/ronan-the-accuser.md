# Ronan the Accuser

Packet 6 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors scenario
5, Ronan the Accuser (16103–16116): Ronan the Accuser (I)–(III), Interception
Imminent (1A/1B) and "Take What Is Mine" (2A/2B), the Kree Command Ship, the
Universal Weapon, Fanaticism, Cut the Power, Pincer Maneuver, Superior Tactics,
Single-Minded Fury, Kree Physiology and "You Stand Accused!". The plan is
[gmw-wave-plan.md](gmw-wave-plan.md) (MARVEL-904). The scenario's insert is
`pack:mc16:ronan-the-accuser`.

## Boundary

- **The scenario is authored and withheld.** Every face has a row, and the
  Standard and Expert recipes are declared (`ronan_the_accuser`,
  `ronan_the_accuser_expert`): Ronan (I)/(II), or (II)/(III) for Expert; both main
  schemes; the Standard (and Expert), Power Stone and Ship Command sets; Kree
  Militants as the modular set; the thirteen remaining scenario cards with the
  vendored MarvelSDB quantities; and the Kree Command Ship and Universal Weapon set
  aside for the first main scheme's setup. The scenario's faces are not an
  admitted part of the box, so discovery removes both recipes
  (`SetupCatalog.WithoutFaces`). The scenario is not admitted because its
  encounter deck includes the Ship Command set, which waits for the table to
  present one seat paying with another seat's Milano (MARVEL-905;
  [ship-command-power-stone.md](ship-command-power-stone.md)).
- **No face is unauthored.**

## Interpretations

Each choice is recorded in the row note that owns it. MarvelCDB has no FAQ entry
for these cards; the rulings are Hall of Heroes entries in `datasets/rulings/`.

- **The player who controls the Power Stone** is the player in whose play area
  it is, attached to their identity. Encounter cards are under the scenario's
  control (rr:ownership-and-control.2), but Ronan and Single-Minded Fury name a
  player who controls the stone, and card text wins (rr:the-golden-rules); an
  attachment's "you" already follows the controller of the card it is attached
  to (rr:attachment.2). **Attached to Ronan** is the stone in the villain's play
  area (`villainPlayArea`).
- **Ronan's Forced Interrupt** gives him one more boost card when he attacks or
  schemes against the player who controls the stone. The printed ability is split
  by the activation's kind, as for Hysteria (02020); "you" is the player the
  activation is against (rr:you-your.7).
- **Setup.** The Power Stone's Setup keyword attaches it to the villain in setup
  step 11, and Interception Imminent's Setup, resolved in step 12a
  (rr:appendix-ii-setup.step.12.a), moves it to the first player's identity
  without it leaving play (rr:attach-to.3.1). The same Setup puts the set-aside
  Kree Command Ship and Universal Weapon into their printed destinations; the
  Milano's own Setup keyword puts it under the first player's control
  (rr:setup-keyword.1).
- **The Kree Command Ship** is an optional ability on an encounter card, so any
  player may be offered it (rr:ability.8); only the first player can trigger it,
  for a treachery any player reveals. The Milano it exhausts cannot also pay the
  resource (rr:exhausted.1). Canceling the When Revealed effects leaves the card
  revealed and discarded (rr:cancel.4) and cancels its surge, which is a When
  Revealed ability (ruling 4e44bf466c0d8971). During another player's peril card
  the first player cannot trigger it (rr:peril.1).
- **"Take What Is Mine"** reads whether Ronan holds the stone before attaching it:
  a stone already on him gives him a facedown boost card for his next activation
  (rr:boost-boost-icon.6). Its 2B side prohibits threat removal only while Ronan
  holds the stone (rr:cannot).
- **The Universal Weapon**'s action costs damage and a facedown encounter card
  before the arrow, as for Focused Rage (01027). As a boost card it attaches to
  Ronan, as Red Skull's Luger (04132) does, and its ATK+1 counts toward the attack
  it was revealed for. Attached to Ronan, it follows his next stage
  (rr:villain-defeat.3.1).
- **Fanaticism** enters with 1 fury counter plus 1 per player who started the
  scenario and is discarded when the last one leaves (rr:uses-x-type.1). Engine
  choice: the counter its attacks spend is removed by a forced response to the end
  of each attack Ronan makes against a player, which is every attack the interrupt
  gives overkill and piercing.
- **Cut the Power**'s boost choice is the attacked player's, as for Android
  Efficiency (01144a); an exhausted Milano leaves only the threat
  (rr:choose-option.1), and any player may exhaust the Milano for an encounter
  card's option (ruling 65d0813b02e75e59).
- **Superior Tactics** gives `cannot_be_unattached` to a stone in the villain's play
  area. No ability moves it to another host while it applies (rr:cannot), including
  the stone's response to a hero dealing 3 or more damage. Its When Revealed reads
  whether Ronan holds the stone before attaching it.
- **Single-Minded Fury** attacks the controlling player in either form
  (rr:attack-enemy-activation.1.1). It gains surge when no player controls the stone
  or when a stunned Ronan's attack is replaced (rr:activation.7), as for Mad Genius
  (02013).
- **Kree Physiology** reads Ronan's tough status before giving one
  (rr:status-cards.1), as for Psionic Ghost (16135).
- **"You Stand Accused!"** gives the villain another boost card only while the
  villain is the activating enemy (ruling fba0af85176f8559).

## Engine changes and existing content

No admitted card changes behaviour, so the replay identity stays
`engine-replay-v36` ([session-ledger.md](../session-ledger.md)). The words are
documented in [card-dsl.md](../card-dsl.md).

- `cardsIn` accepts `area: "villainPlayArea"` (`AbilityPlayAreaCards`).
- `startingCounters` accepts `additional`, never multiplied (`CardCounterPool.StartingFor`).
- `cannot_be_unattached` is a modifiable field; `attachCards` does not move an
  attachment that has it (`AttachmentTransfer.CanUnattach`).
- An `inPlayerArea` selection of the identity area resolves to one card.
- A projected threat-removal permission reads a titled card in the villain's play
  area from the projection's membership and attachment hosts
  (`AbilityThreatPermissionProjection`), so a removal power decides "Take What Is
  Mine" 2B before it resolves. A titled card entering play under the projection
  raises.

A differential of 220 complete non-gmw games (Rhino, Klaw Expert, Ultron, Risky
Business, Crossbones, Wrecking Crew Standard, Mutagen Formula, Red Skull Expert and
Zola, and Rhino and Ultron with the admitted Power Stone modular set; one and two
players; seeds 1–10), hashing every prompt and state digest, is identical on
4639bef7 and this packet.

## Evidence

- Synthetic boards over the production book: `RonanVillainTests`,
  `RonanEncounterTests`, `RonanSchemeTests`; the vocabulary alone:
  `RonanLoweringTests`.
- Research-only complete Standard games, dealt directly rather than through
  discovery, with exact replay, and the Standard and Expert setup:
  `RonanTheAccuserResearchGameTests`.
- Every new decision has a mutant, and all 60 are killed: the engine words and the
  projected villain play area test and its shape, each row's conditions and amounts (including
  "you" on Ronan's interrupts, "per hero" on Superior Tactics and the villain-only
  boost of "You Stand Accused!"), and the Kree Command Ship's first-player,
  treachery and peril limits.
- A scan of 3,040 complete research games (every hero alone and in 19 pairs,
  Standard and Expert, seeds 1–40) raises nothing from this scenario: 2,981 end
  with every player eliminated, 58 with the villain completing the final main
  scheme, none with Ronan defeated and none unfinished. One game raises MARVEL-914, a Doctor Strange
  Invocation discard after Images of Ikonn defeats Open the Dark Dimension.

## New interactions

None. Every choice uses an existing prompt: an interrupt opportunity with a cost,
an option, a defense, an action on an encounter card. The scenario waits only for
the Ship Command set's cross-seat Milano payment (MARVEL-905).
