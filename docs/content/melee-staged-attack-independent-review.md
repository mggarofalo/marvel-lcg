# MARVEL-728 independent engine review

Current reviewed production commit: `86f6af32493ec2751a8b659e8fffd3ac0f1c7fc2`.
Verdict: **accept the bounded staged-attack engine prerequisite**. The original
double-consumption finding is resolved and the independent regression passes.
This accepts the engine shape described below; it does not admit actual Melee or
claim public, persistence, transport, native, pack, or product readiness. The
review commits contain tests and evidence only.

## Resolved finding from the initial review

Initial reviewed production commit:
`a5dcd631c9f99c03f585747b3fe16ae046441b70`. Its verdict was request changes;
review test commit `e1042797f24d1410112ded3a65bfc46ad8253ca6` preserved the
failing regression. The defect and its distinguishing evidence were:

`AbilityResolutionChoices.RestoreChoice` restored `PowerExecution` through
`RestorePersisted`, but omitted `Power`, `PowerActor`, `PowerAmount`, and
`PowerTargets`. The second staged hit reached `AbilityAttackDamageExecution`
with `context.Power == null`. Its unwrapped-attack path consumed the registered
attack modifier, then `FinishResumedPower` consumed it again.

The independent `AttackModifierConsumesOneUseForTheWholeStagedAttack` theory
registers an engine-supported `attackDamage` effect and resolves a synthetic
staged attack followed by a separate one-damage attack in the same paid event.
The staged hits each deal five. With one use, the following attack correctly
deals one, producing six total villain damage. With two uses, it must deal three,
producing eight total villain damage; the initial revision produced six.
`ContinuousEffectRegistry.UseCore` decrements the remaining use count on each
call, so one-use fixtures mask the duplicate consumption.

The fix now uses `CreateResumedState` for choice restoration as well as procedure
restoration. It restores the retained power kind, amount, actor, targets and
thwart attribution, together with invocation/player/cursor facts. Final modifier
consumption and the general choice-inside-power refusal remain intact.

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

The initial review test commit passed the normal pre-commit hook, retained in
`review-commit.log`: 66 architecture checks, complexity and file length. Its
behavioral regression was deliberately red on the initial implementation.

## Targeted re-review of the correction

The exact two-file production diff at `86f6af32` was inspected, including all
`RestoreChoice` consumers. Both query/description and submitted-answer paths use
the shared constructor. For choices without retained power, its null/default
power fields match the ordinary choice behavior, and `RestorePersisted` retains
the same selection, damage, execution, and triggering facts. For retained power,
the context now matches the procedure-resumption path. The invocation's answering
player still comes from the same decoded continuation. No additional finding or
concrete missing test was identified in this correction.

Independent re-review logs are at
`C:/Users/mggar/Source/.agent-evidence/marvel728-independent-rereview/`:

- `build.log`: nonincremental Release build succeeds, no warnings or errors.
- `focused.log`: **142 passed**, including all staged cases, independent one/two-use
  and next-Embiggen-event cases, continuation and continuation-binding cases,
  counted attack, aftermath and existing wrapper-modifier controls.
  Filter: `StagedAttack|ActionAbilityContinuation|ActionAbilityCoreAnAttackModifier|CountedAttack|SimultaneousAttack|Aftermath|PowerReplacement`, each qualified with
  `FullyQualifiedName~`; execution used `dotnet test ... -c Release --no-build`.
- `adjacent-choices.log`: **34 passed** for `SimultaneousCardAttackTests`,
  `AttackReplacementContinuationTests`, `DamageCostReplacementContinuationTests`
  and `ActionAbilityTypedChoiceOperandsSimultaneousChoiceAdvertisesEveryEffectTests`,
  selected by the same `FullyQualifiedName~` filter form with `--no-build`.
- `review-commit.log`: this evidence update's normal pre-commit hook output.

The correction implementer's logs under
`C:/Users/mggar/Source/.agent-evidence/marvel728-power-context/` were also inspected:
baseline two-use red reproduction; 466 passing focused cases on the corrected
source; a compiled deletion of final modifier consumption killed by **both**
one-use and two-use cases; forced nonincremental restored build and passing
focused run; and 66 architecture checks, complexity and file length in the normal
commit hook. The prior surviving consumption mutant is no longer a survivor
after context restoration. These are separately attributed implementation and
independent observations, not one aggregated test count.

No broad final Content, managed/integration, Linux, public setup, transport,
save/restart, or native interaction run was performed by this independent review.
Those remain outside this prerequisite acceptance. The implementation's
pre-cleanup full-suite run does not certify the final source. Complete the
applicable broader delivery gates on the integrated revision before making a
broader readiness claim.
