# Galaxy's Most Wanted wave plan

The Galaxy's Most Wanted campaign box (`gmw`, rules insert `mc16`) has 207
printed faces, 16001a–16187. The canonical pack record is MARVEL-573; the wave
is tracked in MARVEL-904 and this plan is its delivery record. Printed text comes from `datasets/cards/`,
physical quantities from the vendored MarvelSDB snapshot
(`datasets/marvelsdb/pack/gmw.json`, `gmw_encounter.json`) and the rules from
`datasets/rules-packs/mc16/`. The vendored insert text for both starter decks is
truncated mid-list (`pack:mc16:groot-protection`, `pack:mc16:aggression`), so
starter quantities are a repository choice from the MarvelSDB snapshot, as for
Quicksilver. MarvelCDB has no FAQ entries for any `gmw` card; Hall of Heroes
rulings are cited per row.

## Inventory

| Set | Faces | Content |
|---|---:|---|
| Groot (hero, starter, obligation) | 25 | 16001a/b–16025: identity, 10 signature titles, Protection 16012–16018, Basic 16019–16024, Wilt |
| Groot nemesis | 3 | 16026 Blazing Inferno, 16027 Furnax, 16028 Fan the Flames ×3 |
| Rocket Raccoon (hero, starter, obligation) | 25 | 16029a/b–16053: identity, 10 signature titles, Aggression 16040–16046, Basic 16047–16052, Crisis on Halfworld |
| Rocket nemesis | 4 | 16054 Vendetta, 16055 Blackjack O'Hare, 16056 Blackjack's Bazooka, 16057 Planetary Invasion |
| Brotherhood of Badoon (scenario 1) | 14 | Drang I–III, two main schemes, Badoon Ship, Drang's Spear, Badoon Engineer, four side schemes |
| Infiltrate the Museum (scenario 2) | 11 | Collector I–III, The Grand Collection, Biogram Image, Monarch Starstalker, four treacheries |
| Escape the Museum (scenario 3) | 14 | Collector A1/A2 and B1/B2 flip villains, three main schemes, Library Labyrinth/Museum Ship, two treacheries |
| Nebula (scenario 4) | 17 | Nebula I–III, two main schemes, Nebula's Ship, five Technique attachments, Lethal Intent, three treacheries |
| Ronan the Accuser (scenario 5) | 16 | Ronan I–III, two main schemes, Kree Command Ship, Universal Weapon, Fanaticism, three side schemes, three treacheries |
| Modular sets | 30 | Band of Badoon 5, Galactic Artifacts 9, Kree Militants 4, Menagerie Medley 3, Space Pirates 4, Ship Command 7 (incl. the Milano), Power Stone 1 |
| Campaign: The Market | 28 | 16150–16177 player cards with unit costs, including eight Milano Mod upgrades |
| Campaign: Challenge and Badoon Headhunter | 15 | five double-sided challenge side schemes 16178–16182, Badoon Headhunter set 16183–16187 |

Reprints already in the ability book: Desperate Defense (16013 = 09015), The
Power of Protection (16015), Indomitable (16018), Energy/Genius/Strength
(16021–16023, 16049–16051), Into the Fray (16042), Relentless Assault (16044) and
Chase Them Down (16041). Reprints still need their own rows.

## Capability map

**Expressible now.** Counter placement and removal costs (`placeCounters`,
`removeCounters`, `countersOn`), numeric `min`/`add`/`mul` for "to a maximum of
10" (ruling `39c26b6b8d77edfd`: the maximum is local to the ability), basic-use
interrupts and responses, defense events and after-defense responses
(`heroDefended`, `attackDamagedYou`), "play under any player's control",
Team-Up by title in either form (`rr:team-up`, ruling `9e3a063b5c91ec99`),
conditional retaliate grants, `reduceNextCardCost`, indirect damage, obligations
with surge, nemesis side schemes, villainous, amplify, quickstrike, patrol,
guard, toughness, stalwart, hinder, incite, overkill, piercing, victory display,
environments and `Special` abilities.

**New vocabulary (small engine work).**

- Bracketed trait prerequisites. "Play only if your identity has the
  [guardian] trait" prints the trait as an icon token; `PrintedPlayRestrictions`
  reads it. Affects 16019, 16047, 16052 and later Guardians cards.
