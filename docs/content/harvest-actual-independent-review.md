# Independent actual Harvest review

Accept the bounded 05029 authored row at `93b11189`. The joined printed text in
`datasets/cards/cards.json` requires every in-play Persona support to exhaust,
heals the villain for each support actually exhausted, and grants surge only
when none was exhausted. The row follows those three clauses in order, uses a
global support selection, and reads the committed exhaustion receipt rather
than counting the selected cards or the amount of healing. `rr:exhausted.1`
excludes previously exhausted cards; `rr:heal.1` caps healing without changing
the exhaustion count. The vendored MarvelCDB FAQ queried 05029 and has no entry
for it; the vendored rulings corpus has no Harvest-specific match.

The full-book cases in `HarvestEffectReceiptTests` distinguish player ownership,
resolving seat, non-Persona supports, allies, ready hand cards, already exhausted
Personas, capped healing, and zero healing at full villain health. Its zero
cases use the complete encounter procedure and verify the surged minion and
encounter cleanup. The separate synthetic cases establish receipt persistence
through choices and zero overwrites; the prior independent paid-action review
establishes separation from cost exhaustion. The lead's actual-row
supports-to-allies mutant is relevant content evidence; this review does not
claim to have rerun that mutation.

Added `HarvestActualIndependentTests` using the unmodified full ability book.
For either resolving seat, a first Harvest exhausts the other player's Persona
and heals exactly one. A second Harvest encounters that exhausted Persona plus
ready Persona supports in hand, deck, and discard and a ready non-Persona support
in play. It heals zero, exhausts none, surges to the correct player's minion,
and completes both encounter cleanups. This distinguishes a retained prior
result and out-of-play trait matching at the actual card boundary.

Validation on Windows: the focused Harvest, exhaustion receipt, ability-data,
and citation filter passed 46 tests. Command:
`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~Harvest|FullyQualifiedName~ExhaustionReceipt|FullyQualifiedName~AbilityData|FullyQualifiedName~EveryCitedRuleExists"`.
The preceding Release build and narrower 13-case run also passed. Logs are
`C:/Users/mggar/Source/harvest-actual-independent.log` and
`C:/Users/mggar/Source/harvest-actual-independent-expanded.log`.

No production code or ability data changes are part of this review. This accepts
the actual Harvest content row and its covered engine behavior; it does not
admit Ms. Marvel publicly or establish complete pack, native, persistence, or
cross-platform readiness. Integrated delivery gates remain with the lead.
