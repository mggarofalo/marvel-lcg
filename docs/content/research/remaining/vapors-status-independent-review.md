# MARVEL-809 independent engine review

Verdict: accept the bounded engine prerequisite at
`7e2b9c14e4d3ce663c9bc91b0b8a7208094280d1` against `05cb51c2`.
No blocking defect remains in the claimed physical-status replacement and
Invocation continuation flows. This does not admit Doctor Strange, author a
production ability row, or certify a public product journey.

Review branch: `review/vapors-status`, worktree
`C:/Users/mggar/Source/marvel-vapors-review`, 2026-10-26. Changes in this review
are tests and evidence only; no engine, book, printed data, provenance or UI edits.

## Authority and implementation assessment

Printed `09035` chooses a physical status in play, replaces it with a different
kind, and separately discards the Invocation. Current RR1.8 `status-cards.1`,
`status-cards.1.1`, `steady.1`, `stalwart.1`, `cannot`, and `vulnerable.1/.3`
govern the limits and consequences. The older duplicate-cap ruling
`dd639f2e28801fbc` is consistent with ordinary caps; RR1.8's Steady exception
prevents reading that answer as a universal one-card limit. The Ronan cannot
ruling `17ea4e93b0ab8a25` and 2024 Stalwart ruling `66a4e0d2b3a71959` agree
with rejecting prohibited destinations before removing the old status.

The local graph's audited clarification `8883bc6b0af1c9b8` explicitly supersedes
the earlier no-status prohibition. Its 2024 scoped answer permits the separate
self-discard sentence when replacement has no eligible target. No conflict with
the current RR1.8 any-part targeting clauses was found. The 2024 Sinister ruling
`0416488a6eb376e3` distinguishes replacement from giving a status. The reviewed
primitive emits only `Replace_Status` move/create/attach records for replacement;
it does not synthesize a `Give_Status` event or claim Sinister integration.

The rules layer owns physical candidates, live-host checks, distinct destination,
caps and mutation. Cards binds the actual selected object through the existing
choice machinery. Inspection found no `09035` branch in these implementations.
The bounded three-option recognizer filters a physical status whose nested
replacement choices are all illegal. It deliberately recognizes the supported
shape rather than implying arbitrary choice/projection support.

`Statuses.Limit` is the appropriate shared owner for explicit cannot-have-status
restrictions, so ordinary `Inflict` and replacement use the same cap. The three
modifier names are engine spellings and do not themselves implement a printed
restriction card. Their addition does not change registered digest fields. No
production ability currently emits these new names. Future dynamically gained
restriction cards still need their own lifecycle and authority evidence; this
packet does not prove arbitrary removal of already-held statuses on gaining a
restriction.

The compatibility decision is acceptable for this nonpublic prerequisite:
existing serialized event subtypes and the existing string verb carry the new
result; supported production abilities do not invoke it. The internal
`AbilityCardQuery` enum insertion does shift following numeric ordinals, so the
checkpoint's phrase "no enum value" should be read as no serialized wire enum
change, not as a claim that every C# enum ordinal is unchanged. No serialized
use of this compiled query enum was identified. A future public admission still
needs the existing protocol/replay integration gates.

## Distinguishing independent evidence

`StatusReplacementIndependentReviewTests` supplies eight cases on a different
test-local Invocation face (`09036`), explicitly synthetic rather than printed
Winds behavior. Six cover every directed pair of distinct status kinds. A
non-first resolving player selects a later physical status on another player's
identity while an earlier same-kind villain status remains untouched. Assertions
bind the removed id, newly allocated id, attachment host and causal trigger,
exclude `Give_Status`, preserve RNG consumption, and require the Invocation
continuation to reach the root prompt with an empty agenda. Another case uses
explicit cannot restrictions to leave only an independently empty replacement
sentence and proves self-discard without fabricated replacement.

The eighth case preserves an unsupported-projection boundary: a synthetic
`chooseCard(statusCards)` containing `then(replaceStatus, draw)` raises an
explicit projected-resolution exception. Spell Mastery exhaustion has already
committed; the old status and public Invocation top remain, with no fabricated
replacement or draw. An initial review expectation of rollback failed. That
expectation was corrected after reading the established contract in
[`doctor-strange-spell-mastery.md`](../../doctor-strange-spell-mastery.md),
which explicitly permits unsupported Special execution to raise after committed
costs. This is a limitation, not a regression in supported answer rejection.
The rejected assumption remains visible in the external review log; it is not
reported as a passing run or an engine blocker.

Final commands on the reviewed engine sources plus these tests:

- Content Release filter for `StatusReplacementIndependentReviewTests`,
  `VaporsStatusReplacementTests`, `StatusReplacementLoweringTests`, and
  `ActualAbilityCompletionTests`: **33/33 passed**, including eight independent
  cases. Log: `C:/Users/mggar/Source/vapors-independent-final-content.log`.
- Rules Release filter `StatusReplacementTests`: **13/13 passed**. Log:
  `C:/Users/mggar/Source/vapors-independent-rules.log`.
- Normal commit hooks are required without bypass; their result is recorded
  with the review commit. No new engine decision was introduced by this review;
  the implementation packet separately records four compiled mutants.

The initial synthetic dependent-text attempt failed six cases at the explicit
projection boundary (`vapors-independent-projection-initial.log`). The subsequent
rollback expectation failed one of eight cases (`vapors-independent-review.log`).
The final 33-case pass uses the documented unsupported-boundary expectation and
the ordinary supported shape for the six replacement cases. No whole Content,
whole solution, socket, save/restart, native UI, player-comprehension or Sinister
response result is claimed by this review. The earlier abandoned whole Content
run in the implementation checkpoint remains without a result.
