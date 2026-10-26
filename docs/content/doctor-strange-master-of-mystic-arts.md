# Master of the Mystic Arts — MARVEL-798

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
