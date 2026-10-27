# Seven Rings recipient prerequisite (MARVEL-815)

Base: `36628a0b`. Scope: engine/DSL capability with test-local printed 09034;
no production ability row, public admission or native-client delivery.

Authority: `datasets/cards/cards.json` 09034 gives up to three characters each
Tough, then independently discards itself. Current RR 1.8
`rr:choose-game-element.3.1` prohibits duplicate targets; `rr:status-cards.1`
caps each status; `rr:target.3.5` excludes cannot restrictions. Vendored ruling
`8c9c9d30f4c01189` preserves independent Invocation self-discard. Existing
Master of the Mystic Arts lifecycle owns discard, new-top reveal, recycle and
exact physical return; this packet reuses that lifecycle without modifying it.

The probe found a sufficient existing `TargetRequest` min/max/distinctness
contract, but no status group execution: `chooseCard` is singular and
`chooseCardsToHand` specifically moves out-of-play cards. The shared bounded
`chooseStatusRecipients` instruction reuses the ordinary continuation and group
request, selects only legal in-play characters via existing status limits, and
commits a complete subset in physical-id order. Empty candidates skip the
instruction; an explicit empty subset finishes with no successful status effect.
The following independent discard remains part of the Invocation Special.

## Evidence

- `SevenRingsRecipientTests`: zero/one/three recipients, enemies, both arrows,
  resolving seat 1, original top retained until complete selection, exact physical
  discard, reveal, recycle and return, existing Tough/cannot restriction, and
  duplicate/too-many/noncharacter/stale/wrong-offer/resource rejection preserving
  digest, RNG and pending work.
- `StatusRecipientContractTests`: malformed lowering; offer and continuation
  query purity from pre-query baselines; subsequent causal events match an
  unprobed control even with reversed submitted group order.
- Compiled maximum-plus-one mutant failed the four-recipient rejection test.
- Compiled ignore-status-limit mutant failed both no-meaningful-recipient cases
  by suspending before the independent discard. Both mutants were restored.
- Restored nonincremental Release build: green, zero warnings/errors.
- Bounded whole-class run: 66 passed, including both new classes and existing
  InvocationEmptyChoice/Review, InvocationSelfDiscardAdmission, Vapors replacement,
  SpellMastery and matching MasterMystic classes. No full Content suite was run.
- Logs are outside the repository at `%LOCALAPPDATA%/Temp/seven-*.log`.

## Remaining integration

Independent review must assess raw authority, diff and distinguishing controls.
The eventual production row and pack admission are separate delivery work.
View/Decisions/Protocol must carry complete subset constraints, resolving seat,
causal source, paid costs, status consequences and independent discard/return
context. Native staging/unstaging, explicit zero confirmation, clear commitment,
multiplayer authority, stale-answer recovery and visible comprehension remain
unverified downstream obligations. Engine tests establish none of that product
acceptance or public replay/save compatibility.

## Independent prerequisite review (2026-10-26)

Reviewed source `8733cc9b1848f82c84bd0663cd3194f553e7a01b` against
`36628a0b` in the separate `review/seven-rings` worktree. Verdict: approve this
bounded engine/DSL prerequisite; no correctness findings in the reviewed scope.
The printed 09034 text, current RR 1.8 choose/target/status clauses and vendored
Invocation self-discard ruling agree. The vendored MarvelCDB FAQ records 09034
as queried with no card-specific entry; no older ruling overrides these current
status limits. The new engine path contains no 09034 face switch, reuses
`TargetRequest.Allows` for complete-group validation, and delegates status
capacity and mutation to `Statuses.Count`/`Limit` and `Reveal.Afflict`.

`SevenRingsIndependentReviewTests` adds 20 independent cases:

- Exactly two recipients, including an enemy and another player's identity,
  through both arrows with and without the last-card recycle. Each selected
  host receives the matching physical created/attached status pair before
  exactly one Invocation self-discard; only Master returns the physical card.
  A shuffled card may naturally become top again, so return is distinguished by
  the explicit return event rather than by assuming a different shuffled top.
- Synthetic generic-status controls prove ordinary and Steady capacities for
  Stunned/Confused, Stalwart exclusions, Tough's independence from those two
  keywords, and explicit cannot-have-status restrictions.
- A formerly offered second recipient becomes forbidden before submission.
  The entire two-recipient answer rejects without giving the first a status,
  preserving the digest, RNG, pending work and prompt; a valid retry completes.

Independent compiled mutant: count Tough cards instead of the requested status
in recipient admission. Four new ordinary/Steady Stunned/Confused capacity cases
failed on the intended candidate-legality assertion (16 passed, 4 failed).
The mutant was restored byte-for-byte. A forced nonincremental Release build of
`Marvel.Content.Tests.csproj` passed with zero warnings/errors, followed by 111
passing cases, zero skipped, using whole-class name filters for `SevenRings`,
`StatusRecipientContractTests`, `InvocationEmptyChoice`,
`InvocationSelfDiscardAdmissionTests`, `VaporsStatusReplacementTests`,
`SpellMastery`, `MasterOfMysticArts` and
`StatusReplacementIndependentReviewTests`.
Logs: `%LOCALAPPDATA%/Temp/seven-independent-tests.log`,
`seven-independent-mutant.log`, `seven-independent-restored-build.log` and
`seven-independent-restored-tests.log`.

This review changes only its independent test file and this evidence record.
It does not accept production-book authoring, pack admission, hosted Invocation
integration, public replay/save compatibility, downstream semantic contracts or
native interaction/product readiness. Those remaining obligations above stand.
