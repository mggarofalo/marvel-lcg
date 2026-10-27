# Ronan two-face independent review

Reviewed source: `abefd5fc53e10676b40163d942f1ad34f7ce0ff4`, against
`ca149e0d7c0079142af8049806fad371d301a3e5`. Verdict: accept the bounded
90003 The Accused and 90005 You Dare Oppose Me? packet. No correctness finding
remains in these two rows. This is not complete Ronan acceptance or admission.

Authority was the joined `datasets/cards/cards.json`, current vendored RR 1.8,
the pinned original described in `ronan-source-pin.md`, and the vendored FAQ
and ruling corpus. Neither face has a specific FAQ answer. RR `alteration-effect`
explicitly uses You Dare Oppose Me? to define the temporary interruption after
each matching discard; `encounter-deck.1` and `.2` require immediate reset and
ending this discard instruction at exhaustion. Current `overkill` requires an
actual ally defeat. No superseded overkill ruling supplies an expectation.

The source's six test cases cover full non-first-player reveal and scheme-Boost
attachment to an alter-ego, attachment retention after Boost cleanup, exact
attack bonus and expiry, an unrelated defeat control and Ronan defeat removal,
ordered physical Kree discard dealing, multiplayer ownership, and overkill
through a completed attack followed by an ordinary attack.

Eight independent cases in `RonanTwoFaceIndependentReviewTests` additionally
distinguish:

- Attack-Boost attachment happens after attack initiation: no retroactive bonus,
  followed by the printed bonus on the next attack.
- The initiation target decides The Accused's bonus before a foreign Iron Man
  defends. Attacking the other identity provides the no-bonus control.
- Exhaustion stops before five discards, resets immediately, and deals only the
  exact matching physical cards from this instruction, including a matching last
  card. A same-face card already in the discard stays in the reset deck.
- Zero matching discards deal no encounter cards.
- A scheme Boost cannot grant overkill during an ordinary scheme or borrow a
  suspended outer attack. A documented synthetic observer pauses the next boost
  so the latter assertion observes the grant's possible lifetime directly.

Evidence is under `C:/Users/mggar/Source/evidence/`:

- `ronan-two-face-independent.log`: original six plus eight independent cases,
  14 passed, zero failed or skipped.
- `ronan-two-face-review-mutant-build.log`: non-incremental compiled mutant,
  exit 0. Only actual 90005's `activationIs` changed from attack to scheme.
- `ronan-two-face-review-mutant.log`: both scheme cases failed. The nested case
  observed forbidden overkill at its exact assertion; the ordinary scheme raised
  the explicit outside-named-period error. This kills the wrong activation guard.
- `ronan-two-face-review-restored-build.log`: exact original bytes restored,
  non-incremental build exit 0.
- `ronan-two-face-review-final.log`: original 84-case focused filter plus the
  eight independent cases, 92 passed, zero failed or skipped. The base filter is
  `ronan-remaining-filter.txt`; its hash is recorded in the source checkpoint.

The restored ability book SHA-256 is
`9411a9fd446fb0c50d0957d9f214bb0a67a51ffb0d41e658dc4501662334bee8`.
No production, setup, engine, or admission file changes belong to this review.
90002 and 90004 remain absent in the reviewed source. Complete Kree Fanatic
delivery and public admission remain separate work.
