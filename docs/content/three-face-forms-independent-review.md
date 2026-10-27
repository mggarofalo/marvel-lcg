# Explicit three-face selection independent review — MARVEL-861

Reviewed source: `9a3339827661c12b1ee0be183c17df04cfd3822b`.
Independent worktree: `C:/Users/mggar/Source/marvel-three-face-review`.
Review date: 2026-10-27. This record accompanies the review-only commit.

## Verdict and authority

Accepted for the bounded, nonpublic engine prerequisite: explicit selection of
the other physical identity face, voluntary-limit admission, and scheduled form
occurrences. No production correctness finding was identified in that scope.
This is not acceptance or admission of Ant-Man, Wasp, their abilities, a public
game journey, replay/archive compatibility, or a client form selector.

Reviewed the implementation diff, existing tests, root/src/tests guidance,
forms/timing/affordances documentation, the implementation evidence record and
the 569–574 research/inventory. Authority is the generated actual identity
trios `12001a/b/c` and `13001a/b/c`, mc12/mc13 Rules Clarifications and current
RR 1.8 form/flip clauses. Each trio remains one physical identity card.
The clarification permits every directed transition between its three faces.
The historical Taskmaster ruling corroborates hero-to-hero qualification and
does not displace the current clauses or its printed response.

## Independent challenges

`ThreeFaceFormReviewTests` adds twelve cases on manually constructed nonpublic
boards, without authoring or admitting either pack:

- Both physical identities expose exact Tiny/Giant destination labels and
  distinguishable printed size names. Only the active seat receives them;
  answer assessment rejects the other seat for either offered handle.
- Identical seeded worlds and answers produce identical offers/handles,
  completed events, next offers, digest and RNG state.
- The real voluntary `Game.Resolve` path reaches authored Taskmaster after
  Tiny → Giant for each identity: exactly three damage and the exact top boost
  discarded. Giant → alter-ego deals zero and leaves that boost in the deck.
  Both return to the active seat, spend the receipt and offer no second change;
  further scheduler work does not repeat Taskmaster or consume the next card.
- Unknown, now-current, spent-receipt and prohibited selections preserve the
  same prompt, digest, RNG, agenda, voluntary receipt and pending information
  signal on rejection.

The existing twelve actual-face transition cases cover all legal destinations;
existing tests also cover card preservation and invalid direct destinations.
Source inspection confirms the two-face branch calls the identical anchored
offer allocator and retains its old label/toggle. Existing two-face and player
phase regression tests passed. No wire/digest/RNG field was added.

## Executed evidence and limits

Affected Content tests plus citation checks: **100 passed, zero failed**.
Command filter: `ThreeFaceForm|FormDataTests|PlayerPhaseTests|TaskmasterAbilityTests|AllTiedUpProhibitionTests|RuleCitationTests`.
Log: `C:/Users/mggar/Source/three-face-independent-review-final.log`.
The initial review run failed two size-name assertions because the fixture
expected title case while printed traits are uppercase; the fixture was
corrected to the generated printed values. The operation tests passed then.

No production source or authored data changed during review. No additional
mutant was necessary for these review-only tests; the implementation packet
records its compiled destination/limit mutants separately. The published
immediate DSL timing defect and face-specific DSL/projection work remain
separate MARVEL-862 dependencies. Native UI, public admission and full saved
three-face game journeys were not exercised or accepted here.
