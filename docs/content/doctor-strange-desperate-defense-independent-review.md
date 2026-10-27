# Desperate Defense independent engine review (MARVEL-833)

Reviewed prerequisite revision `011e9bdbebce18ef488da9a8ed3055d1b629f710`
for MARVEL-824 in isolated branch `review/desperate-defense`.

Verdict: accept the scoped engine/DSL mechanism. The printed 09015 cost-1
Hero Interrupt grants +2 DEF for that attack and readies the defending hero only
if that attack dealt it no damage. Current RR 1.8 defense and delayed-effect
clauses govern; no historical ruling overrides them. This review does not admit
09015 or Doctor Strange, change the production ability book, or accept native UI.

The source binds a persisted World.Effects registration to its exact activation
and original hero incarnation. Its query admission requires the live committed
hero-defense occurrence, matching hero, enemy and activation. Attack-placement
receipts preserve damage attribution independently of boost/ability damage and
current accumulated damage. Source tests establish both-seat costs and defense,
zero/positive damage, boost damage, expiry, stale input, query purity (baseline
before query, full RNG, pending agenda/effects and unprobed subsequent control),
and hero departure/reentry. Independent source inspection found no extra mutable
side table or Runner/Agenda change.

Four new independent cases add Tough prevention with existing hero damage,
with and without a live ready prohibition; enemy departure/reentry rejecting
stale defense admission plus unused activation cleanup; and a genuinely queued
second enemy attack. For the last case a documented synthetic Armored Vest
ability requests enemyAttacks during the defense interrupt. Actual-text 09015
pays and resolves through the complete production book. The normal scheduler
finishes the original attack, readies its hero, then starts the queued villain
attack; the hero can defend again, takes the separate boosted damage, and stays
exhausted. Both attacks complete and the agenda drains. This replaces the
source packet's direct-notification-only evidence for queued activation isolation.
The synthetic vest is not a claim about its printed ability or public setup.

A compiled mutation removing only ReadinessPermissions.CanReady survives because
the AbilityRunner readiness contract independently enforces the same prohibition.
That mutation is equivalent for this supported route. A second compiled mutant
removes both readiness permission checks; the prohibited=true case fails with a
wrongly ready hero while the other three cases pass. Sources are restored with
fresh timestamps, then non-incrementally rebuilt before final focused tests.

Evidence (Release, Windows; logs outside the repository in C:/Users/mggar/Source/):

- desperate-defense-review-initial.log: independent baseline 4/4.
- desperate-defense-review-mutant-readiness.log: single-check equivalent mutant 4/4.
- desperate-defense-review-mutant-readiness-both.log: distinguishing compiled mutant killed.
- desperate-defense-review-restored-build.log: forced restored build.
- desperate-defense-review-restored-tests.log: whole DesperateDefense, ExpertDefense,
  HeroDefense, DoctorStrangeUnflappable and RuleCitation classes.
- desperate-defense-review-rules.log: whole ContinuousEffectTests class.

Integration follow-up resolved: `docs/card-dsl.md` now documents
`readyUndamagedDefender`, including its fixed spelling, committed defense,
exact attack and original hero incarnation.
The original `~AbilityDataTests` filter did not match the split concrete classes;
its passing result did not establish dataset-vocabulary coverage. The lead's
integrated `~AbilityData` run passes all 33 cases on the 679-row candidate
(`strange-679-complete-ability-data.log`). This closes the documentation and
coverage follow-up, not an engine defect. Defense-labeled initiation is covered
by the existing complete HeroDefense classes; a new 09015-specific labeled-defense
transcript is not claimed. No full Content suite, public setup/save transcript,
full suite, platform certification or native acceptance was run.

Final Content result: Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:    53, Duration: 10 s - Marvel.Content.Tests.dll (net8.0)
Final Rules result: Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 160 ms - Marvel.Rules.Tests.dll (net8.0)
