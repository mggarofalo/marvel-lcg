# Black Widow event first composition

Base `6447b459`, 2026-10-26. These are nonpublic, test-local full printed
interpretations of `08003` Covert Ops and `08004` Dance of Death. No production
ability row, setup, admission or engine source changes are included.

Printed authority is `datasets/cards/cards.json`. Covert Ops costs three and is
an Action (thwart): remove four threat from a scheme, then confuse the villain.
Dance of Death costs three and is a Hero Action with no outer attack label:
perform three attacks dealing one, two and three damage in order.

Current RR 1.8 `rr:labeled-ability.6` cancels the entire labeled ability except
costs. `rr:attack-player-ability-type.3` makes Dance's three instances separate
attacks; `.3.1` distinguishes modifiers to one attack. `rr:target.3` permits an
ability when any part can affect its target. Vendored Covert Ops ruling
`0fc2a7cc9197e127` (October 2024, scope 1.6) expressly permits confusion alone
without a valid scheme target. It is consistent with current target rules.
Historical Dance rulings `455cba4abf6a78cf`, `4bbba517135f51fc` and
`98c1b2bd0b2ab525` agree with the current separate-attack rule; they do not
replace current RR 1.8 damage/actual-defeat timing.

## Observations

`BlackWidowEventCompositionTests` provides eleven cases:

- Covert Ops removes exactly four and confuses in either form.
- Confusion on the second player's identity cancels both sentences, pays all
  three resources, and preserves the first player's confusion, with six or zero
  threat on the main scheme.
- An already-confused villain permits removal of the remaining two threat;
  without threat, that same status cannot alone make the event legal.
- Dance offers three separate choices with settled cumulative damage between
  them; stun cancels only the first attack. All choices belong to the second
  player. Another case targets villain, another seat's minion, then the villain
  again, dealing four and two damage respectively.
- Two explicit diagnostics preserve unsupported Covert Ops compositions. They
  assert engine rejection rather than claiming correct confusion-only behavior.

Covert Ops' confuse sentence is inside its one thwart body. An independent tail
trial with an already-confused villain compiled but failed while constructing
the scheme choice (`requires a choice and has no legal option`); it is not the
final interpretation.

## Remaining mechanism

With no valid scheme, the unconditional `chooseCard(thwartableSchemes)` requires
a choice before the thwart and rejects initiation, despite the valid villain
confusion effect. Guarding that choice with `if exists(thwartableSchemes)` cannot
represent a branch that merely confuses: `AbilityInitiation.ValidatePowerLabels`
requires `GuaranteesOneLabeledPower` on every branch and raises
`has a thwart label without exactly one saveable thwart power`.

The power representation requires a concrete scheme: `CanTargetThwart` admits a
scheme and `AbilityStructuralPowerExecution.Power` requires a reconstructable
target. `automaticTarget` still requires a scheme, honors Patrol, and is for an
already-determined target; assigning the main scheme would invent a target for
the confusion-only resolution. Omitting the outer label would lose its whole
ability status cancellation and its classification as a thwart. These are not
sound substitutes. The next engine packet must support a labeled thwart
resolution without a scheme target when only its independent effect resolves,
including its ownership, status cancellation and completion occurrence.

## Evidence and limits

External evidence directory:
`C:/Users/mggar/AppData/Local/Temp/widow-events-proof/`.
`seat-correct.log` and `compiled-red-probe.cs` preserve the first compiled
confusion-only failure; `conditional.log` and `conditional-red-*.cs` preserve the
conditional trial. `extended.log` and `independent-tail-rejected-fixture.cs`
preserve the independent-tail counterexample. `full-body.log` passes thirteen
cases including rule-citation checks on the final full-body composition.

The compiled final Dance damage mutation three to four fails the unstunned and
stunned expectations (six/five versus seven/six). A restored nonincremental
Release build and the same focused/citation checks pass. No full suite or native
journey was run. Retaliation, intervening defeat/retargeting, attack modifiers,
post-first-attack stun and public admission remain unverified. Covert Ops is
blocked on its legal targetless partial effect; neither event is production
readiness evidence.
