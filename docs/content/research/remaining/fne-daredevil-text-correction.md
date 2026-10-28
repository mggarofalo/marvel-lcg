# The Man Without Fear: printed text correction

The pinned MarvelSDB record for `60018` spells the identity name `Daredeveil`
in its damage cost. The original English card reads `Daredevil`. The supplement
corrects that spelling; the remainder of the text is unchanged. This resolves
the one player-record discrepancy identified by the Fear No Evil acquisition
review without refreshing the upstream snapshot or admitting runtime content.

The original scan was inspected at its full 710 × 1030 resolution on 2026-10-28:
[The Man Without Fear, Daredevil 15/15, card 18](https://hallofheroeslcg.com/wp-content/uploads/2026/08/60018.jpg).
SHA-256: `7eaa5029883c2612951db89dfce521c58d0d655de6e1789a2b7c322ae4560ca1`.
It is retained outside the repository at
`C:/Users/mggar/Source/evidence/fne-acquisition/60018-original.jpg`.
The separately acquired upstream commit
`44b7f2c9b01faa50f5a96cb40a9c0ecbbace7776` contains the same corrected text;
the printed scan supplies the authority for this correction.

A fresh Release extractor build and offline generation/check passed. A recursive
comparison confirms changes to `60018.text` and `60018.text_plain`, each replacing that one typo, plus the supplemented-record count
from 69 to 70. Search metadata is unchanged. Independent review remains pending.
No ability, setup, public admission or accepted-product count changes.
