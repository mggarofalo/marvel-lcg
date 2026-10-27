# Paid-event status cancellation regression closure

Base: `54b9055a`. This packet changes tests and one Core transcript; it does
not change the production engine, authored abilities, catalog obligations,
authority fingerprints, admission qualifiers, or pinned outcome digests.

RR 1.8 `rr:status-cards.2` gives status abilities priority over conflicting
triggered abilities. `rr:labeled-ability.6` cancels the entire labeled ability
except its costs. `rr:cancel.2` prevents the effects from initiating;
`rr:cancel.3` still regards the event as played and discards it.
`rr:initiating-abilities.step.5` through `.step.7` put payment before attempted
effect initiation. `rr:confuse-confused.5` and `rr:stun-stunned.5` retain costs.
These clauses support cancellation before an effect's recipient choice. A
recipient choice printed as an arrow cost remains a cost, outside this packet.

The For Justice!, Clear the Area, Swinging Web Kick, synthetic staged Melee,
actual Melee, and Core Swinging Web Kick transcript now explicitly observe
completion without an effect-target answer. Payment, event discard, status
removal, unchanged threat/damage and absence of the conditional draw remain
asserted. Clear the Area's ordinary helper still requires and answers its
target prompt; actual Melee's unstunned branch still answers its enemy choice.
For Justice!'s fixture includes a player-deck card so an empty-deck reset does
not recycle its payment before the exact discard observation.

Windows Release verification:

- 165 tests pass: the complete `CoreThreatAbilityTests`, `ClearTheAreaTests`,
  `StagedAttackEdgeTests`, `SpiderManCardsTests`, `MeleeActualRowReviewTests`,
  complete `Behavior` namespace (including the passing Core corpus), and
  `RuleCitationTests`.
- `Marvel.Behavior.Index check` passes with no regenerated diff.
- `Marvel.Cards.Extract check` passes with no regenerated diff.
- `git diff --check` passes.

The distinguishing mutation replaced
`effectPrevented = AbilityEventStatusPriority.Cancel(execution, cast);` with
`effectPrevented = false;` in `AbilityInitiationPayment.OpenEventInitiation`.
This defers status handling to the effect body. All three initial cancellation
tests fail at their no-target assertions; the Core transcript fails at line 331
because `01005: choose a card` remains pending. The selected mutation run has
12 failures (the remaining failures share the cached corpus result). The
original source bytes were restored, and the expanded 165-test run passes.

Commands use `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --filter` with the class/namespace names listed above. Mutation
selection includes the three initial cancellation methods and
`CardActionBranchesHavePinnedOutcomes`. Logs are outside the repository under
`C:/Users/mggar/Source/evidence/status-priority-closure-`: `mutant.log`,
`restored.log`, `index.log`, and `cards.log`.

This bounded closure establishes the regression scope above. It does not claim
the complete content suite, second-platform certification, public pack
admission, or UI acceptance.
