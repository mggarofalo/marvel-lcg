# Independent review of individual boost cancellation

Reviewed engine prerequisite `59715a540a563bf5cf9f0a84f0be5b225a990869` in isolated
worktree `C:/Users/mggar/Source/marvel-preemptive-review`, branch
`review/preemptive-boost`, on 2026-10-26. Verdict: **changes required**.
No authored ability-book row or public admission for 05014 is included.

## Live-count repair candidate, 2026-10-26

The repair on `fix/preemptive-live-icons`, based on review commit `2c69a87d`,
makes `Remaining` read the card's current modified numeric icons and amplify,
less the actual quantity cancelled by earlier effects. Discovery, dependent
quantity projection, preparation and commitment share that value. The final
Boost contribution uses the same calculation after the star resolves, so later
star-added icons contribute without changing damage already dealt for cancellation.
Exact world, activation, incarnation and outstanding occurrence checks remain.

The two earlier-interrupt regressions now pass for +1 and -1 changes. Added
zero and over-reduced controls keep the faceup window open, establish zero live
icons, withdraw the cancellation offer, retain the event/payment in hand and
complete the attack with no boost contribution. Direct Rules component callers
now run scheduled faceup work and explicit defender decisions; their order,
numeric contribution, damage accounting and discard-routing assertions remain.
An encounter reserve keeps the two-card fixtures from exhausting their deck.

Focused Windows Release evidence outside the repository:

- `C:/Users/mggar/Source/preemptive-live-icons-final-content.log`: **35 pass**,
  covering cancellation, independent-review controls, existing boost/activation,
  Green Goblin, admission and citation tests.
- `C:/Users/mggar/Source/preemptive-live-icons-rules-focused.log`: **53 pass**,
  the full Rules `FullyQualifiedName~Boost` selection plus rule citations.
- `C:/Users/mggar/Source/preemptive-live-icons-mutant-frozen.log`: a compiled
  mutant freezes the initial total again; all four +1/-1/zero/over-reduced
  controls fail at their live-count assertions, with exit 1.
- Production source was restored in `finally`, followed by an explicit
  `--no-incremental` Release build: zero warnings/errors in
  `preemptive-live-icons-restored-build.log`. The restored 26-case focused set
  passes in `preemptive-live-icons-restored-focused.log`; the broader sets above
  subsequently pass against restored production source.

This is a repair candidate awaiting independent review and integration gates;
these focused results do not establish full managed or cross-platform acceptance.

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
