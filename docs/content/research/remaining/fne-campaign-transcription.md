# Fear No Evil: campaign card transcription candidate

Status: **supplied-source transcription accepted by [independent proofread](fne-campaign-proofread.md)**. Research only; no dataset admission or gameplay implementation follows from this packet. Baseline `3e4c919d482def31ed808aba0adf24e6d50f911f`.

All **12 supplied original scans** were viewed at original resolution. They contain **12 distinct gameplay faces** at six printed campaign positions, and support an inference of **six physical cards**, each with an A/B pair: `60205a/b`, `60206a/b`, `60207a/b`, `60208a/b`, `60209a/b`, and `60210a/b`. The first five positions pair completed and failed environment faces; the sixth pairs Typhoid Mary and Bloody Mary ally faces. The A/B environment faces differ in gameplay and flavor; there are no supplied duplicate-face variants. Matching footer positions and complementary printed numbers support the pairs and quantities, but a physical inventory has not been independently examined.

The ally faces each show one consequential-damage icon on each stat; Typhoid Mary has mental and Bloody Mary physical resource icons. Both have unique icons and a star before their forced response. No boost field appears on these faces. The official Rules Reference v1.7 page 22 visual key and current vendored v1.8 `rr:icons` were consulted. No transcription field was judged illegible, although independent review remains necessary, especially for printed emphasis, glyphs, footer credits and source regions.

External evidence under `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `campaign-candidate.json`, SHA-256 `e953ed28ef1a9bc53c1d6b267b43b4701bf15e1cd627a93558a7bcf71eeddb46`, records text, emphasis, traits, stats, glyphs, credits and field-specific original hashes and visual locations.
- `build-campaign-candidate.py` builds offline and `--check` compares bytes; `validate-campaign-candidate.py` verifies pinned originals, field references, pair accounting and selected ally stats. `campaign-transcription-findings.json` records the structural result.
- `scan-manifest.json`, SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, records acquisition URLs and individual original hashes. The candidate pins the glyph-key image hash.

Builder byte comparison and structural validation passed. These checks cannot establish correct transcription, visual regions, physical quantities, or source completeness. The supplied card faces do **not** establish the complete campaign rules: setup, order, persistence and completion require separate review of the campaign insert. A local rulebook PDF exists but its campaign provisions were not examined in this bounded card transcription. Admission, redistribution, runtime behavior, Plane updates and campaign readiness remain outside this packet.
