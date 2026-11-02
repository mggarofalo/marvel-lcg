# Galaxy's Most Wanted modular sets

Packet 5 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors four
modular encounter sets: Galactic Artifacts (16122–16130), Kree Militants
(16131–16134, setup key `kree_militant`), Menagerie Medley (16135–16137) and
Space Pirates (16138–16141). Ruling 408decdf6cd8c4b5 names all four as modular
encounter sets of the box. The plan is [gmw-wave-plan.md](gmw-wave-plan.md)
(MARVEL-904).

## Boundary

- **Kree Militants, Menagerie Medley and Space Pirates are admitted** as modular
  sets (`GalaxyModularSetAdmission`). Each needs its faces authored and in `gmw`,
  and its exact recipe, in declared order, with the modular role. Copy counts are
  pinned to the vendored MarvelSDB snapshot. A missing face or a changed recipe
  closes that set alone (`GalaxyModularSetAdmissionTests`). Any scenario slot that
  accepts a modular set accepts them.
- **Galactic Artifacts is withheld.** Eight of its nine faces have rows. Crystal
  Ball (16130) is unauthored: its When Defeated lets the defeating player play a
  card from hand for 3 less. Card play inside an encounter card's resolution is
  admitted only for an Action-context play, and the engine has no rule for playing
  an event or other card outside an action window during a When Defeated
  resolution. Authoring it needs that play procedure: a scoped card play opened
  from an encounter effect, with the cost reduction applied and the play's own
  timing restrictions checked. `galactic_artifacts` is removed from discovery.

None of the admitted sets needs a new interaction. Every choice uses an existing
prompt: a ranked tie for an attachment target, a "may" option, a cost payment,
an attack's defense.

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

New vocabulary only; no admitted card changes behaviour, so the replay identity
stays `engine-replay-v36` ([session-ledger.md](../session-ledger.md)). The words
are documented in [card-dsl.md](../card-dsl.md).

- `scheme` is a ranked selector (`AbilityCardRank.Scheme`), read on the live board.
- `attacks_deal_indirect_damage` is a modifiable field read when an enemy attack
  is prepared (`IndirectEnemyAttacks`).
- `removeFromGameAtRandom` and `removeTopFromGame` remove player cards from the
  game (`PlayerCardRemoval`).
- `placeThreatOnMainScheme` is a cost (`AbilityThreatCost`).
- A player's elimination whose departing upgrade defeats the last other identity
  ends the game once (rr:player-elimination.4). The scan found it with Endurance
  (05023) on a partner; the game previously raised there
  (`EliminationCascadeTests`).

## Evidence

- Synthetic boards over the production book: `GalacticArtifactsTests`,
  `GalacticArtifactSchemeTests`, `KreeMilitantsTests`, `MenagerieMedleyTests`,
  `SpacePiratesTests`.
- Admission: `GalaxyModularSetAdmissionTests`.
- Complete public games with exact same-seed replay, two per admitted set:
  `GalaxyModularPublicGameTests`; a fresh-host round trip per admitted set:
  `GalaxyModularPublicRestartTests`.
- A zero-raise scan of every admitted scenario mode × seeds 1–40 × one and two
  players, with each admitted set as the modular set, is reported on MARVEL-904.
- Complete non-gmw games played on main and on this branch produce the same
  prompts and state digests, so the replay identity is unchanged.
