# Fear No Evil: independent Typhoid Mary proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository revision
`5c96ad7d27da6b84c30e2c4c112642666b55a5ee` and the
[transcription packet](fne-typhoid-mary-transcription.md).

**Verdict: accept the printed transcription candidate.** Independent comparison
with all 18 original scans found no correction to a captured printed field.
This accepts 14 gameplay faces on 15 physical cards; dataset admission and
executable support remain separate work.

## Exact reviewed inputs

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/typhoid-mary-candidate.json`,
  SHA-256 `56cf6551e9d4d9737a26446521ca8db0ea9705972bca6a53d1e5c84c76661b10`.
- Original manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
  Every Typhoid Mary original was opened individually at original resolution
  from the external `scans/` directory, including every repeated copy and
  reverse face. Original byte lengths, full hashes and acquisition identities
  match both manifest and candidate.
- Separate review findings:
  `C:/Users/mggar/Source/evidence/fne-acquisition/typhoid-mary-proofread-findings.json`.
  These record each original's filename, full hash, dimensions, printed number,
  footer and individual semantic verdict. External
  `review-typhoid-mary-independent.py` reproduces the independent structural
  checks offline without running the author's candidate builder or validator.

The original scans and candidate were not changed. Storage and licensing limits
remain those of the [acquisition packet](fne-acquisition-candidate.md).
The author's inspection flags and image filenames were not evidence for printed
text or quantity. The comparison read titles, subtitle absence, type, traits,
uniqueness symbols, all rules and boost text, emphasis, stat boxes, reminder
stars, numeric boost icons, player glyphs, stage letters, flavor, credits and
footers directly from every original.

## Face and quantity findings

Every row accepts its candidate fields. Titles use the documented title-case
normalization; `name_printed` retains small-capital spelling. Every subtitle is
empty. Every footer prints TYPHOID MARY with denominator 15. Numeric boost
counts below exclude the separate boost star.

| Face / original file(s) | Title; type; traits | Printed number / footer position(s) | Quantity and distinguishing verified facts |
|---|---|---|---|
| 60110a / `60110a.jpg` | Typhoid Mary; Villain; MUTANT, PSIONIC, UNDERLING | 110A / 1 | 1; unique; printed stage A; SCH 2, ATK 1; HP and reset 10 per player. Forced Interrupt replaces defeat with 1 damage token on Disturbed Psyche. |
| 60110b / `60110b.jpg` | Bloody Mary; Villain; MUTANT, PSIONIC, UNDERLING | 110B / 1 | 1; unique; printed stage A; SCH 1, ATK 2; HP and reset 10 per player. Same damage-token replacement, naming Bloody Mary. |
| 60111a / `60111a.jpg` | Typhoid Mary; Villain; MUTANT, PSIONIC, UNDERLING | 111A / 2 | 1; unique; printed stage B; SCH 3, ATK 1; HP and reset 13 per player; Toughness; same damage-token replacement. |
| 60111b / `60111b.jpg` | Bloody Mary; Villain; MUTANT, PSIONIC, UNDERLING | 111B / 2 | 1; unique; printed stage B; SCH 1, ATK 3; HP and reset 13 per player. When Revealed deals 1 damage to each identity; no Toughness keyword. |
| 60112 / `60112.jpg` | Disturbed Psyche; Environment; no traits | 112 / 3 | 1; Setup keyword plus bold Setup instruction: put the villain into play on a random side, with italic “(and reveal it)”. Forced Interrupt flips the villain when the villain phase ends. Entire bold victory sentence counts 3 or more tokens here without a token-type restriction. |
| 60113a / `60113a.jpg` | Establish Trust; Side Scheme; no traits | 113A / 4 | 1; Permanent and Setup; starting threat 8 per player plus an independent reminder star; starred sentence grants hinder 3 per player in expert mode. When Defeated places 1 threat token on Disturbed Psyche, then flips this card. No scheme icon. |
| 60113b / `60113b.jpg` | Mary Walker; Attachment; PERSONA | 113B / 4 | 1; unique; Permanent; attach to the villain; SCH and ATK boxes contain only stars. Starred sentence prevents attached villain from activating. Forced Response after villain damage or round end deals each player 1 facedown encounter card and flips this card. |
| 60114 / `60114.jpg` | Telekinesis; Attachment; PSIONIC, SUPERPOWER | 114 / 5 | 1; SCH +2; Hero Action spends two mental resources before the arrow and discards this card. 1 numeric boost icon plus star; Typhoid Mary boost takes 3 indirect damage and exhausts each character damaged this way. |
| 60115 / `60115.jpg`, `60115_2.jpg` | Psionic Slash; Treachery; no traits | 115 / 6, 7 respectively | 2; When Revealed checks for the Mary Walker attachment, flips it and reveals Establish Trust; the villain schemes. Star-only boost row; if Typhoid Mary is activating, deal 3 damage to an ally you control. |
| 60116 / `60116.jpg` | Psi Nova; Treachery; no traits | 116 / 8 | 1; reveal deals 1 damage to each character you control and stuns you. 2 numeric boost icons plus star; boost stuns you if Typhoid Mary is activating. |
| 60117 / `60117.jpg` | Assembled Armor; Attachment; ARMOR | 117 / 9 | 1; ATK +2; Forced Interrupt places any incoming villain damage here instead, then discards this attachment at 5 or more damage, or 8 or more in expert mode. 1 numeric boost icon plus star; boost confuses you if Bloody Mary is activating. |
| 60118 / `60118.jpg`, `60118_2.jpg` | Hail of Blades; Treachery; no traits | 118 / 10, 11 respectively | 2; same Mary Walker check/flip/reveal, then villain attacks you with italic “(even if you are in alter-ego form)”. Star-only boost row; take 2 damage if Bloody Mary is activating. |
| 60119 / `60119.jpg` | Scavenged Bulwark; Treachery; no traits | 119 / 12 | 1; Surge; reveal gives the villain a tough status card. 2 numeric boost icons plus star; boost gives Bloody Mary a tough status card if she is activating. |
| 60120 / `60120.jpg`, `60120_2.jpg`, `60120_3.jpg` | Hand Lookout; Minion; HAND, NINJA | 120 / 13, 14, 15 respectively | 3; SCH 0, ATK 2, fixed HP 4; Guard and Toughness. Each has 1 numeric boost icon plus star; give the activating enemy an additional boost card, without a named-villain condition. |

The independently read footer numerators cover exactly 1–15. Positions 1, 2
and 4 each have two opposite gameplay faces with matching printed number stems:
60110a/b, 60111a/b and 60113a/b. All six reciprocal `reverse_face` references
match those originals. The face quantities sum to 18; counting each of these
three pairs once gives 15 physical cards. Other `reverse_face: null` values
describe the absence of a supplied gameplay reverse, not generic card-back art.
Packaging counts are unnecessary to reconcile this family.

## Normalization and provenance findings

No illegible field or printed conflict remains. Removing layout wraps,
normalizing small-capital titles, apostrophe typography and flavor attribution
spacing are documented editorial choices. Bold ability labels and victory text,
italic reminders, boost separators, resource glyphs and the cost/effect arrow
preserve the printed distinctions. Flavor quotations, illustrator credits and
copyright lines match under that normalization. Every `text_plain` independently
reproduces the reviewed revision's `Printed.Plain` transformation of `text`.

Villain letters A/B are independent of bottom card suffixes A/B: both 110
faces print stage A and both 111 faces print stage B. The candidate preserves
`printed_stage` without an invented numeric `Stage` attribute. No villain face
has a stat reminder star or boost row. Uniqueness diamonds, clause stars, stat
stars, boost stars and per-player pictograms remain distinct facts.

Mary Walker's star-only boxes are retained as `stat_annotations.SCH+` and
`.ATK+`, each with `value: "★"` and `special_star: true`, without numeric
zeroes. Those JSON key spellings are editorial; the printed boxes label SCH
and ATK and contain no plus sign or numeral. Psionic Slash and Hail of Blades
correctly have zero numeric boost icons plus a star, distinct from an absent
boost row. Every face 114–120 has a boost separator and ability; earlier faces
have none. Per-player HP/reset amounts, starting threat and hinder are preserved;
Hand Lookout's HP is fixed.

All 371 field-source pointers resolve and name every applicable original copy
with its matching full hash, printed card number, footer position and visual
region. Editorial IDs, pack/set slugs, key spellings and glyph normalization
are separate from printed facts. The full-card supplemental reader already
reads `StatAnnotations` at the reviewed revision. Admission must still
deliberately handle quantities, source bindings, printed stage letters and other
research metadata outside that reader's fields.

## Verification and limits

The acquisition `verify.py` passed offline, checking primary acquisition inputs
and all 201 original scan lengths and hashes. The independent review script
passed exact candidate/manifest hashes, exact 18-scan family inventory, 14 unique
face IDs, original byte/hash/dimension checks, footer and quantity reconciliation,
reciprocal pairing, every source pointer and copy binding, boost metadata,
letter stages, star-only modifiers, per-player flags and plain-text normalization.
The separate findings JSON records the checks and every individual scan review.
Structural checks support provenance and consistency; the image comparison
establishes the semantic proofread verdict.

No dataset, supplement, generated output, setup, ability, extractor or runtime
source was edited or regenerated. Build/unit lanes are unnecessary for this
documentation-only proofread; normal commit hooks provide repository policy
checks. The next separate step is deliberate offline dataset admission.
