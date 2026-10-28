# Fear No Evil: Drive transcription candidate

MARVEL-697, 2026-10-27. Repository baseline
`1677ebcdf26bc44b1f092b91af4b973689c1f92f`.
This completes the first transcription pass for the five Drive originals from
the [pinned acquisition candidate](fne-acquisition-candidate.md). Independent
original-scan proofread and correction closure are [accepted](fne-drive-proofread.md). This is research evidence; it does not
admit cards, setup, abilities or runtime support.

The local evidence root is
`C:/Users/mggar/Source/evidence/fne-acquisition/`. Its unchanged
`scan-manifest.json` SHA-256 is
`feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
Only its `Drive` section supplies this candidate. All five source images were
individually inspected using `view_image` at original resolution, including
rules boxes, stat badges, boost rows, credits, printed numbers and footers.

| Original / printed number | Printed title | Printed footer | Captured distinguishing facts |
|---|---|---|---|
| `60186.jpg` / 186 | Sports Car | DRIVE (1/5) | Attachment; VEHICLE.; +1 ATK; highest ATK attachment condition; 4-damage discard threshold; 2 boost icons. |
| `60187.jpg` / 187 | Motorcycle | DRIVE (2/5) | Attachment; VEHICLE.; one +1 SCH/THW badge; highest SCH condition; 3-damage threshold; 1 boost icon. |
| `60188.jpg` / 188 | Utility Van | DRIVE (3/5) | Attachment; VEHICLE.; no stat modifier; fewest remaining hit points condition; 6-damage threshold; 1 boost icon. |
| `60189.jpg` / 189 | Traffic Jam | DRIVE (4/5) | Side scheme; starting threat 2 with special star and no per-player glyph; Hinder 2 with per-player glyph; Forced Interrupt and two threat bullets; crisis icon; 3 boost icons; Ivan Fiorelli credit. |
| `60190.jpg` / 190 | Carjacking | DRIVE (5/5) | Treachery; When Revealed; discard/reveal VEHICLE attachment; optional 3 resources of the same type; 2 boost icons; Joey Vazquez credit. |

All five footer numerators are distinct and cover 1–5/5. There are **5 original
scans, 5 distinct faces and 5 physical cards**, one copy of each, with no supplied
gameplay reverse faces or face pairs. This resolves only the Drive quantities;
the acquisition packet's whole-product quantity question remains separate.
No boost stars, boost abilities, resource icons, unique icons or flavor text
are printed on these five faces. No illustrator credit is printed on the three
attachment originals. Every face carries `© MARVEL © 2026 FFG` and the footer
product pictogram.

## Candidate and provenance

`drive-candidate.json` SHA-256:
`75e3618514e3cdcf80796030e41341f2aae8c90d90a2c90a738cc39f4d313907`.

The candidate contains the transcribed printed text, emphasis, traits, types,
stat modifiers, stars, per-player glyph, boost counts, absence observations,
credits, numbers and footers. Each card field has a resolvable `field_sources`
pointer with the correct original filename/hash, printed number, printed set
position and its specific visual region. `scans` and the findings file retain
the complete original-hash inventory:

| Original | SHA-256 |
|---|---|
| `60186.jpg` | `547917effd0bfae599c9724d339f12eb3834893adea0c61e7a8e1161fbfb63c4` |
| `60187.jpg` | `e58e91b3005f86f441feaa3226406086615d63c3205c8b155085c3ae8b3b2809` |
| `60188.jpg` | `69e80fc5cc8f612f646943d8133d52736bb5b84683e4893b1a005ba2744864a8` |
| `60189.jpg` | `b76762cf5289581e246f0543921e8957df95acf27ed92ab985b58b9ffe1a008b` |
| `60190.jpg` | `0c82d1fdadc4f1f5513b4e186d59e62394e854244c895912103efde11c624772` |

Printed originals are the text authority. IDs, slugs, title case, attribute key
names, HTML emphasis and glyph token spellings are editorial conventions. In
particular, Motorcycle's literal single `+1 SCH/THW` badge is preserved in
`printed_modifiers`; `SCH+` and `THW+` are its editorial attribute mapping.
Conditional “gains surge” is retained in text without inventing an opening
Surge keyword. Traffic Jam's starting-threat special star is independent of
the per-player glyph in its Hinder text.

Glyph names were checked against the current vendored Rules Reference v1.8
[icons entry](../../../../datasets/rules-reference/entries/icons.md), especially
`rr:icons.7`, `.9`, `.10` and `.13`, and the individually viewed retained
official v1.7 visual key `rr-v17-page-22.png` (SHA-256
`65efd0d7160acfdbf2c2a07382b081f2fc81d6ebb5e9d35af8927213a5e2d157`).
The large icon on Traffic Jam is named **crisis**. Its three boost icons sit
along the left edge, separate from its starting-threat star.

## Offline reproduction and remaining review

From the evidence root:

```powershell
python build-drive-candidate.py
python build-drive-candidate.py --check
python validate-drive-candidate.py
```

The builder validates the unchanged acquisition manifest and each original's
size/hash before producing UTF-8 candidate bytes. Its `--check` mode proves
byte-identical offline regeneration. The validator passed the exact family
inventory, original hashes, visual-key hash, field-pointer/source coverage,
quantity/face accounting, plain-text normalization and distinguishing stat/icon
checks. Retained results are `drive-transcription-findings.json` and
`drive-validation.txt` under the evidence root.

No printed-text uncertainty was identified in this pass. Structural validation
cannot establish semantic transcription correctness. The separate reviewer
compared all five originals and accepted the corrected hash after Carjacking’s
two title source locations were changed to its horizontal top banner. The
original candidate is preserved as `drive-candidate-before-proofread.json`. Dataset
admission, runtime interpretation and scan redistribution rights remain outside
this packet. No dataset or runtime files were changed and no builds, hooks or
commits were run.
