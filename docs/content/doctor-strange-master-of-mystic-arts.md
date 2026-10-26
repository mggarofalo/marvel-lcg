# Master of the Mystic Arts â€” MARVEL-798

Base `e9584f11`, branch `feat/master-of-mystic-arts`, 2026-10-26.
This is a bounded nonpublic engine packet. Production ability rows, setup,
admission, other Invocation effects, persistence and UI remain outside it.

The generated `09005` text governs the test-local Master program: pay its event
cost and the captured Invocation's printed cost together, resolve that Special,
then return the original physical card faceup. Doctor Strange does not exhaust.
The controlling September 15, 2025 answer is
`ruling:b07d53e12625ab23`, reconciled in
[the authority packet](research/remaining/doctor-strange-ruling-authority.md).
Its discard/reveal/reset sequence supersedes the older no-look/no-recycle answer.

`invocationTop` retains the existing owner/object/incarnation/cost capture.
Event affordability and commitment expand its dynamic amount into the combined
payment; the existing event adjustment applies only to Master's event cost.
`resolveInvocation` uses the existing Special scheduler, without the Wakanda
final-step bonus. Its procedure-completion result survives the parent suspension
and enables the existing `then` frame. `returnInvocation: "you"` is an
engine-chosen DSL instruction, using the captured physical copy after the child
Special has completed its self-discard and refreshed the Invocation deck.
The typed return permits a recycled copy at any deck position, conceals the
intermediate top and exposes the returned top through ordered movement/flip events.

The focused cases use actual zero-cost Winds and a labeled synthetic draw/discard
Special on actual two-cost Crimson Bands. Actual Crimson Bands remains unauthored
and explicitly raises. Tests distinguish combined payment, a two-point event
discount that cannot reduce the two-point Invocation arrow, insufficient payment
without mutation, second-seat ownership, suspended resource payment and Special
choice, changed capture rejection, exact intermediate reveal/reset ordering,
seeded shuffle consumption and card conservation. Only Master emits a normal
event-play move. A recycled original below the shuffled top is returned correctly.

Evidence outside the repository, under `C:/Users/mggar/Source/`:

- `strange-master-focused.log`: 47 focused Master, Spell Mastery and existing
  variable-event payment cases passed.
- `strange-master-reveal-mutant.log`: compiled omission of Invocation discard
  refresh killed by both two-card and last-card cases (missing reveal/reset).
- `strange-master-top-mutant.log`: compiled omission of captured top identity
  comparison killed by replacement across resource-payment suspension; it reaches
  a wrong unauthored Special instead of rejecting the captured top.
- `strange-master-restored-build.log` and `strange-master-restored-tests.log`:
  non-incremental Release restoration build and 47 focused cases passed.
- `strange-master-architecture.log`: 66 focused source layout, debt, test quality,
  engine policy and shared-data checks passed.

Final focused verification and normal commit hooks also run on the committed
packet. Independent engine review and integrated broader checks remain with the
delivery lead. This packet does not establish complete Doctor Strange admission.

Independent engine review of `b3b566af` found a blocking admission gap. The
minimized synthetic Special in
`MasterOfMysticArtsReviewTests.SyntheticSpecialStillReturnsWhenOnlyItsSelfDiscardChangesState`
heals a full-health identity, then discards itself. The child procedure is skipped:
`ResolveSpecial` in `AbilityResolutionRevelation` gates execution through
`AbilityEntryAdmission`; healing has invalid target legality, while
`discardInvocation` falls through to no target legality. Their combined legality
is invalid. Master nevertheless resumes its completed-procedure receipt and
returns the original from `AdditionalDeck`, without the required self-discard or
intermediate reveal. Self-discard must count as an affected target for these
Invocation semantics. Production repair and a retest are required before acceptance.

Independent evidence, outside the repository:

- `strange-master-independent-focused.log`: the original 47 focused cases pass.
- `strange-master-independent-review.log`: 54 pass, one intentional regression
  fails; includes exhausted Strange and a synthetic choice after discard that
  observes the intermediate public top before exact-card return, with and
  without recycling. No production ability rows are added.
- `strange-master-independent-deck.log`: all 17 Invocation deck cases pass.


## Invocation self-discard admission repair

