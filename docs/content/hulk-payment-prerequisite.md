# Hulk payment prerequisite — MARVEL-844

Base: `fdd16b0f9f3f7c72631ed1d2033db2f6d06ea386`. Isolated branch:
`feat/hulk-payment-prerequisite`. This packet supplies P1 and P3 engine mechanisms;
it does not admit Hulk, modify authored card rows, or change public setup.
Doctor Strange integration remains separate. Synthetic definitions are labeled in
the test classes and must be replaced with actual card behavior during Hulk acceptance.

## Authority and implementation

Printed `datasets/cards/cards.json` 10002: "You can only spend [physical]
resources to pay for this card." The adjusted printed price determines the
quantity; an independent arrow cost is not this price. RR 1.8
`rr:initiating-abilities.step.4` applies cost modifiers before payment;
`rr:wild-resource.1` permits declaring wild as physical;
`rr:cost.4.1` excludes excess generated resources from resources actually paid.

`printedCostResource: "R"` is an engine-chosen DSL spelling, admitted only on
Action timing. Pricing exposes the existing repeated-resource component.
Feasibility and preparation derive the same repeated requirement from the
adjusted printed amount. The existing allocation ledger validates declarations
before any resource execution, discard, discount consumption or event movement.
The separate arrow component keeps its own requirements. This packet uses no
new receipt representation or Agenda/AbilityRunner edits.

Printed 10018 Martial Prowess says "generate a [physical] resource for an
[[Attack]] event." The `attackEvent` enum/DSL extension uses both event kind and
Attack trait in the existing ResourceSourceEligibility owner. Drop Kick 10014 and
Crushing Blow 10002 qualify. Hulk Smash 10003 has only Superpower and does not
qualify; the earlier handoff's positive Hulk Smash example is corrected here.
Basic attacks, thwart events and unrelated ability costs cannot use it. Existing
Arrow, Aspect, Event and Preparation restrictions retain their owners.

## Verification

- Affected regression: 101 tests passed before the final programmatic-admission
  assertion was added, including PhysicalPrintedPriceTests,
  AttackEventResourceTests, RestrictedResource*, PreparationResource*,
  VariableEventPayment*, EventRestrictedResource* and MsMarvelPayment*.
- Compiled mutant removing the physical printed-price requirement: killed by both
  forged nonphysical declaration cases (2 failures).
- Compiled mutant broadening AttackEvent to all events: killed by Hulk Smash and
  thwart-event negative controls (2 failures, 4 positive/other controls passed).
- Restored sources receive a non-incremental build before final regression.
  Full content suite, final admission check and normal hooks are reported in the
  completion evidence below. Raw logs are outside the repository under
  `C:/Users/mggar/hulk-payment-*.log`.

The three-cost synthetic event deliberately makes partial/mixed payment observable;
it does not assert Drop Kick has Crushing Blow's restriction. Coverage includes
physical/wild-physical, mixed and declared-wild rejection, overpayment, discounts
to two and zero, no illegal offer for mental-only hands, independent mental arrow
payment and unchanged card digest/RNG/outstanding work on rejection.

## Q3 remains a separate packet

Trace: `CardPayment.Generators` asks `IResourceCardAbilities.ResourcesGeneratedBy`
for each hand card. `AbilityProgramQueries.ResourcesGeneratedBy` reads printed
icons and returns them without form checking; its subsequent program-backed query
only doubles icons by classification. `AbilityOfferQueries.ResourceAbility`
checks form for triggered resource abilities, which does not constrain this hand
source path. Therefore adding a hero-form Resource ability for Limitless Strength
alone does not enforce its printed hand-source restriction.

The follow-up needs a narrowly authored hand-resource eligibility capability,
used consistently in offers, allocation and final payment source validation.
Prove Limitless Strength hand-source availability in hero form, rejection in
alter-ego, and revalidation after a form change while payment is pending. Also
prove ordinary printed icon counting outside generation remains RRR, and that
blanked text does not globally erase printed resource icons. No Q3 repair or
acceptance is claimed by this packet.

## Completion evidence

Full restored Content suite: Passed!  - Failed:     0, Passed:  7497, Skipped:     0, Total:  7497, Duration: 8 m 10 s - Marvel.Content.Tests.dll (net8.0)

Final focused admission/payment check: Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28, Duration: 1 s - Marvel.Content.Tests.dll (net8.0)

Normal pre-commit hooks run with the packet commit; see the external commit log.

## Independent P1/P3 review

Reviewed packet `a88a09353e3bfc8a6f69b0314f9ea50ac5304ed6` on isolated
`review/hulk-payment`, 2026-10-26. Verdict: **accept this engine prerequisite**.
No production correctness finding remains in the reviewed scope. This does not
accept Hulk card behavior, public admission, complete pack delivery, or Q3.

The review traced adjusted printed quantity through feasibility, described
components and final allocation, including zero and excess discounts, wild
declarations, overpayment and the independent arrow component. Invalid declared
types are rejected before source execution, movement or discount consumption.
Strict JSON admission and programmatic lowering validate the new property.
AttackEvent uses event kind plus the Attack trait: the generated printed facts
qualify Drop Kick and Crushing Blow, while Hulk Smash has only Superpower.
The packet's changed paths include no authored ability book, public admission,
Agenda or AbilityRunner changes. Synthetic fixture descriptions remain explicit.

RR 1.8 cost modification, declared wild type and overpayment clauses govern the
payment behavior. The DSL spelling, physical-only admission boundary and ledger
representation are repository choices. No historical ruling is substituted for
those current clauses, and no nonempty-payment condition is introduced at zero.

Independent evidence (logs outside the repository):

- Original whole affected classes: **102 passed**, in
  `C:/Users/mggar/hulk-payment-independent-affected.log`.
- Added synthetic `PhysicalPriceReceiptReviewTests`: a payment-dependent effect
  available only for a wild declaration cannot borrow that same icon's physical
  declaration to pay the restricted price. A same-price physical-declaration
  control remains offered and executes. Both cases verify query purity using
  card digest, RNG state and outstanding work.
- Compiled mutant replacing the nonvariable receipt preflight's `PaysDeclared`
  with `Pays`: **killed**, with the intended illegal-offer assertion failing and
  the physical control passing (1 failed, 1 passed), in
  `C:/Users/mggar/hulk-payment-independent-mutant.log`.
- Restored sources rebuilt with `--no-incremental`: zero warnings/errors, in
  `C:/Users/mggar/hulk-payment-independent-restored-build.log`. Whole affected
  classes plus the review controls: **104 passed**, in
  `C:/Users/mggar/hulk-payment-independent-restored-affected.log`.

The affected filter includes PhysicalPrintedPrice, AttackEventResource,
RestrictedResource, PreparationResource, VariableEventPayment,
EventRestrictedResource and MsMarvelPayment classes. The earlier 7,497-test
Content run is author evidence, not an independently repeated full-suite run;
combined integration certification remains the lead's responsibility. Normal
review-commit hooks are recorded in
`C:/Users/mggar/hulk-payment-independent-commit.log`.
