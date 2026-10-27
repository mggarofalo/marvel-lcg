# Fear No Evil: Cops transcription candidate

Research for MARVEL-697, 2026-10-27, repository baseline `a2439588384a10c503cebe8ec1f5b0578dce0e8c`. Follows the [pinned acquisition](fne-acquisition-candidate.md). This is a transcription candidate for independent proofreading, not dataset admission or executable support.

External evidence root: `C:/Users/mggar/Source/evidence/fne-acquisition/`. `cops-candidate.json` SHA-256 is `c6737f2a331c2655eca7012fec4dc9fd29245adf10ed390e1de2dd2ecf95f915`; `scan-manifest.json` SHA-256 is `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`. Original scans remain external; acquisition and license findings remain those of the acquisition packet. No scans are redistributed here.

The structured candidate records printed title, type, traits, rules and plain text, stats, stars, per-player symbols, boost icons and ability, physical quantities, flavor, credits, footer and reverse-face metadata. Each field has source bindings to the original filename, full SHA-256, printed card number, footer position and visual region. Card IDs, keys, slugs and normalization are editorial allocations. No supplied gameplay reverse face occurs; `reverse_face: null` does not describe generic card backs.

## Printed inventory

All **five originals were individually viewed at original resolution**, establishing **four faces on five physical cards** and every COPS footer position **1–5/5**. Quantity comes from printed positions, not filenames.

| Candidate face | Printed title | Number | Footer | Quantity |
|---|---|---:|---|---:|
| 60182 | Cop | 182 | 1/5, 2/5 | 2 |
| 60183 | Crooked Cop | 183 | 3/5 | 1 |
| 60184 | Wanted | 184 | 4/5 | 1 |
| 60185 | Police Cordon | 185 | 5/5 | 1 |

Cop has POLICE, Quickstrike and Vulnerable, ATK 2 and fixed HP 3. Its SCH box contains a **standalone star without a numeral**, tied to its forced interrupt: when it would scheme, it attacks the villain instead. Its defeat places 2 threat on the main scheme. Both copies have the same text and illustrator credit, Facundo Moyano; each has its own printed footer. No boost symbols appear on either scan.

Crooked Cop has CRIMINAL and POLICE, Quickstrike and Vulnerable, SCH 1, ATK 2 and fixed HP 4. Its defeat makes the defeating player deal themself a facedown encounter card. Its boost row contains a star and **zero numeric icons**, with a boost ability to take 2 indirect damage. The illustrator is Facundo Moyano.

Wanted is an obligation with two numeric boost icons. Its hero action discards encounter cards until a POLICE minion is discarded, puts it into play engaged with the player, then discards Wanted. One hazard icon appears centered beneath the ability. Independent visual comparison with the official FFG Rules Reference v1.7 page 22 ICONS identifies the glyph; current RR 1.8 names it in `rr:icons.8`. The candidate records `center_icon: hazard` and `Hazard: 1`, each bound to that original region. No illustrator credit is printed.

Police Cordon has **fixed starting threat 3 and a special star**, with Hinder 1 per player. Each POLICE minion gains guard and patrol; its when-revealed ability searches the encounter deck and discard pile for a POLICE minion and reveals it. It has three numeric boost icons and no illustrator credit. Its flavor is attributed to a police officer.

Normalization removes layout-only wraps, retains emphasis in HTML or `[[trait]]` markup, and separates flavor from rules text. `[star]` and `[per_hero]` identify distinct printed glyphs. The candidate records all visible printed icons and their visual locations, including Wanted's hazard icon. No illegible word or numeral remains; independent closure accepts the repaired candidate; see the linked review record.

## Validation and next step

`build-cops-candidate.py` reconstructs the candidate from manual transcription and the pinned manifest. `validate-cops-candidate.py` checks the candidate and original source bindings offline; `cops-validation.txt` records the run and `cops-transcription-findings.json` records its findings. The acquisition `verify.py` also passed offline for all 201 original hashes, 171 candidate faces and 146 numbered components. Family checks passed for the five exact originals and their bytes/hashes, four faces, complete footer coverage, copy counts and reverse relationships, field-source pointers and every copy's bindings, plain-text normalization, standalone SCH star, star-only boost, fixed threat versus per-player hinder and the source-bound hazard icon. These structural checks do not establish transcription correctness.

**Independent proofreading identified one correction:** Wanted's symbol is a hazard icon. The source builder now records that fact and the candidate was regenerated and structurally revalidated. The original candidate is preserved externally as `cops-candidate-before-hazard.json`; the [independent review](fne-cops-proofread.md) records its original hash and authority. Independent closure accepts the repaired hash after an exact comparison limited to the intended hazard correction. The candidate retains its pre-review status text as historical provenance; the independent review is the acceptance record. Deliberate offline dataset admission follows separately. No dataset, supplement, generated output, setup, ability, runtime or existing family file was changed. No build, hook or commit was run.
