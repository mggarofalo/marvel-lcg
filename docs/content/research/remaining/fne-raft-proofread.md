# Fear No Evil: independent Raft proofread

Independent review, 2026-10-28. **All captured printed values agree with the
originals; the candidate requires one provenance-region correction before full
packet acceptance.** The reviewed inventory is ten gameplay faces on nine
physical cards, with the main scheme's A/B faces sharing position 1. This is a
research transcription review; dataset admission and runtime acceptance remain
separate work.

## Exact inputs and method

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/raft-candidate.json`,
  SHA-256 `8c1594f9839464489a1bb021c15c9ac89543c281f7e63af36d6ac9b3cd8e0650`.
- Manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
- Retained visual glyph key: `rr-v17-page-22.png`, SHA-256
  `65efd0d7160acfdbf2c2a07382b081f2fc81d6ebb5e9d35af8927213a5e2d157`.
  This image is version 1.7; the current semantic entries inspected are Rules
  Reference 1.8 `rr:icons.1–4`, `.9–10`, `.12–14` and
  `rr:star-icon.1`, `.2`, `.6`.

Each of the ten originals below was individually opened at original resolution.
The glyph key was also viewed at original resolution. The comparison covered
every captured field: full rules and separated boost text, printed emphasis,
flavor and punctuation, titles, subtitle absence, traits, uniqueness, stats,
resource order, stat and boost stars, per-player symbols, cost arrow, absent
fields, footer numbering, credits, copyright, product pictogram, pairing and
quantity. Line-wrap removal, title case, apostrophes, glyph tokens and emphasis
follow the candidate's declared normalization. Neither the candidate nor its
builder supplied printed authority; the builder was not read or executed.

An independent offline check verified the candidate, manifest and glyph-key
hashes; each scan's byte length and SHA-256; equality of the embedded scan list
with the manifest's Raft section; and all 281 field/source key, filename and
hash bindings. All 281 region descriptions were read against the originals;
280 are accepted and one needs correction. No scan or candidate was changed.

## Printed face and quantity findings

Each footer reads THE RAFT BREAKOUT with denominator 9. All cards have no printed
cost or player resource field. Each supplied gameplay face has quantity 1;
the two scheme faces represent one physical card and are not summed twice.

| Original | Position / number | Fields | Distinguishing readings confirmed |
|---|---|---:|---|
| `60142a.jpg` | 1 / 142A | 25 | Stage 1A, paired with 1B; Contents and Setup bold; UNDERLING and PRISONER bold italic; parenthetical notes italic; no flavor or boost field. Joey Vazquez credit. |
| `60142b.jpg` | 1 / 142B | 30 | Stage 1B, paired with 1A; 11 threshold, +1 acceleration and 2 starting threat each have a per-player glyph. Quoted italic flavor with Proxima Midnight attribution; bold When Defeated label, ELITE trait and loss clause. No large scheme icon or boost field. Joey Vazquez credit. |
| `60143.jpg` | 2 / 143 | 28 | Unique Master Key attachment; ITEM trait; star-only SCH modifier and corresponding Forced Response star; Permanent. Both italic flavor lines begin with `>` and have no quotation marks: `>Security overridden.` and `>Door lock disengaged.` No boost field. Ivan Fiorelli credit. |
| `60144.jpg` | 3 / 144 | 29 | Unique Absorbing Man; BRUTE, ELITE, PRISONER; SCH 1, ATK 1 with reminder star, fixed HP 6. Villainous; Forced Interrupt. Resource order is energy, mental, physical, wild; the intervening word *and* is italic. Three boost icons, no boost star. Eleonora Carlini credit. |
| `60145.jpg` | 4 / 145 | 29 | Unique Baron Zemo; ELITE, HYDRA, PRISONER; SCH 2, ATK 1 without reminder star, fixed HP 6. Villainous; DECEIVED bold italic and When Revealed bold. Three boost icons, no boost star. Afif Khaled credit. |
| `60146.jpg` | 5 / 146 | 30 | Unique Drang; BADOON, ELITE, PRISONER; SCH 1, ATK 1 with reminder star, fixed HP 6. Villainous and starred Forced Response. Quoted italic flavor. Three boost icons, no boost star. Joey Vazquez credit. |
| `60147.jpg` | 6 / 147 | 29 | Unique Mysterio; CRIMINAL, ELITE, PRISONER; SCH 2, ATK 1 with reminder star, fixed HP 6. Villainous; starred Forced Response for treachery boost cards. Three boost icons, no boost star. Joey Vazquez credit. |
| `60148.jpg` | 7 / 148 | 29 | Unique Proxima Midnight; BLACK ORDER, ELITE, PRISONER; SCH 1, ATK 2 with reminder star, fixed HP 7. Villainous; starred Forced Interrupt and printed comma before “or.” Three boost icons, no boost star. Joey Vazquez credit. |
| `60149.jpg` | 8 / 149 | 29 | Unique Rhino; BRUTE, ELITE, PRISONER; SCH 1, ATK 3 with reminder star, fixed HP 7. Toughness and Villainous; starred overkill text and separated starred Boost ability. Three numeric boost icons plus an independent boost star. Kaihound credit. |
| `60150.jpg` | 9 / 150 | 23 | Imprisoned obligation; no uniqueness or traits. Prohibition and bold Hero Action label; cost arrow follows “3 or more.” Separated starred Boost ability. Zero numeric boost icons and one boost star. Facundo Moyano credit. |

The minion traits are bold italic in their recorded order. No minion stat has a
per-player or consequential-damage symbol. Every face carries the recorded
copyright line and product pictogram. Positions 1–9 are covered exactly once
after pairing 142A/142B. No other supplied opposite gameplay faces or flavor
variants occur; generic backs and packaging are outside this review.

## Required correction

**RAFT-PROOF-001 — provenance region, `60142a`.** JSON pointer
`/cards/0/field_sources/flavor/region` currently reads
`left rules panel; italic flavor above rules`. The 1A original has no flavor;
the italic flavor above rules belongs to 1B. Change the description to
`left rules panel; no flavor`. The captured value `fields.flavor` is correctly
empty, so this finding does not require a printed-value change.

The lead should preserve the exact reviewed candidate before correcting the
pointer, then record the resulting hash and closure. This verdict applies to
the original hash above and does not approve a later candidate automatically.
Machine-readable evidence is external at
`C:/Users/mggar/Source/evidence/fne-acquisition/raft-proofread-findings.json`.

Only that findings file and this assigned document were authored. No dataset,
supplement, generated output, setup, ability, runtime source, build, hook,
commit or Plane record was changed.

## Independent correction closure

**Accepted after correction**, 2026-10-28. The original review and finding above
remain intact. The reviewed original is preserved externally as
`raft-candidate-before-proofread.json`, SHA-256
`8c1594f9839464489a1bb021c15c9ac89543c281f7e63af36d6ac9b3cd8e0650`.
The corrected `raft-candidate.json` has SHA-256
`b5442676adcf7ac32d0eec33af22dcc3b281f70235aa3baffed703da4bb7f4a2`.

An independent recursive comparison of the entire parsed JSON found exactly one
scalar change: `/cards/0/field_sources/flavor/region` now reads
`left rules panel; no flavor`. This closes RAFT-PROOF-001 against the original
1A image observation. Every printed value and all other JSON data are unchanged.
The manifest and retained glyph-key hashes, plus all ten original scan hashes
and byte lengths, were independently rechecked and remain unchanged. No image
reread was needed for this metadata-only repair.

The corrected packet therefore accepts ten gameplay faces, nine physical cards
and all 281 provenance pointers, with no remaining proofread finding. Independent
closure evidence is external at
`C:/Users/mggar/Source/evidence/fne-acquisition/raft-proofread-closure.json`.
This acceptance covers research transcription and provenance; it establishes
neither dataset admission nor runtime support. The lead's builder and validator
runs were not rerun by this reviewer.
