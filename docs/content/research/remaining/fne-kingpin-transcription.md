# Fear No Evil: Kingpin transcription candidate

Status: **supplied-source transcription accepted by [independent proofread](fne-kingpin-proofread.md)**. This research packet does not admit cards to the dataset or implement gameplay.

All 27 supplied originals were individually viewed at original resolution, including each of the three Bag of Tricks and three Unrelenting Hate copies. Printed numbers and `(n/22)` positions establish 23 distinct gameplay faces, 18 printed base numbers and 22 physical cards. All 27 original hashes are distinct. Acquisition filenames are locators: `_2` and `_3` identify supplied scans of repeated designs, not additional gameplay face identities.

| Printed cards | Positions | Physical quantity |
|---|---|---|
| Kingpin 159A/B, stages A1/A2 | 1 | 1 |
| Kingpin 160A/B, stages B1/B2 | 2 | 1 |
| The King’s Gambit 161A/B, stages 1A/1B | 3 | 1 |
| Endgame 162A/B, stages 2A/2B | 4 | 1 |
| Public Support 163A/B, LOW/HIGH | 5 | 1 |
| Bear Hug 164 through Organized Crime 174 | 6–16 | 11, one each |
| Bag of Tricks 175 | 17–19 | 3 |
| Unrelenting Hate 176 | 20–22 | 3 |

The candidate records 782 field/source pointers, each with an original filename, SHA-256 and visual region. It retains titles, uniqueness, type, trait punctuation and emphasis, main rules, separated boost abilities, printed emphasis and glyph placeholders, stats and annotations, flavor and credits, copyright, product-symbol placement, set positions and A/B pairing. Quantity counts distinct printed positions for a gameplay face; `printed_copy_quantity` records the single physical position represented by each scan. Pairing and quantity use the printed faces and footers, without packaging reconciliation.

Kingpin’s A1 and B1 ATK fields print a star alone; their SCH values are 2 and 3. A2 and B2 have numeric SCH/ATK values with reminder stars on both, and the printed stages are preserved literally. All four faces print per-player hit points. The King’s Gambit prints threshold 11 per player and starting threat 3 per player; Endgame prints threshold 7 per player and starting threat 0 per player; each B face has acceleration +1 per player. Public Support’s LOW formula is preserved as `2 [per_player]+2`, and HIGH prints removal of `1 [per_player]` support counters.

Black Cat’s one blue badge is labeled SCH/THW with value 1, and its action contains a mental-resource glyph and cost arrow. The candidate records SCH and THW from that shared badge; consequential damage in its ability is text, not printed consequential-damage icons. James Wesley and Vanessa Fisk print SCH +1 modifiers and star-only boost fields; Kingpin’s Cane prints ATK +1 and a boost star plus one numeric boost. Diamondback has an ATK reminder star; Stilt-Man has SCH and ATK reminder stars. Kingpin’s Influence prints a hazard icon; Organized Crime has no scheme icon. Bag of Tricks prints a star-only boost field; Unrelenting Hate prints one numeric boost plus a star. No resource icons appear in the card borders.

The official Rules Reference v1.7 page 22 visual key and current vendored v1.8 `rr:icons` and `rr:star-icon` entries were consulted. The older visual key identifies glyph shapes; current rules remain authoritative. This packet transcribes printed facts without adjudicating timing, campaign difficulty or the separately recorded ambiguity873.

External evidence is under `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `kingpin-candidate.json`, SHA-256 `f1561395c66caec960e059b21793b304236c02a027f0d454aee6c138c14439f7`.
- `build-kingpin-candidate.py` constructs the complete candidate offline; `--check` compares exact bytes. Each original’s pinned size and SHA-256 is checked before generation.
- `validate-kingpin-candidate.py` checks complete manifest-section coverage, original sizes/hashes, field/source parity, all printed positions, all five face pairs, distinct design counts and quantities, stage markers, stats, modifiers, glyph distinctions, scheme icons and boost fields. It writes `kingpin-transcription-findings.json`.
- `scan-manifest.json`, unchanged SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, pins acquisition URLs and all original paths/hashes. The candidate separately pins the visual-key hash.

Offline generation, exact-byte `--check` and structural validation passed for the candidate hash above. Independent proofread must inspect all 27 originals and every recorded field/source region, including Endgame A-face flavor emphasis and absent versus present illustrator credits. No known unreadable printed field or placeholder footer was encountered in this family. Independent visual certification, redistribution and dataset admission remain pending or outside this packet; structural checks do not establish transcription acceptance.

Independent proofread accepted the unchanged candidate hash above after individually inspecting all 27 originals and checking all 782 fields/source pointers. No correction or unresolved transcription finding remains. Campaign difficulty question MARVEL-873 and dataset/runtime admission remain separate.
