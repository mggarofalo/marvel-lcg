# Black Widow prevention packet checkpoint

Plane: MARVEL-779. Base: e04f2946. Branch: feat/widow-prevention.

This is an incomplete authoring checkpoint, not reviewed or accepted content.
The four appended faces are 08006 Attacrobatics, 08008 Grappling Hook,
08017 Counterintelligence, and 08032 Defensive Stance. The 628-row base prefix
is preserved; the book now has 632 rows. No generated card, engine, public
admission, setup, protocol or replay changes are included.

Authority is the generated printed text in datasets/cards/cards.json and current
Rules Reference 1.8: prevent.1.1, prevent.2, cancel.4, interrupt, and
labeled-ability. Rulings reviewed include 0fc2a7cc9197e127 (RR 1.6), which allows
Attacrobatics to cancel icons under Guard without attacking the villain;
historical 6919a92cb0376eac must not govern that case. Ruling 1ea208998735ef4b
places amplify/constants before icon cancellation and Boost abilities afterwards.
Grappling Hook cancels keywords under 49d5aba04226396f and 48dd96f060641cb8.
Historical a693e5ba5379e414 is not a basis to publish a successful Preparation
receipt after stunned cancels the ability; current resolve.2 and the existing
420bd5b1763b9506 relationship govern the actual Widowmaker/Synth-Suit rows.

Focused command: dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-restore --filter FullyQualifiedName~BlackWidowPrevention.
Final checkpoint result: 13 passed, 0 failed. Full log outside repository:
C:/Users/mggar/AppData/Local/Temp/widow-prevention-checkpoint.log.

Evidence per face:

- 08006: complete villain attack and scheme, another attacked/scheming seat,
  exact physical boost occurrence, zero surviving numeric icons, two damage to
  villain, discard cost, activation completion and no game ending.
- 08008: own Shadow of the Past canceled with its gained surge suppressed;
  decline retains upgrade and allows the surge encounter deal; treachery cleanup.
- 08017: main-scheme prevention capped at remaining assignment in both forms;
  exact remaining-threat receipt and placement; other source player; side-scheme
  and zero-amount negative controls.
- 08032: full attack prevention of one/five damage, retained damage-dealt receipt,
  discard cost, actual Preparation completion receipt, actual Widowmaker offer,
  actual Synth-Suit exhaust/readies identity; rejected unknown handle preserves
  digest/RNG/agenda before valid retry. Negative alter-ego, foreign identity and
  controlled-ally direct damage retain the upgrade and place damage normally.

Remaining work before acceptance: meaningful Guard partial-attack red probe and
separate engine prerequisite if confirmed; actual zero/star-only/amplify/multiple
boost and stunned/cancellation composition; Grappling Hook wrong-seat/form/kind
controls and actual completion responses; threat/damage instance isolation and
decline controls; admission/data inventories; compiled distinguishing mutant and
forced non-incremental restoration; independent review. No mutant evidence or
ability-data suite result is claimed at this checkpoint. The current attack wrapper
has not established the Guard ruling and must not be described as complete.

## Bounded verification follow-up

Branch `feat/widow-prevention-finish` starts at `bcc7005b`. The authored four rows
are unchanged, preserving the 628-row prefix and 632 total. The Guard regression
is committed separately as `de9f2363`; on this engine it fails because the actual
08006 ability is not offered with Hydra Mercenary (01101) engaged with its
controller. The same positive-icon attack without Guard is covered and passes.
This confirms the separate engine/content prerequisite; no engine fix is included.

The new controls establish:

- 08006: zero-icon and star-only boost cards do not offer cancellation; Weapons
  Runner's actual star still puts it into play. Amplify and constant icons count
  toward the canceled quantity. Two physical boost cards keep distinct receipts,
  including declining each offer. Actual I See You gains its conditional icon
  after cancellation. Alter-ego has no offer. Stunned pays the discard cost,
  removes stun, leaves numeric icons intact and offers neither actual completion
  response. Every activation control asserts completion and no game ending.
- 08008: another seat's treachery, alter-ego and a revealed minion do not offer
  cancellation, and their actual effects resolve. Accepted cancellation supplies
  its actual Preparation receipt, offers Widowmaker and Synth-Suit, and readies
  through Synth-Suit; an existing stunned status is unaffected.
