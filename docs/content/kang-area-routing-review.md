# Kang local routing independent review

Reviewed source: `5dcc1efaf57ed78f3a4c6d36b00f109d7764d764` on Windows,
2026-10-27. **Reject pending the absent-scheme response repair below.** This
review concerns the routing prerequisite only, not Kang setup, admission,
stage-I defeat, split orchestration or reunion.

## Blocking finding: absent local scheme loses the scheming enemy

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
