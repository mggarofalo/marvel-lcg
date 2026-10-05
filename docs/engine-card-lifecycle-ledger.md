# Effective card lifecycle closure ledger

Verified baseline: main `3246b4a5261fbcf3f33db01c91a6e58b28631b12`.
Working branch: `fix/marvel-385-effective-card-lifecycle`.
This ledger records the local source freeze on 2026-10-05, before normal commit
hooks and final PR-head review/CI. The PR and Plane hold subsequent landing
status. See [local validation](engine-card-lifecycle-validation.md).

| Requirement | Owner and correction status | Evidence required for closure | Dependency |
|---|---|---|---|
| MARVEL-444: discarded cards retain damage, exhaustion and counters | Rules: composed copy state detaches on departure; focused rules and damage journeys pass | Generic departure/reentry, in-play preservation, pending effects, villain carryover and event regressions; killed behavioral mutants; full gates | MARVEL-385 shares copy lifecycle |
| MARVEL-384: BP, Drone and Tech selectors contain scenario knowledge | Cards: generic trait/facing/public-area/order expressions; BP literal removed from interpreter | Control/ownership/trait/scope/order/purity/privacy tests; equivalent-program comparison and fixed-workload timings; full gates | Existing MARVEL-376 is Done |
| MARVEL-385: location implies Drone; physical identity and active role conflated | Rules/Cards: explicit authored blank-minion profile preserves physical id, owner and face | Legal Core Ultron, Upgraded Drones, 01185 ruling, owner restoration and defeat timing tests; mutation and replay evidence | MARVEL-384 |
| MARVEL-385: environment defeat response must be authored as data | Cards/Rules: authored 01140 forced response uses captured profile, facing and incarnation | Current response, stale reentry, facing and permanent-removal regressions pass; M04–M06 killed; final PR gates pending | Landing |
| New assignment state and corrected lifecycle affect replay/digest | Core/Server: explicit digest v3, mandatory profile field; documentation being reconciled | Canonical format pins, profile differentiation/roundtrip, old-version rejection, in-process/socket save compatibility and bounded baseline comparisons | Stable candidate |
| Independent review findings | Cards: active source continuation, global/engaged ordering, nested ordered villain replacement and projected re-engagement and hosted player-area upgrades corrected | Focused regressions pass; M02/M07–M11/M15 killed; final PR-head review pending | Landing |
| Full-game sweep finding | Cards: historical defeat selectors exclude permanently removed cards | Removal regression and all 80 acceptance cases pass; M06 killed | Landing |
| Landing | Requires PR/Plane evidence beyond this local checkpoint | Normal hooks; own PR; adversarial review of final head; Windows/Linux CI; authorized squash merge; green main; Plane evidence | All required rows above |

The current scope is copy lifecycle, temporary blank-minion identities and the
recorded selector migrations. Presentation consolidation and MARVEL-441 smaller
windows remain separate work. Existing acceptance requirements stay in this
ledger until their evidence is complete.

The existing MARVEL-384/385 issues belong to Architectural Decomposition and
Simplification; MARVEL-444 belongs to Engine Core. All remain In Progress until
landing. The supported temporary identity is a blank minion, sufficient for Core
Ultron. Other temporary roles require separate contracts. Unordered `last` after
projected re-engagement explicitly raises because the trace lacks chronology;
no migrated Core card uses that unsupported composition.
