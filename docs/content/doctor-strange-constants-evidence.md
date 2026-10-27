# Doctor Strange constants and resources (MARVEL-801)

Bounded content packet on base `2660ed01`, branch `feat/doctor-strange-constants`.
The book has 654 unique rows: the prior 651 raw JSON objects are preserved
(checkout newlines normalized for comparison), followed by actual printed faces
09009 Cloak of Levitation, 09011 The Eye of Agamotto and 09026 The Sorcerer Supreme.
This is 9 of 40 Doctor Strange faces, with no starting counters, setup, public
admission or engine changes.

Authority is joined printed `datasets/cards/cards.json` and current RR 1.8:
ability 8.2/9/13, initiating abilities steps 2 and 5, and play restrictions and
permissions 1. The vendored FAQ inventory contains no individual clarification
for these three faces. The ordinary constant, ready, named-title selector,
resource-cost and current printed-play-prerequisite mechanisms are reused.

`DoctorStrangeConstantCardTests` uses the actual three rows and explicitly empty
synthetic identity bodies to isolate this packet from Invocation support. It
proves a non-first seat's Aerial in both forms and loss on departure; effective
hero-only Cloak exhaustion/ready and absence when ready or exhausted; Eye wild
resource/controller/form/exhaustion; hand-size changes across forms without
upgrade departure; and a legal same-cost Cloak control for Supreme's restriction.
`DoctorStrangeEyeSuspensionTests` retains the actual Eye/Cloak bodies with explicitly
synthetic lethal-generator and optional-defeat-interrupt bodies. Eye stays
exhausted and unavailable during the later generator's suspended cost, invalid
reuse preserves digest/RNG/agenda, and the payment continuation completes once.
This is engine-context research evidence, not a public Doctor Strange game.

Verification logs are outside the repository under `C:/Users/mggar/Source/`:

- `strange-constants-restored-build.log`: forced nonincremental Release build,
  zero warnings/errors after restoring all row mutations.
- `strange-constants-restored-test.log`: 45 focused tests pass, covering the
  Doctor Strange subset, ability-data inventory and affected Black Widow row
  counts. The Supreme restriction theory is excluded because of the prerequisite
  below; its two Mystic positive cases passed in the preceding focused run.
- `strange-mutant-cloak-trait.log`: valid lowered constant granting Mystic instead
  of Aerial fails the Aerial assertion.
- `strange-mutant-cloak-form.log`: valid lowered alter-ego Cloak action fails the
  hero action offer assertion.
- `strange-mutant-eye-valid-resource.log`: valid lowered Eye energy generation
  (both generated and printed resource fields changed) fails expected wild.
  The earlier mismatched Eye mutant is a lowering rejection and is not a
  behavioral mutation kill.
- `strange-mutant-supreme-form.log`: valid lowered alter-ego hand-size condition
  fails both form-dependent hand-size cases.
- `strange-constants-hooks.log`: normal pre-commit architecture/complexity/length
  gates; the final hook result is recorded with this packet's commit handoff.

The row mutants run against the compiled Release test assembly and are lowered
from the mutated actual data on each new test process. Each fails at its intended
behavioral assertion; all restored characteristic tests pass after the forced
nonincremental build. No broad suite or native evidence is claimed by this slice.

## Required separate prerequisite

**The three-face packet is not accepted until the printed-prerequisite fix is
integrated and the complete restriction theory passes.**

`PrintedPlayRestrictions.IdentityTrait` only recognizes the prefix
`Play only if your identity has the `. Supreme prints
`Play only if you have the Mystic trait.` Consequently the current catalog fails
to report its Mystic prerequisite and offers it to a non-Mystic identity. The
otherwise-valid same-cost control and preserved non-Mystic negative test expose
this defect. The two current Mystic forms already play successfully. The parent
owns the separate generic printed-text parser implementation and independent
review; this content packet does not edit the engine or weaken the assertion.

An initial suspension fixture used the default alter-ego Playing state, making
Eye unavailable before payment. The final fixture explicitly starts in hero form.
The initial wrong-cost controls were corrected against generated printed values;
these fixture failures are not engine findings.

## Independent combined review (2026-10-26)

The independent reviewer combined content commit `60e4a9f7` with prerequisite
`e34f921a` (review-worktree cherry-pick `fc6e03698cc64cf339582ec791ffcb2e6311ccf8`).
Review covered actual printed 09009/09011/09026, RR 1.8 you/your, identity,
traits, form, constant/triggered abilities and printed restrictions, the generic
reader, current effective-trait legality, rejected-play purity and payment
suspension. No blocking correctness finding remains in either bounded scope.
No card ID or trait name is special-cased by the reader; controlled-character
restrictions remain separate. The 651 preceding raw JSON object texts are
unchanged after checkout-newline normalization; only the intended three unique
rows are appended, giving 654 rows. Inventory edits preserve the existing
negative controls and do not expand public admission.

The complete unchanged `SupremeRequiresCurrentMysticWithValidSameCostControl`
theory passes with the prerequisite integrated. The reviewer added
`GrantedMysticAllowsActualSupremeAndLaterTraitLossDoesNotDiscardIt`: a non-first,
non-Strange identity gains Mystic, pays for the actual upgrade, then loses the
trait while retaining the upgrade and its hero-form bonus. This distinguishes
the play prerequisite from a continuing condition on the hand-size effect.
The prepared deck contains a card to keep the payment in discard rather than
triggering empty-deck recycling.

Final Release evidence, including the added review test: **119 passed, zero
failed/skipped**, in `C:/Users/mggar/Source/strange-constants-review-final.log`.
The filter includes all `DoctorStrange`, `HaveTraitPlayRestrictionTests`,
`IdentityTraitPlayRestrictionTests`, `ControlledCharacterPlayRestrictionTests`,
`AbilityData`, the four affected Black Widow inventory/card classes and
`RuleCitationTests`. No Supreme exclusion applies. The earlier combined run
before adding the review test passed 118 tests, recorded separately in
`strange-constants-review-focused.log`. Normal pre-commit gates remain enabled;
their result is in `strange-constants-review-hooks.log`.

Verdict: approve the combined three-face research packet and generic parser
prerequisite for integration. This review does not establish complete Doctor
Strange admission, whole-game behavior, native UI or platform certification.
