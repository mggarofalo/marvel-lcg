# Lightning Strike typed payment progress

MARVEL-761 is integrated at `63937f6d`, from source `84469c7b` and reviewed
remediation `f314856c`. This is a candidate record, not merged-pack acceptance.

The actual Lightning Strike fixture starts through the public Thor factory,
then explicitly prepares its hand and form. It is a prepared interaction test,
not a full game. Printed cost one and X energy remain separate costs. At X=2,
the UI explains possible coverage for each component, distinguishing generic
and energy shortages. Explicit assignments still determine readiness; possible
coverage does not silently allocate the player's resources.

The engine supplies these facts through `ResourcePaymentProgress.Components`.
Decisions forwards selected sources and variable values. Godot renders the
returned counts and resource icons. The original scalar progress and complete
allocation APIs retain their behavior. The Godot dependency inventory registers
only the reviewed immutable `ResourceComponentProgress` contract.

The initial native reproduction showed X=2 but only generic progress text.
The completed 100% and 150% rendered journeys prove partial payment, cancellation,
clean reopening, Energy assigned to the energy component, Genius assigned to the
printed component, one commitment and two damage. The lead and independent
reviewer inspected captures, including the fully visible 150% commitment and
excess-resource warning. Results and captures:
`C:/Users/mggar/AppData/Local/Temp/lightning-typed-rendered2/`.

Source validation passes 38 focused Rules, 50 Decisions and ten focused Godot
checks. A compiled omission of selected variable values fails the intended
Decisions test. Independent native driver, existing variable-payment and
resource-declaration regressions pass, under
`typed-review-driver-84469c7b` and `typed-review-native-84469c7b` in that Temp root.

Independent review found factorial exploration of interchangeable icons during
partial coverage, and a typed-cost tie preference violation. Remediation uses
memoized icon groups while preserving source-bound eligibility, printed types,
distinct declarations and deterministic tie order. All 127 Prompts checks pass.
Four compiled mutants are killed: typed priority, equivalent-source grouping,
distinct declarations and bound-source collapse. The original synthetic probe's
warmed cases report zero milliseconds at timer resolution in Release and Debug;
this is bounded probe evidence, not a universal latency guarantee. An independent
oracle agrees on 600 mixed wildcard, printed and distinct-component cases.
Evidence is in `typed-coverage-fix-evidence` and
`typed-review-probe-84469c7b/independent-oracle.log` under the same Temp root.

Both implementation commits pass normal architecture, complexity and file-length
hooks. Independent review approves the final remediation. Broader combined
managed, native and platform gates are recorded separately at the final candidate.
