# Thor engagement prerequisite — MARVEL-741 / MARVEL-563

Base: `7e352dbc`. This packet adds a nonpublic engine capability; Thor is not
admitted by this work. Independent review of `69459de2` identifies a Core timing
change requiring a new replay compatibility decision before integration; the
packet's unchanged v25 identity does not yet account for that change.

Authority: generated `06001a` draws two after engaging a minion, once per phase;
`rr:engage` distinguishes entry from re-engagement; `.2` makes either direction
of the engagement relationship equivalent; `.3` excludes same-player engagement.
The vendored `06014` MarvelCDB FAQ confirms that exclusion. `rr:limit` assigns
the phase limit to the ability instance. `rr:quickstrike.2` places its forced
attack after When Revealed text, before optional responses under `rr:ability`.
The printed `06003` and `06014` texts and the Thor section of `563-568.md` were
reviewed. `thor-starter-source.md` resolves the starter source question separately.

The engine chooses `WhenMinionEngaged` as a condition. Its occurrence retains the
physical minion in `Subject` and destination seat in `Player`. Actual minion entry
adds this condition to its entry/reveal occurrence; revealing a treachery does
not. A transferred minion uses the existing retained-card entry procedure and
continuation, but gains neither an enters-play condition nor entry state resets.
A same-seat instruction completes with zero movement and no engagement condition.
Discard-until first-player placement now uses this procedure as well.

Quickstrike finishes before the optional engagement response for reveal and direct
non-reveal entry. The interpreted-transfer correction is recorded below. Teamwork
remains entry-specific. Existing ability
limits retain phase reset ownership; no Thor-specific use counter was introduced.

`ThorEngagementTests` uses local ability bodies only. It covers both Thor seats,
other-player entry, reveal versus non-reveal entry, exact subject/seat facts,
transfer, damage/exhaustion retention, same-seat no-op, repeated phase-limited
engagements, both phase resets, Quickstrike after reveal text and before Thor,
the Aerial condition in a synthetic Get Over Here body, treachery exclusion and
query purity followed by comparison with an unqueried control. The synthetic
06003 body isolates its placement instruction; it is not complete Defender text.

Validation:

- Thor-specific tests: 14 passing.
- Final focused entry, continuation, phase-limit, encounter-search and discard
  tests: 158 passing.
- Final full Content suite: 5295 passing. The encounter-search assertions retain
  their physical-card and event-order checks after draining scheduled entry.
- Full Rules suite: 1098 passing.
- Architecture tests: 129 passing.
- Compiled mutant suppressing entry engagement: killed by four entry/reveal cases.
- Compiled mutant allowing same-seat movement: killed by the independent no-op
  control. Both sources restored, followed by a forced Release rebuild with zero
  warnings/errors.

Engagement recording belongs to `MinionEngagement.RecordEntry`; reveal placement
delegates that responsibility without changing complexity thresholds or baselines.
Logs are preserved outside the checkout in the sibling
`marvel-thor-engagement-evidence` directory.

## Independent engine review

Review checkout: `marvel-thor-engagement-review`, based on `69459de2`.
`ThorEngagementReviewTests` adds four distinguishing cases. On that revision,
three pass and interpreted transfer fails; this is a rejected candidate until
the ordering finding is fixed and the compatibility work is complete.

1. **Interpreted transfer exposes Thor before Quickstrike.** A synthetic placement
   body transfers the existing Vulture from seat 1 to Thor at seat 0. Thor's
   response is offered with zero identity damage while the attack remains after
   the entry frame. `rr:quickstrike.1` defines Quickstrike as a forced response;
   `rr:ability.step.4` orders forced responses before optional responses. The
   direct-scheduling control passes, isolating the failure to interpreted entry
   composition. The passing control also proves no Core Hawkeye entry response
   or synthetic When Revealed damage, and retains the minion's incarnation,
   instance state, damage, exhaustion, counters and confused status.
2. **Core ordering changes from `7e352dbc`.** Real authored Core Hawkeye and
   Vulture now reach Hawkeye's optional entry response after Vulture has dealt
   three Quickstrike damage, for both reveal and non-reveal entry. The same
   tests compile and fail on `7e352dbc`, where damage is still zero when Hawkeye
   is offered. This is authority-correct behavior on the candidate, but changes
   an existing supported Core decision trace. `docs/release-policy.md` requires
   an engine replay contract change when the same setup and decisions can
   resolve differently. No version files or historical expected facts are
   changed by this review.

The three adapted Core encounter-search cases retain their original exact-card,
discard-order, destination and event-order assertions; draining scheduled entry
does not weaken those observations.

Evidence outside the checkout, under `marvel-thor-engagement-evidence`:
`review-candidate-red.log` (3 pass / 1 fail), `review-transfer-red.log` (diagnostic
pending-frame evidence), and `review-baseline-hawkeye.log` (2 compiled failures
on `7e352dbc`). No engine fix or final acceptance is claimed by this review.

## Interpreted transfer timing correction

Correction checkout: `marvel-thor-transfer-fix`, based on review commit `dd609c45`.
`Agenda.BeforeResponses` includes scheduled child frames inheriting the applying
procedure's execution owner. The guard continues to exclude frames belonging to
another invocation. The transfer's Quickstrike therefore completes before Thor's
optional response without changing the retained card, entry conditions or reveal
semantics. No ability data or public admission changes are included.

The four independent review cases pass on the correction. Scheduler controls
distinguish same-execution children from another invocation. A synthetic placement
follow-up observes suspension through Quickstrike defense and Thor's response for
both initial entry and transfer. Validation logs live outside the checkout in
`marvel-thor-transfer-fix-evidence`:

- Restored nonincremental full solution Release build: zero warnings/errors.
- Full Content suite: 5,301 passing; full Rules suite: 1,100 passing.
- Architecture suite: 129 passing.
- Focused entry, review and suspension controls: 11 passing.
- Compiled inherited-child exclusion mutant: killed by the same-execution control.
- Compiled ownership-guard removal mutant: killed by the other-invocation control.
- Both mutants restored before the full rebuild and final passing suites.

Independent review remains required.

The Core Hawkeye timing change identified above remains a replay compatibility
change. This timing correction does not establish v25 compatibility or implement
the separately required replay v26 decision and route proof.

Remaining: independent timing retest; engine replay
compatibility decision and route proof; complete Defender discard-until/cost composition;
full Get Over Here event initiation/payment and stale-cost controls; replay/save
coverage of engagement response suspension; full pack abilities/setup/admission.
No public Thor gameplay, native/UI acceptance or complete-pack readiness is claimed.
