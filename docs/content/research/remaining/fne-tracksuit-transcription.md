# Fear No Evil: Tracksuit Mafia transcription candidate

Status: **supplied-source transcription accepted by [independent proofread](fne-tracksuit-proofread.md)**. This research packet is not dataset admission or runtime implementation.

Seven original-resolution scans cover five printed card numbers and seven physical set positions: Ivan Banionis 200 (1/7), The Clown 201 (2/7), Tracksuit Mafioso 202 (3/7), Tracksuit Bro 203 (4/7, 5/7, 6/7), and Tracksuit Mafia 204 (7/7). The three Bro originals have distinct flavor lines, so the candidate retains each physical original separately. No opposite gameplay face is supplied. No transcribed field is marked illegible; independent review may identify errors.

External evidence is under `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `tracksuit-candidate.json`, SHA-256 `58b607d3db09778bb95f7a13f0048b69cec0f1a3ffd8e4af8206d43609613b8d`, records each field with its original filename, hash and actual visual region.
- `build-tracksuit-candidate.py` builds offline; `--check` compares exact bytes. `validate-tracksuit-candidate.py` checks manifest and scan hashes, field-source coverage, positions and distinguishing glyphs and writes `tracksuit-transcription-findings.json`.
- `scan-manifest.json`, SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, records acquisition URLs and original hashes. The retained official RR v1.7 page 22 visual glyph key is hashed in the candidate; the vendored v1.8 icons entry names the symbols.

The first three minions have unique icons; Ivan and The Clown have SCH stars. Keywords are printed in the rules box. The side scheme's two boost icons are on its left edge and its starting threat is 3 per player. The remaining numeric boosts are 2, 2, 3, 2, 2 and 2 in position order. Traits, rules emphasis, flavor, credits and footers are preserved per original.

The builder byte check and structural validation passed. These checks do not establish semantic accuracy: a separate reviewer must compare every field and region with each original. Redistribution rights, dataset admission and executable coverage remain outside this packet.
