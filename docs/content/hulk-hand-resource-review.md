# Independent Q3 review — MARVEL-853

Reviewed source: `f45a6d393b417662c2f98fdf63210d33335f6b21`, in the separate
`review/hulk-hand-resource` worktree. This review covers the printed hand-resource
form prerequisite only. It does not admit Hulk, author production rows or certify
pack, setup or native interaction readiness.

## Finding: ineligible contributors can hide in sufficient payment

Status: **closed by the independently verified repair below**. The following
finding and failure results describe the original reviewed source revision.

`CardResourcePayment.Validate` queries live resources for each hand contributor,
but checks only aggregate sufficiency. When Limitless Strength's owner changes
from hero to alter-ego after pricing, that query returns an empty string. If an
ordinary hand card already covers the cost, validation still succeeds and
`Contribute` discards both cards. A zero-cost payment also accepts and discards
the restricted card. A stale explicit price/allocation does not close this gap.

This violates printed card `10007`: "Spend this card only in hero form."
`rr:the-golden-rules` gives the printed restriction precedence over the generic
hand-discard resource permission in `rr:resource.1`. Offers omit the card, but
the public lower-level `AbilityResourcePayment.Spend` must independently enforce
the restriction before any contribution commits. The new theory uses a non-first
seat, a stale hero price, and an ordinary contributor ordered before the
restricted one; it requires unchanged digest, RNG, agenda and event output on
rejection and permits a legal retry.

Repair must reject each ineligible selected hand resource before costs commit,
including when the other cards suffice or the total price is zero. Preserve
legal unrestricted overpayment and zero-cost payment: the separate controls
select two ordinary cards in alter-ego and require both discards and complete
payment. Recheck contribution eligibility when a suspended payment resumes;
previously committed generator costs do not imply a rollback guarantee.

The independent non-first-seat control confirms that first-player form does not
govern the contributor, changing the contributor's face changes eligibility,
and raw printed icons remain visible in alter-ego. Strict JSON/programmatic
admission, authored-face validation and raw-icon APIs were inspected against the
source diff and existing tests. No additional production finding was established.
The source's 47-test and three-mutant results are implementation evidence, not
newly executed review evidence.

## Evidence

`HandResourceFormReviewTests` is synthetic engine evidence. Against the source
revision, the initial compiled run failed both stale-contributor cases with
`Assert.Throws() Failure: No exception was thrown`; the independent live-form
control passed. The final reviewer run compiled and reported **2 failed / 3
passed**: both stale-contributor cases fail, and the live-form and two separate
legal overpayment/zero-cost controls pass. The fixture supplies a nonempty player
deck so discards are observable without an immediate empty-deck recycling.
Logs are outside the repository:
`C:/Users/mggar/hulk-hand-resource-review-initial.log` and
`C:/Users/mggar/hulk-hand-resource-review-final.log`.

The initial review committed intentionally failing regression tests pending a
separate production repair by the source author. No production files were changed
in that initial review.

## Independent repair closure

Source repair `697de00b6581254c4ce5a2c0e6326bc51236a04e` was cherry-picked
alone onto the original reviewer commit as `d025af3c`. The explicit
`IResourceCardAbilities.CanUseHandResources` predicate separates permission from
quantity. AbilityRunner forwards the immutable program's live form query;
NoCardAbilities permits unrestricted sources. `CardResourcePayment.RequireHand`
uses this permission both in complete-payment preflight and at the individual
contribution, so a stale price cannot authorize an ineligible discard.

The original five reviewer cases now pass. Source repair tests establish that
a form change during generator suspension rejects an uncommitted restricted
hand card while preserving committed generator costs, and that an unrestricted
zero-icon source remains eligible. An additional independent lifetime control
spends Limitless Strength in hero form before a later generator suspends, changes
form, and completes payment successfully: eligibility is not retroactively applied
to an already committed contribution. These nine focused controls all passed.

An independently compiled mutation removed only the contribution-time
`RequireHand(card)` call, preserving preflight validation. It failed precisely
the changed-form suspended payment case with `No exception was thrown`; the
other eight controls passed. This distinguishes the resumed contribution check
from validation of the original selection. Production sources were restored,
then rebuilt with `--no-incremental`: **zero warnings/errors**. The restored
affected Content test filter passed **142 tests**, including the original five,
the added lifetime control, and all source repair controls. The filter covers
HandResourceForm, HandResourceCommitment, ResourceIconDiscard,
PhysicalPrintedPrice, AttackEventResource, ThorResourceCard,
ResourcePaymentContinuation, RestrictedResource, VariableEventPayment,
EventRestrictedResource, MsMarvelPayment and PreparationResource classes.

Commands used the Content test project in Release configuration, with
`--no-build` for the restored test run after its forced rebuild. Closure logs
are outside the repository under
`C:/Users/mggar/hulk-hand-resource-review-closure-`: `initial.log`,
`mutant.log`, `restored-build.log`, `restored-tests.log` and `commit.log`.
The source author's whole-solution build and mutation results remain separate
implementation evidence; this review reran the affected project and controls.

The demonstrated commit finding is closed. Hulk public admission, production
ability rows, setup, pack acceptance and native interaction evidence remain
outside this review's scope.
