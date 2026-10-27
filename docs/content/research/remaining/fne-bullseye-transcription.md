# Fear No Evil: Bullseye transcription candidate

Research for MARVEL-697, 2026-10-27. Baseline
`4bce901674f2beae4ac901d44183c07905a41911`; follows the
[pinned acquisition](fne-acquisition-candidate.md). This packet transcribes one
complete printed family for independent proofreading. It establishes neither
dataset admission nor executable support.

The external candidate is
`C:/Users/mggar/Source/evidence/fne-acquisition/bullseye-candidate.json`, SHA-256
`15493e07551120ddab97397600ae88746740c8422aa522ca88f03e00c0a6b30f`.
Its `groups[].cards[]` shape carries the supplemental full-card fields, plus
research metadata. Each face has a readable authority citation, printed name and
number, exact rules/boost text with markup and plain text, traits, numeric stats,
special stars, icons, stage, per-player flags, reverse face and physical quantity.
`field_sources` ties each field to the scan filename, SHA-256, printed card number,
footer position and visual region. Editorial ID/slug and formatting choices are
identified separately from printed facts.

## Printed quantity reconciliation

All **15 original scans** were viewed individually at original resolution,
including repeated copies. They establish **12 gameplay faces on 14 physical
cards**, covering every printed BULLSEYE footer position **1–14/14**.

| Candidate face | Printed title / stage | Printed number | Footer positions | Physical quantity |
|---|---|---|---|---:|
| 60065 | Bullseye I | 65 | 1/14 | 1 |
| 60066 | Bullseye II | 66 | 2/14 | 1 |
| 60067 | Bullseye III | 67 | 3/14 | 1 |
| 60068a / 60068b | Adamantium-Laced Spine / Damaged Spine | 68A / 68B | 4/14, both sides | 1 shared |
| 60069 | Raise the Stakes | 69 | 5/14 | 1 |
| 60070 | Deadly Sai | 70 | 6/14 | 1 |
| 60071 | Incessant Pursuit | 71 | 7/14, 8/14, 9/14 | 3 |
| 60072 | Lady Bullseye | 72 | 10/14 | 1 |
| 60073 | Deranged Bloodlust | 73 | 11/14 | 1 |
| 60074 | Everything is a Weapon | 74 | 12/14 | 1 |
| 60075 | Unerring Desire | 75 | 13/14, 14/14 | 2 |

The sum of face-row quantities is 15 because the Spine's two sides share one
physical card. Quantity comes from distinct printed positions, rather than
filename multiplicity. The set denominator and complete footer coverage supply
the family reconciliation independently of product-wide or marketing totals.

## Facts needing particular care in review

The first villain prints Roman **I**, with SCH 0, ATK 1 and HP 14 per player.
The acquisition note's sample-cell stage label is corrected from 0 to I in this
packet. The other stages print II / III, SCH 1 / 1, ATK 2 / 3 and HP 16 / 18 per
player. All three have special reminder stars beside both SCH and ATK.

Damaged Spine prints a star-only SCH modifier; Raise the Stakes prints a
star-only ATK modifier. These are recorded as symbolic annotations without an
invented numeric zero. Deadly Sai prints ATK +2 with a separate reminder star.
Lady Bullseye prints ATK 1 with a reminder star, SCH 2 and fixed HP 6.
Deranged Bloodlust prints fixed starting threat 4, one amplify icon, one numeric
boost icon and a separate boost star. Everything is a Weapon has one numeric
boost icon plus a boost star; Unerring Desire has three plus a boost star.

The product pictogram before 68A in Bullseye I's text is retained as
`[printed_product_icon]`; no letter is guessed for that graphic. The candidate's
normalization section documents all other glyph tokens and markup choices.
No unresolved illegible word or numeral was found during transcription.

## Validation and next acquisition step

The candidate generator and validation report are external beside the candidate:
`build-bullseye-candidate.py` and `bullseye-validation.txt`. The offline acquisition
verifier passed, confirming the original pinned source bytes remain unchanged.
Candidate validation confirms 12 unique face IDs, 15 source scans, the reciprocal
Spine pair, all 14 physical positions, per-face quantity/position consistency,
source hashes and field-source coverage. This is structural validation, not the
independent semantic proofread.

**Independent proofreading remains pending.** Compare every field with the
candidate's pinned scans before a separate acquisition change. Confirm especially
the clause stars, boost stars, star-only modifiers, fixed threat and stage I
correction. No setup or ability interpretation is part of this packet.

The current `Supplement.ReadEntry` accepts full-card inputs but leaves
`Card.StatAnnotations` at its default empty dictionary. Its constructor `[]`
argument is `LinkedTo`, not annotations. A later admission
change must preserve the captured special stars explicitly; copying these rows
alone would lose them. The candidate records this observed limitation without
changing the extractor. No generated card, supplement, vendored snapshot, setup,
ability or rules dataset was edited or regenerated. Original scan/PDF storage and
source-license findings remain those of the pinned acquisition packet.
