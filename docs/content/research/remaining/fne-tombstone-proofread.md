# Fear No Evil: independent Tombstone proofread

Research for MARVEL-697, 2026-10-27. Reviewed repository baseline
`a2439588384a10c503cebe8ec1f5b0578dce0e8c` and the
[transcription packet](fne-tombstone-transcription.md).

**Verdict: accept the printed transcription candidate.** Independent comparison
of all five original scans found no correction to a captured printed field. This
accepts five gameplay faces on five physical cards; dataset admission and
executable support remain separate work.

## Reviewed inputs and method

- Candidate: `C:/Users/mggar/Source/evidence/fne-acquisition/tombstone-candidate.json`,
  SHA-256 `c2be6106963aa855da276aef633394bab616729529bf00829a5c4ee81fdb1817`.
- Manifest: `C:/Users/mggar/Source/evidence/fne-acquisition/scan-manifest.json`,
  SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
- Each original below was individually opened at original resolution. The images
  establish the printed numbers, footer positions, quantities and semantic comparison.

The comparison covered titles, subtitle absence, type, traits, uniqueness,
complete rules and boost text, emphasis, stats, stars, player symbols, flavor,
credits, copyright and footers. Layout wraps, title case, apostrophe style and
attribution spacing follow the candidate's documented normalization. No scan
or candidate was changed. Storage and licensing limits remain those of the
[acquisition packet](fne-acquisition-candidate.md).

## Face and quantity findings

Every row accepts the corresponding candidate fields. Each footer reads TOMBSTONE
and has denominator 5. Boost counts exclude the independent special star.

| Original | Printed face and number | Footer | Distinguishing facts checked |
|---|---|---|---|
| `60195.jpg` | Cold as Ice, 195 | 1 | Attachment; +2 SCH with star; attaches to highest-base-HP minion and gives tough, otherwise gains surge; after it schemes, confuse first player's identity and discard this card; 2 boost icons; Tombstone flavor. |
| `60196.jpg` | Hard as Marble, 196 | 2 | Attachment; +2 ATK with star; same attachment, tough and conditional surge text; after it attacks, stun attacked character and discard this card; 2 boost icons. |
| `60197.jpg` | Beetle, 197 | 3 | Unique AERIAL, CRIMINAL minion; SCH 1, ATK 1, fixed 5 HP; Villainous; searches encounter deck and discard pile for an attachment that can attach to a minion and attaches it to Beetle; 2 boost icons. |
| `60198.jpg` | Tombstone, 198 | 4 | Unique CRIMINAL minion; SCH 2, ATK 2, fixed 8 HP; Quickstrike; star-only boost row and separated Boost ability giving the villain tough; flavor; Sebastián Guidobono credit. |
| `60199.jpg` | Hit List, 199 | 5 | Side scheme; 3 starting threat per player; after villain phase begins, 3 indirect damage to first player, then remove 1 threat per player for each ally defeated this way; 3 boost icons; Facundo Moyano credit. |

Neither attachment nor Beetle prints an illustrator credit. Hit List has no large
scheme icon. Its starting-threat and removed-threat player pictograms are two
separately captured facts. The attachment stars accompany both their stat
modifiers and their forced response text; surge is conditional. Tombstone's
boost row has zero numeric icons and one star. The independently read footer
numerators cover 1–5; no supplied opposite gameplay faces occur. Null reverse
metadata does not assert anything about generic card backs.

## Provenance and limits

An independent offline check verified the exact candidate and manifest hashes,
all five family scan byte lengths and SHA-256 hashes against both manifest and
candidate, five unique face IDs, per-face quantity and footer coverage, and all
136 field-source bindings: each pointer resolves and names the applicable scan
with matching hash, printed number, footer and nonempty region. The candidate's
plain text matched markup removal and glyph-token normalization. Findings and
validation are recorded externally as `tombstone-proofread-findings.json` and
`tombstone-proofread-validation.txt`. These consistency checks support provenance;
direct image comparison establishes the semantic verdict.

No dataset, supplement, generated output, setup, ability or runtime source was
edited or regenerated. Deliberate offline dataset admission is a separate step;
this verdict does not establish runtime support.
