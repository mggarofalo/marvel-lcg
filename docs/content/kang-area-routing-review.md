# Kang local routing independent review

Reviewed source: `5dcc1efaf57ed78f3a4c6d36b00f109d7764d764`, followed by repair
`30dc5ac3a077baf47973fbb3dc36f02e62149836`, on Windows, 2026-10-27.
**Accept the routing prerequisite with the repair.** This
review concerns the routing prerequisite only, not Kang setup, admission,
stage-I defeat, split orchestration or reunion.

## Resolved finding: absent local scheme loses the scheming enemy

The source explicitly supports a local minion continuing to scheme after its
local main scheme is absent, without placing threat on central stage 2B. In
`PhaseStep.SchemeThreat`, a null destination returns `new Occurrence(id, [])`.
`ThreatProcedure.SchemeThreat` subsequently adds `WhenSchemeEnds`, but the
occurrence has no subject, actor, actor facts or player. A response bound to
the minion that just schemed cannot match.

`MinionSchemeRetainsItsResponseWhenLocalSchemeIsAbsent` supplies real Weapons
Runner and Quake faces in a prepared local area and copies Quake's existing
production response into a test-local ability book. Its paired control retains
the local main scheme: Quake is offered and defeats Weapons Runner. Removing
only that local scheme causes `Sequence.Work` to finish without offering Quake.
Central stage 2B remains in play in both cases. Quake's printed text is
`datasets/cards/cards.json`, face `08012`: the condition is the minion scheming,
not threat placement. RR `rr:scheme-enemy-activation.step.3` governs placement;
it supplies no cancellation of the completed scheme when placement has no
destination. The fix must retain the enemy's exact causal roles and player
without inventing a threat recipient, and must preserve the completion order.

## Other review observations

The phase planner selects a villain per seat through existing game-area
membership; grouped players can share their villain and main scheme. Engaged
minions remain seat-specific. The two ability ports share one engine resolver.
Central stage 2B is excluded by exact game-area membership. The source rejects
multiple eligible local villains or main schemes explicitly. Wrecking Crew's
active villain and corresponding-side-scheme overrides remain ahead of ordinary
routing; its physical deck ownership is unchanged.

Six independent passing cases additionally cover mixed hero/alter-ego forms in
both player orders and both ports, reversing physical villain allocation order,
and distinguishing a minion's physical owner from its source play area. The
mixed-form cases drain real attack/defense and scheme execution, check boost
hosts in player order, damage, local threat, central exclusion and completion.
An initial test fixture used a one-icon boost while expecting zero; the fixture
was corrected to Hard to Keep Down after checking the generated card facts.

Other global-villain consumers (target queries, overkill and effect projections)
are unchanged and still require review in later split orchestration work. No
new persisted fields, RNG consumption, public admission or wire shape is added
here. Ordinary admitted games retain one villain/main scheme, while Wrecking
Crew retains its explicit overrides; this diff gives no reason to migrate their
replay version. Prepared Kang topology is not publicly constructible yet, so
this evidence does not establish a public save/restart journey.

## Evidence

Logs are outside the repository in `C:/Users/mggar/Source/`:

- `kang-review-restored-build.log`: forced non-incremental content build passed.
- `kang-review-restored-content.log`: 57 tests passed, covering both Kang routing
  classes before the new response probe, ActivationCards, Wrecking scheme
  destinations/other-villain schemes, RedSkullScenario and citation checks.
- `kang-review-rules.log`: 27 tests passed across VillainPhase, NextMinionActivation,
  ActivationCompletion, WreckingAttackRouting and EncounterTopology classes.
- `kang-review-mutant-owner.log`: compiled mutant replaces a player-owned
  enemy's source with its physical owner's identity before resolving the scheme;
  both ownership cases failed with the wrong local scheme. Four mixed-form
  cases still passed. Exact source was restored with `git restore`, followed by
  the non-incremental build and the 57-test restored run above.
