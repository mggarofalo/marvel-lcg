# Activation defeat outcome independent review

MARVEL-857. Reviewed production revision:
`8a8f4f6829220c09e94529f175c69a4b1d38fb7c`.
Verdict: accept the bounded activation defeat receipt. No production correction
was identified. This review adds synthetic tests; it neither authors 90004 nor
admits Ronan to the supported product.

## Authority and causal review

The printed 90004 boost condition is “If this activation defeats a character,
deal the first player 1 facedown encounter card.” The original composition probe
is [recorded separately](ronan-hammer-composition-probe.md). RR 1.8
`rr:boost-boost-icon.3` excludes boost damage from activation damage;
`rr:scheme-enemy-activation.step.3` places threat rather than dealing attack
damage. `rr:activation.8` queues activations initiated during another activation,
and `.8.2` resolves the original activation's triggered abilities first.

The completed receipt copies `EnemyAttack.Defeats.Items`, whose registration
requires the exact damage occurrence, card and incarnation. `Defeat.Record`
establishes that fact before departure and identity elimination. Both normal
completion and early completion publish the immutable copy. Retaliate and later
attack-end responses occur after that copy; their defeats cannot change it.
The delayed body receives the exact activation's receipt count and retains it
through ordinary continuation memory, rather than re-reading the board. The
activation ID guard excludes a foreign completed receipt. There is no nested
activation running inside the original activation under the current queue rule.

## Added distinguishing evidence

`ActivationDefeatOutcomeTests` now also checks:

- One overkill attack defeats its defending ally and that ally's identity. After
  elimination and a delayed choice, the result remains exactly two, and the
  printed conditional deals exactly one encounter card to the surviving first
  player. The eliminated player's ally is in the removed area after cleanup.
- A would-defeat interrupt defeats a different ally with direct damage and heals
  the attack recipient. The attack deals five damage but defeats no character:
  the delayed count remains zero and no encounter card is dealt.

The tests drain the agenda and assert the game remains unfinished. The existing
class also covers lethal versus surviving equal damage, first-player ownership,
delayed choice, schemes, boost-only defeats, early identity elimination, queued
activation separation and would-defeat recovery.

## Verification

Windows isolated worktree: `C:/Users/mggar/Source/marvel-lcg-activation-review`.
The final focused filter is
`FullyQualifiedName~ActivationDefeatOutcomeTests|FullyQualifiedName~RuleCitationTests`:
13 passed (11 behavior cases and two citation checks). After mutation restoration,
the test project was rebuilt with `--no-incremental` before the final run.

A compiled mutant replaced the defeat count with `Math.Min(1, count)`. The final
tests rejected it with exactly one failure: expected two, observed one in the
overkill case; the other twelve checks passed. The restored source passes the
same filter. Logs live outside the repository under
`C:/Users/mggar/Source/evidence/activation-defeat-review-*`, specifically
`final-mutant.log`, `forced-build.log` and `final.log` with that prefix.

An initial fixture assertion expected the defeated ally to remain discarded;
player elimination correctly moved it to the removed area. That assertion was
corrected before the passing runs. The first mutant was rerun after moving the
count observation inside the suspended choice's effect.

Retaliate exclusion and stale-incarnation exclusion were inspected in the causal
capture and immutable value path; no dedicated new interaction test is claimed
for them. This review does not independently rerun the broad managed suite,
public admission, actual 90004 authoring, native UI, or save/replay compatibility.
