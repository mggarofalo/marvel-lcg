# Fear No Evil: Art Museum Heist transcription candidate

Status: **corrected supplied-source transcription accepted by [independent proofread](fne-museum-proofread.md)**. This packet is research only; it does not admit cards to the dataset or implement gameplay.

All ten supplied original scans were inspected at original resolution. They represent **eight distinct gameplay faces**, **nine physical cards / set positions**, and **ten original scans**. Seven printed card numbers occur: 121 has the paired 1A and 1B faces at position 1; 122â€“125 are four unique Art attachments at positions 2â€“5; 126 Art Thief has two physical copies at positions 6â€“7; 127 Tug of War has two physical copies at positions 8â€“9. The duplicate pairs have matching gameplay text and no printed flavor variants. These counts reconcile the printed set positions; they do not establish that any other source outside the supplied manifest is complete.

The candidate preserves text, printed emphasis, traits, types, numbers, glyphs, modifiers, boost text and symbols, credits, footer positions and physical copies. Each transcribed field names the original scan hash and visual region. The four attachment resource glyphs are mental, wild, physical and energy respectively; the minions have a boost star with a boost ability and no numeric boost icons. The 1B main scheme prints a 9-per-player threshold, 1-per-player starting threat, and +X per player with a star; its text defines X. The official RR v1.7 page 22 visual key and the vendored v1.8 icons entry were consulted. No field is marked illegible; this is a transcription judgment, not independent verification.

External evidence lives in `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `museum-candidate.json`, SHA-256 `82a5a276b58ec9b2c49895e01de60e5d6378e6addac08cd2c84ef790705f5372`.
- `build-museum-candidate.py` builds offline and `--check` compares exact bytes; `validate-museum-candidate.py` verifies source hashes, field source coverage, accounting and distinguishing glyph inventory and writes `museum-transcription-findings.json`.
- `scan-manifest.json`, SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, identifies each original and acquisition URL. The glyph key hash is in the candidate.

The builder byte check and structural validation passed. They do not prove the text, emphasis, glyph reading, or visual regions correct. A separate reviewer must inspect all ten originals and every field before treating this transcription as accepted. Redistribution, dataset admission and executable coverage are outside this packet.

The original candidate is preserved as `museum-candidate-before-proofread.json`. Independent review identified one missing italic marker on 60121b; the corrected candidate differs only in that rules-text field. The proofread record preserves the rejection and exact-revision closure.