- Counter-soaking prevention (Groot's Flora Colossus): remove as many counters
  as damage remains, preventing one per counter (rulings `a1a372ea9b59ba4e`,
  `7cf2a0ea8a6b2a1f`, `c8268c2138f6c809`).
- Exact-remaining-hit-points damage condition (Starhawk; ruling
  `dc116cb456de74af`: a card can take damage beyond its remaining hit points).
- A starting-hit-points comparison for Fighting Fit and Dauntless ("starting
  hit points" is the value after setup, ruling `7cb6a0fe6e2d6039`).
- Charge counters without Uses (`startingCounters` with `uses: false`), moving a
  counter between cards (Battery Pack), "highest cost upgrade" (Crisis on
  Halfworld), excess-damage increase (Follow Through), per-damage healing until
  end of turn (Schadenfreude). Rocket packet.

**New engine capability.**

- *The Collection* (scenario 2): an out-of-play shared game area with a
  discard-replacement interrupt for every card leaving play to a discard pile
  (`pack:mc16:the-collection`, Q&A in `pack:mc16:aggression`), a loss condition
  counting its cards, and a player action that returns a card to its owner's
  discard pile.
- Infinite hit points and flip-instead-of-defeat villains (scenario 3,
  `pack:mc16:infinite-hit-points-new`), round-end flip and dial reset, and
  "advance to stage 2A (the players win by advancing)".
- Nebula's Technique attachments: first-per-round surge, resolving every
  attached `Special` on activation, evasion counters setting X on the main
  scheme's escalation threat.
- Power Stone transfer between villain and heroes (permanent, setup, transfer on
  a 3+ damage attack) and "the player who controls the Power Stone".
- The Milano: an encounter support under first-player control, used by First
  Player Actions across Ship Command and every scenario; peril treacheries.
- Campaign mode: units, The Market (unit costs, group-unique market cards outside
  deck size), challenge side schemes per scenario, the Badoon Headhunter thread,
  expert-campaign persistent damage and elimination rules
  (`pack:mc16:units-and-the-market`, `pack:mc16:persistent-damage`,
  `pack:mc16:elimination-and-victory`). The Rise of Red Skull campaign supplies
  persistent progress, saves and rewards to build on.

**New UI interactions** (owner review). The Collection needs a visible shared
out-of-play pile with a per-card action; scenario 3's flip villain with infinite
hit points needs a readable dial; the campaign market needs a between-scenario
purchase screen. The two hero packets reuse existing interactions.

## Packets

Each packet is independently mergeable and closes its whole product: a missing
face or recipe component closes that product without affecting predecessors.

1. **Groot hero** — 16001a/b–16028 (29 faces), the 40-card starter (15 signature,
   17 Protection, 8 Basic), Wilt and the five-card nemesis set. Public admission.
2. **Rocket Raccoon hero** — 16029a/b–16057 (30 faces). Charge-counter weapons,
   Battery Pack, Follow Through, Schadenfreude, Crisis on Halfworld, villainous
   Blackjack O'Hare. Public admission. Flora and Fauna's Rocket option becomes
   reachable in a Groot + Rocket game.
3. **Ship Command and the Milano, Power Stone** — 16142–16149. Prerequisite for
   scenarios 1, 3, 4 and 5. Power Stone is admitted as a modular set; Ship
   Command is authored and withheld until cross-seat Milano payment has UI.
4. **Brotherhood of Badoon** with Band of Badoon — scenario 1 Standard/Expert.
5. **Remaining modulars** — Galactic Artifacts, Kree Militants, Menagerie Medley,
   Space Pirates; each admitted as a modular set like Kree Fanatic.
6. **Ronan the Accuser** — scenario 5 (Power Stone, Kree Command Ship cancel).
7. **Nebula** — scenario 4 (Techniques, evasion counters).
8. **Infiltrate the Museum** — scenario 2 (The Collection). Owner UI review.
9. **Escape the Museum** — scenario 3 (flip villains, infinite hit points). Owner UI review.
10. **Campaign** — The Market, challenges, Badoon Headhunter, expert campaign.
    Owner UI review for the market.

## Packet 1 status

Packet 1 is authored and admitted; its evidence is in
[groot-public-admission.md](groot-public-admission.md).

## Packet 2 status

Packet 2 is authored and admitted; its evidence is in
[rocket-raccoon-public-admission.md](rocket-raccoon-public-admission.md).
Flora and Fauna's Rocket option is reachable in a Groot and Rocket Raccoon game.
The first closed face of the box is Drang (16058).

## Packet 3 status

Packet 3 is authored. Power Stone (16149) is admitted as a modular set; Ship
Command (16142–16148) is authored and withheld because the Milano pays for any
player and the table cannot yet present one seat spending another seat's card.
The evidence is in [ship-command-power-stone.md](ship-command-power-stone.md).

## Packet 4 status

Packet 4 is authored. Band of Badoon (16117–16121) is admitted as a modular set.
The Brotherhood of Badoon scenario (16058–16069) is authored and withheld, not
admitted because its encounter deck includes the withheld Ship Command set; its
closed faces keep both recipes out of discovery. Drang (III) (16060) stays
unauthored because ruling 82bcc63411002a00 defers the quickstrike, teamwork and
responses of the minions his When Revealed puts into play until all of them have
engaged, which the engine's per-entry windows cannot do. The evidence is in
[brotherhood-of-badoon.md](brotherhood-of-badoon.md).
