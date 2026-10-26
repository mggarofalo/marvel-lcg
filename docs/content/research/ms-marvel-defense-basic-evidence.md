# Ms. Marvel defense and basic research packet

Base: `f715cd84abebce8c2473ccfb4bda2f9a8159d849`. This packet authors only
05012 Nova and 05013 Get Behind Me!: 569 total faces, 27 Ms. Marvel faces.
Public admission is unchanged. The existing expected-face inventory also gains
05025 and 05027, already authored in the base but missing from that gate.

Authority: raw `datasets/cards/cards.json` faces 05012/13/14/17/18, vendored
`datasets/marvelcdb-faq/faq.json` entries 05012/17 and the cited rulings below.
Nova's printed interrupt has no form or exhaustion restriction, spends an energy
resource, and damages the initiating enemy. Get Behind Me! has no restriction
to the revealing player: the paying hero receives the replacement villain attack.

## Evidence

`MsMarvelDefenseCardTests` uses the actual authored rows in nonpublic prepared
engine states. It covers Nova versus villain/minion in both forms while exhausted,
energy and attacked-controller restrictions, attacker defeat aborting the remaining
attack (faq:05012), and Get Behind Me! cross-seat cancellation, hero/treachery
restrictions, payment, nested attack completion and eventual event discard.
No public pack, replay/save, UI, or complete-game acceptance is claimed.

Final focused command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-restore --filter "FullyQualifiedName~MsMarvelDefenseCardTests|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests|FullyQualifiedName~RuleCitationTests"`.
External final log: `C:/Users/mggar/Source/ms-defense-twoface-final.log`.

Valid data mutants compiled and failed their intended behavioral assertions:
Nova damage 2 to 1 (3 failures); Get Behind Me! restricted to revealing seat
(1 failure); exploratory Energy Barrier prevention 1 to 2 (2 failures).
Each mutation loaded through the real parser/lowerer. The exact original dataset
bytes were restored in `finally`, followed by `dotnet build ... -t:Rebuild` and a
passing 54-test restored run before the two additional FAQ probes.
Logs: `C:/Users/mggar/Source/ms-defense-{nova,getbehind,barrier}-mutant.log`,
`ms-defense-restore-build.log`, and `ms-defense-restored.log`.

## Energy Barrier: unsupported remaining-damage admission

The exploratory 05017 row and failing full-attack test are preserved outside the
repository at `C:/Users/mggar/Source/ms-defense-energy-barrier-repro/` as
`05017.row.json` and `MsMarvelDefenseCardTests.with-failing-barrier.cs`.
The test `ASecondBarrierCannotTriggerAfterTheOnlyDamageWasPrevented` creates two
3-counter Barriers and a real `AttackStageOperation(DealAttackDamage)` with one
calculated damage. Resolve the first interrupt and choose the villain for its
1 damage. `Sequence.Work` still offers the other Barrier in the same damage
window; `Sequence.Finish` throws `RulesNotImplementedException` naming
`Interrupt to WhenDamageWouldBeDealt`. Expected: no further Barrier offer, zero
identity damage, one villain damage, counter pools 2 and 3, completed agenda.
Raw faq:05017 explicitly says the second cannot trigger after the only damage
was prevented. The final exploratory run was 55 passed / 1 failed; see
`C:/Users/mggar/Source/ms-defense-final.log`.

Owner: card window admission and the imminent-damage replacement context,
`AbilityWindowAdmission` / `AbilityWindowResolution`, together with the Rules
damage operation. Admission must inspect residual would-take damage without
consuming prevention, altering RNG, or leaking it into the following damage.
This face is removed from authored data and starting-counter inventory until the
engine capability receives independent implementation and review.

Related raw rulings: `ruling:90677f0ff8689684` permits preventing Focused Rage's
cost damage (the unpaid cost prevents its draw); `ruling:bce66dea550554f7`
places Barrier before Tiger Shark's attack-end tough; `ruling:d595cd69528dfd0a`
places forced damage interrupts before Barrier's optional interrupt.

## Preemptive Strike: unsupported per-card boost cancellation receipt

Minimal required scene: a villain attacks with two physical boost cards bearing
2 and 1 icons. Turn the first faceup, pay 1 for 05014, cancel only that card's
current icons, deal exactly 2 damage to that attacking villain, and retain the
second card's icon. The defense label establishes the paying hero as defender
when eligible, including another player's otherwise undefended attack. Star
Boost abilities still resolve; a previously canceled icon cannot be canceled a
second time and cannot supply dependent damage.

`StepConditions` exposes aggregate `WhenBoostCardsFlipped`, not the required
individual-card faceup occurrence. The closed DSL effect algebra lacks numeric
boost-icon cancellation and its actual-canceled-count receipt. The owning
primitive is the Rules boost-flip procedure with Cards lowering/execution and
safe projection. `cancelOccurrence` would cancel different work and is not a
substitute. Raw `ruling:1ea208998735ef4b` orders amplify/constants, faceup
interrupts, then Boost effects; `ruling:33d8c914aa86d19f` forbids double use of
already canceled icons; `ruling:937d6ff4f970a1da` includes modified icons.
No exploratory implementation or face row is committed.

## Lockjaw: unsupported passive discard-origin play permission

Minimal required scene: on the controller's turn, put 05018 in that player's
discard and provide 4 payable resources and free ally capacity. Ordinary play
offers must include that physical Lockjaw, preserving normal cost, uniqueness,
entry and responses. The same hand copy is an eligibility control; a foreign
player's discard copy and off-turn copy must not be offered. Resource payment
must not treat discarded Lockjaw as a hand resource.

`GamePrompts` enumerates hand cards; `CardPlayLegality` admits a nonhand card
only through `CardPlayOrigin.CanPlayEvent`, which is event-only and storage-only.
The Cards `makeTheCall` effect puts an ally into play through another resolving
ability and is not this printed passive play permission. Owning primitive:
engine-owned card-play origin/offer enumeration with an explicit live permission
from authored text. `ruling:f53a301e8096657d` expressly distinguishes discard
play from treating the card as in hand. No face row is committed.

## Separate Core finding

Existing 01078 Get Behind Me! has `trigger.player: trigger.player`, restricting
it to a revealed treachery's seat. Its raw printed text equals 05013 for this
behavior and contains no such restriction. The new row omits it; the valid-data
mutant restoring that restriction fails the cross-seat test. Core 01078 is
unchanged in this packet and requires a separately reviewed correction and
behavior/transcript consequences.
