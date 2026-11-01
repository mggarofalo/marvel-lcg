# Ship Command and Power Stone

Packet 3 of The Galaxy's Most Wanted (`gmw`, rules insert `mc16`) authors the
Ship Command modular set — the Milano (16142), Rogue Vessel (16143), Cannonade
(16144), Blind Side (16145), Hull Breach (16146), Power Siphon (16147) and Special
Delivery (16148) — and the one-card Power Stone modular set (16149). Ruling
408decdf6cd8c4b5 names both as modular encounter sets of the box. The plan is
[gmw-wave-plan.md](gmw-wave-plan.md) (MARVEL-904).

## Boundary

- **Power Stone is admitted** as a modular set (`StandaloneProductAdmission.PowerStone`).
  It needs 16149 authored, in `gmw`, and the exact one-card `power_stone` recipe
  with the modular role. A missing face or a changed recipe closes the set alone,
  and every other product stays open (`PowerStoneAdmissionTests`). Any scenario
  slot that accepts a modular set accepts it.
- **Ship Command is authored and withheld.** All seven faces have rows and
  engine support, but the set stays closed: the Milano's "generate a [wild]
  resource for any player" lets one seat spend a card another seat controls.
  The engine offers the first player's Milano in every player's payment, as an
  alliance payment offers another player's sources. The table has no
  presentation for paying with a card controlled by another seat, or for that
  seat's part in the decision. That is new UI, so the set waits for it.
  `ship_command` is removed from discovery with the rest of the closed box.

## Interpretations

Each choice is recorded in the row note or code comment that owns it. MarvelCDB
has no FAQ entry for these cards; the rulings are Hall of Heroes entries in
`datasets/rulings/`.

- **The Milano follows the first player.** "The first player controls the
  Milano" moves it to the new first player's play area when the token passes
  and when the first player is eliminated (ruling 027e5dbfef0d5861;
  `FirstPlayerControl`). It keeps its exhausted state. It is an encounter card,
  so the scenario owns it (rr:ownership-and-control).
- **Any player may exhaust the Milano for an encounter card's option** without
  controlling it (ruling 65d0813b02e75e59). An exhausted Milano is not a valid
  target, so its option is not offered (rr:choose-option.1, rr:exhausted.1).
- **First Player Actions** (Rogue Vessel, Cannonade) are offered only to the
  first player (`isFirstPlayer`). Rogue Vessel's cost exhausts the Milano, so the
  Milano cannot also generate one of the two resources it spends (rr:exhausted.1,
  rr:exhausted.2).
- **Peril.** While a player resolves a peril card, other players cannot trigger
  abilities (rr:peril.1), so the first player's Milano cannot pay for that player
  and the first player cannot interrupt Hull Breach's damage (ruling
  1a4413236d2a28d8). The rule applies to every peril card.
- **"The first player"** in Blind Side, Hull Breach and Power Siphon is the first
  player, not the revealing player.
- **Power Stone moves on damage dealt by one attack.** The forced response
  counts damage dealt, not taken (ruling ea69fd70a070ee2c), so a tough status
  card does not stop it. Overkill damage counts (ruling b6fbf7538dd6b8d2), and a
  hero's attack includes the attacks of its events and upgrades
  (rr:you-your.12–.14). An ally's attack does not (rr:you-your.15), and neither
  does a minion's. A villain whose final stage the attack defeats ends the game
  first (ruling fd5a4d77de2f47a6). A defeated earlier stage is succeeded by a new
  stage that is the same character and keeps the stone (rr:villain-defeat.3.1,
  .3.2), so the stone moves to the attacker from it. The engine adds
  `WhenAttackDealtDamage` to the completing attack's occurrence, with the damage
  per recipient.
- **A permanent attachment whose host leaves play** resolves its "attach to"
  text (rr:permanent.5). Power Stone on an eliminated identity returns to the
  villain (ruling 8944f7176577c908, which supersedes 327f218277679f1e). Only a
  direct attachment with exactly one valid target is modelled; any other case
  raises.

## Engine changes and existing content

- `WhenAttackDealtDamage` joins every attack's occurrence that deals damage.
  Response-window labels name their occurrence's conditions, so the replay
  identity is `engine-replay-v36` ([session-ledger.md](../session-ledger.md)).
- The peril restriction on other seats' abilities applies to every peril card,
  including those already admitted.
- A card the first player controls by its own text keeps the scenario as its
  owner when it enters a player's play area (rr:ownership-and-control.2.2 names
  only player card backs).
- `AttachmentTransfer` moves an in-play attachment to a new host;
  `PermanentAttachments` and `FirstPlayerControl` are the two new rules.

## Evidence

- Synthetic boards over the production book: `ShipCommandMilanoTests`,
  `ShipCommandTreacheryTests`, `PowerStoneTests`.
- Admission: `PowerStoneAdmissionTests`.
- Research-only complete games with Ship Command, dealt directly rather than
  through discovery, with exact same-seed replay and the Milano's setup:
  `ShipCommandResearchGameTests`.
- Complete public games with exact same-seed replay:
  `PowerStonePublicGameTests`.
- A zero-raise scan of every admitted scenario mode × seeds 1–40 × one and two
  players, with Power Stone as the modular set, is reported on MARVEL-904.
