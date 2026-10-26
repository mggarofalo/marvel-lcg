# MARVEL-724 independent capability review

Reviewed implementation: `26b3823feb00026763b1f657893ee30a1eaecd12`, compared
with `de1e4e42b90330c45fa5c96f8f84d63a40aa4b5c`. Verdict: accept this shared
ordered-movement prerequisite; no blocking correctness finding in the reviewed scope.

Authority: joined `datasets/cards/cards.json` 05006 requires one selected card
from your discard pile to the bottom of your deck, then one draw. `rr:then`
requires full preceding resolution. Vendored FAQ/rulings searches expose the
05006 acquisition inventory but no card-specific ruling. Production Aamir
authoring, alter-ego restriction and complete pack/public admission are not included.

Independent retained evidence is `DeckBottomMovementReviewTests` (six cases):

- Removed cards and destinations hosted by departed cards reject placement
  without changing source/destination state or order.
- Resolving player 1 receives the bottom placement and dependent draw, with
  physical ownership deliberately different from the resolver. Player 0's hand
  and deck stay unchanged; original deck order and RNG consumption are checked.
- Both affected areas, deck and discard, refuse a later unstable singular read
  before payment. Same-cost read-only controls remain eligible. Baselines precede
  all admission queries and include digest, RNG word count and agenda head.
- An absent selected target produces no movement and takes the otherwise draw.

Author tests were reviewed for exact discard selection, 0/1/3-card initial decks,
normal reset, semantic landing index, unchanged RNG and invalid foreign-choice
atomicity followed by valid retry. The independent continuation probes validate
the conservative refusal contract; they do not claim arbitrary post-movement
singular queries are implemented.

Verification command, Release, rebuilt restored sources:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~DeckBottomMovement|FullyQualifiedName~TypedMovement|FullyQualifiedName~Shuffle|FullyQualifiedName~RuleCitation'
```

Result: 125 passed, zero failed/skipped. Log:
`%TEMP%/aamir-bottom-independent-final.log`.

An independent temporary mutant changed the committed movement verb from
`Place_On_Bottom` to `Place_On_Top` while retaining actual bottom placement.
It compiled and failed the second-player dependent-draw case because the draw
did not execute. This distinguishes observed completion from a mere successful
movement call. Original source bytes were restored before the passing rebuilt
run. Mutant log: `%TEMP%/aamir-bottom-independent-receipt-mutant.log`.

Production source is unchanged by review. Normal commit hooks remain enabled.
No broad suite, native UI or full Aamir admission claim is made.
