# Independent entry-payment review

Candidate: `f00c867647e0d661e2699214c50961e34de25f9e`, base `75314782dc4b30d99829f1ddef3645523decf109d`. This review covers the receipt foundation only, not Hank's interrupt, counters, HP settlement or MARVEL-885's fourth-ally ordering.

The current RR 1.8 cost clauses distinguish generated excess from paid resources; entry includes both play and put into play, and the official Make the Call FAQ identifies the separately paid ally as the card being paid for. The candidate captures its payment before the spend window, retains strings on the entry occurrence, and leaves the existing played-card receipt paid-only. Source tests cover separate event and ally costs, conditional allocation, nested spending, other owner, reentry, and generator/ally-limit continuations. The final extraction of the original entry checks into `ValidateEntry` and `ValidateAndCapture` preserves their previous ordering and conditions; the new receipt check follows uniqueness. Earlier broad Rules, Architecture and replay runs predate that extraction, as the implementation record states; they are not a final-revision broad run.

Independent controls on the candidate: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~EntryPaymentOriginReviewTests --nologo` passed 2 tests (log `C:/Users/mggar/Source/evidence/hank-review-controls.log`): a target still outside its captured origin is rejected by paid entry when uniqueness does not block it; ignoring the resource cost yields no entry receipt and preserves an empty paid-only play receipt.

Two synthetic contract probes failed (log `C:/Users/mggar/Source/evidence/hank-origin-review.log`):

- After capture, move the target to a different out-of-play area and then back to the origin. `MatchesPending` remains true because out-of-play moves do not increment incarnation. RR `leaves-play.1` applies to leaving play, not this round trip; no supported response accomplishing the round trip was demonstrated. This is a seam-policy question, not a demonstrated rules or gameplay defect.
- After capture, move the target out of its origin and put a matching unique ally in play. Direct `AllyCardEntry.PutPaid` returns without rejecting the stale receipt because uniqueness precedes `MatchesPending`. Without that matching ally, the control above rejects it. No card enters in this case. The actual Make the Call continuation checks origin at the beginning of `AbilityChoicePayment.Commit` before calling `PutPaid`, so a reachable gameplay failure has not been shown. A current receipt blocked by uniqueness is correctly a no-effect attempt under `rr:unique-icon.4.1`.

The failed probes are not committed as failing tests. Both are synthetic seam probes, not a replayable legal game sequence. These observations do not establish a required change to the public method. Review does not certify full integration or final-revision broad lanes.


The complete EntryPaymentProvenanceTests, AllyEntryPaymentTests, and independent control class passed 20 tests together on review commit `81154588` (log `C:/Users/mggar/Source/evidence/hank-review-classes.log`). Normal commit hooks passed 66 architecture checks, complexity and file length on that commit.

