# Fear No Evil: Disasters transcription candidate

Research for MARVEL-697, 2026-10-27. Baseline `1677ebcdf26bc44b1f092b91af4b973689c1f92f`; follows the [pinned acquisition](fne-acquisition-candidate.md). This packet records an independently proofread printed transcription; it does not establish dataset admission or runtime support.

The external candidate is `C:/Users/mggar/Source/evidence/fne-acquisition/disasters-candidate.json`, SHA-256 `c66794e43284b3bd8e280817d66c204761f572c9fbad6a4e0b14a29cadbe7c61`. The same directory contains `build-disasters-candidate.py`, `validate-disasters-candidate.py`, `disasters-transcription-findings.json`, and `disasters-validation.txt`. Its pinned `scan-manifest.json` SHA-256 is `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`. The images remain external; source and licensing limits are in the acquisition packet.

All five original files were individually inspected with `view_image` at original resolution. Each candidate field links to its source filename, SHA-256, printed number, footer position and visual region. The images are the printed-text authority; IDs, slugs, JSON keys, markup, normalization and visual descriptions are editorial. `reverse_face: null` means no gameplay reverse supplied in these scans, not a claim about generic backs.

| Face | Printed title | Printed number | Footer | Physical quantity | Original SHA-256 |
|---|---|---:|---|---:|---|
| 60177 | Mystic Incursion | 177 | 1/5 | 1 | `29b58637abde5e08800ebdd59d3946a60b607d333e13c0f4bdc5901e91123946` |
| 60178 | Sinking Ferry | 178 | 2/5 | 1 | `d5cb1bc7afeb6f8335c6ad30e0e58932072c8788447a36e6a0914128bad94577` |
| 60179 | Towering Inferno | 179 | 3/5 | 1 | `f966c58a787cd16bbb95a4226b3dc59fd190055242ad5001ed7b1d99d41cb322` |
| 60180 | Collapsing Bridge | 180 | 4/5 | 1 | `8bcd20377f0d979fc298dc590e9b6fcbab68cb5741d94c1f827b1144d4444fd5` |
| 60181 | Bystanders in Peril | 181 | 5/5 | 1 | `7406c904d0066f8b7179d7860501ccfed8e689a5b460936eadc70acb1fc82386` |

Five distinct faces cover all five positions and establish five physical cards in this family; no supplied faces share a position. The three Disaster environments each print Uses (2 civilian counters), two boost icons, a cost/effect arrow, two matching resource glyphs (mental, energy, physical respectively), and the same large pictogram. Their exhaustion alternatives differ. Collapsing Bridge prints a 1 with an adjacent special star in the starting-threat box, and a separate star followed by Hinder 2 with a per-player glyph in the rules box; it also prints two boost icons and a plus/card pictogram. Bystanders in Peril prints Peril, a boost ability, one numeric boost icon and a separate boost star. These distinctions are captured without treating a star as a per-player symbol.

Pictograms retain their visual descriptions and now include the independently identified crisis (60177–60179) and hazard (60180) attributes. The official Rules Reference v1.7 printed page 22 supplies the visual key; current vendored rr:icons.7 and rr:icons.8 identify the names. This identifies printed glyphs without claiming runtime support. The separate reviewer compared every field and footer against all five pinned originals, including resource glyphs, the two separate stars and the per-player glyph on 60180, 60181's combined boost star and icon, typography and credits. The family count does not resolve the acquisition packet's product-wide one-card quantity discrepancy.

Reproduce offline with:

```powershell
python C:/Users/mggar/Source/evidence/fne-acquisition/build-disasters-candidate.py
python C:/Users/mggar/Source/evidence/fne-acquisition/validate-disasters-candidate.py
```

The validator passed manifest pin, original bytes and hashes, family inventory, positions, quantities, reverse relationships, source pointers, normalization, resource glyphs, and selected star/icon distinctions. These structural checks do not establish semantic accuracy. The linked independent proofread and closure establish printed-field accuracy and glyph identity. Product-wide reconciliation, future offline-input admission and executable behavior remain outside this packet. No dataset or runtime files changed.

The regenerated candidate corrects Peril's source location to the first line of
the rules box, locates Collapsing Bridge's boost row on the lower left edge,
and explicitly records the absence of a large pictogram on Bystanders in Peril.
The original candidate is preserved as `disasters-candidate-before-proofread.json`.
The offline validator passes on the regenerated hash; independent closure is
recorded separately in [the proofread](fne-disasters-proofread.md).
