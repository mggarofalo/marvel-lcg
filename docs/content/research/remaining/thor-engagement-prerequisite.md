# Thor engagement prerequisite — MARVEL-741 / MARVEL-563

Base: `7e352dbc`. This packet adds a nonpublic engine capability; Thor is not
admitted by this work. Compatibility remains v25 / protocol 39.

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

Quickstrike finishes before the optional engagement response for reveal, ordinary
non-reveal entry and transfer. Teamwork remains entry-specific. Existing ability
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

Remaining: independent review; complete Defender discard-until/cost composition;
full Get Over Here event initiation/payment and stale-cost controls; replay/save
coverage of engagement response suspension; full pack abilities/setup/admission.
No public Thor gameplay, native/UI acceptance or complete-pack readiness is claimed.
