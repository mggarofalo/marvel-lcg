# Kang retained-group validation repair

MARVEL-858 under MARVEL-568. This repairs the specific suspended-choice finding
in [the independent coincident-departure review](kang-coincident-departures-independent-review.md)
of `3f4dbc0a571deaccb1fdc8c89a82a8c1dbbd47d9`. It remains a bounded nonpublic
capability change; it does not admit complete Kang or change production content.

Captured-group validation is an engine admission choice. A join validates its
selected departure and every retained departure before generating a choice,
accepting a destination or unique discard, or moving play areas. Ordering and
scheduling validate the complete retained batch too, including before selected
completion cards are removed. The managed join still extends only the capture
whose source is its destination; scheduling validates those updated captures
after the join. An uncontrolled change cannot acquire that extension.

The independent regression is versioned unchanged as
`tests/Marvel.Content.Tests/Play/KangRetainedCaptureReviewTests.cs`. It uses
nonempty player decks and requires refusal before the unique discard with the
card state, topology, events, agenda and RNG unchanged. Additional synthetic
probes change a third retained group while a different source/destination pair
is suspended. They cover destination-answer rejection and work before both a
unique-copy prompt and a physical join without a unique conflict. Their
snapshots also retain complete pending operation/frame descriptions.

Windows verification used `MSBUILDDISABLENODEREUSE=1`, serial `-m:1`, and
nonincremental Release Content test-project builds. The repaired build passed
with zero warnings/errors and the complete `FullyQualifiedName~Kang` filter
passed 152/152 cases. Existing one-to-four-player Kang cases, including managed
destination rebasing and explicit coincident ordering, are the positive controls.

A separately compiled mutant restored selected-departure-only validation in
`KangJoinProcedure.RequireOwner`, leaving the other batch guards present. All
four `FullyQualifiedName~KangRetained` cases failed: the suspended unique answer
and destination answer committed instead of refusing, the unique query offered
another prompt, and the no-conflict join changed topology before scheduling
refused the stale capture. The last case's full snapshot distinguishes timely
refusal from merely throwing after mutation. Exact source bytes were restored
before a fresh nonincremental build (zero warnings/errors) and another 152/152
passing Kang run.

Raw logs are outside the repository under `C:/Users/mggar/Source/evidence/`:
`kang-retained-build.log`, `kang-retained-focused.log`,
`kang-retained-mutant-build.log`, `kang-retained-mutant.log`,
`kang-retained-restored-build.log` and `kang-retained-restored-focused.log`.
The unchanged imported regression and external original have identical SHA256.
Normal commit hooks are recorded separately in `kang-retained-commit.log`.

This packet does not cover eliminated players, complete Kang, competing delayed
effects, public setup/session/replay, native interaction, Linux or broad managed
gates. Independent closure review remains separate from source-author evidence.