- 08017: a one-threat instance cannot carry spare prevention into the next
  two-threat instance. Both offers may instead be declined without discard.
- 08032: two complete one/two-damage attacks keep prevention to one instance;
  declining preserves the card and both damage instances. The existing foreign
  identity, controlled ally and alter-ego controls now execute complete attacks
  and assert that no Defensive Stance offer appears.

The explicit ability-data face inventory includes only these four additions.
The predecessor five-row inventory retains its fixed positions; the new four-row
inventory verifies the suffix and absence of starting counters. Black Widow's
incomplete public admission remains outside this packet.

Verification command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-restore --filter 'FullyQualifiedName~BlackWidowPrevention&FullyQualifiedName!~AttacrobaticsUnderGuard|FullyQualifiedName~BlackWidowPreparationCardTests.NewRows|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests|FullyQualifiedName~RuleCitationTests'`.
The earlier restored result was 72 passed, including 31 prevention cases. The
latest source additionally asserts absence of Counterintelligence offers on zero
threat, rather than silently declining them: 71 passed and one failed. A zero
assignment currently produces an actual legal offer; this admission finding
needs authority adjudication and resolution before acceptance. Side-scheme
prevention remains correctly excluded. Latest log:
`C:/Users/mggar/AppData/Local/Temp/widow-prevention-final-tests.log`.
The separate Guard red log is `widow-prevention-guard-red.log` in that directory.

Mutation: actual 08032 prevention amount 3 changed to 2; an explicit
`dotnet build ... -c Release --no-restore --no-incremental` succeeded, then the
characteristic five-damage case failed with expected 2 damage and actual 3.
The one-damage case remained green (one failed, one passed). The original data
was restored in a `finally` block, followed by another explicit non-incremental
build and the same characteristic test inside a passing 72-case run. Logs are
`widow-prevention-mutant-build.log`, `widow-prevention-mutant-test.log`,
`widow-prevention-restored-build.log`, and `widow-prevention-restored-test.log`
in that external temporary directory. The restored ability book has no diff.

Remaining acceptance: partial attack prerequisite and actual Guard green result,
the zero-threat admission finding, combined candidate inventories, independent
review, and integrated broad gates.
This packet supplies focused engine-context evidence; it does not claim public
setup, save/restart, native interaction, complete pack or owner acceptance.

## Counterintelligence zero-threat adjudication — MARVEL-785

Branch `fix/counterintelligence-zero` starts at `9d2978e0`. The authority is the
current vendored Rules Reference 1.8: `rr:target.3` makes a target valid if any
part of the ability can affect it; `rr:target.3.3` excludes the cost from that
assessment; `rr:target.3.4` permits another effect on the same target to qualify
the ability; `rr:prevent.2` reduces the assignment before placement. Printed
08017 discards itself as a cost to prevent three of the imminent main-scheme
threat. No Counterintelligence-specific entry was found in the vendored FAQ or
rulings datasets. This decision does not use a blanket game-state-change rule.

Scheduling an initial zero assignment is a synthetic API boundary probe, not
the evidence establishing reachable gameplay. Two actual Counterintelligence
copies controlled by different players establish the defect: player 0 prevents
all of a positive one-threat assignment, and player 1's copy is still offered
for that same assignment. Its only effect can no longer affect the target, and
its discard cost cannot supply eligibility. With four initially assigned threat,
the second copy remains eligible to prevent the one threat left after the first.

The engine change classifies `preventThreat` in target admission using its
existing partial-resolution analysis. It does not suppress the entire threat
window, add a card-id exception, or change authored abilities. New tests verify
the actual two-controller path, the preserved second copy's next exact threat
instance, zero and positive prevention amounts with the same discard cost, and
both orders of a synthetic simultaneous prevention/removal ability whose other
effect can affect the same scheme. Repeated queries and rejected answers retain
the digest, RNG consumption, pending work and threat remainder; final digests,
RNG consumption and event sequences match unprobed controls.

The existing destination-reentry movement fixture now offers exhaustion of the
hero instead of zero prevention, and asserts that exhaustion. Its assignment
therefore retains positive unprevented threat, preserving the observation that
the departed/reentered scheme cannot receive it. A temporary simultaneous-effect
test answer omitted the required permutation targets; supplying `[0,1]` or
`[1,0]` corrects that fixture. It is not an unresolved production issue.

Final focused verification on the candidate source: **88 passed**. The filter
covers `CounterintelligenceAdmissionTests`, `BlackWidowPrevention` excluding
the separately owned `AttacrobaticsUnderGuard`, `ActualAbilityCompletionTests`,
`SimultaneousThreatReviewTests`, `ThreatMovement` and `RuleCitationTests` in
`Marvel.Content.Tests`, Release. External evidence is under
`C:/Users/mggar/AppData/Local/Temp/`:

- `counterintelligence-restored-build.log`: explicit `--no-incremental` build,
  zero warnings and errors.
- `counterintelligence-restored-test.log`: final restored 88-case pass.
- `counterintelligence-mutant-build.log` and
  `counterintelligence-mutant-test.log`: bypassing the new target check compiled
  and produced two intended failures out of five cases: zero prevention amount
  and the real fully prevented one-threat assignment. Restoration used a
  `finally` block and an explicit nonincremental rebuild.
- `counterintelligence-composite-pre.log`: running the final six new cases with
  the predecessor production file produced exactly those two expected failures;
  four controls passed, including both simultaneous-effect orders. Restoring the
  production fix and rebuilding nonincrementally preceded the final 88-case pass.

The ability book, public admission boundary, wire/save versions, RNG and digest
format are unchanged. Invalid zero-threat offers disappear; historical decisions
that selected such an offer are not claimed compatible. No broad historical
replay, public pack admission or integration acceptance is established here.
Independent review and the integrated candidate's broader gates remain required.

## Independent final packet review — findings remain open

Review candidate: `695d64ae` plus Guard engine dependency `194bef88` and its
independent review `32983ebc`, cherry-picked as `3731a864` and `8d96f972`.
The dependency conflict in `BlackWidowPreventionCompositionTests` retains both
the existing Grappling Hook tests and the added Guard tests. No ability row,
public admission, setup, or production source was changed by this review.

Each actual row was compared independently with `datasets/cards/cards.json` and
the current vendored Rules Reference 1.8, then with the local rulings and FAQ:

- **08006 Attacrobatics:** hero interrupt, attack label, discard cost, one physical
  faceup boost and damage equal to its canceled numeric icons match the printed
  text. Ruling `0fc2a7cc9197e127` permits independent cancellation under Guard
  and an attack with no attacked enemy; the historical `6919a92cb0376eac` answer
  does not govern that case. Ruling `1ea208998735ef4b` and the current timing
  order put constant/amplify icons before cancellation and Boost additions after
  it. Current `rr:labeled-ability.6` and `rr:resolve.7` govern stunned cancellation;
  historical `a693e5ba5379e414` does not establish successful resolution. Existing
  characteristic and dependency tests cover these distinctions. No row finding.
- **08008 Grappling Hook:** own revealed treachery, hero form, discard cost,
  cancellation and discard match the printed text. `rr:cancel.4` retains the
  revealed/discarded card; rulings `49d5aba04226396f` and `48dd96f060641cb8`
  include keywords. New real Hunted by Hydra (04106) reveal tests prove that
  accepting cancels Incite, both heroes' damage and random hand discard, while
  declining retains the upgrade and resolves those effects. The decline control
  explicitly answers the keyword/When Revealed ordering and each-player ordering.
  Existing tests cover wrong seat/form/kind and actual completion responses.
  No row finding.
- **08017 Counterintelligence:** either form, main scheme, any placing player,
  discard cost and three-threat prevention match the printed text. The target
  classification in `AbilityEffectTargetAdmission.ThreatPreventionTargetLegality`
  delegates to partial-resolution analysis; it does not suppress the occurrence
  or identify this particular card. `rr:target.3`, `.3.3`, `.3.4` and
  `rr:prevent.2` support rejecting a pure prevention effect after the remainder
  becomes zero and excluding the discard cost from eligibility. Existing tests
  prove actual copies across players, initial one/four threat, the preserved copy's
  next instance and query/rejection purity. New tests start with a real positive
  assignment, have player 0's actual copy fully prevent it, then prove player 1's
  synthetic prevention/removal ability may still affect the same scheme while
  player 1's actual Counterintelligence remains absent. Both simultaneous effect
  orders pass. No zero-gate finding.
- **08032 Defensive Stance:** hero form, own identity, discard cost and prevention
  of three damage match the printed text, which does not restrict damage to an
  attack. **Blocking runtime finding:** revealing actual Concussive Blast (01154)
  through `RevealStageOperation` places nonattack damage without offering the
  actual Defensive Stance. Both new accept/decline cases fail when the expected
  offer is absent. The test also requires own-seat damage prevention, unchanged
  damage to the other hero, actual Synth-Suit completion, preserved stunned
  status, card cleanup and finished agenda when this path is fixed. The direct
  damage path reaches `SimultaneousDirectDamage` / `DamagePlacement.Replace`;
  `AbilityDamageReplacementExecution` currently selects forced interrupts only.
  Attack-specific scheduler windows do not prove ordinary direct damage support.

No selected-face entries exist in the vendored MarvelCDB FAQ. The local rulings
search yielded no Counterintelligence or Defensive Stance clarification that
changes their printed prevention scope. The source audit confirms 628 prefix rows
equal `e04f2946` structurally, 632 total rows and the exact four-face suffix.
The complete ability-book Git blob equals `bcc7005b`:
`0c44b0dcc2908b0b76e85478cf9f8fa9bc473e3d`. Both Guard dependencies have no
dataset changes. Existing face/no-counter inventories run in the focused filter.
The destination-incarnation threat-movement fixture and its valid exhaustion
interrupt remain intact.

Final independent command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-restore --filter 'FullyQualifiedName~BlackWidowPrevention|FullyQualifiedName~CounterintelligenceAdmissionTests|FullyQualifiedName~ActualAbilityCompletionTests|FullyQualifiedName~SimultaneousThreatReviewTests|FullyQualifiedName~ThreatMovement|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests|FullyQualifiedName~RuleCitationTests'`.
Result: **137 passed, 2 failed**, with both failures confined to the new nonattack
Defensive Stance cases. Evidence outside the repository:
`C:/Users/mggar/AppData/Local/Temp/widow-prevention-independent-final.log` and
`widow-prevention-independent-audit.log`. Earlier `independent-new` and
`independent-combined` logs include malformed ordering answers in the new Incite
decline fixture; the final source supplies both required ordering decisions and
that control passes. No production finding is inferred from those fixture errors.

