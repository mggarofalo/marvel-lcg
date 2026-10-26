# Independent review of individual boost cancellation

Reviewed engine prerequisite `59715a540a563bf5cf9f0a84f0be5b225a990869` in isolated
worktree `C:/Users/mggar/Source/marvel-preemptive-review`, branch
`review/preemptive-boost`, on 2026-10-26. Verdict: **changes required**.
No authored ability-book row or public admission for 05014 is included.

## Confirmed finding

`BoostIconCancellation.Remaining` captures the count before any faceup interrupt
and does not reflect an earlier interrupt changing that card's boost icons.
The new synthetic `EarlierFaceupInterruptChangesTheIconsCancelledAndDamage`
regression completes a costed, exhausting faceup interrupt first and proves the
boost card's modified count is respectively 3 or 1. Preemptive Strike then deals
2 damage in both cases, rather than 3 or 1. The +1 case also leaves one boost
icon contributing to the attack instead of cancelling every pre-star icon.

Authority is the raw joined 05014 text in `datasets/cards/cards.json` (cancel all
icons; damage for each cancelled this way) and vendored ruling
`937d6ff4f970a1da`: Crest can modify the counted icons before Preemptive Strike,
which deals additional damage for the added/cancelled icon. This control tests
the common faceup-interrupt prerequisite, without claiming to implement Crest's
separate count-triggered timing or admitting that card.

Rulings `1ea208998735ef4b` and `33d8c914aa86d19f` were also reviewed: constants
and amplify precede faceup interrupts, star effects follow them, star-added icons
survive cancellation, and already cancelled icons cannot be cancelled again.
The repair must update live admission/projection and cancellation consistently,
while retaining actual cancelled quantities separately from later star changes.

## Evidence

All logs are outside the repository, under `C:/Users/mggar/Source/`.

- `preemptive-review-new-tests-final.log`: all six new review cases compile;
  **4 pass, 2 fail** for the confirmed +1/-1 quantity finding. Passing controls
  cover a second event unable to cancel the same icons, a Boost-requested
  deferred attack keeping its own boost card and icons, a boost card entering
  play through its star and remaining there, and stale hand payment rejection.
- The stale-payment control compares card digest, complete RNG state and
  consumption, current occurrence, pending frames, activation/attack and event
  trace before rejection, then compares the completed run with an unprobed
  control. It does not claim a dedicated hidden-information-signal inspection.
- `preemptive-review-baseline.log`: focused original cases, citation checks and
  the initial five review cases: **26 pass, 2 fail**. The later sixth zone-change
  control is recorded separately above.
- `preemptive-review-mutant-activation.log`: compiled bug-restoring mutant removes
  the exact activation-ID equality in `AttackBoost.MatchesActivation`.
  `CompletionFromAnotherActivationCannotContributeToTheSameEnemy` kills it:
  an unexpected `BoostResolved` contributes 2 icons to the same enemy under a
  different activation ID. The mutant run exits 1 for the intended assertion.
- Production source was restored in a `finally` block. A subsequent explicit
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-restore --no-incremental` passes (`preemptive-review-restored-build.log`).
  The same completion control and original focused/citation set then pass:
  **23 passed**, `preemptive-review-restored-focused.log`.

The review does not establish full managed, native or cross-platform acceptance.
The existing star-addition, constants/amplify, zero-icon, multiplayer defense and
stale completion controls were read and run in the focused set. No production,
agenda, runner, ability-book or version change is part of this review packet.
