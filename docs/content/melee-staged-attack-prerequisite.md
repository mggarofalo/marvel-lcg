# Melee staged attack engine prerequisite (MARVEL-728)

This candidate extends the card interpreter with one bounded subsequent-recipient
choice within one retained attack. It is an engine prerequisite, not public Melee
or Ms. Marvel admission. Tests use physical Melee (05030), its printed three-resource
cost, and a synthetic test-only body. No ability-book row, AbilityRunner, Agenda,
client contract, or UI is changed.

The source baseline is `30574a1e`; the implementation branch is
`feat/marvel728-staged-attack`. This document accompanies the candidate commit.
Independent engine review and the unresolved checks below remain required.

## Authority and supported shape

The generated card dataset supplies Melee's printed instruction: deal 3 damage to
an enemy, then deal 3 damage to another enemy. `rr:ability.4` resolves an ability
one sentence at a time. `rr:attack-player-ability-type.2.1`, `.2.2`, and `.5`
establish one attack, increases applying to both instances, and an attack against
each targeted enemy. `rr:stun-stunned.5` and `.7` require costs despite cancellation
and establish that the canceled attempt is not an attack. `rr:leaves-play.1`
treats a departed/reentered minion as a new copy.

Vendored ruling ids in `datasets/rulings/` establish the distinguishing cases:

- `e0333bc6316bd7f0`: first-sentence Guard defeat unlocks the villain for sentence two.
- `9aa6221cd0950fbb`: the next stage of the same villain is not another enemy.
- `f32c53356db718a6`: full-attack responses wait for Melee; first-recipient defeat
  remains available after full resolution, with per-enemy responses retained.
- `a43333f9f9d3634a`: Embiggen increases both damage instances to 5.

The engine-chosen DSL spelling is a first `chooseCard` from `attackableEnemies`,
containing one `attack` whose target is `chosen` and whose exact body is a sequence
of constant `dealAttackDamage` to `chosen`, then `attackAnotherEnemy` with a positive
constant amount. Lowering rejects standalone or misplaced subsequent selection,
other powers, counted bodies, and a first Power.Target other than Chosen.
Arbitrary choice-inside-power remains unsupported and retains negative controls.

## Execution and identity

First damage completes its owned damage/defeat procedures before the next pool is
computed. One remaining candidate still produces an explicit prompt; no remaining
candidate completes the paid event. Both prompts belong to the acting seat.
One retained power and attack occurrence carry modifiers, immutable recipient facts,
damage taken, defeats, retaliation tracking, and the final attack response window.

The first enemy's identity is captured before damage. Configured multi-villain
groups retain their own stages, including independent groups with equal titles.
Ordinary succession uses the same promotion-side calculation as VictoryDisplay,
including Risky Business. Reentered minions are compared by incarnation. The second
prompt freezes its offered object-id/incarnation membership and revalidates shared
live legality before tracking resolution. Repeated-first, forged, and stale answers
preserve prompt, digest, full RNG, agenda/current occurrence, and subsequent trace.

## Evidence record

Raw local logs and packet checkpoints are under
`C:/Users/mggar/Source/.agent-evidence/marvel728-melee/`. These local artifacts are
not repository test inputs; preserve or transfer them with the review handoff.
Earlier passing counts establish their named revision only.

| Packet / log | Observed result and scope |
|---|---|
| `design.md`, `probe.log` | Initial prerequisite proposal and two passing isolation probes; no implementation acceptance. |
| `receipts-focused.log` | 109 passed for receipt, stage, Embiggen, and existing attack/continuation controls. |
| `mutant-receipt.log` | Compiling zero-DamageTaken mutant killed by both observer theory cases. |
| `mutant-stage.log` | Compiling physical-id-only exclusion mutant killed by successor exclusion. |
| `restored-build.log` | Nonincremental restored Release Content-project build passed. |
| `receipts-content-full.log` | 4969 passed before the safety and final-edge changes. |
| `safety-focused.log` | 62 passed for incarnation validation, identity, placement, purity and existing controls. |
| `safety-mutant-offer.log` | Compiling stale-offer mutant killed by invalid retry assertion. |
| `safety-mutant-placement.log` | Compiling placement mutant killed by standalone instruction admission test. |
| `safety-mutant-promotion.log` | Compiling promotion mutant killed by Risky successor exclusion. |
| `safety-mutant-reentry.log` | Compiling physical-id reentry mutant killed by returning-minion eligibility. |
| `safety-restored-build.log` | Nonincremental restored Release Content-project build passed. |
| `safety-content-full.log` | Discovery only, no final result; not passing evidence. No matching active process remained when final-edge edits began. |
| `final-edges-focused.log` | 57 passed after fixing fixture assumptions; initial failed attempts are retained in the worker history. |
| `final-mutant-target.log` | Compiling missing-Chosen-target restriction mutant killed by malformed-shape admission test. |
| `final-restored-build.log` | Nonincremental restored Release build passed, zero warnings/errors. |
| `final-restored-focused.log` | 63 passed, including explicit arbitrary nested-power negatives and staged, counted, simultaneous, aftermath/replacement controls. |
| `final-architecture.log` | 66 policy/layout/debt/test-quality/engine-source/shared-data checks passed. |
| `final-content-full.log` | 4986 passed on complete behavioral candidate before the final behavior-preserving decomposition. |
| `final-complexity.log` | Failed at CCN11 in choice dispatch and receipt completion; prompted the final decomposition, with no threshold exception. |
| `final-file-length.log` | File-length script passed with empty successful output. |

Final-edge coverage includes suspended first-target When Defeated choice, defeated
and surviving Guard, lone enemy, paid stun cancellation with silent attack-end
observer, Tough on either recipient, both retaliations after second selection, and
nonzero acting-seat prompt ownership and disposal. Receipt coverage checks one
whole-attack response, two per-recipient responses, departed first defeat, ordinary
villain promotion, and actual authored Embiggen accept/decline through disposal.
Safety coverage separately exercises configured villain groups and Risky promotion
without claiming full scenario play from synthetic fixtures.

Final cleanup groups resumable effect-choice answer handling under one continuation
owner and gives immutable recipient receipt publication its own named operation.
The matching post-cleanup focused run and normal hook outcomes are recorded below
before commit. The earlier full suite is retained as pre-cleanup regression evidence;
it is not described as a run of the final refactored revision.

Post-cleanup `cleanup-focused.log` records 63 passing tests using the same filter
as `final-restored-focused.log`, with the current Release projects rebuilt.
`cleanup-hooks.log` records a successful normal `dotnet husky run --group pre-commit`:
66 architecture policy tests, cyclomatic complexity, and file length all pass.
No gate, exception list, or baseline was changed. The commit also executes the
normal pre-commit hook; its output is preserved in `cleanup-commit.log`.

## Unresolved acceptance checks

Independent engine review must assess the exact commit, authorities, and plausible
wrong implementations that the tests still permit. Additional planned evidence is
the next event after Embiggen disposal and characteristic modifier-consumption and
first-defeat-loss mutants. Existing tests prove modifier registration disposal and
retained first defeat, but those additional distinguishing controls are not claimed.

Broad managed/integration lanes and cross-layer/native interaction evidence remain
outside this packet. No product acceptance or pack admission follows from these
engine tests. The canonical Plane record owns delivery status.
