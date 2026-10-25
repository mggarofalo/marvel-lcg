# Wrecking Crew caused-defeat implementation checkpoint

Candidate adds explicit `wasDefeatedByDamage` without changing `wasDefeated` occurrence semantics. Direct authored damage nodes retain exact completed DamageResult target/incarnation and defeat outcome; continuations retain that resolution-local receipt. A chosen binding is compared by its original incarnation even after departure. No health or departure heuristic determines defeat. Engine choice: the predicate reads the most recent direct damage node, and an empty receipt is false.

Canonical 07016 targets hero-form identities and allies, with first-player selection among tied lowest remaining HP. Its three threat targets Wrecker's Day of Reckoning regardless of the active villain. Own ordinary and forced defeat interrupts are eligible alongside When Defeated abilities; optional acceptance/decline, ordering and single execution are covered. Returned-ally tests use a synthetic ally interrupt with the canonical treachery, not an authored Red Dagger. Public admission remains closed.

The 2026-10-25 continuation corrected fixtures to preserve discard assertions with deck sentinels and merge authored membership alongside local abilities. Four signature schemes pass 62 focused cases. Combined receipt, card and own-interrupt checks pass; logs: `%TEMP%/wrecking-receipt-final.log` and `%TEMP%/wrecking-canonical-final.log`. Independent content review approved 07016 against printed text, rr:first-player.1 and ruling:28d343ac007bd784.

Independent receipt review found the empty-node stale receipt and supplied lifetime and incarnation regressions. Restoring lazy receipt allocation fails the empty-target case; ignoring incarnation fails the reselected-card case. Logs: `%TEMP%/wrecking-receipt-mutant-empty.log` and `wrecking-receipt-mutant-identity.log`. Restoring either own-interrupt exclusion also kills its tests: `%TEMP%/self-defeat-initial-mutant.log` and `self-defeat-pending-mutant.log`. Sources were restored and rebuilt.

Remaining explicit requirements: MARVEL-669 shared multi-scheme ordering; 07022 excess routing; 07030 completed defendedness; 07031 threat movement/actual response-trigger receipt; 07035 attack prohibition; 07051 attachment defender restriction. MARVEL-650 Escaped Convict authority conflict remains unresolved. Breakout B phase-boundary integration passes focused canonical tests; shared ordering remains outstanding. Product admission remains closed.

Architecture preparation relocates three existing AbilityConditionProjection baseline diagnostics by one line without changing their debt. It removes a default body from IThreatCardAbilities (both production implementations already provide it) and explicitly reviews the client's scenario-variant wire-data dependency. Existing architecture gates remain intact. The broader wave still needs combined integration and product verification.


## Completed attack increment: Bulldozer's Helmet (07049)

Canonical Helmet attaches to the physical Bulldozer, adds its printed ATK, and
resolves as an ordinary forced attack-end response before optional responses.
Its discard count reads the completed occurrence's attack-step damage dealt,
including prevented damage and excluding boost ability damage. The attacked
player owns that discard even when another character defends. The Hero Action
uses the existing simultaneous exhaustion and random-hand-discard cost.

Engine choice: `CompletedEnemyAttack` retains activation identity, final defender,
and attack-step damage dealt on the exact occurrence; `completedAttackDamageDealt`
reads that occurrence and returns zero when it has no completed enemy attack.
This increment does not implement Lightning Blast (07030), whose scheduled
activation receipt still needs defendedness retained after responses.

Focused evidence: `WreckingHelmetTests` covers ordinary damage, Tough prevention,
basic hero defense, lethal ally defense with overkill, stun replacement, foreign
attacker, multiplayer deck ownership, and the optional response boundary.
Existing random-cost regressions also run. Logs: `%TEMP%/wrecking-helmet.log`.
The combined Wrecking/Defeat run passes 840/846; its six failures are the known
missing 07015/07030 rows. Log: `%TEMP%/wrecking-helmet-regression.log`.
Public admission remains closed. Mutation and final regression results are
recorded in the agent handoff for independent review.

Final Helmet review: 21 focused cases pass, including canonical overkill, helper-player deck ownership and boost-damage exclusion. The prevention mutant fails the Tough case as expected and was restored. Architecture: 129/129 passed. Logs: `%TEMP%/wrecking-helmet-reviewed.log`, `wrecking-helmet-mutant-prevention.log`, and `wrecking-helmet-architecture.log`.
