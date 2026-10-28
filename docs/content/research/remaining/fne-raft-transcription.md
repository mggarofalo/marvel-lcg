# Fear No Evil: The Raft Breakout transcription candidate

Status: **corrected supplied-source transcription accepted by [independent proofread](fne-raft-proofread.md)**. This research packet does not admit cards to the dataset or implement gameplay.

All ten supplied originals were viewed individually at original resolution. Their printed card numbers and positions account for ten distinct gameplay faces, nine printed base numbers and nine physical positions. Main scheme 142A/B shares position 1; Master Key 143 occupies position 2; Absorbing Man 144, Baron Zemo 145, Drang 146, Mysterio 147, Proxima Midnight 148 and Rhino 149 occupy positions 3–8; Imprisoned 150 occupies position 9. Pairing and quantities follow the printed A/B markers and `(n/9)` footers. This reconciles the supplied manifest section; no packaging or marketing count is used as a gate.

Every recorded field has an original filename, SHA-256 and visual region: 281 field/source pointers across ten originals. The candidate preserves main rules, separated boost abilities, printed emphasis, glyph placeholders, traits and punctuation, flavor, artist/copyright credit, product-symbol placement, set positions, pairings, resource/boost fields and stat markers. JSON names and the acquisition prefix `60` are editorial choices. No resource icons were observed in the card borders; resource symbols within Absorbing Man’s ability remain in its rules text.

The main scheme prints threshold 11 per player, starting threat 2 per player and acceleration +1 per player. Master Key has a unique glyph and a star-only SCH field. Every minion is unique and has ELITE and PRISONER traits; Absorbing Man, Drang, Mysterio, Proxima Midnight and Rhino have ATK reminder stars. Baron Zemo has no ATK reminder star. All six minions print three numeric boost icons. Rhino additionally has a boost star and separated boost ability. Imprisoned has zero numeric boost icons with a boost star, a separated boost ability and a Hero Action cost arrow. Printed values and stars are recorded separately.

The retained official Rules Reference v1.7 page 22 visual key and current vendored v1.8 `rr:icons` and `rr:star-icon` entries were consulted. The older page identifies glyph shapes; current rules remain authoritative. This packet records printed facts without asserting timing interpretations. The six minion identities were read from their original scans, independently of the campaign-context list.

External evidence is under `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `raft-candidate.json`, SHA-256 `b5442676adcf7ac32d0eec33af22dcc3b281f70235aa3baffed703da4bb7f4a2`.
- `build-raft-candidate.py` constructs the candidate offline; `--check` compares exact bytes. It checks every original’s pinned size and hash before generation.
- `validate-raft-candidate.py` checks manifest section coverage, original sizes/hashes, field/source parity, positions, face pairing, uniqueness, stats/annotations, boosts and the obligation cost arrow. It writes `raft-transcription-findings.json`.
- `scan-manifest.json`, SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, pins all ten original paths, acquisition URLs and image hashes. The candidate also pins the visual-key hash.

Offline generation, exact-byte `--check` and structural validation passed against the corrected candidate hash above. Independent review checked all ten originals and 281 fields/source pointers. The one correction changes the A-face flavor source region to “left rules panel; no flavor”; no printed value changed. Exact whole-JSON comparison and original hashes were independently rechecked at closure. No transcription finding remains. Redistribution and dataset admission remain outside this result.

The original candidate is preserved as `raft-candidate-before-proofread.json`, SHA-256 `8c1594f9839464489a1bb021c15c9ac89543c281f7e63af36d6ac9b3cd8e0650`. The original rejection remains in the linked proofread record and `raft-proofread-findings.json`; `raft-correction-audit.json` and `raft-proofread-closure.json` record the one-field correction and exact-hash acceptance.
