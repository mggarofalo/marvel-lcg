# Galaxy's Most Wanted modular sets

Packet 5 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors four
modular encounter sets: Galactic Artifacts (16122–16130), Kree Militants
(16131–16134, setup key `kree_militant`), Menagerie Medley (16135–16137) and
Space Pirates (16138–16141). Ruling 408decdf6cd8c4b5 names all four as modular
encounter sets of the box. The plan is [gmw-wave-plan.md](gmw-wave-plan.md)
(MARVEL-904).

## Boundary

- **All four sets are admitted** as modular sets (`GalaxyModularSetAdmission`).
  Each needs its faces authored and in `gmw`, and its exact recipe, in declared
  order, with the modular role. Copy counts are pinned to the vendored MarvelSDB
  snapshot. A missing face or a changed recipe closes that set alone
  (`GalaxyModularSetAdmissionTests`). Any scenario slot that accepts a modular set
  accepts them. Galactic Artifacts opened with Crystal Ball (16130), MARVEL-911.

None of the sets needs a new interaction. Every choice uses an existing
prompt: a ranked tie for an attachment target, a "may" option, a cost payment,
an attack's defense, and for Crystal Ball the scoped card-play prompt that
Chaos Magic and Team-Building Exercise already use, here declinable.

## Interpretations

Each choice is recorded in the row note or code comment that owns it. MarvelCDB
has no FAQ entry for these cards; the rulings are Hall of Heroes entries in
`datasets/rulings/`.

- **Ranked attachment targets are read before the card attaches**
  (rr:attach-to.3). Cloak of Hercules and Vandarian Power Stone go to the lowest
  ATK or SCH; Kree Combat Armor and The Beyonder's Blazer to the highest. A tie is
  the first player's choice (rr:first-player.1). SCH joins the ranked selector
  vocabulary for this.
- **"Any player can do this"** on Obedience Potion and The Poison overrides
  rr:attachment.2.1 (rulings fc47a4c917f820fc, 01adfa3a8a285689). Our reading:
  the player who uses Obedience Potion's action pays its cost, so that player's
  identity takes the 1 damage (rr:cost).
- **The Beyonder's Blazer's threat is a cost.** It is paid before the effect and
  is not prevented by an effect that prevents threat (rr:cost; ruling
  00217fa2dcbc0f88 treats the analogous damage the same way). No interrupt
  window opens for it. Only Wrecking Crew side schemes respond to threat placed,
  and those modes refuse modular sets.
- **The Poison** counts its counters after placing one, so its first trigger
  deals 1 damage and each later one deals one more.
- **Crystal Ball** lets the player credited with defeating it play one card from
  their hand for 3 less, never below 0. "The player who defeated this scheme" is
  the credited defeater, as for Mystique's Manipulations (ruling
  7130a7550f1a2d8b): the thwarting identity's player, or an ally's controller.
  The text is a permission to play outside the game's timing
  (rr:play-restrictions-and-permissions.2), so it works in any phase and in either
  form. Only the resource cost changes; every other cost and restriction stays in
  effect, as for Fetch Quest's play ignoring its resource cost (ruling
  4eedaeb17e53584f): "[type] form only" (rr:play-put-into-play.1), uniqueness,
  limits, and an event's arrow cost, paid together with the reduced cost
  (rr:cost.5). An event is played by triggering one of its Actions
  (rr:player-turn.5.d); an Interrupt or Response event cannot be played, because
  its own triggering condition is not occurring (rr:initiating-abilities step 2).
  Resources beyond the reduced cost are not paid for the card (rr:cost.4.1), so a
  paid-resource condition reads only what the reduced cost took; every
  paid-resource event in the admitted pool costs 3 or less, so it reads an empty
  payment. With no card that can be played nothing is asked; the player may also
  decline (rr:may). Ship Command and the Milano are withheld, so they never meet
  Galactic Artifacts in an admitted game. In research games the Milano's "wild
  resource for any player" pays a Crystal Ball play as it pays any other card play,
  through the cross-seat payment that MARVEL-905 still needs a table for.
- **The three artifact side schemes** offer only their defeater the optional
  benefit (`eachPlayer player: defeater` with a decline option). Victory 0 puts
  them in the victory display (rr:victory-x.2).
- **Kree Combat Armor** reduces each attack's damage by 1 after tough (ruling
  d7dcff2da6228c89); the damage dealt is unchanged (ruling 05670207b5acc4d9). Its
  "spend 3 resources of the same type" is three actions, one per printed type; a
  wild resource covers any of them.
