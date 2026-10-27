# Fear No Evil: Cops independent proofread

MARVEL-697, 2026-10-27. Review of `cops-candidate.json` SHA-256 `720a1470aa537603f1830a4bebc7d30b34ff3db478b9077b795fde359157adef` at baseline `a2439588384a10c503cebe8ec1f5b0578dce0e8c`. **Verdict: correction required.** This is a review of the pinned candidate, not an admission of cards or runtime support.

I individually viewed all five pinned originals at their original 710 × 1030 or 1030 × 710 resolution, comparing every printed title, type, traits, rules, stats, glyphs, stars, boost area, flavor, credit, copyright, product icon, card number and footer with the candidate. Both Cop scans were compared independently. No discrepancy was found in those fields other than the unresolved identification below. Field-source entries were checked against the matching file, hash, printed number and footer position; their descriptive regions were reviewed against the card faces. Normalized IDs, slugs and text markup are editorial representations. `reverse_face: null` means no supplied opposite gameplay face, not proof about a generic card back.

| Face | Original scan and COPS position | Quantity | Key observations |
|---|---|---:|---|
| 60182 Cop | `60182.jpg` 1/5; `60182_2.jpg` 2/5 | 2 | Standalone SCH star, ATK 2, fixed HP 3; no boost symbols on either; same text, flavor and Facundo Moyano credit. |
| 60183 Crooked Cop | `60183.jpg` 3/5 | 1 | SCH 1, ATK 2, fixed HP 4; boost star without numeric boost icons, with matching boost ability; Facundo Moyano credit. |
| 60184 Wanted | `60184.jpg` 4/5 | 1 | Two boost icons; central **hazard icon**; no flavor or illustrator credit. |
| 60185 Police Cordon | `60185.jpg` 5/5 | 1 | Fixed starting threat 3 with star; Hinder 1 per player is separate; three boost icons; flavor attributed to Police officer; no illustrator credit. |

The five footer numerators establish four faces and five physical positions, 1–5/5, in the provided scan set. This does not independently settle the full product's card-count reconciliation.

## Required correction: Wanted hazard icon

The central symbol is a hazard icon. The [official FFG Rules Reference v1.7](https://images-cdn.fantasyflightgames.com/filer_public/d2/a4/d2a442c9-0f16-4f7c-b02a-6afbac4afc24/mc_rulesreference_v17-compressed.pdf), printed page 22, ICONS, visually shows the same white cross on a black angled card with a lower stripe next to the label “Hazard Icon.” I visually compared that original page render with `60184.jpg`, not just the extracted text. The local downloaded PDF SHA-256 is `15878d907443bb0f06e9659c2dae6411fdd655d8ff08d83e2756abbcd62d41d8`; page render `rr-v17-page-22.png` is retained in the external evidence root. The vendored v1.8 [icon index](../../../../datasets/rules-reference/entries/icons.md) (`rr:icons.8`) and [hazard entry](../../../../datasets/rules-reference/entries/hazard-icon.md) (`rr:hazard-icon`) identify its effect. The icon's printed presence on an obligation is an observation; no additional application or timing conclusion is inferred here.

Update the source transcription for `/center_icon` to identify the hazard icon, its `/field_sources/center_icon` description, and the top-level uncertainty that says identification is unresolved; update the candidate narrative accordingly. Regenerate and revalidate the candidate; do not edit generated data by hand. Re-review the new hash before treating the correction as closed. The candidate's existing empty per-card uncertainty array also should not be mistaken for resolution of the top-level uncertainty.

## Evidence and limits

External evidence root: `C:/Users/mggar/Source/evidence/fne-acquisition/`. `independent-cops-check.py` verifies the reviewed candidate hash, manifest hash, each original's bytes, footer inventory, and each recorded field-source file/hash/number/position; it passed. The source author's `validate-cops-candidate.py` and `verify.py` also passed offline on the reviewed hash. These checks do not prove visual transcription; the observations above are the independent visual proofread. `independent-cops-findings.json` records the verdict. No build, hook, commit, dataset or runtime change was made.

## Closure of hazard finding

The regenerated candidate SHA-256 `c6737f2a331c2655eca7012fec4dc9fd29245adf10ed390e1de2dd2ecf95f915` closes the finding. A recursive structural comparison with the preserved `cops-candidate-before-hazard.json` at the original reviewed hash found only the intended changes: `center_icon` is `hazard`; `attributes.Hazard` is `1`; both source references identify the one glyph on `60184.jpg` at COPS 4/5; and the resolved icon uncertainty was removed. All other candidate facts and bindings remain unchanged. The source builder contains those same facts, and the source transcription narrative reflects the correction. Source validation and the complete acquisition verification both passed offline against the repaired candidate. The validator and candidate still label independent proofreading pending; this closure supplies that review evidence without claiming dataset admission or runtime support. No build, hook or commit was run.