Verdict: the zero-threat fix and three other actual row interpretations withstand
this focused review; the four-row packet is **not accepted** until ordinary
nonattack damage offers Defensive Stance and the new regression passes. Existing
compiled zero-target and prevention-amount mutant evidence remains separately
recorded above. This review adds no production decision and claims no new mutant,
public pack admission, broad suite, native UI or full-game evidence.


## Nonattack damage prevention prerequisite — MARVEL-787

Candidate branch `fix/nonattack-damage-prevention` starts at `185eaea9`.
The preserved actual Concussive Blast/Defensive Stance use and decline regressions
both pass. Damage-taking prevention now has an exact nonattack assignment receipt
and ordinary optional window after forced replacement and Tough. Dealt damage is
retained separately. Individual and simultaneous direct damage both suspend before
placement; nested damage cannot borrow prevention and recipient reentry invalidates
the old assignment. Attack scheduling and the ability book remain unchanged.

New controls cover individual use/decline and the next instance, malformed-answer
purity, Tough/zero suppression, forced absorption precedence, recipient reentry,
nested same-target damage and actual Energy Barrier completion in both forms.
The previous Energy Barrier positive review only queried `Waiting` directly and
did not establish the live nonattack scheduling path.

Evidence is external under `C:/Users/mggar/AppData/Local/Temp/`: the
`nonattack-restored-build.log` is an explicit nonincremental restored build;
`nonattack-restored-test.log` covers the complete original review filter plus new
controls and damage/remaining-damage/Energy Barrier checks. The compiled mutant
bypasses ordinary-window scheduling, making both preserved Concussive Blast cases
fail; `nonattack-mutant-build.log` and `nonattack-mutant-test.log` retain that run.
Restoration used a finally block and a forced nonincremental rebuild. Additional
rules damage checks are in `nonattack-rules-test.log`. The final restored Content filter passed 193 cases and Rules damage filter passed
110 cases. The initial normal hook passed 66 architecture cases, then rejected
`DamageResolution.Continue` at CCN 11; preparation is now a cohesive helper and
normal hooks are rerun. No dataset, public
admission, RNG, digest schema or wire version changes are included. Historical
replay sequences that omitted these optional decisions are not claimed compatible.
Independent review and integrated broad gates remain required.