The bounded repair on `fix/invocation-self-discard-admission`, based on `fc93062f`,
classifies explicit `discardInvocation` as affecting its source only when the
resolving player's typed Invocation deck confirms that exact owned, bound physical
card is its current public top in its draw pile. Execution uses the same query.
The unchanged independent synthetic regression now discards and returns correctly.
The completed-procedure receipt remains unchanged: it records finishing the owned
child procedure, rather than requiring every child effect to change state. Wakanda
Forever's existing policy for an unresolvable Special remains covered and unchanged.

The new nonpublic cases cover both arrows, last-card reset, ordered intermediate
reveal before exact physical return, card conservation, and bound top versus
non-top, discarded, unbound same-face and foreign-resolver sources. The Images
control uses its printed effects with a direct binding to this fixture's sole main
scheme; it does not establish complete Images target-choice handling or pack
admission. Current authority is `ruling:8c9c9d30f4c01189`, whose graph relationship
explicitly reconciles its Invocation self-target reasoning with Rules Reference 1.8.

The genuine Images `chooseCard` version exposed a separate choice-handling finding:
`CanInitiateSequence` requires every child's `CanInitiate`, and
`CanInitiateChooseCard` rejects the zero-threat scheme choice even when a sibling
self-discard has valid target legality. The whole Special is skipped. The bounded Images choice repair is recorded below; Vapors status replacement
and Doctor Strange readiness remain separate work. Its reproducible test source and four failing cases are
preserved outside the repository in `C:/Users/mggar/Source/invocation-empty-choice-review.cs`
and `invocation-empty-choice-review.log`.

Repair evidence outside the repository under `C:/Users/mggar/Source/`:

- `invocation-admission-red.log`: unchanged independent regression fails at missing self-discard.
- `invocation-admission-mutant-build.log` and `invocation-admission-mutant.log`:
  omission of explicit admission compiles; six distinguishing cases fail while four
  invalid-source controls pass.
- `invocation-admission-restored-build.log` and `invocation-admission-restored.log`:
  non-incremental Release restoration builds cleanly; 76 focused content cases pass,
  including Master, Spell Mastery, variable-event payment, Black Panther Specials,
  new admission cases and citation validation.
- `invocation-admission-deck.log`: all 17 Invocation lifecycle cases pass.
- `invocation-admission-architecture.log`: 92 focused architecture cases pass.

Independent review of `d242e47a` accepts only this non-choice self-discard
admission repair. The typed query checks the resolving seat, exact bound physical
membership, ownership, current top and draw-pile location; execution shares that
validation. The review adds direct query assertions to the existing two-player
mulligan and purity tests, including an unprobed subsequent discard/recycle
control. No production code changes are part of the review.

Independent evidence outside the repository under `C:/Users/mggar/Source/`:

- `invocation-admission-independent-content.log`: all 76 focused content cases
  pass, including the unchanged self-discard-only regression, ordinary Winds and
  Black Panther Special controls.
- `invocation-admission-independent-rules.log`: all 22 focused lifecycle,
  rules-graph and citation cases pass with the additional query assertions.
- `invocation-admission-independent-hooks.log`: normal review-commit hooks,
  including architecture policy, complexity and file-length checks.

That review verdict covers only the non-choice repair; the subsequent Images
choice repair is recorded below. Neither verdict establishes Vapors behavior,
all Invocations or Doctor Strange admission. Integrated wider checks remain with the delivery lead.


## Independent empty scheme-choice sentence — MARVEL-806

The bounded repair on `fix/invocation-empty-choice`, based on `081c443a`, admits
an empty direct `chooseCard` from schemes with `removeThreat` on its chosen
scheme as an independent sequence step. Whole-ability target admission remains
unchanged: another effect must supply a valid target. The existing choice
executor completes an empty choice without asking a mandatory question. A
nonempty choice still offers only legal schemes and waits for its answer.
`discardInvocation` is classified as independent of player/card selection,
since it always addresses its captured physical source.

