# Original card payment: independent review

Plane: MARVEL-753 (`cd2c431f-cadd-4a35-8dca-d656381bcf7d`).
Reviewed implementation: `96c4da97015893cdb929a802ec7d8e8c9ee1162b`.
Review branch: `test/ally-payment-independent-review`.

The 19-file implementation preserves the accepted payment allocation through
`GameActions`, payment preparation, committed contributions, ally entry and its
occurrence. `AbilityPaidResourceQuery` binds the original price to the matching
source; `PlayedCardPayment` rejects a different physical card, playing player or
incarnation. The receipt is occurrence-local and omitted from occurrence JSON;
session replay reconstructs it from the accepted decisions. The source-layout
baseline changes only three diagnostic locations, retaining their measured debt.
No runtime correctness finding remains in this bounded review.

Printed 06012 supplies the original-price distinction: its response says
"if you paid for this card using a [energy] resource". The review tests use
synthetic counter effects and a separate response cost to make incorrect payment
provenance observable; they do not implement Valkyrie's printed damage effect or
admit Thor content. Five unrelated damaged em-dash comments in `CardPlay.cs` are
repaired without behavior changes.

`AllyPaymentReplayReviewTests` adds two independent cases through the existing
`IDurableGameFactory` seam, `EngineHost`, `SessionSaveJson` and `SessionReplay`.
Both stop at the pending ally-entry response, serialize/read/verify the open
journal, restart the host, and complete the same response on uninterrupted,
replayed and restarted games. One declares original wild icons as energy and
pays the response with physical; the other declares them as physical and pays
the response with energy. Assertions compare ordered semantic events, pending
prompts, complete card digest, RNG state and consumption, phase and round. The
completed save also replays to the same result, including the response cost's
discard and the original-price-dependent counter amount.

Independent Windows Release results:

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~AllyEntryPaymentTests`: 11 passed.
- `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter FullyQualifiedName~AllyPaymentReplayReviewTests`: 2 passed.
- `dotnet test tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release`: 129 passed.

Logs are external to the repository at
`C:/Users/mggar/Source/ally-payment-review-{content,server,architecture}.log`.
The initial added fixture omitted the ally play's required identity target, then
omitted public response allocations; both fixture errors were corrected using
the offered target and `DecisionComposer`. A subsequent comparison retained a
pre-transaction game reference; it now compares the host's committed replacement.
The final two-case run passes.

The existing content cases additionally exercise suspended generators and ally
limits in memory, malformed allocation atomicity, put-into-play reentry and
receipt copy/player/incarnation rejection. This review does not establish a save
boundary inside a suspended generator or ally-limit choice, actual Valkyrie
damage, full pack admission, public production setup for Thor, native UI behavior,
cross-platform CI or broad regression acceptance. The implementation worker's
reported mutants were not independently rerun here.
