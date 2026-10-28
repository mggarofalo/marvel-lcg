# Fear No Evil: The Getaway transcription candidate

Status: **supplied-source transcription accepted by [independent proofread](fne-getaway-proofread.md)**. This research packet does not admit cards to the dataset or implement gameplay.

All ten supplied originals were viewed individually at original resolution. Their printed card numbers and set positions account for eight distinct gameplay faces, six printed base numbers and eight physical positions. The scheme 128A/B shares position 1; Out Front/Alongside 129A/B shares position 2; Escort Vehicle 130 occupies position 3; Jackknifed Tanker Truck 131 occupies position 4; Burning Rubber 132 has copies at positions 5 and 6; Pedestrians 133 has copies at positions 7 and 8. These pairings and quantities follow the printed A/B markers and footers. They do not establish a separate packaging inventory or completeness beyond the supplied manifest section.

Every recorded field has an original filename, SHA-256 and visual region. The candidate preserves rules and printed emphasis, numbered steps and bullet choices, traits, flavor, credits, footers, resource/boost fields, stat markers and glyph placeholders. JSON names and the acquisition prefix `60` are editorial choices. Out Front has a star-only ATK field; Jackknifed Tanker Truck has a starting-threat value of 2 with a separate reminder star, a per-player glyph in its Hinder text and a crisis icon. The main scheme prints a threshold of 9 per player, starting threat of 2 per player and acceleration of +2 per player. No resource icons were observed on these ten faces.

The retained official Rules Reference v1.7 page 22 visual key and current vendored v1.8 `rr:icons`, `rr:star-icon` and `rr:resource` entries were consulted. The older visual key identifies glyph shapes; it does not replace the current rules authority. This packet records printed facts rather than resolving gameplay timing.

The original candidate records one uncertainty on both Burning Rubber originals: the exact repeated-e count in the italic sound effect, tentatively transcribed as `Skreeeeeee!` with seven e characters. Each copy was inspected separately. Independent proofread confirmed seven e characters on both originals and checked every other field and all 254 source pointers. The exact candidate bytes retain the original uncertainty metadata; the linked review resolves it. Structural checks do not certify visual accuracy.

External evidence is under `C:/Users/mggar/Source/evidence/fne-acquisition/`:

- `getaway-candidate.json`, SHA-256 `cc843ded1ef721a0674d07f995d9c0f9bb318c0403750541af2ef0b80e0565f3`.
- `build-getaway-candidate.py` constructs the candidate offline; `--check` compares exact bytes. The builder checks every original's pinned size and hash.
- `validate-getaway-candidate.py` checks source coverage, field/source parity, original hashes, positions, face pairs, printed markers, boosts and duplicate-copy consistency; it writes `getaway-transcription-findings.json`.
- `scan-manifest.json`, SHA-256 `feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`, pins the ten originals, acquisition URLs and image hashes. The candidate records the visual-key hash.

The offline builder byte check and structural validation passed against the candidate hash above. Independent review accepted the exact candidate above. Redistribution and dataset admission remain outside this result.
