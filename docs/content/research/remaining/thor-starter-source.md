# Thor starter source review — MARVEL-563 / MARVEL-702

Reviewed 2026-10-25 against `a6093e286c87ae553e0845a26401065260475e9b`.
This source-only supplement resolves the Thor starter-divider check in
[563–568](563-568.md); it does not implement or admit Thor.

The original printed **Thor Deck** divider is now pinned in
[`datasets/rules-packs/mc06/`](../../../../datasets/rules-packs/mc06/UPSTREAM.md),
with source URLs, byte lengths, SHA-256 hashes, attribution and exact transcription.
Hall of Heroes links the image as its
[Starter Deck](https://hallofheroeslcg.com/wp-content/uploads/2020/03/thorstarterdeck1.jpg).
The source is FFG's component, visibly © MARVEL © 2019 FFG, rather than an
editorial deck list.

The visible list confirms all 40 player copies in the position-derived
candidate `06002–06025`: 15 signature, 17 Aggression and 8 Basic. Names and
IDs reconcile to `datasets/cards/cards.json`, and the quantities reconcile
to the local `datasets/marvelsdb/pack/thor.json`. The starter membership is now
**reconciled**. Its 25 Aggression/Basic additions are distinct from the
extra customization pool `06031–06034`; those four cards are absent from
the printed starter. The separate identity is `06001a/b` and obligation
is `06026×1`.

The source review found a concrete divider discrepancy: it prints Frost Giant
once and Trickster twice, reversing the local inventory quantities. Three
separately pinned original component scans settle the physical composition:
Frost Giant `06029` occupies **THOR NEMESIS (3/5)** and **(4/5)**; Trickster
`06030` occupies **(5/5)**. The reconciled set remains Family Feud `06027×1`,
Loki `06028×1`, Frost Giant `06029×2`, Trickster `06030×1`. Preserve the literal
divider transcription for audit, but author the nemesis setup from the
numbered components. No printed behavior is derived from these scans.

Inspection covered the existing local mc06 rulesheet extraction and manifest,
the research packet and face inventory, joined card names/IDs, upstream
quantities, and the mc04/mc05 component provenance patterns before obtaining
the missing divider. A targeted local `mc06`/Thor source-file search found no
local original divider. The original product page's Starter Deck link supplied
the scan; the same page's individually numbered nemesis scans supplied the
quantity correction. No packaging-count reconciliation was needed.

Validation: visually read the divider and all three numbered nemesis scans;
checked source byte lengths and hashes; compared all 24 distinct starter IDs
and quantities with the local inventory. The 40-card player list has no
discrepancy. No runtime, setup, card dataset or ability-book change is included.
The next content slice can use this pinned list when authoring Thor's research
setup. Complete reachable ability coverage and normal acceptance evidence still
precede public admission.
