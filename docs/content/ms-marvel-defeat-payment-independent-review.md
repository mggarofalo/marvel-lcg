# Independent preparation-boundary re-review

Reviewed `a0c1edffd29f0a8d2bc48de146417b1d5dfe7f8d`, based on the original
independent review `32ad2490`, on 2026-10-26. Review worktree:
`C:/Users/mggar/Source/marvel-window-review`.

Verdict: **accept MARVEL-731 and the four actual card rows for their reviewed
research scope**. The Red Dagger finding in
[the original review](ms-marvel-ally-condition-independent-review.md) is
resolved. Endurance, Down Time and Morale Boost retain their prior acceptance;
the independent health, recovery, recipient and timing checks pass with the fix.
This does not establish complete-pack or public admission acceptance.

## Contract and source review

Reviewed the complete engine/Cards diff, all production implementations of
`IWindowAbilities`, all `ResolveWindow` call sites and occurrence-trigger call
sites, payment preparation/capture, recipient validation, and the native
retaliation and activation-replacement paths.

`AbilityRunner.PrepareWindow` checks printed conditions, availability,
initiation, the submitted recipient and complete selected arrow/event payment
before returning its invocation body. Preparation creates resolution-local
objects and immutable payment selections. Execution registration,
`EnterExecution`, resolution tracking and cost commitment occur afterward.
Both authored decorators forward preparation. Ordinary, defeat-choice,
defeat-resumption and automatic mandatory callers commit their trigger only
after successful preparation and before costs can suspend.

Integration note: the full architecture lane rejected the original default
interface body in `NarrowPortsCannotSilentlySupplyCardBehavior`. The lead moved
that forwarder into `NoCardAbilities`; the boundary assertion is unchanged.
This correction requires combined verification beyond the original review.

The explicit no-card provider supplies a preparation forwarder for resolvers
without answer-dependent validation or costs. The narrow interface requires an
implementation; it supplies no default behavior. Native retaliation and activation replacement do not
take player-selected costs or recipients; they retain their native dispatch.
This forwarder is not a rollback guarantee for arbitrary custom resolvers.
The prepared body is consumed synchronously by the engine, with no intervening
player decision. Accepted costs and nested effects retain the existing
continuation semantics. The existing suspended distinct-resource test also
proves that caller mutation cannot rewrite the retained allocation.

No further production finding was identified. No production source or dataset
was changed by this re-review.

## Independent distinguishing tests

The original actual-row Red Dagger test now succeeds after an invalid
energy/energy allocation and a legal energy/mental retry.

Two additional cases in `MsMarvelDefeatAcceptanceReviewTests` use the unmodified
full book, controller seat 1 and physical owner seat 0. They reject a player
identity as the enemy recipient and reject paying with a card in the physical
owner's hand. Each rejected answer preserves digest, RNG, outstanding work,
resolution records and both origin/containing occurrence trigger eligibility.
The same prompt then supports either a legal retry or decline. The successful
retry deals exactly 2 damage and returns the exact ally to its owner, while
decline pays nothing and completes defeat. Both paths finish without pending
agenda work. Existing nested payment, forced ordering, native retaliation and
default-provider tests pass alongside these cases.

## Evidence

Release builds and focused runs on the reviewed engine revision plus the two
new test cases:

- Content: **229 passed, 0 failed**. Selected Ms. Marvel actual-row tests,
  ability-data/lowering, window payment atomicity, distinct-resource payment
  suspension, native retaliation ordering, Green Goblin forced ordering,
  step-seven defeat, forced defeat completion, window intent, self-defeat,
  defeat bindings, When Defeated and would-be-defeated tests. Log:
  `C:/Users/mggar/Source/window-review-final.log`.
- Rules: **33 passed, 0 failed**. Selected native retaliation lifetime,
  indirect retaliation, Green Goblin activation replacement, defeat windows
  and Wrecking Crew retaliation lineage. Log:
  `C:/Users/mggar/Source/window-review-rules.log`.
- The author's compiled early-trigger mutant log was inspected: its three
  intended regressions fail. The independent review did not mutate production
  source. Normal pre-commit hooks remain enabled.

The Rules command used
`FullyQualifiedName~NativeRetaliationLifetimeTests|FullyQualifiedName~NativeIndirectRetaliationTests|FullyQualifiedName~GreenGoblinActivationReplacementTests|FullyQualifiedName~DefeatWindowTests|FullyQualifiedName~WreckingRetaliationLineageTests`
with `dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release
--filter` and `--nologo`. The Content command used the corresponding named
classes above and `FullyQualifiedName~MsMarvel`, `~AbilityData` and
`~AbilityLowering` against `tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj`.

No broad suite, Server, native UI, public setup or save/restart run is claimed.
