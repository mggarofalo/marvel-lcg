# Printed card dataset provenance

This is generated data. The committed inputs are the pinned snapshot under
`datasets/marvelsdb/` and `supplement.json`; regeneration is offline:

```text
dotnet run --project tools/Marvel.Cards.Extract -- write
dotnet run --project tools/Marvel.Cards.Extract -- check
```

`cards.json` supplies printed game facts. `search.json` is acquisition/search
metadata generated from the same inputs. See [the dataset contract](../../docs/card-dataset.md)
for field meanings, supplement correction rules and the generated-data gate.
Every supplement group states its reason and printed authority. Engine-only
faces are explicitly identified by their groups rather than claimed as print.

The 171 missing Fear No Evil scenario/campaign face IDs are supplied through
18 independently accepted local transcription candidates, whose source hashes,
normalization choices, quantities and retained Protection Racket provenance
limits are recorded in [the admission evidence](../../docs/content/research/remaining/fne-printed-catalog-admission.md).
The committed supplement contains the admitted transcription; external scan and
candidate files are acquisition evidence and are not required to regenerate.
Original scans remain external; their acquisition/licensing findings are in
[the acquisition packet](../../docs/content/research/remaining/fne-acquisition-candidate.md).
Catalog inclusion does not establish executable product support.
