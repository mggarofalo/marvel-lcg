# Nebula

Packet 7 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors scenario
4, Nebula (16088–16102): Nebula (I)–(III), The Art of Evasion (1A/1B) and Warp
Drive Initiated (2A/2B), Nebula's Ship, the five Technique attachments (Cutthroat
Ambition, Evasive Maneuvering, Unyielding Persistence, Weapon Mastery and Wide
Stance), Lethal Intent, Barrel Roll, Combat Ready and Ruthless. The plan is
[gmw-wave-plan.md](gmw-wave-plan.md) (MARVEL-904). The scenario's insert is
`pack:mc16:nebula`.

## Boundary

- **The scenario is authored and withheld.** Every face has a row, and the
  Standard and Expert recipes are declared (`nebula`, `nebula_expert`): Nebula
  (I)/(II), or (II)/(III) for Expert; both main schemes; the Standard (and
  Expert), Power Stone and Ship Command sets; Space Pirates as the modular set; the
  scenario's encounter cards with the vendored MarvelSDB quantities; and Nebula's
  Ship set aside for the first main scheme's setup. The scenario's faces are not
  an admitted part of the box, so discovery removes both recipes
  (`SetupCatalog.WithoutFaces`). The scenario waits, as Ronan the Accuser does,
  for the table to present one seat paying with another seat's Milano
  (MARVEL-905; [ship-command-power-stone.md](ship-command-power-stone.md)).
- **No face is unauthored.**

## Interpretations

Each choice is recorded in the row note that owns it. The rulings are Hall of
Heroes entries in `datasets/rulings/`.

- **Nebula's Forced Interrupts** resolve the "Special" ability of every Technique
  attached to her when she attacks or schemes against a player, one at a time in
  an order the player chooses (rr:special), and then discard Techniques: every one
  for stage I, one the player chooses for stage II, and for stage III one the
  player chooses only after removing the top card of their own deck from the game.
  The removal pays for the discard, so the option is offered only while the
  player's deck holds a card (ruling 304a4798e0b453b4, rr:choose-option.1).
  "You" is the player the activation is against (rr:you-your.7); the printed
  ability is split by the activation's kind. Engine choice: stage I discards its
  Techniques in attachment order, which no rule orders and no state reads. The
  hero Nebula's Technique ruling (ce1123737f82aa7e) does not apply to the villain,
  and the hero Nebula cannot be played in this scenario (ruling 5bdb22a5edc6a0b6).
- **The first Technique revealed each round gains surge.** Each Nebula stage has
  a forced interrupt to a card being revealed: for the first Technique attachment
  revealed in a round it records a round mark and gives that card surge until the
  end of the phase. Canceling the card's effects, as Black Widow (01075) does,
  cancels the surge, a When Revealed ability (rr:surge.1; ruling 4e44bf466c0d8971),
  but the card was still revealed, so the next Technique does not gain surge. A
  Technique resolved as a boost card is not revealed (rr:boost-boost-icon) and does
  not use the round's surge.
- **A Technique boost card** attaches to Nebula after the activation and then
  resolves its Special, in the printed order. Attaching makes the card a new
  object, so the Special names it as the same physical card now attached
  (`attachedThis`).
- **Setup.** The Art of Evasion's Setup, resolved in step 12a
  (rr:appendix-ii-setup.step.12.a), puts the set-aside Nebula's Ship into play and
  discards the top 2 encounter cards per player, attaching each Technique among
  them to Nebula. A Technique attached this way is not revealed, so its surge and
  When Revealed do not resolve. The Power Stone and the Milano are already in play
  from their Setup keywords (rr:setup-keyword.1).
- **Escalation.** Both main schemes' X is the number of evasion counters on
  Nebula's Ship, not multiplied per player, and 0 without the ship in play
  (rr:main-scheme-main-scheme-deck).
- **Warp Drive Initiated** reads the counters after placing its 2, and discards 2
  cards per counter from each player deck and the encounter deck (rr:discard.4).
- **Shoot the Thrusters!** is authored twice, for 1 resource and for 2, each
  offered only while the ship has as many evasion counters as it spends; spending
  2 to remove 1 does no more than the 1-resource action. Only the first player may
  take it (rr:action.1); the Milano is exhausted as part of the cost
  (rr:exhausted.1).
