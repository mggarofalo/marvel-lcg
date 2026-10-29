# Team-Building Exercise engine checkpoint

Base: `d5df33de` on `feat/team-building-engine`. This is implementation research, not an admitted capability or pack acceptance. A scoped cost-quotation primitive and focused tests are present but are not wired to a play action. No dataset behavior has changed. Focused quotation evidence is recorded below; no nested-play composition has been tested.

The printed 12024 effect is a Hero Action with an exhaust cost followed by playing a card from the player's hand sharing a trait with their hero, reducing its resource cost by one. `rr:modifiers.4` floors the resulting cost at zero; `ruling:8337b7a07cb58a64` confirms that the reduction is not a resource and does not increase Hank Pym's overpayment.

## Event invocation trace

`GameActions.TriggerAction` schedules a `PlayerAction` carrying a `PendingAbility`, payment, targets, variables and allocations. `PlayerActionProcedure.Apply` calls `ActionResolution.ExecuteAction`, which describes the selected action, builds a decision, and calls `AbilityExecutions.Run` with an occurrence and `AbilityResolutionActions.Act`. The latter rechecks `ActionAdmission`, validates recipient and arrow costs, prepares `AbilityEventPayment`, validates payment-receipt-sensitive effects, and then commits the event and arrow costs. It initiates the event resolution, runs the selected compiled effect, and performs the ordinary event cleanup. Suspended payment and effect work is owned by that invocation.

A nested event therefore needs an actual selected action address, not just a card ID, and must use this lifecycle. `CardPayment.Price` explicitly excludes events. `AbilityPaymentPricing.CombinedPrice`, `EventPrice`, `AbilityPaymentRules.EventPayable`, `AbilityPaymentReceiptAdmission.Admit` and `AdmitEnergy`, and `AbilityEventPayment.Prepare` all independently price or test payment. Discounting only the displayed event price or the final payment leaves inconsistent admission. Cost-selection paths in `AbilityDiscardReceiptAdmission` call `EventPayable` again; the same scoped pricing context must reach these paths. Variable and additional costs must remain separate from the discounted printed resource cost.

## Current pricing foundation boundary

`ScopedCardCostReduction` binds a positive amount to object references for one world, seat and card, the card's face, and its in-play incarnation. `CardPayment.CostOf` and `Price` can quote with it explicitly; the ordinary overloads have no reduction. The value is a price input, not an authorization to play: it does not bind a host, selected event action, prompt or payment, enforce the source's trait/form condition, or represent a completed play. Nothing currently consumes it in gameplay. The existing incarnation counter changes on entry to play, not on every out-of-play movement, so it does not by itself distinguish hand-to-discard-to-hand. No reachable counterexample requiring a new movement marker has been established; use current prompt/revision and live legality first, and investigate a concrete reachable failure and authority before widening state or digest formats.

## Commit boundary and continuation

`AbilityResolutionChoices.Chose` regenerates its prompt and assesses the answer before tracking resolution or applying it. However `GameResolution.ResolveInput` clears information signals before calling the sequence that reaches `Chose`. A stale nested answer must be preflighted at the public decision boundary before that clearing if the unchanged-information guarantee is to hold. Root-card `ValidateLiveCardPlay` does this only for root play offers. A shared read-only continuation validation capability is a possible seam; it must be conditioned on the current exact agenda frame and preserve existing continuation behavior.

Permanent plays call `CardPlayPayment.Play`, which first prepares payment and then creates a child batch invocation. `CardPaymentEntry` retains the adjusted cost, physical card incarnation, area, payment and targets through suspended payment and entry. The parent effect must suspend until that child completes; event invocation needs the same ownership discipline. `AbilityResolutionEffectChoices.SuspendAfterProcedure` records a continuation after child work, but its interaction with synchronous completion and the current choice frame needs a behavioral probe before relying on it.

## Remaining implementation

1. Complete the play request around the scoped pricing primitive: design one immutable play request bound to the actual game, answering seat, hand-card incarnation, reduction, selected host or event action, and accepted payment. Pass it explicitly through both quote and commit paths; never register a global next-card modifier. Keep ordinary modifier receipts and actual payment resources separate.
2. Implement live trait and form checks, own-hand enumeration, ordinary legal and affordable permanent offers including host prices, and supported authored event action offers. Initial action admission must account for the Exercise source after its own exhaust cost, including generator eligibility; receipt-sensitive and unsupported shapes must fail closed.
3. Add a typed DSL instruction and its reachability, choice, lowering, admission, dispatch and continuation handling. Preflight the exact live answer before any mutation, including information clearing. Route to ordinary permanent play or event action and resume the parent once after all owned child work.
4. Add synthetic behavioral tests for both dispatch branches, live stale rejection, real payment versus reduction, host and generator suspension, continuation ownership, and non-leakage. Run the focused and broader required checks and meaningful mutants on the tested revision before claiming completion. The actual 12024 row and public admission are separate work.

This checkpoint deliberately leaves the effect unadmitted. The event action preflight and cross-layer continuation-validation design remain unimplemented; there is no passing composition evidence.

## Quotation-only evidence

On the working tree based on `d5df33de`, the four focused tests passed. A compiled mutant that removed the scoped reduction failed the zero-cost and stacked-modifier tests; after restoration a non-incremental build succeeded and the focused tests plus `CardPlayAnOmittedCostTests` passed (20 total). Logs are outside the repository under `C:/Users/mggar/Source/evidence/team-building-engine-*.log`. This evidence covers quotation only and does not establish nested-play or event behavior.
