# Fear No Evil: independent Disasters proofread

MARVEL-697; 2026-10-27. Repository baseline `1677ebcdf26bc44b1f092b91af4b973689c1f92f`. This is an independent printed-source review of the candidate, not gameplay, admission, or product-wide acceptance.

**Verdict: corrections required to source-location descriptions.** The transcribed printed text, numerical values, symbols, credits and family footer accounting match the supplied scans under the documented normalization; three source-binding descriptions do not accurately locate what they claim. This verdict applies only to candidate SHA-256 `aeafb36f8b829607fb49754228d3bcf9267ccc51bca07d0bb54dbec37b88fac9`; it does not automatically apply to a regenerated candidate.

## Method and observations

Independently opened each original `60177.jpg` through `60181.jpg` at original resolution from `C:/Users/mggar/Source/evidence/fne-acquisition/scans/`. Compared all candidate card fields, including title, type, trait, rules text, emphasis, arrow and resource symbols, stats and stars, per-player glyph, boost symbols and text, large pictograms, flavor, illustrator, copyright, card number, footer and quantity. Also inspected each field-source region and checked all field-source filenames, hashes, numbers and footer positions against their own face and scan; all five image bytes hash to the candidate pins. Candidate and `scan-manifest.json` hashes were independently checked; the latter remains `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.

| Face | Distinguishing observations |
|---|---|
| 60177 Mystic Incursion | Environment, DISASTER; mental/mental, MYSTIC, two boost icons; no illustrator credit; 177, 1/5. |
| 60178 Sinking Ferry | Environment, DISASTER; energy/energy, AERIAL, two boost icons; Sebastián Guidobono; 178, 2/5. |
| 60179 Towering Inferno | Environment, DISASTER; physical/physical, tough status card, two boost icons; Sebastián Guidobono; 179, 3/5. |
| 60180 Collapsing Bridge | Side scheme; starting threat 1 with adjacent star; separate rules-box star and Hinder 2 per-player symbol; two boost icons on lower left edge; Sebastián Guidobono; 180, 4/5. No per-player symbol in the starting-threat box. |
| 60181 Bystanders in Peril | Treachery; Peril printed in rules box; when-revealed and boost text; one boost icon and separate boost star; no illustrator credit; 181, 5/5. |

Each face prints © MARVEL © 2026 FFG. No flavor text is printed. The five distinct footer positions support one copy of each supplied face and five family positions; a null reverse records no supplied gameplay reverse, not evidence about generic card backs. Product-wide reconciliation is outside this review.

## Source-binding corrections

1. `60181 /attributes/Peril`: `left Peril stat box` is incorrect. The word `Peril.` appears as the first line of the rules box; there is no Peril stat box on this face.
2. `60180 /boost_icons`, `/boost_star`, and `/attributes/Boost`: their references to a bottom boost row or numeric icons at bottom should identify the lower **left edge** boost row. Two icons and no boost star are correctly recorded.
3. `60181 /printed_pictograms`: the description currently says `large pictogram in lower rules box`; the value is empty and no such large pictogram is printed there. State the inspected region and absence explicitly.

These are location-description defects, not discovered errors in the associated values. Generic `/reverse_face` binding describes how a matching opposite side would be identified; interpret its null as scoped to supplied scans, as the candidate does.

## Icon identification

The official Rules Reference v1.7 printed page 22 visual key, independently inspected at `C:/Users/mggar/Source/evidence/fne-acquisition/rr-v17-page-22.png`, labels the exclamation/explosion glyph on 60177–60179 as **crisis**, and the plus/card glyph on 60180 as **hazard**. The current vendored v1.8 [`rr:icons.7`](../../../../datasets/rules-reference/entries/icons.md) and `rr:crisis-icon` describe the crisis icon; `rr:icons.8` and `rr:hazard-icon` describe hazard. This matches glyph identity; it does not establish executable behavior or resolve other mechanics. The v1.7 reference is the visual comparison, while v1.8 is the vendored current rules text.

Resource glyphs and the separate star, boost, cost-arrow and per-player symbols were also visually matched to the official key. The editorial visual descriptions in the current candidate remain legible; canonical names should be captured in a subsequent corrected candidate if desired, with these authorities.

The acquisition packet's unresolved product-wide one-card discrepancy, licensing, offline dataset admission, rules interpretation, and runtime support remain outside this proofread. No candidate, dataset or runtime files were edited during the review.


## Closure on regenerated candidate

Independently compared every recursive JSON change against the preserved original and inspected the builder's changed source bindings and icon attributes. The new candidate SHA-256 is `c66794e43284b3bd8e280817d66c204761f572c9fbad6a4e0b14a29cadbe7c61`; the preserved prior candidate hashes to `aeafb36f8b829607fb49754228d3bcf9267ccc51bca07d0bb54dbec37b88fac9`. The three source-location categories above are corrected. Each of 60177–60179 gains one crisis attribute and 60180 gains one hazard attribute, with correct face, footer and hash binding; these identifications match the previously inspected official visual key. All five Boost attribute source descriptions now share their respective boost-icon location. The sole removed uncertainty is resolved pictogram meaning. No other printed facts changed in the recursive comparison.

**Closure verdict: printed-source proofread passes for the five supplied Disasters faces at this exact hash.** This does not certify product-wide quantity reconciliation, offline dataset admission, gameplay rules or runtime support. Review did not run builds or hooks, alter the candidate, or establish rights to redistribute images.