- `kang-review-absent-response.log`: final paired response probe, including the
  passing local-scheme control and failing absent-scheme case.
- `kang-review-hooks.log`: normal commit-hook evidence.

The source's existing 961-content/70-rules logs were inspected and are broader
regression evidence for that source revision; they do not cover the discovered
response gap. No full managed, Linux or native certification is claimed.

## Absent-scheme repair follow-up

The repair in this commit is based on review revision
`c06f4162f4e25792ed4d8b1c24e6a79f798fedbf`. `PhaseStep.SchemeThreat`
retains the enemy subject, actor, captured actor facts and activating seat when
there is no local scheme. That occurrence has no threat assignment or target;
the existing threat procedure supplies `WhenSchemeEnds` and completes the
activation in its ordinary order. Central stage 2B receives no threat.

The paired Quake probe now covers both activating seats, including the
non-first player. It asserts the captured minion incarnation and causal roles,
the absent assignment/target/placement conditions, and the actual response
defeating Weapons Runner. A local villain with no scheme supplies the negative
control: its completed scheme does not offer Quake's minion-only response.
This remains prepared nonpublic topology; no Kang split orchestration or public
admission is established. Independent reviewer closure is recorded below.

Windows verification on 2026-10-27, against the repaired sources:

- Initial independent review class: 11 passed.
- Compiled mutant restoring the empty occurrence: 2 failed, 9 passed. Exactly
  the two absent-scheme minion cases failed because Quake was not offered;
  both retained-scheme controls passed.
- Exact source restored by SHA-256 comparison, then `dotnet build
  tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-incremental`: passed with zero warnings and errors.
- Restored content run: 62 passed across both Kang routing classes,
  ActivationCards, Wrecking scheme destinations and other-villain schemes,
  RedSkullScenario and rule citation checks.
- Restored Rules run: 56 passed across VillainPhase, NextMinionActivation,
  ActivationCompletion, WreckingAttackRouting, EncounterTopology and rule
  citation checks.

Logs under `C:/Users/mggar/Source/` use the `kang-scheme-response-` prefix:
`fixed-paired.log`, `mutant.log`, `restored-build.log`,
`restored-content.log`, `restored-rules.log`, and `hooks.log`.
These are focused Windows checks; no full managed, native or Linux
certification is claimed for this repair.

## Independent repair closure

The reviewer cherry-picked only repair `30dc5ac3` onto review `c06f4162`,
producing local review revision `a4300371`. Inspection confirms that the
null-destination occurrence preserves the exact enemy subject, actor snapshot
and activating seat. Its target remains absent and it carries no threat
assignment or placement conditions. `Threat.Apply` therefore places zero threat;
the existing procedure subsequently adds the scheme-end condition and completes
the activation normally. Quake's response retains its minion-only restriction.

Independent reruns against that revision passed:

- 62 Content tests, including the complete repaired independent review class,
  original local-routing class and affected Core, Wrecking Crew and Red Skull
  controls. Log: `C:/Users/mggar/Source/kang-review-repair-content.log`.
- 29 Rules tests across VillainPhase, NextMinionActivation, ActivationCompletion,
  WreckingAttackRouting, EncounterTopology and citation checks. Log:
  `C:/Users/mggar/Source/kang-review-repair-rules.log`.

The reviewer inspected the repair's compiling empty-occurrence mutant evidence
(exactly the two absent-scheme cases fail; nine controls pass), exact restoration
and forced-build evidence. Repeating that same mutation adds no distinguishing
case. The earlier independent physical-owner mutant remains applicable.
The closure documentation commit uses normal hooks, recorded in
`C:/Users/mggar/Source/kang-review-repair-hooks.log`.

The blocking finding is closed. This accepts local enemy activation and ordinary
main-scheme routing with the repair; the stage-I defeat/split/reunion work and
complete Kang admission remain outstanding as described in the handoff.
