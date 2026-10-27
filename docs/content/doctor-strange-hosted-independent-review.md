# Independent hosted Invocation return review

MARVEL-838 review of original source commit
`35bb59957a7425b102fe3fc4d0f748f98c439a44`, applied independently to final Astral
runtime `ed03a479` as local `3cca7a05`. Only the new independent test and this
review record belong to the review commit.

The production DatasetGameFactory observer never prepares or mutates gameplay
state. The real public seed-7 Rhino specification has Captain Marvel in seat 0,
Doctor Strange in seat 1, and no modular set. The independent test pins the
hosted checkpoint to 14 accepted decisions with six threat on 09029.

Reviewed printed authority in `datasets/cards/cards.json`: 09029 places the top
Invocation facedown beneath itself when revealed and shuffles that card into
the Invocation deck when defeated. 09003 costs two and removes three threat
before looking at the encounter top and applying any boost-dependent addition.
The vendored MarvelCDB FAQ queried both codes and contains no entry for either.

The original source proof checks the exact original hosted physical Invocation,
owner 1, unchanged incarnation, ordered movement/detachment/shuffle/reveal/scheme
departure, all five physical Invocations, and independent restarts at hosted
and completed checkpoints. The restart helper round-trips strict durable JSON,
creates distinct game/world instances, compares RNG state and consumption,
digest, prompt, revision, board, history and continuation records/events, and
rejects stale decisions without changing stored bytes.

Two evidence gaps are closed by `PublicDoctorStrangeHostedReturnIndependentTests`:

- Astral is required to be an actual root commitment of a naturally held card,
  paid through engine offers, targeting only 09029. An unpaid attempt is rejected
  without changing durable bytes. The continuation must contain the exact
  six-to-three `Remove_Threat` result with Strange as the thwart actor; the played
  physical Astral must reach Strange's ordinary discard. Engine cause source
  identifies the thwart actor, not the event card.
- Exactly one Invocation face may be visible, with the exact public-top physical
  and printed identity; every other card has neither identifier nor face. This
  production View check runs for None and restricted seats 0/1 at both checkpoints
  before and after restart. It rejects an extra leaked face that the original
  helper's expected-top-only predicate would miss.

The independent proof also checks distinct host/store/factory instances, full
StoredSession bytes after each accepted continuation, and returned Invocation
owner/incarnation after the completed restart. No authenticated spectator,
restricted-seat transport, query purity, native UI, historical-save compatibility
or complete pack acceptance is claimed.

Validation uses Release and the complete PublicDoctorStrangeHostedReturnTests,
PublicDoctorStrangeHostedReturnIndependentTests, PublicDoctorStrangeRestartTests
and PublicDoctorStrangeRemainingRestartTests classes. Early fixture failures and
an overlapping apphost-copy build failure are diagnostic runs, not passing evidence.

Verdict: accepted for the bounded hosted-return proof. All nine tests passed.
Final log: C:/Users/mggar/Source/strange-hosted-independent-final-validation.log.
Normal pre-commit architecture, complexity and file-length hooks are mandatory.