- **Kree Lieutenant's +3 boost icons** are counted, since a Boost ability resolves
  before the icons are counted (rr:attack-enemy-activation.step.3.b, step.3.c),
  and an interrupt that cancels printed icons does not cancel them (ruling
  1ea208998735ef4b).
- **Starshark's attacks deal indirect damage** after the defense, and only the
  defender, or the identity of an undefended attack, is attacked
  (rr:indirect-damage.5, .5.1; ruling 2206f35b71f2b1f6).
- **Removed from the game.** Pirate Commander removes a random card in hand and
  Pirate Lackey the top card of the deck after their attack damages the identity.
  A removed card is out of the game and may be looked at (ruling
  57fc672ad0921420). A deck emptied this way resets at once (rr:player-deck.1).
- **The pirates' boost** gives the villain an additional boost card only while
  the villain is the activating enemy (ruling fba0af85176f8559).
- **Honor Among Thieves** reveals the discarded Criminal minion first, including
  its quickstrike, then gives it tough (ruling 37020901515c3f59). The sentence
  resolves before the quickstrike attack's steps, so tough is in place for that
  attack (ruling ef63f3668700ef73).

## Engine changes and existing content

The words are documented in [card-dsl.md](../card-dsl.md). The shared scoped
play changes Chaos Magic (15003) in admitted Scarlet Witch games: an event it
offers is priced through its Action, its arrow cost is paid, and an event it
cannot play is not offered. The replay identity is therefore
`engine-replay-v37` ([session-ledger.md](../session-ledger.md)).

- `playFromHand` (MARVEL-911) plays a card from the resolving player's hand at a
  stated reduction, optionally declinable. It shares the scoped play of
  `playSharedHeroTraitCard` and `playIgnoringResourceCost`. In all three an event
  now plays through the ordinary Action procedure at the scoped price, including
  an arrow cost and its targets, and a scoped play with no playable card resolves
  nothing without asking.

- `scheme` is a ranked selector (`AbilityCardRank.Scheme`), read on the live board.
- `attacks_deal_indirect_damage` is a modifiable field read when an enemy attack
  is prepared (`IndirectEnemyAttacks`).
- `removeFromGameAtRandom` and `removeTopFromGame` remove player cards from the
  game (`PlayerCardRemoval`).
- `placeThreatOnMainScheme` is a cost (`AbilityThreatCost`).
- A player's elimination whose departing upgrade defeats the last other identity
  ends the game once (rr:player-elimination.4), as when Endurance (05023) leaves
  with its owner and defeats the partner it is attached to
  (`EliminationCascadeTests`).

## Evidence

- Synthetic boards over the production book: `CrystalBallTests`, `GalacticArtifactsTests`,
  `GalacticArtifactSchemeTests`, `KreeMilitantsTests`, `MenagerieMedleyTests`,
  `SpacePiratesTests`.
- Admission: `GalaxyModularSetAdmissionTests`.
- Complete public games with exact same-seed replay, two per admitted set:
  `GalaxyModularPublicGameTests`; a fresh-host round trip per admitted set:
  `GalaxyModularPublicRestartTests`. Galactic Artifacts' are `CrystalBallPublicTests`:
  three public starter games (one with two players) in which Crystal Ball's
  defeater plays a card, and a fresh-host round trip at that prompt. The deal is
  unarranged; a player thwarts Crystal Ball whenever a turn offers it.
- A zero-raise scan of every admitted scenario mode × seeds 1–40 × one and two
  players, with each admitted set as the modular set, is reported on MARVEL-904.
- Complete non-gmw games played on main and on this branch produce the same
  prompts and state digests, so the replay identity is unchanged. For
  MARVEL-911, 890 completed games (every hero in twelve scenarios, Scarlet Witch
  and Ant-Man for the existing scoped plays, pairs, and six modular sets) match
  919ceb77 exactly.
- Crystal Ball's own scan (MARVEL-911): 1,600 games with Galactic Artifacts as the
  modular set, played plainly, again thwarting Crystal Ball when offered, and again
  with Crystal Ball placed at setup so most games reach its play. The last made
  1,572 Crystal Ball plays of 147 distinct cards (564 events, 418 upgrades, 321
  allies, 269 supports). All three had zero raises and zero failures.