- **Cutthroat Ambition** limits the damage Nebula takes from each attack to 5 in
  total across that attack's damage instances (rr:cannot); the smallest maximum
  applies, and Wide Stance's reduction of 1 comes first. The consequence preview
  shows the damage she can still take.
- **Evasive Maneuvering and Unyielding Persistence** give stalwart; gaining it
  removes a stunned or confused status card Nebula already has (ruling
  c03bdba88590962d).
- **Lethal Intent** and **Combat Ready** discard until a Technique and reveal it,
  so its surge and When Revealed resolve; Combat Ready then resolves the revealed
  card's Special for the revealing player.
- **Ruthless** places an evasion counter when its activation deals damage or
  places threat; damage dealt to a tough hero counts (ruling ea69fd70a070ee2c).

## Engine changes and existing content

The replay identity stays `engine-replay-v36`
([session-ledger.md](../session-ledger.md)): no admitted card changes behaviour in
a reachable state. One admitted card reads a changed rule: Pirate Lackey's (16139)
removal of the top card of a player's deck now has no target while that deck is
empty. A player deck that empties resets at once (rr:player-deck.1), so the state
cannot arise in play; were it reached, the game state would be the same and only
the event trace could differ. The words are documented in
[card-dsl.md](../card-dsl.md).

- `markThisRound` and `markedThisRound` record and read a named fact that lasts
  until the end of the round (`RoundMarks`).
- `revealed` names the card the resolving effect revealed; the lowering requires
  a revealing effect and refuses a read before it.
- `attachedThis` names this physical card once the ability has attached it; the
  lowering requires an `attachCards` of `this` in the same effect.
- `discardEach` discards every card a plural selection finds.
- `attack_damage_maximum` is a modifiable field; damage placement applies the
  smallest maximum to an attack's damage after its reductions
  (`AttackDamageMaximum`).
- A sequence continues after resolving ordered Special abilities, and
  `removeTopFromGame` has a valid target only while the named deck holds a card.
- Lowering refuses a `when` on a When Revealed ability, which nothing reads
  (rr:when-revealed-abilities); the condition is an `if` inside the effect.

A differential of 280 complete non-gmw games (Rhino, Klaw Expert, Ultron, Risky
Business, Crossbones, Wrecking Crew Standard, Mutagen Formula, Red Skull Expert and
Zola; Rhino and Ultron with the admitted Power Stone modular set; Rhino, Klaw and
Ultron with the admitted Space Pirates modular set, whose Pirate Lackey removes a
deck's top card; one and two players; seeds 1–10), hashing every prompt and state
digest, is identical on 09a59418 and this packet.

## Evidence

- Synthetic boards over the production book: `NebulaVillainTests`,
  `NebulaTechniqueTests`, `NebulaSchemeTests`, `NebulaEncounterTests`; the
  vocabulary alone: `NebulaLoweringTests`.
- Research-only complete Standard and Expert games, dealt directly rather than
  through discovery, with exact replay, and the setup: `NebulaResearchGameTests`.
- Every new decision has a mutant: 69 in all, over the engine words, the attack
  maximum and its preview, the When Revealed guard, the revealed and attached-card
  validators, the top-card removal's target, `discardEach`'s option admission, and
  each row's conditions and amounts (including "you" on the Techniques' Specials
  and Ruthless, "per hero" on the setup, the first player on Shoot the Thrusters!,
  the reveal interrupt's round mark and surge, and the boost's attach-then-Special
  order). 63 are killed; 6 are equivalent and documented where they live: the last
  revealed card, `discardEach`'s two guards, Nebula (I)'s Technique condition, and
  the Technique tests of the setup and of the reveal interrupt.
- A scan of 3,040 complete research games (every admitted hero alone and in 19
  pairs, Standard and Expert, seeds 1–40) raises nothing: 2,380 end with every
  player eliminated, 660 with the villain completing the final main scheme, none
  with Nebula defeated and none unfinished.

## New interactions

None beyond the existing Ship Command need. Every choice uses an existing prompt:
the order of Special abilities, a card choice, an option, an action on an
environment. Evasion counters on Nebula's Ship appear as card counters; the
first player's Shoot the Thrusters! spends the Milano across seats (MARVEL-905).
