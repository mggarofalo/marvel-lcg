# Independent review: candidate self play-cost pricing

Reviewed engine revision: `f1935a21e010f8c23b0d06630d562a85716307a9`.
Review branch: `review/thor-hercules`; review additions are tests and this record.

Verdict: no blocking correctness finding for the bounded, hand-candidate pricing
mechanism. This verdict does not cover an authored 06011 ability, Thor admission,
public gameplay, native interaction, or replay/product acceptance.

Printed authority is the generated `datasets/cards/cards.json` 06011 text:
"Reduce the cost to play Hercules by 1 for each minion engaged with you."
The cost ordering authority is `rr:initiating-abilities.step.4` and `.step.5`.
The positive scalar, direct unconditional Constant envelope, hand-only port and
its names are engine choices, rather than additional printed card requirements.

The implementation reads the explicit playing seat. Ordinary ally entry uses
that same seat as controller; this path does not offer playing Hercules into a
different player's ally area. The candidate must belong to that hand and owner,
so foreign cards and candidates already exposed for payment supply no reduction.
This is the documented hand-only mechanism, not a general out-of-zone discount.
Counting uses effective Minion kind and the matching engagement area. Constant
queries skip this instruction alone; unrelated unsupported constants still raise.
The one-line AbilityRunner port forwards to the Cards-owned query; payment,
uniqueness, ally-limit handling and accepted receipts retain their existing owners.

`SelfPlayCostReviewTests` adds nine independently written cases:

- A positive scalar of two, mixed card kinds, another seat's engagement and an
  encounter-deck minion distinguish quantity, kind and area filtering. It also
  checks foreign-seat pricing and no continuous grants from hand or play.
- Two rejected stale underpayments preserve digest, RNG words/index/consumption,
  registered effects, agenda and emitted events, followed by a valid retry that
  spends the one-use discount and payment exactly once.
- A pricing Constant followed by an unsupported draw Constant still raises for
  the draw; filtering a whole card's constants would fail this check.
- An accepted resource generator suspends for a defeat interrupt; all engagement
  leaves during suspension, and the paid card still enters play at its accepted
  price, with the generator discarded and no pending agenda or game termination.
- Five otherwise valid envelopes add a condition, cost, any-player flag, limit,
  or maximum and assert the specific unconditional-Constant refusal diagnostic.

Existing `SelfPlayCostTests` additionally supply the zero floor, stacked discounts,
uniqueness, ally-limit choice, and probed-versus-unprobed execution evidence.
Existing first-ally and E2 variable-event payment tests were run alongside both
self-cost classes; their separate printed/variable payment obligations still pass.

Verification on this review revision:

```text
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~SelfPlayCostReviewTests|FullyQualifiedName~SelfPlayCostTests|FullyQualifiedName~FirstAllyCostTests|FullyQualifiedName~VariableEventPaymentTests"
36 passed
dotnet test tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release
129 passed
```

Logs: `C:/Users/mggar/AppData/Local/Temp/hercules-review-focused.log` and
`hercules-review-architecture.log`. `dotnet tool restore` and `dotnet husky install`
completed; normal pre-commit hooks remain enabled. No engine mutation was made
in this review worktree; the author's mutation claims are separate evidence and
are not counted as independently executed here. Broad managed/platform/native
gates remain integration work.