Current `ruling:8c9c9d30f4c01189` explicitly includes the Invocation itself as a
target; `ruling:8883bc6b0af1c9b8` limits an ineffective choose instruction to its
own sentence. These results remain consistent with RR 1.8 `rr:target.2`,
`rr:target.3`, `rr:ability.4` and `rr:and.1` (resolve as much as possible).
The test-local 09033 program follows the printed confuse, choose scheme/remove
four, and self-discard structure. The original direct-scheme control remains a
separate simplified admission test.

Ten new nonpublic cases cover both Spell Mastery and Master with ordinary and
last-card decks, no manufactured confusion or threat removal, exact physical
self-discard and intermediate reveal/reset, subsequent top or Master return,
card conservation, one resource discard and event commitment, and a real
positive scheme-choice suspension. An all-ineffective ordinary action stays
unavailable without paying its exhaust cost; an empty independent choice does
not satisfy a dependent `Then`. Nested/dependent choice bodies and other empty
choice effects retain their existing structural admission. This packet does not
implement Vapors status replacement, production Invocation rows or pack admission.

Evidence outside the repository under `C:/Users/mggar/Source/`:

- `invocation-empty-choice-admission-mutant.log`: compiled omission of the
  sequence admission allowance fails five of ten new cases.
- `invocation-empty-choice-binding-mutant.log`: compiled omission of fixed-source
  binding classification fails all four positive chosen-scheme variants.
- `invocation-empty-choice-completion-mutant.log`: explicit empty-choice receipt
  removal is equivalent on these paths; that redundant production change is
  omitted. The dependent-completion control is retained.
- `invocation-empty-choice-restored-build.log` and
  `invocation-empty-choice-restored-tests.log`: restored non-incremental Release
  build (zero warnings/errors) and all 172 focused content cases passed.
- `invocation-empty-choice-rules.log`: all 23 Invocation lifecycle and authority cases passed.
- `invocation-empty-choice-architecture.log` and
  `invocation-empty-choice-hooks.log`: all 89 focused architecture cases passed;
  normal commit hooks run on this packet. The initial hook identified a CCN16
  lifecycle test; its discard/reveal/return assertions are cohesively extracted
  before restored verification and the final normal hook run.

Independent review and integrated wider checks remain with the delivery lead.

Lead closeout restores the RNG assertion to the test scope that owns its baseline
after the lifecycle extraction. The final nonincremental build passes with zero
warnings/errors, and 103 focused Invocation, Master, Spell Mastery, Covert Ops,
existing Special and citation cases pass. These final logs are
`Source/evidence/invocation-empty-choice-root-restored-build.log` and
`invocation-empty-choice-root-focused.log`; earlier 172-case evidence precedes
that test-only extraction. Normal hooks and independent review follow.

Independent review of `bf51629a` against `081c443a` accepts the bounded empty
scheme-choice repair and fixed-source discard binding classification. The
allowance recognizes only a direct schemes query whose body removes threat from
`chosen`; it does not bypass whole-ability target legality, cost payment, or
dependent completion. Neither card ids nor a main-scheme substitution govern it.
The current RR 1.8 target and partial-resolution clauses remain consistent with
the two audited Invocation rulings cited above. No production finding or change
is part of this review.

Three additional synthetic cases in `InvocationEmptyChoiceReviewTests` challenge
that structural boundary with the same exhaust cost and a valid confusion
sibling. The direct empty choice resolves the sibling, exhausts its source and
returns to the root without a prompt; nested removal and dependent-choice bodies
remain unavailable and preserve the complete fixture snapshot. This documents
the present bounded support rather than claiming those broader shapes are fully
implemented.

Independent evidence outside the repository under `C:/Users/mggar/Source/`:

- `invocation-empty-choice-independent-build.log`: nonincremental Release content
  build, zero warnings/errors.
- `invocation-empty-choice-independent-reviewed-content.log`: all 996 affected
  content cases pass, including whole ActionAbility, TargetReference, Invocation,
  Master, Spell Mastery, Covert Ops, Special and citation test classes.
- `invocation-empty-choice-independent-rules.log`: all 19 selected Invocation
  deck, rules-graph and citation cases pass.
- `invocation-empty-choice-independent-hooks.log`: normal review-commit hooks.

This verdict does not implement or accept Vapors status replacement, generic
empty choices, production Invocation rows, Doctor Strange pack admission or a new
native interaction. Integrated wider checks remain with the delivery lead.
