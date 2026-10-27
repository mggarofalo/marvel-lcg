# Judge, Jury, Executioner engine composition

Printed authority is `datasets/cards/cards.json`, card 90002: “After a friendly
character is defeated by an enemy attack, place 2 threat on the main scheme.”
RR 1.8 `rr:attack-enemy-activation.step.6.a` groups forced attack/damage/defeat
responses at attack completion; `rr:triggering-condition.2` describes one attack
both damaging and defeating a character. No card-specific 90002 FAQ entry is
present in the vendored MarvelCDB FAQ.

The acceptance fixture injects this printed body into a test-local full ability
book. It does not author production 90002 content or admit Kree Fanatic. Both
`WhenAttackEnds` plus `WhenCardDefeated` and `WhenDamageDealt` plus
`WhenCardDefeated` work with the attack-damage predicate.

`EnemyAttackDefeats` retains exact damage-owned occurrence/recipient/incarnation
registrations. `Defeat.Record` supplies established defeat facts before departure,
and `AttackCompletion` transfers them into the common response occurrence.
This includes identity elimination, which can finish the attack before damage
execution resumes. No current-zone or damage-total inference supplies a defeat.
When elimination occurs inside an overkill continuation, ending the activation
preserves every frame belonging to the current occurrence, including its response
owner.

`RonanJudgeCompositionProbeTests` asserts two threat for each lethal ally attack
and zero for a surviving ally, with local and remote controllers and both trigger
compositions. It checks original controller, incarnation and actual two-damage
placement, then fully drains the attack and checks the effect did not repeat.
`RonanJudgeDefeatReceiptTests` covers surviving-game identity elimination, overkill
defeating ally and identity, two simultaneous indirect ally defeats, replaced
defeat, Tough prevention, a cancelled following attack, nested nonattack defeat,
Boost ability damage and enemy retaliation. Synthetic observer abilities expose
the genuine engine windows; they do not generate plausible damage/defeat events.

Ordinary defeat responses also belong to that common window: `rr:damage.step.9`
includes after-defeat responses alongside after-damage responses, and
`rr:triggering-condition.2` explicitly unifies every triggering condition created
by one attack. Attack step 6 places the shared forced tier before optional
responses. Damage settlement retains historical defeat facts for its outcome
and step-seven interrupts, but does not open an earlier ordinary defeat response.
`EnemyAttackDefeatResponseOrderingTests` distinguishes that timing from merely
deduplicating the final effect: a plain defeat response first appears after the
actual AttackCompleted event and departure, alongside the after-attack response;
using it leaves the after-attack option available without offering it again.

Evidence logs are outside the repository under
`C:/Users/mggar/Source/evidence/ronan-enemy-defeat-*.log`. Independent engine review
and production ability-book authoring remain separate delivery steps.
