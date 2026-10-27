# Judge, Jury, Executioner composition probe

This is an unsupported, test-local prototype for printed card **90002**, not
authored production content or public Kree Fanatic admission. The production
ability book remains at the supplied base `ca149e0d` (719 authored faces).

Printed authority is `datasets/cards/cards.json`, card 90002:
“After a friendly character is defeated by an enemy attack, place 2 threat on the
main scheme.” Current RR 1.8 `rr:attack-enemy-activation.step.6.a` groups forced
attack/damage/defeat responses at attack completion;
`rr:triggering-condition.2` explicitly describes one attack both damaging and
defeating a character. The vendored MarvelCDB FAQ lists 90002 in its inventory
but contains no card-specific FAQ entry for that code.

## Executable observations

`RonanJudgeCompositionProbeTests` contains six passing diagnostic cases. A
test-local 90002 body is combined with `AuthoredCards.Book`; its face is also
added to that local book's `Authored` set. Synthetic Web-Shooter observers
replace only that fixture's 01008 abilities to expose the real timing windows.

The full reveal procedure puts Judge into the side-scheme area with six threat
for two players. A complete Rhino I attack targets player 1. The explicitly
chosen Shuri defender belongs to player 0 or player 1. The fixture checks the
printed ally kind and effective maximum health of three. Armored Rhino Suit is
the zero-icon boost; twelve encounter cards and eight cards per player prevent
unintended deck-exhaustion termination.

Rhino deals exactly two damage. A pre-damaged Shuri is defeated and discarded;
an undamaged Shuri survives. In the lethal cases, a synthetic Forced Interrupt
at the genuine defeat window fires once. At attack completion, a separate
synthetic forced damage response fires once for the exact ally recipient.
An optional attack-end observer then exposes these established facts:

| Fact | Lethal defender | Surviving defender |
|---|---|---|
| Actual received-attack-damage receipt | Exact enemy, ally, controller, amount 2 | Same |
| Defeat interrupt observation | 1 | 0 |
| Attack-end `Occurrence.Defeats` | Empty | Empty |
| Attack-end `DamageFromAttack` | False | False |
| Prototype main-scheme threat | **0; printed requirement is 2** | 0 |

Both candidate associations fail in the lethal cases:
`WhenAttackEnds` plus `WhenCardDefeated`, and `WhenDamageDealt` plus
`WhenCardDefeated`, with `ForcedResponse`, `subject: game`, `actor: enemy`,
`player: trigger.player`, and the defeated-ally/identity predicate. The latter
is deliberately broader than the external candidate: it omits
`damageFromAttack`, yet still cannot bind a defeat. Restoring that predicate
cannot recover the absent receipt, and its observed false value at step six is
an additional mismatch with that external candidate.

Every case subsequently drains the real continuation and asserts an empty
agenda, no live attack/activation, a non-ended game, exactly one
`AttackCompleted` naming the selected ally, and Judge still in play. The zero
assertion is explicitly a diagnostic of unsupported behavior, not a passing
acceptance claim for the printed card.

## Responsible boundary and next action

`AttackDamageResolution` settles defeat against its owned execution occurrence.
`AttackDamageReceipts.Record` retains positive damage and original role facts.
`AttackCompletion.Finish` transfers those damage receipts into the separate
step-six occurrence, but does not transfer causal defeat receipts.
`AbilityTriggerInstances.DefeatInstances` requires the actual occurrence's
`Defeats` collection; an `alsoHappened` association cannot invent that binding.
The demonstrated prerequisite is Rules-owned transfer of exact enemy-attack
defeats into the shared attack-end response occurrence, preserving incarnation,
controller, actor and causal attack identity. It is not a renderer/setup issue
or a reason to infer defeat from the current discard pile.

Next, implement and independently review that bounded Rules prerequisite, then
replace the diagnostic zero expectation with authority-derived 90002 acceptance
and author the actual row. Include nonattack and Boost-ability damage negative
controls, prevented/replaced defeat, remote control, and multiple causal defeats
(overkill/indirect where reachable). This probe establishes the ordinary lethal
ally path only; it does not establish identity elimination, multiple defeats,
Boost entry, nonattack filtering, or complete card correctness.

## Reproduction and evidence

Run:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter FullyQualifiedName~RonanJudgeCompositionProbeTests --tl:off
```

Observed: 6 passed, 0 failed. Exact external evidence:

- `C:/Users/mggar/Source/evidence/ronan-judge-probe-run.log`
- `C:/Users/mggar/Source/evidence/ronan-judge-probe/ronan-judge-probe.trx`

The final test run includes the full-book injected prototype and intermediate
Spider-Sense declines. Earlier unsuccessful fixture/build attempts are not
acceptance evidence.
