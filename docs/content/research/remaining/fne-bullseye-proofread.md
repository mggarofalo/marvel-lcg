# Fear No Evil: independent Bullseye proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository revision
`0e0057e0ed4b168a1de474c7e679fdee3a2d9465` and the
[transcription packet](fne-bullseye-transcription.md).

**Verdict: accept the printed transcription candidate.** Independently viewing
all 15 original scans found no correction to a captured printed field. This
verdict covers the 12 Bullseye faces and their quantities; it establishes neither
dataset admission nor executable support. The separate acquisition change needs
the annotation-preservation work described below.

## Exact reviewed inputs

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/bullseye-candidate.json`,
  SHA-256 `15493e07551120ddab97397600ae88746740c8422aa522ca88f03e00c0a6b30f`.
- Original scan manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
  Every scan below was opened individually at original resolution from its
  `scans/` directory. Its complete SHA-256, byte length and acquisition URL
  match the candidate and pinned manifest.
- Separate review findings: `C:/Users/mggar/Source/evidence/fne-acquisition/bullseye-proofread-findings.json`.
  The candidate and source originals were not changed. Source-storage and
  licensing limits remain those of the [acquisition packet](fne-acquisition-candidate.md).

The comparison read the title, absence of subtitle, type, trait line, uniqueness
symbol, every rules and boost sentence, clause stars, numeric icons, stat boxes,
HP/player symbol, stage, card number and set footer directly from each original.
The candidate author's inspection flags and scan filenames were not used to
establish text or quantity. Repeated copies were checked individually, including
their rules text and printed footer numerators.

## Face and quantity findings

All rows below accept the corresponding candidate fields. Titles are shown in
the candidate's documented title-case normalization; `name_printed` retains
capitals. Every face has an empty `subname`. A boost count excludes the separate
boost star. All footers print BULLSEYE with denominator 14.

| Face / original file(s) | Title; type; traits | Printed number / footer position(s) | Quantity and distinguishing verified facts |
|---|---|---|---|
| 60065 / `60065-1.jpg` | Bullseye; Villain; ASSASSIN, UNDERLING | 65 / 1 | 1; unique; stage I; SCH 0 and ATK 1, both with special stars; HP 14 per player. Set-aside clause names Adamantium-Laced Spine and the product pictogram followed by 68A. Activation interrupt adds one boost icon to each boost card and grants ranged to an attack. |
| 60066 / `60066-1.jpg` | Bullseye; Villain; ASSASSIN, UNDERLING | 66 / 2 | 1; unique; stage II; SCH 1 and ATK 2, both with special stars; HP 16 per player. Find-and-attach Spine clause; same activation interrupt. |
| 60067 / `60067-1.jpg` | Bullseye; Villain; ASSASSIN, UNDERLING | 67 / 3 | 1; unique; stage III; SCH 1 and ATK 3, both with special stars; HP 18 per player. Find-and-reveal Deranged Bloodlust clause; same activation interrupt. |
| 60068a / `60068a-1.jpg` | Adamantium-Laced Spine; Attachment; TECH | 68A / 4 | 1 shared physical card; unique; Permanent. Interrupt prevents all but 3 damage, places prevented damage here, then flips at 6 or more damage. No numeric stat or boost field. |
| 60068b / `60068b-1.jpg` | Damaged Spine; Attachment; TECH | 68B / 4 | Same physical card; unique; Permanent. SCH box contains only a star. Starred response removes 3 damage after Bullseye schemes, then flips if no damage remains. No boost field. |
| 60069 / `60069-1.jpg` | Raise the Stakes; Attachment; CONDITION | 69 / 5 | 1; ATK box contains only a star; 3 boost icons. Attach clause and starred interrupt match, including the additional boost card, removal of the defeated friendly character and this card, and the otherwise-shuffle clause. |
| 60070 / `60070-1.jpg` | Deadly Sai; Attachment; WEAPON | 70 / 6 | 1; ATK +2 with special star; 2 boost icons. Loses ranged and gains piercing. Hero Response follows hero defense and spends 2 different resources before the printed arrow and discard effect. |
| 60071 / `60071-1.jpg`, `60071_2-1.jpg`, `60071_3-1.jpg` | Incessant Pursuit; Attachment; CONDITION | 71 / 7, 8, 9 respectively | 3 distinct printed positions; 1 boost icon each. All copies match the ally-without-this-attachment/surge clause and interrupt replacing the villain's attack, granting overkill and removing a defeated attached ally. |
| 60072 / `60072-1.jpg` | Lady Bullseye; Minion; ASSASSIN, HAND | 72 / 10 | 1; unique; SCH 2; ATK 1 with special star; fixed HP 6; 3 boost icons. Villainous reminder and starred interrupt match, including resolving boost cards, the undefended condition and 1 additional boost card for this attack. |
| 60073 / `60073-1.jpg` | Deranged Bloodlust; Side Scheme; no traits | 73 / 11 | 1; fixed starting threat 4; 1 amplify icon; 1 boost icon plus boost star. Additional threat equals Bullseye's stage number; boost clause reveals this card after the activation. |
| 60074 / `60074-1.jpg` | Everything is a Weapon; Treachery; no traits | 74 / 12 | 1; 1 boost icon plus boost star. Alter-Ego clause deals 2 indirect damage; Hero choice and both alternatives match. Boost condition names Bullseye attacking, adds an additional boost card and grants piercing. |
| 60075 / `60075-1.jpg`, `60075_2-1.jpg` | Unerring Desire; Treachery; no traits | 75 / 13, 14 respectively | 2 distinct printed positions; 3 boost icons plus boost star each. Both copies match the facedown-boost-card/PERSONA-support choice and attack-only overkill boost clause. |

The shared number 68A/68B, common footer 4/14 and reciprocal flip text support
the recorded Spine reverse-face pair. Other `reverse_face: null` entries mean
that this family source supplies no gameplay reverse; they make no claim about
generic card-back art. The unique physical footer positions are exactly 1–14,
giving **14 physical cards, 12 faces and 15 face-row copies**. No marketing box
count is needed for this family reconciliation.

The first villain unambiguously prints Roman I. The transcription packet's
correction of the earlier acquisition sample's stage 0 label is accepted;
`printed_stage: "I"` and `attributes.Stage: "1"` are correct.

## Normalization and acquisition requirement

No transcription error was found. Removed layout wraps, title case, trait-array
spelling, HTML ability/reminder markup, `[[PERSONA]]`, the boost separator and
named glyph tokens are documented normalization choices. The unidentified
product pictogram is visibly present before 68A and remains symbolic as
`[printed_product_icon]`; it is not an illegible letter. Each `text_plain` value
also matches the current `Printed.Plain` transformations independently checked
from `text`.

The special stat stars are distinct from HP per-player symbols, uniqueness
diamonds, clause stars and numeric boost icons. In particular, 60068b's SCH and
60069's ATK are star-only boxes: their annotations correctly contain
`value: "★"`, with no invented numeric zero. The numeric +2 on 60070 is retained
independently of its special star. Boost stars on 60073–60075 are separate
annotations and do not increase their numeric boost counts.

Code inspection confirms the acquisition limitation, with a precision correction
to the source packet's phrase “empty stat-annotation list”:

- [`Supplement.ReadEntry`](../../../../tools/Marvel.Cards.Extract/Supplement.cs)
  constructs a full `Card` without assigning `StatAnnotations`. The `[]`
  constructor argument supplies `LinkedTo`, not stat annotations.
- [`Card.StatAnnotations`](../../../../tools/Marvel.Cards.Extract/Card.cs)
  consequently retains its default empty dictionary. The full-card path does
  not read the candidate's `stat_annotations` object.
- [`StatAnnotations.Read`](../../../../tools/Marvel.Cards.Extract/StatAnnotations.cs)
  already reads the required `value`, `special_star` and `per_player` fields;
  [`Cards.Write`](../../../../tools/Marvel.Cards.Extract/Cards.cs) already writes
  the property. A separate acquisition implementation must populate
  `Card.StatAnnotations` from that reader on the supplemental full-card path
  and add focused offline extraction evidence for numeric-with-star,
  star-only and boost-star cases. Numeric attributes must remain independent.

Directly copying these rows into the current supplement would therefore lose
the captured annotations. Research metadata, including quantity, source links
and reverse-face fields, is also outside the current full-card reader's fields;
the reviewed inventory and provenance must remain explicitly available during
admission. `text_plain` is regenerated from `text`, rather than imported.
The wording correction was returned to the lead for the separately owned
transcription document; this review does not silently alter that source.

## Verification and limits

The existing external `verify.py` passed offline: all pinned acquisition files
and 201 original scans retained their recorded lengths and SHA-256 hashes.
Independent candidate checks passed for the exact candidate hash, JSON shape,
12 unique IDs, all 15 original hashes and manifest identities, field-source
pointer resolution and hash/number/footer consistency, every expected quantity
and footer position, the reciprocal Spine pair and plain-text normalization.
The separate findings JSON records those results and the precise importer
requirement; it does not replace the semantic comparison above.

No unresolved illegible field or printed-text conflict remains in this family.
No dataset, supplement, generated output, runtime, setup, ability or extractor
source was edited. Unit/build lanes are unnecessary for this documentation-only
proofread; normal commit hooks provide the repository's documentation checks.
