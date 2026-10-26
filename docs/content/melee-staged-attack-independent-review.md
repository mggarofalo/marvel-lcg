# MARVEL-728 independent engine review

Reviewed production commit: `a5dcd631c9f99c03f585747b3fe16ae046441b70`.
Verdict: **request changes**. The review test commit deliberately preserves one
failing regression until the implementation is corrected. No production source,
ability dataset, public admission, Runner, Agenda, or UI change is included.

## Finding: the second choice consumes an attack modifier twice

`AbilityResolutionChoices.RestoreChoice` restores `PowerExecution` through
`RestorePersisted`, but does not restore `Power`, `PowerActor`, `PowerAmount`, or
`PowerTargets` as the procedure-resumption path does. Consequently, the second
staged hit reaches `AbilityAttackDamageExecution` with `context.Power == null`.
Its unwrapped-attack path consumes the registered attack modifier. Then
`FinishResumedPower` consumes the captured modifier again.

The independent `AttackModifierConsumesOneUseForTheWholeStagedAttack` theory
registers an engine-supported `attackDamage` effect and resolves a synthetic
staged attack followed by a separate one-damage attack in the same paid event.
The staged hits each deal five. With one use, the following attack correctly
deals one, producing six total villain damage. With two uses, it must deal three,
producing eight total villain damage; the reviewed revision produces six.
`ContinuousEffectRegistry.UseCore` decrements the remaining use count on each
call, so one-use fixtures mask the duplicate consumption.

The minimal correction boundary is restoration of the retained power context on
the effect-choice resume path, consistent with `CreateResumedState`. Review all
retained power fields and other choice paths rather than deleting final modifier
consumption or weakening the general choice-inside-power refusal. A temporary
diagnostic restoring only `Power` made all 27 staged checks pass. That diagnostic
was removed; it establishes the cause, not a complete production fix.

## Authority and independent observations

Reviewed generated physical Melee `05030` (cost three; two sentences dealing
three damage to an enemy and another enemy), the raw vendored rulings
`e0333bc6316bd7f0`, `9aa6221cd0950fbb`, `f32c53356db718a6`, and
`a43333f9f9d3634a`, and the applicable attack, ability, event, defeat, and
leaves-play clauses. The independent synthetic tests make no content-admission
claim.

The exact lowering restriction, frozen second-offer membership and incarnation
validation, live legality query, immutable recipient publication, and ordinary /
configured villain succession were inspected. The `VictoryDisplay` helper
extraction preserves its prior promotion-face calculation verbatim. Existing
staged controls exercise Guard removal, suspended defeat choices, same-villain
stage exclusion, Risky Business, configured villain groups, departing/reentering
minions, invalid answer/query purity, stun, Tough, retaliation, receipt counts,
and non-first-player ownership. These observations reveal no additional finding
within the bounded engine shape; they do not establish public or UI readiness.

An added independent next-event test pays and resolves actual authored Embiggen,
observes five damage to both first-event recipients and event modifier disposal,
then pays a second physical Melee while Embiggen remains exhausted. The next
event deals three damage per recipient and completes. An attempted direct
discard-to-hand recycle after prompt creation was an invalid stale-prompt fixture
and was removed; no same-physical-event replay result is claimed.

## Reproducible evidence

Logs are outside the repository at
`C:/Users/mggar/Source/.agent-evidence/marvel728-independent-review/`.
All mutation and restoration builds used Release, with serial execution. Final
restoration used `dotnet build ... --no-incremental` before `--no-build` tests.
`git diff --exit-code -- src` confirms that all production edits were restored.

| Evidence | Result |
|---|---|
| `final-restored-build.log` | Nonincremental Content-project build succeeds. |
| `final-restored-focused.log` | 28 pass, one expected failure: two-use modifier loses its second use. Includes all staged tests and two existing wrapper-modifier controls. |
| `diagnostic-choice-power-build.log`, `diagnostic-choice-power.log` | Temporary `Power` restoration compiles; all 27 staged cases pass. Diagnostic removed. |
| `mutant-eager-build.log`, `mutant-eager.log` | Compiled eager consumption after first damage is killed by second-recipient damage: expected five, actual three. Recorded before the one-use test became a one/two-use theory. |
| `mutant-defeat-build.log`, `mutant-defeat.log` | Compiled deletion of first-target defeat receipts before second damage is killed: expected one retained defeat, actual zero; no-defeat control passes. |
| `mutant-modifier-build.log`, `mutant-modifier-forced.log` | Compiled deletion of final consumption survives the one-use check. This exposed alternate consumption during the second-choice path and led to the two-use regression. It is not counted as a killed mutant. |

The normal pre-commit hook is required for this review test commit. Its output is
retained in `review-commit.log`. The behavioral test remains deliberately red on
the reviewed implementation; architecture/complexity/layout success does not
override that finding.

No broad final Content, managed/integration, Linux, public setup, transport,
save/restart, or native interaction run was performed by this review. The
implementation's pre-cleanup full-suite run does not certify the final source.
After a separately implemented correction, independently rerun the new theory,
existing choice/continuation controls, staged tests and applicable broader gates
against the exact corrected commit. No engine acceptance is granted here.
