# Thor component sources — vendored

These images scan FFG's printed Thor components, visibly credited © MARVEL
© 2019 FFG. Hall of Heroes hosts the scans; the printed components supply
the setup and physical-copy evidence. Retrieved 2026-10-25 from the
[Thor product page](https://hallofheroeslcg.com/thor/).

| Local file | Original image | Bytes | SHA-256 |
|---|---|---:|---|
| `thor-deck-divider.jpg` | https://hallofheroeslcg.com/wp-content/uploads/2020/03/thorstarterdeck1.jpg | 237731 | `9184f24f0f04d6bd3b240807d9581c0b214767385a7d54a8b98118e0eead5552` |
| `frost-giant-3-of-5.jpg` | https://hallofheroeslcg.com/wp-content/uploads/2020/09/thorn3.jpg | 355622 | `f5fb485a53a7291a147b5d6bc4754608a91c5d7d1b74b6f43dafd5100cca1fbb` |
| `frost-giant-4-of-5.jpg` | https://hallofheroeslcg.com/wp-content/uploads/2020/09/thorn4.jpg | 362244 | `aa53b1d085d36b1c1b7e1bf9ca2939ce6de06cfd70f58d34b7aab5c0fc5a09dd` |
| `trickster-5-of-5.jpg` | https://hallofheroeslcg.com/wp-content/uploads/2020/09/thorn5.jpg | 338997 | `e9119e6bac87d51c717dcaaf582a9c382676472a88a6fd11c645a68ec25175e5` |

Rights: Marvel/FFG artwork and printed components; no license is granted by
this repository. These exact reviewed images are retained as offline setup
and quantity authority. They are separate from the PDF harvester's inputs.
Verify bytes offline with `Get-FileHash datasets/rules-packs/mc06/*.jpg -Algorithm SHA256`.

The publisher's separately pinned `mc06_thor_rulesheet.pdf`, recorded in the
parent `UPSTREAM.md` and `sources.manifest.json`, directs the first game to use
the cards before the divider and locates the complete decklist on its reverse.
The PDF does not reproduce the list. `thor-deck-divider.jpg` supplies that list.

## Exact divider transcription

The component prints local card numbers. The `060` prefix maps each number to
`datasets/cards/cards.json`; card titles below match that generated catalog.
An unmarked quantity on the divider means one copy. The nemesis entries below
preserve the divider's printing, including its quantity discrepancy.

| Printed group | Canonical cards and quantities | Total |
|---|---|---:|
| Hero Cards | 06002 Lady Sif ×1; 06003 Defender of the Nine Realms ×3; 06004 For Asgard! ×1; 06005 Hammer Throw ×3; 06006 Lightning Strike ×2; 06007 Asgard ×1; 06008 God of Thunder ×2; 06009 Mjolnir ×1; 06010 Thor's Helmet ×1 | 15 |
| Aggression Aspect Cards | 06011 Hercules ×1; 06012 Valkyrie ×1; 06013 Chase Them Down ×2; 06014 Get Over Here! ×3; 06015 Mean Swing ×3; 06016 The Power of Aggression ×2; 06017 Hall of Heroes ×1; 06018 Battle Fury ×3; 06019 Jarnbjorn ×1 | 17 |
| Basic Cards | 06020 Heimdall ×1; 06021 Invulnerability ×3; 06022 Energy ×1; 06023 Genius ×1; 06024 Strength ×1; 06025 Avengers Mansion ×1 | 8 |
| Obligation Cards | 06026 Odin's Anger ×1 | 1 |
| Nemesis Set, as printed on divider | 06027 Family Feud ×1; 06028 Loki ×1; 06029 Frost Giant ×1; 06030 Trickster ×2 | 5 |

The printed player starter is 40 cards: 15 hero, 17 Aggression and 8 Basic.
The identity is the separate double-sided card `06001a/b`. Obligation and
nemesis cards are listed separately from the player deck. Extra customization
cards `06031` Under Surveillance, `06032` Teamwork, `06033` Second Wind and
`06034` Enhanced Physique do not appear in the printed starter list. Their
three copies each in the local upstream inventory are not inferred from the
divider or from a marketing pack count.

## Nemesis quantity discrepancy resolved by numbered components

The divider prints one Frost Giant and two Trickster copies. The individual
printed components instead identify Frost Giant as both **THOR NEMESIS (3/5)**
and **THOR NEMESIS (4/5)**, each card number 29, and Trickster as **THOR NEMESIS
(5/5)**, card number 30. The three pinned scans establish two Frost Giants
and one Trickster. This agrees with `datasets/marvelsdb/pack/thor_encounter.json`
and its set positions 3 and 5. The reconciled nemesis composition is therefore
`06027×1, 06028×1, 06029×2, 06030×1`; do not copy the divider's swapped
quantities into setup.

The card scans here establish physical copy positions only. Printed behavior
and corrected card text continue to come from `datasets/cards/`. This evidence
does not itself authorize runtime product admission.
