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


## Bounded follow-up candidate

The `fix/thor-defender-review` follow-up refuses any cost `seq` containing
`discardUntilMinionEngaged` during lowering, including either component order
and nested or single-component sequences. The exception names the unsupported
simultaneous payment. Standalone arrow-cost entry, nested responses, decline,
query purity and acting-player ownership retain their positive tests. This is a
bounded refusal under `rr:cost.5`, not implementation of compound payment or
printed 06003 admission.

The shared citation consumer admits explicit `card_clarifications` keys alongside
`modifications`. The existing hash/printed-text provenance checks remain in place;
a new control rejects unrelated vendored rulings and unknown ruling/rule IDs.
The earlier four passing Rules-project checks did not exercise Content's citation
gate. The follow-up focused Content run includes that gate and passes 77 tests;
the Rules/provenance filter passes 18 tests.

Two targeted mutants were killed: removing the composition refusal fails all four
order/nesting cases, and admitting all vendored rulings fails the unrelated-ruling
control. Both source files were restored before nonincremental final rebuilds.
UTF-8 logs are outside the repository in the temporary directory with prefix
`thor-defender-fix-`. No whole-game, native-client, saved-session replay, or broad
integration acceptance is claimed. Independent review of this follow-up remains
separate from these author checks.

## Independent follow-up closeout

Reviewed exact candidate `47f3556076b64e7bef371f3327ec6cbbd9fc71b4` in
`C:/Users/mggar/Source/marvel-thor-defender-fix-review`, branch
`review/thor-defender-fix`. **Approved for the bounded engine prerequisite;
both findings above are closed.** No production source, test assertion or ability
dataset was changed during this closeout.

The refusal occurs in cost lowering, before a runner can execute payment.
Recursive lowering refuses the innermost sequence containing the entry cost,
so nesting cannot hide it. Both component orders and their nested forms pass
the four explicit refusal cases, including the required operation and
simultaneous-payment exception text. Standalone entry continues to pass its
positive timing, nested-response, decline, acting-player and query-purity tests.
Public runner construction from an ability book still requires lowering, and
the validated program constructor remains internal.

Citation admission adds only explicit graph `card_clarifications` keys. The
unrelated-vendored-ruling and unknown-id controls pass; the pinned printed-text
and ruling-hash relationship gates remain intact. No relationship was moved
into general Rules Reference modifications.

Independent Release nonincremental builds of Content and Rules each produced
**0 warnings and 0 errors**. The Content filter documented above passed
**71 tests**, including all eight `MinionEntryArrowCostReviewTests` cases and
the assembly citation gate. The Rules filter
`CardRulingRelationshipTests|RuleCitationTests|RulesModificationTests` passed
**18 tests**. Counts describe these exact independent filters, rather than the
author's broader Content filter. Test runs left the worktree clean before this
documentation update.

UTF-8 logs are outside the repository in the temporary directory with prefix
`thor-defender-fix-independent-`, suffixes `content-build.log`, `content.log`,
`rules-build.log`, and `provenance.log`. Tool restoration and Husky installation
ran normally. This closeout inspected the author's mutation evidence but did
not repeat mutation trials. Actual printed 06003 remains unauthored; no pack,
whole-game, saved-session replay, native-client, Linux or broad integration
acceptance is claimed. The next delivery must author and accept the actual row
within its own tracked scope.
