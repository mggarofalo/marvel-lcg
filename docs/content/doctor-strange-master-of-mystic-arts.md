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

A genuine Images `chooseCard` version remains a blocking choice-handling finding:
`CanInitiateSequence` requires every child's `CanInitiate`, and
`CanInitiateChooseCard` rejects the zero-threat scheme choice even when a sibling
self-discard has valid target legality. The whole Special is skipped. This separate
finding must be resolved before complete Images/Vapors coverage or Doctor Strange
readiness is claimed. Its reproducible test source and four failing cases are
preserved outside the repository in `C:/Users/mggar/Source/invocation-empty-choice-review.cs`
and `invocation-empty-choice-review.log`; the delivery lead owns follow-up.

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

Independent review and integrated wider checks remain with the delivery lead.
