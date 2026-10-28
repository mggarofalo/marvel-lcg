# Fear No Evil: Tombstone transcription candidate

Research for MARVEL-697, 2026-10-27, repository baseline
`a2439588384a10c503cebe8ec1f5b0578dce0e8c`. This follows the
[pinned acquisition](fne-acquisition-candidate.md) and establishes a candidate
for independent proofreading, not dataset admission or executable support.

External evidence root: `C:/Users/mggar/Source/evidence/fne-acquisition/`.
`tombstone-candidate.json` SHA-256 is
`c2be6106963aa855da276aef633394bab616729529bf00829a5c4ee81fdb1817`;
`scan-manifest.json` SHA-256 is
`feac2f63e3153339d11c427fc72724834d13c4530f6dc8965f4d1b8a34067ccb`.
Original scans remain external; the acquisition packet records source storage
and license findings. Each candidate field points to the source filename, full
hash, printed card number, footer position and relevant visual region. Names,
IDs, attribute keys, slugs and normalization are editorial; the printed faces
are the authority.

## Inventory

All **five originals were individually viewed at original resolution**. They
show five gameplay faces, five physical cards, and all footer positions 1–5/5.
No supplied gameplay reverse faces occur; null does not describe generic backs.

| Candidate face | Printed title | Number | Footer | Quantity |
|---|---|---:|---:|---:|
| 60195 | Cold as Ice | 195 | 1/5 | 1 |
| 60196 | Hard as Marble | 196 | 2/5 | 1 |
| 60197 | Beetle | 197 | 3/5 | 1 |
| 60198 | Tombstone | 198 | 4/5 | 1 |
| 60199 | Hit List | 199 | 5/5 | 1 |

Cold as Ice and Hard as Marble have respectively +2 SCH and +2 ATK, each with
a special star tied to its printed Forced Response. Both attach to the minion
with the highest base hit points and give it a tough status card, otherwise
gaining surge. Their triggered effects differ: after the attached minion
schemes, Cold as Ice confuses the first player's identity; after it attacks,
Hard as Marble stuns the attacked character. Each then discards itself.
Conditional surge is retained as text, not an unconditional keyword.

Beetle is unique, AERIAL and CRIMINAL, with SCH 1, ATK 1, fixed 5 HP and
Villainous. Its When Revealed searches the encounter deck and discard pile for
an attachment that can attach to a minion and attaches it to Beetle. Tombstone
is unique and CRIMINAL, with SCH 2, ATK 2, fixed 8 HP and Quickstrike. It has a
star-only boost row and a separated Boost ability that gives the villain a tough
status card. The two attachments and Beetle each print two numeric boost icons;
Hit List prints three. Neither attachment nor Beetle prints an illustrator
credit; their empty credit fields record that absence.

Hit List prints 3 starting threat per player. Its Forced Response after the
villain phase begins deals 3 indirect damage to the first player; for each ally
defeated this way, it removes 1 threat per player from Hit List. The two player
pictograms are preserved independently. No large scheme icon appears. Its
illustrator is Facundo Moyano; Tombstone's is Sebastián Guidobono. Flavor is
kept outside rules text; layout-only wraps and apostrophe/attribution typography
are normalized. `[star]`, `[per_hero]` and `<hr>` retain distinct printed glyphs
and the separator. No illegible word, number or icon was identified during this
transcription; no ruling or gameplay interpretation is inferred.

## Validation and next step

External `build-tombstone-candidate.py` builds from the manual transcription and
pinned manifest. `validate-tombstone-candidate.py` checks source hashes, exact
family inventory, footer/quantity coverage, field pointers, normalization and
specific star and per-player distinctions. `tombstone-validation.txt` and
`tombstone-transcription-findings.json` record the structural checks. The
acquisition `verify.py` also passed offline for all 201 scans and primary inputs.
Structural validation does not establish semantic correctness.

**Independent proofreading remains pending:** compare every field against all
five originals, especially the attachment modifier stars, conditional surge,
Tombstone's star-only boost row and separate ability, and both Hit List player
symbols. Deliberate offline dataset admission is a separate change. No dataset,
supplement, generated output, setup, ability or runtime file was changed.
