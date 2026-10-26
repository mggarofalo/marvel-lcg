# Full healing costs â€” MARVEL-804

Candidate: `fix/full-healing-cost`, based on `c1d692a3`. Momentum Shift (09016)
remains test-local; no ability-book admission, setup or client changes are included.

The generated card requires healing two damage before its arrow. Rules Reference
1.8 initiating-abilities step 5 aborts a cost that cannot be paid; cost clause 5
requires simultaneous costs. Ruling `ecabf270719f2990` (pre-1.5, Michael Boggs)
confirms the full two-damage cost and is consistent with the current clauses.
Ordinary healing effects continue to remove only the damage present.

Offer discovery and exact payment validation require the full heal amount. A
concrete plan can use one damage-paying resource ability plus hand resources,
including a non-event spend-and-heal ability. The projection uses the live damage
cost modifier policy, forced replacement projection, Tough and every active
prevention effect. A partially prevented taking cost cannot supply damage. Optional
prevention can be declined; actual continuation and full-heal commitment remain
authoritative if intervening choices change the result.

The capability deliberately raises before payment when the missing healing would
require multiple selected resource abilities, another state-changing generator
cost, an unknown replacement, lethal generator damage, or a combined direct damage
and heal arrow cost. It does not infer a plan by adding independently payable
generators. A currently fully payable heal does not need that future projection.
These limits are not generic payment-planning coverage.

Focused evidence (logs outside the repository under `C:/Users/mggar/Source/`):

- `full-healing-expanded.log`: full-heal tests, existing healing commitment tests
  and rule-citation gate pass (19 tests).
- `full-healing-mutation-results.json`, `full-healing-mutant-partial.log`, and
  `full-healing-mutant-prevention.log`: compiled partial-heal and ignored-prevention
  mutants fail distinguishing full-heal tests.
- `full-healing-restored-build.log`, `full-healing-restored-tests.log`: restored
  sources receive a nonincremental Release build and all 15 full-heal cases pass.
- `full-healing-regressions.log`: 90 focused tests pass, including existing damage
  cost, Backflip, Core prevention, nonattack prevention and attack-scoped prevention.
- Normal pre-commit results are recorded in the delivery handoff, with the resulting
  commit identifying the reviewed candidate.

Tests include generated Momentum Shift with zero, one and two damage, exact
selected versus merely available resources, Tough, stacked active prevention,
partial taking costs, non-event payment, ordinary healing, second-seat authority,
unsupported-projection purity, and repeated queries versus an unprobed execution
control. No native behavior or pack readiness is claimed.
