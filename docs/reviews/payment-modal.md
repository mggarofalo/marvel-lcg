# Card payment review — MARVEL-440

Reviewed 2026-09-29 on `feat/marvel-440-payment-modal`, based on PR #397
(`6014cfeb`). The review used the working Debug build before its initial commit.
The scope is card payment, the shared Pass control and hand-card stacking.
This is not approval of the complete table redesign.

## Independent real-client review

A separate reviewer operated the engine-backed app using game goals rather
than a click script. The app was at 80% interface scale. Native captures were
960×572; the reviewer did not infer logical viewport dimensions from the
scaled captures.

The final scoped verdict was **pass**. The reviewer could explain cost,
source contributions, reversible selection and explicit commitment from the
modal, then perform these tasks:

- Drag and click a Web-Shooter play; use Peter Parker’s Scientist resource
  through the keyboard and commit it.
- Select hand discards, cancel with Escape, verify no discard happened and
  reopen from restored keyboard focus.
- Play Swinging Web Kick with Web-Shooter plus two hand discards. Two sources
  left payment incomplete; the third enabled confirmation. After committing,
  Web-Shooter was exhausted with two web counters, the two hand cards were
  discarded, and choosing Rhino resolved eight damage before the event was
  discarded.
- Read that further effect choices follow payment before committing.
- Use the fully visible Pass button in the shared decision area.
- Inspect the resting and raised hand fan without lower-card contents showing
  through cards above them.

Review findings fixed and retested: event plays bypassed the modal; payment
copy did not explain subsequent effect choices; Pass was clipped by its panel.
Intermediate drag frames, narrow layouts, other interface scales and broader
interface comprehension were not independently reviewed.

## Automated evidence

Managed tests cover card-play intent independent of ability names, round-trip
transport of that marker, absence of card-play connectors, reversible mixed
payment, insufficient payment, stale bindings and visibility-safe source copy.
The existing Core event and Web-Shooter tests cover actual payment effects.
Native checks exercise real pointer and keyboard input, cancellation, modal
focus, mixed Black Cat payment, access to synchronization during a locked
payment, Pass placement and fan stacking. The normal repository gates run
these through the supported scale/motion/player matrix.

Two targeted manual mutations invert the modal marker and hand-discard source
classification; the focused tests must fail for each. Mutated source is
restored before the final gates.

## Correctness review

Two independent read-only reviewers found an error-recovery blocker: a locked
payment modal obscured Synchronize and could survive return to Join. Locked
submission now closes the modal while retaining the draft and submission
latch; abandoning a session clears its obsolete decision. Both reviewers
verified the fix and reported no remaining concrete defects in scope.

The engine explicitly marks event and ordinary card-play offers. Protocol 18
carries that live contract. Saved prompt records do not contain this marker;
state fingerprints and saved replay comparisons are unchanged.
