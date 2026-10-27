# Fear No Evil: independent Hammerhead proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository revision
`c141fb0052c6590bbdf9ac235086d1360eb176a9` and the
[transcription packet](fne-hammerhead-transcription.md).

**Verdict: accept the printed transcription candidate.** Independent comparison
with all 14 original scans found no correction to a captured printed field.
This accepts 11 gameplay faces and their 14 physical quantities; dataset
admission and executable support remain separate work.

## Exact reviewed inputs

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/hammerhead-candidate.json`,
  SHA-256 `524321a4697b49059a7b0c6056a6ae5b86d8a6358333ff4bf034e439e2b0bd8c`.
- Original manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
  Every Hammerhead scan was opened individually at original resolution from
  the external `scans/` directory, including every repeated copy. Original
  byte lengths, full SHA-256 hashes and acquisition identities match the
  manifest and candidate.
- Separate review findings:
  `C:/Users/mggar/Source/evidence/fne-acquisition/hammerhead-proofread-findings.json`.
  This records each original's filename, hash, dimensions, number and footer,
  the semantic verdict, and independent structural checks.

The original scans and candidate were not changed. Storage and licensing limits
remain those of the [acquisition packet](fne-acquisition-candidate.md).
The author's inspection flags and filenames were not evidence for printed
text or quantity. The comparison read title, subtitle absence, type, traits,
uniqueness symbol, all rules and boost text, emphasis, stat boxes, reminder
stars, numeric boosts, player glyphs, stage, flavor, credits and footer directly
from each original.

## Face and quantity findings

Every row accepts its candidate fields. Titles below use the documented
title-case normalization; `name_printed` retains small-capital spelling.
Every subtitle is empty. All footers print HAMMERHEAD with denominator 14.
Numeric boost counts exclude the separate boost star.

| Face / original file(s) | Title; type; traits | Printed number / footer position(s) | Quantity and distinguishing verified facts |
|---|---|---|---|
| 60086 / `60086.jpg` | Hammerhead; Villain; CRIMINAL, MAGGIA, UNDERLING | 86 / 1 | 1; unique; stage I; SCH 2, ATK 1 with reminder star; HP 14 per player. Starred response requires an attack that damages a character; stun that character, or deal 1 damage if already stunned. |
| 60087 / `60087.jpg` | Hammerhead; Villain; CRIMINAL, MAGGIA, UNDERLING | 87 / 2 | 1; unique; stage II; SCH 2, ATK 2 with reminder star; HP 15 per player. Toughness and Retaliate 1; same response and 1 damage. |
| 60088 / `60088.jpg` | Hammerhead; Villain; CRIMINAL, MAGGIA, UNDERLING | 88 / 3 | 1; unique; stage III; SCH 3, ATK 3 with reminder star; HP 16 per player. Toughness and Retaliate 2; same response with 2 damage. |
| 60089 / `60089.jpg` | Energy Gatling Gun; Attachment; TECH | 89 / 4 | 1; ATK +1; 2 boost icons. Attach to Hammerhead. Forced Response checks that the attacked character was not defeated; discard this card before the printed arrow, then Hammerhead attacks that character again. |
| 60090 / `60090.jpg` | Hardheaded; Attachment; CONDITION | 90 / 5 | 1; SCH +1; 1 boost icon and a separate boost star. Attach clause and granted stalwart match. Hero Action resource pictograms are energy, mental, physical in that order, followed by the arrow and discard. Separated boost ability gives the activating enemy a tough status card. |
| 60091 / `60091.jpg` | Chameleon; Minion; CRIMINAL, MAGGIA | 91 / 6 | 1; unique; SCH 1 with reminder star, ATK 1, fixed HP 5; 3 boost icons. +X SCH uses the highest THW among characters in play; separate When Revealed makes Chameleon scheme. |
| 60092 / `60092.jpg`, `60092_2.jpg` | Maggia Enforcers; Minion; CRIMINAL, MAGGIA | 92 / 7, 8 respectively | 2; star-only SCH, ATK 2, fixed HP 3; Guard; no boost row. Both originals print “Maggia Enforcers’s”; SCH equals the number of emphasized MAGGIA enemies in play, with italic “(including this one)”. |
| 60093 / `60093.jpg`, `60093_2.jpg` | Maggia Underboss; Minion; CRIMINAL, MAGGIA | 93 / 9, 10 respectively | 2; SCH 1 and ATK 1 each with a reminder star; fixed HP 4; Villainous; 1 boost icon each. Starred Forced Interrupt checks whether you are stunned when it activates against you; additional boost card is for this activation. |
| 60094 / `60094.jpg` | Consolidate Power; Side Scheme; no traits | 94 / 11 | 1; fixed starting threat 6; 1 boost icon; no scheme icon. Forced Response gives a defeated minion to the villain as a facedown boost card. |
| 60095 / `60095.jpg` | Power Struggle; Side Scheme; no traits | 95 / 12 | 1; fixed starting threat 5; 1 hazard icon and 1 boost icon. Reveal defeats the minion with the fewest remaining hit points in play, then discards until a minion is discarded and reveals it. |
| 60096 / `60096.jpg`, `60096_2.jpg` | “Head First!”; Treachery; no traits | 96 / 13, 14 respectively | 2; 2 boost icons each. Alter-Ego reveal grants surge. Hero reveal causes Hammerhead to attack you; if you are stunned, +2 ATK and overkill apply to this attack. Title quotation marks and exclamation point match both originals. |

The independently read footer numerators are exactly 1–14, each assigned to one
physical card. The face quantities sum to 14. No gameplay reverse is supplied
by this family source; `reverse_face: null` does not describe generic card-back
art. Packaging counts are unnecessary to reconcile this family.

## Normalization and provenance findings

No illegible field or printed conflict remains. Removing layout wraps and
normalizing title capitalization and flavor attribution spacing are documented
editorial choices. Bold ability labels, emphasized MAGGIA, italic reminder text,
the boost separator, cost/effect arrow and named pictogram tokens preserve the
printed distinctions. Every `text_plain` independently reproduces the current
`Printed.Plain` transformation from `text`.

The Enforcers' SCH annotation correctly has `value: "★"` and
`special_star: true`, with no invented numeric zero. Hardheaded's boost star
does not increase its one numeric boost icon. Villain HP is per player; minion
HP and both side schemes' starting threat are fixed. Uniqueness diamonds,
stat reminder stars, clause stars, boost stars and player symbols remain
distinct facts. Hardheaded's stalwart is granted to Hammerhead in a sentence,
rather than printed as an opening keyword of the attachment.

Every field-source pointer resolves and names every applicable original copy
with matching complete hash, printed card number, footer position and visual
region. Editorial IDs, JSON keys, pack/set slugs and symbolic normalization are
separate from those printed facts. All flavor quotations, printed illustrator
credits and copyright lines match under the documented normalization.

The reviewed revision's supplemental full-card reader already assigns
`StatAnnotations = StatAnnotations.Read(element)`. The annotation import gap
described in the earlier Bullseye review is therefore not present at this
revision. Admission must still deliberately preserve the reviewed quantities,
source bindings and other research metadata outside the current full-card
reader's fields; accepting this candidate does not itself admit those records.

## Verification and limits

The acquisition `verify.py` passed offline, checking primary acquisition inputs
and all 201 original scan lengths and SHA-256 hashes. Independent candidate
checks passed for the exact reviewed hash, JSON structure, exact 14-scan family,
11 unique expected IDs, footer coverage, quantities, original-copy provenance,
all field-source pointers, boost metadata, star-only SCH, fixed threat, HP
per-player flags and plain-text normalization. The separate findings JSON
records these checks and each individual scan review.

No dataset, supplement, generated output, setup, ability, extractor or runtime
source was edited or regenerated. Build/unit lanes are unnecessary for this
documentation-only proofread; normal commit hooks provide repository policy
checks. The next separate step is deliberate offline dataset admission.
