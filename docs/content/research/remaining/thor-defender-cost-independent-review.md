# Defender cost independent engine review (MARVEL-743)

Reviewed candidate `7d9e87a4` in isolated worktree
`C:/Users/mggar/Source/marvel-thor-defender-cost-review`, branch
`review/thor-defender-cost`. Verdict: changes required for the admitted primitive
and its citation integration. This is a synthetic engine prerequisite review;
it is not actual printed 06003 row acceptance or whole-Thor approval.

## Findings

1. **Accepted compound costs expose responses before simultaneous payment.**
   `AbilityCostPayment.CommitImmediate` immediately suspends after starting the
   discard-and-engage cost (`src/Marvel.Cards/Run/AbilityCostPayment.cs:102`).
   In an accepted `seq` of `discardUntilMinionEngaged: "you"` followed by
   `exhaust: "you"`, the engagement response observes the identity still ready.
   Reversing those components exhausts it before the same response. Rule
   `rr:cost.5` requires multiple costs to be paid simultaneously, so authoring
   order cannot expose this intermediate unpaid state. The red theory
   `OtherPhysicalCostsArePaidBeforeEntryResponses(entryFirst: true)` preserves
   the observation; its reverse-order control passes. Actual 06003 has only the
   standalone arrow cost. Rejecting unsupported compound composition before any
   payment is a valid bounded resolution; full compound execution is not required
   for this prerequisite.

2. **The clean candidate fails Content's citation gate.**
   `tests/Shared/RuleCitations.cs:76` reads only graph `modifications` and never
   admits `card_clarifications`. Consequently the original candidate's three
   test citations to `ruling:6e301a347a5392fb` and
   `ruling:a1f4982cfebcd768` fail `RuleCitationTests.EveryCitedRuleExists`.
   These relations use an existing documented graph section:
   `docs/rules-provenance.md:119` distinguishes card clarifications from general
   Rules Reference modifications, and `CardRulingRelationshipTests` already
   validates that section. The fix belongs to citation admission with appropriate
   distinguishing checks; moving a card clarification into general modifications
   would conflate their meanings. The reported four Rules-project provenance and
   citation passes reproduce, but do not establish Content's citations are valid.

## Independent evidence

The added `MinionEntryArrowCostReviewTests` has six cases. Five pass:

- An engagement response may be declined and the post-arrow effect runs once.
- An accepted response suspends into a nested choice; its selected Confused status
  replaces the eventual thwart and is removed, while threat stays unchanged.
  An invalid nested answer preserves card digest, RNG state and agenda.
- Reverse-order compound cost control pays exhaustion before the response.
- Repeated offer queries preserve digest, RNG and pending agenda before execution;
  subsequent settled execution matches an unprobed control using the shared runner.
- A second player's source engages that player's minion, discards to that player's
  area and does not offer Thor's response to the other player's engagement.

The sixth case is the deliberately preserved simultaneity failure above.

Final combined Content filter: **67 passed, 2 failed, 69 total**. Failures are
the preserved compound-cost case and the candidate's citation gate. The original
candidate baseline before adding review tests was **62 passed, 1 failed, 63 total**
with that same citation failure. Rules-project filter
`CardRulingRelationshipTests|RuleCitationTests`: **4 passed**.
Release Content-project nonincremental build: **0 warnings, 0 errors**.

Commands used:

```powershell
dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~MinionEntryArrowCostReviewTests|FullyQualifiedName~MinionEntryArrowCostTests|FullyQualifiedName~DeckArrowCostTests|FullyQualifiedName~ThorEngagement|FullyQualifiedName~AbilityCostLoweringTests|FullyQualifiedName~RuleCitationTests'
dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release --filter 'FullyQualifiedName~CardRulingRelationshipTests|FullyQualifiedName~RuleCitationTests'
```

Full UTF-8 logs are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/thor-cost-independent-` with suffixes
`baseline.log`, `build.log`, `final.log`, and `provenance.log`.
`dotnet tool restore` and `dotnet husky install` ran normally; no hook bypass was
used. No production source or ability dataset was edited. No new mutation trial,
whole-game acceptance, saved-session replay or broad integration run is claimed.
