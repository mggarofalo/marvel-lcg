# Desperate Defense prerequisite (MARVEL-824)

Candidate based on `cfec0321`, branch `feat/desperate-defense-prerequisite`.
The production book remains at 675 authored faces, including 30 Doctor Strange
faces. This packet does not author 09015, admit the pack, change authority data,
or change Runner/Agenda. `DesperateDefensePrerequisiteFixture` appends one
synthetic actual-text candidate to the complete production book.

The authority is generated `datasets/cards/cards.json`, face 09015: a cost-1
Hero Interrupt (defense) when your hero defends, granting +2 DEF for that attack,
then readying your hero if you take no damage from that attack. The vendored
MarvelCDB FAQ records 09015 as queried with no entry. RR 1.8
`rr:defend-defense.2` requires exhaustion for basic defense; `.4.2` also permits
the committed defense condition from a defense-labeled ability. The current
`WhenHeroDefends` mechanism already implements that distinction. The local
ruling corpus's Desperate Defense examples place its interrupt after basic
defense exhaustion; no historical wording overrides the current RR.

Existing `grantUntil` faithfully supplies +2 DEF with activation-bound
`EndOfAttack` expiry. The existing delayed DSL supports discard and stun, but
cannot express this delayed ready. The new engine-chosen spelling is
`readyUndamagedDefender: "you"`. Its Rules-owned admission requires this seat's
live committed hero-defense occurrence, physical hero/enemy incarnations and
owning activation. It registers `ReadyUndamagedDefender` in `World.Effects`,
retaining the hero incarnation and activation ID. It does not use a process
side table or callback. Session saves reconstruct this deterministic pending
state through accepted-decision replay; digest v3 continues to describe cards.

The instruction resolves through `DelayedEffects` at `WhenAttackEnds`, before
responses (`rr:delayed-effect.1`), after the ordinary DEF modifier expires. It
reads the original hero's positive attack-placement receipts, including
prevention results, rather than accumulated hero damage or activation-wide
boost damage. Separate boost/ability damage cannot disqualify the ready. The
captured hero must still be the same in-play copy and pass existing readiness
permissions. The activation completion sentinel retires unused registrations.

Evidence, all Release configuration on Windows:

- Actual-text cases cover both seats, defense commitment/exhaustion, paid cost,
  positive versus zero attack damage, Concussive Blast damage followed by zero
  attack damage, ally/other-seat rejection, +2 expiry and no delayed ready on a
  second attack. A rejected unpaid answer and stale replay preserve card state.
- Query tests capture card digest, full RNG state/consumption, effect registrations and
  pending agenda before the first `CanInstall` query, then compare subsequent
  execution with an unprobed control. Changing the activation rejects the old
  defense occurrence. An unrelated activation's attack-end notification cannot
  consume the registration. A departed/reentered hero cannot inherit it.
- `desperate-defense-mutant-damage.log`: compiled removal of attack-damage
  exclusion failed 2 of 9 cases with wrongly ready heroes.
- `desperate-defense-mutant-activation.log`: compiled wrong activation binding
  failed the characteristic readiness/isolation cases. Both mutants were
  restored; `desperate-defense-restored-build.log` records a non-incremental
  build and `desperate-defense-restored-tests.log` records 48/48 passing tests.
- Logs are outside the repository under `C:/Users/mggar/Source/`, prefixed
  `desperate-defense-`. The final focused run includes the prerequisite class,
  whole ExpertDefense, HeroDefense and DoctorStrangeUnflappable classes and
  citation validation. The final counts and hook outcome are in the handoff.

Limits: this is engine/DSL prerequisite evidence, not public pack or native UI
acceptance. The isolation probe sends a distinct activation's completion
condition directly; it is not a played nested-activation transcript (enemy
activations already queue). Existing HeroDefense tests cover defense-labeled
initiation; this candidate's new characteristic cases use ordinary basic
defense. No full Content suite, public setup/save transcript or new UI is claimed.
