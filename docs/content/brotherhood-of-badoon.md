# Brotherhood of Badoon and Band of Badoon

Packet 4 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors scenario
1, the Brotherhood of Badoon (16058–16069), and the Band of Badoon modular set
(16117–16121). Ruling 408decdf6cd8c4b5 names Band of Badoon one of the box's
modular encounter sets. The plan is [gmw-wave-plan.md](gmw-wave-plan.md)
(MARVEL-904). The scenario's insert is `pack:mc16:brotherhood-of-badoon`.

## Boundary

- **Band of Badoon is admitted** as a modular set
  (`StandaloneProductAdmission.BandOfBadoon`). It needs 16117–16121 authored, in
  `gmw`, and the exact ten-card `band_of_badoon` recipe with the modular role: two
  Badoon Assassin, three Badoon Grunt, one Badoon Lieutenant, two Badoon Sentry
  and two Badoon Warlord, the vendored MarvelSDB quantities. A missing face or a
  changed recipe closes the set alone, and every other product stays open
  (`BandOfBadoonAdmissionTests`). Any scenario slot that accepts a modular set
  accepts it.
- **The Brotherhood of Badoon scenario is authored and withheld.** Its Standard
  and Expert recipes are declared (`brotherhood_of_badoon`,
  `brotherhood_of_badoon_expert`). Both include the Ship Command set, which is
  withheld until the table can present one seat paying with another seat's Milano
  ([ship-command-power-stone.md](ship-command-power-stone.md)), so discovery
  removes both recipes.
- **Drang (III) (16060) is unauthored.** Its When Revealed discards the top 4 per
  player cards of the encounter deck and puts each discarded minion into play
  engaged with the player engaged with the fewest minions. Ruling
  82bcc63411002a00 orders what follows: no When Revealed abilities; each minion's
  Interrupts right after it engages; Quickstrike and Teamwork only after all of
  them have engaged; then Responses. The engine opens each entry's quickstrike
  and response window before the next card enters, so two or more discarded
  minions would resolve in the wrong order. Revealing Drang (III) raises the
  unauthored-card error rather than guess. It appears only in Expert mode.

## Interpretations

Each choice is recorded in the row note that owns it. MarvelCDB has no FAQ entry
for these cards; the rulings are Hall of Heroes entries in `datasets/rulings/`.

- **Charge Up is a Special** on the Badoon Ship (rr:special). Drang (I) and (II)
  resolve it after Drang schemes, the two main schemes and Bombardment after
  step one of the villain phase, Protect the Planet when revealed, and Badoon
  Engineer after it engages a player, after it activates and as a boost. With no
  Badoon Ship in play, the instruction does nothing.
- **"After resolving step one of the villain phase"** is the completion of step
  one, whether or not threat was placed (`WhenVillainPhaseStepOneCompleted`, as
  for Breakout 07001b).
- **The barrage counters.** Each Charge Up places one counter, then tests the
  count, so the count that reaches the test is exactly 4. The 2 indirect damage
  goes to each player, assigned among the characters they control
  (rr:indirect-damage), and all counters are removed.
- **Drang (II)** gives himself a facedown boost card when Drang's Spear is in play
  (it waits for his next activation, rr:boost-boost-icon.6); otherwise he searches
  the encounter deck and discard pile for the Spear, reveals it and shuffles, as
  Absorbing Man does. The revealed Spear attaches to Drang by its own text, gives
  him stalwart and +1 ATK, and a hero discards it for [mental][physical][physical].
- **First Player Actions** on the main schemes and side schemes exhaust the Milano
  and are offered only to the first player (`isFirstPlayer`), as on Cannonade.
  Removing the threat is the encounter card's ability, not the identity's (ruling
  965ad06e4059d294, which names Spatial Positioning). Protect the Planet's damage
  option needs a minion in play (rr:choose-option.1).
- **The Badoon Ship and the Milano at setup.** The recipe sets the Badoon Ship
  aside, and Terrestrial Invasion's Setup puts it into play. The Milano's own
  Setup keyword already puts it under the first player's control
  (rr:setup-keyword.1).
- **Badoon Assassin** attacks the player it engaged with +2 ATK only while that
  player is in hero form: "engages your hero" is quickstrike's wording
  (rr:quickstrike). Its boost gives the attack overkill, piercing and ranged until
  the attack ends.
- **Badoon Grunt** deals the player it engaged one facedown encounter card when no
  other minion is engaged with that player. The card is revealed in the same
  round's villain phase (ruling 8dd7dbf8820fdcfb). As a boost it enters play
  engaged with the activated player, and its own forced response follows.
- **"+2 boost icons for this activation"** (Lieutenant, Sentry, Warlord). A Boost
  ability resolves before the card's icons are added
  (rr:attack-enemy-activation.step.3.b, step.3.c), so the added icons count, and
  cancelling the printed icons does not cancel them (ruling 1ea208998735ef4b). The
  Sentry reads the villain's tough status before giving one, since a character
  cannot hold two (rr:status-cards.1).

## Engine changes and existing content

No engine behaviour changes. Every card is expressed with existing vocabulary.
`RuntimeProductAdmission` gains the Band of Badoon part of the box, and the Galaxy's
Most Wanted parts move into one helper. Existing games, saves and prompts are
unchanged, so the replay identity stays `engine-replay-v36`. A differential of
140 complete non-gmw games (Rhino, Klaw Expert, Ultron, Risky Business,
Crossbones, Wrecking Crew Standard and Mutagen Formula; one and two players;
seeds 1–10) hashing every prompt and state digest is identical on the
predecessor revision and this packet.

## Evidence

- Synthetic boards over the production book: `BadoonShipTests`,
  `BadoonSchemeTests`, `BandOfBadoonTests`.
- Admission: `BandOfBadoonAdmissionTests`.
- Complete public games with exact same-seed replay, Band of Badoon as the modular
  set, a Badoon minion engaged in each: `BandOfBadoonPublicGameTests`.
- A fresh-host round trip at a Badoon minion's first engagement:
  `BandOfBadoonPublicRestartTests`.
- Research-only complete Standard games, dealt directly rather than through
  discovery, with exact replay, the Badoon Ship and the Milano in play from setup:
  `BrotherhoodOfBadoonResearchGameTests`.
- The zero-raise scans are reported on MARVEL-904.
