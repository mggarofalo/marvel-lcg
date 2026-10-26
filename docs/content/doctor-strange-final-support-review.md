# Independent final support and discard review

Reviewed content `a2714257903c3f36dfedb997e673278310077b58` with engine
`43c0b046cefd985d95d35f704856bdf007aef22e`, cherry-picked here as
`eab85238`. No engine or ability-book edits are part of this review packet.

## Verdict

The three actual rows are faithful to generated printed text: Clea's optional
defeat Interrupt shuffles the same ally into its owner's deck; Night Nurse pays
source exhaustion and one medical counter for the independent heal-one and
discard-one-status effects; Iron Man discounts each upgrade on that exact ally
by one. Current RR 1.8 `rr:and.1` and `rr:uses-x-type` govern Night Nurse.
The historical `faq:09019` / `ruling:ad2db09c24f9a27f` Tough clarification agrees
with those rules. All 672 predecessor raw JSON objects compare exactly against
the content commit's parent; the candidate has 675 rows and 30 Strange faces.

Content is accepted within this bounded, nonpublic row scope, conditional on
repairing and reviewing the engine finding below before integration. Existing
actual Iron Man tests exercise Inspired and Enraged bonuses and exact host prices,
foreign ownership, floor zero and ordinary-host controls. The original Clea
eliminated-owner declined-Interrupt case now passes with the engine prerequisite.
No complete-pack, public admission, UI or second-platform acceptance is claimed.

The engine prerequisite is rejected for integration pending the retained failing
regression `EliminatingAnEmptyDeckSeatDoesNotResetOrConsumeAnEncounterCard`.
Its intended foreign-area destination is correct, including separate attachment
cleanup, but elimination of a player with an initially empty deck still calls
`PlayerDeck.Reset` during step 4. The probe's encounter deck changes from
`[6, 7, 8]` to `[6, 7]`: one encounter card is dealt to the eliminated player,
then removed during step 5. The ally also travels through the reset deck instead
of the required discard-to-removal sequence. This is observable shared-deck state,
not merely extra logging.

RR 1.8 `rr:player-elimination.step.4` places owned cards in the eliminated
player's discard, then step 5 removes that player's areas and says eliminated
players no longer participate; `.6` says effects referring to players ignore
eliminated players. `rr:player-deck.4` continues to require delayed resets for
active owners, which the original active-owner controls retain. Suppressing the
eliminated owner's reset must preserve both elimination movement events.

## Independent evidence

Windows Release, isolated review worktree, normal hooks retained. Logs live
outside the repository at `C:/Users/mggar/Source/support-review-*`.

- `baseline.log`: 234 relevant existing Content cases pass on the combined
  source tree, including the originally failing Clea decline. Exact filter and
  results are retained in `results/support-combined-baseline.trx`.
- Four new actual-row cases pass: controller seat 1 / owner seat 2 Clea on an
  empty owner deck distinguishes direct shuffle (no encounter penalty) from
  decline (owner deck reset and owner encounter penalty); foreign-controlled
  Night Nurse's final Use completes heal and exact physical status discard in
  both effect orders after she leaves play. Ordering belongs to the first player;
  the subsequent physical-status decision remains with the activating controller.
- `mutant-heal.log`: valid production Night Nurse heal amount 1 -> 2 compiles
  and fails both independent final-Use cases at expected damage 1 / actual 0;
  both Clea controls pass. This mutation changes only that actual row.
- `restored-build.log`: the exact ability-book bytes are restored, followed by a
  nonincremental Content project build with zero warnings/errors.
- `final-content.log` / `results/support-combined-final.trx`: all 238 selected
  relevant existing and independent Content cases pass after restoration.
- `mutant-elimination.log`: reversing the eliminated-owner predicate compiles
  and fails three of five complete actual Clea cases, including the eliminated
  decline and active-owner controls. No rejected lowering is counted as a kill.
- `final-restored-build.log`: exact engine source restored and nonincrementally
  rebuilt with zero warnings/errors. `final-restored-content.log` and
  `results/support-restored-final.trx` pass all 240 selected Content cases,
  adding the complete independent physical-status-host class to the prior filter.
- `new-rules.log`: 10 original/citation cases pass, and the independent empty-deck
  elimination probe fails for encounter-deck consumption. The failing assertion
  is retained for the separate repair rather than weakened or skipped.
- `final-rules.log`: the complete Rules assembly passes 1,181 cases and fails
  only that retained regression (1,182 total).

Initial independent Night Nurse test failures were fixture expectations: the
effect-order prompt belongs to the first player, and source exhaustion must be
observed in the payment event before departure resets out-of-play state. Those
expectations were corrected before mutation evidence; no source behavior changed.

The lead owns the separate repair and a fresh independent verification. Broad
Content, native and platform runs remain outside this bounded review.

## Integrated support closure

Source `a2714257`, independent review `be3bc369`, owner-discard source
`43c0b046`, reset repair `a6bb7ddd` and independent closure `7cdb2e23` are
combined with reviewed Astral, Magic Blast and reveal-origin prerequisites.
The book contains 675 rows / 30 Strange faces; all 672 predecessor raw objects
remain identical. Combined Content selection: 554 pass with two inventory/docs
failures. The omitted permitted faces and DSL documentation for `statusCardsOn`
and `reduceUpgradeCostOnThis` are corrected; both complete affected classes
then pass all 33 cases. No behavior assertion is weakened. All 124 selected
Rules elimination/discard/deck/citation cases pass, including the independent
Invocation and active-owner reset controls. Logs:
`Source/evidence/strange-support-events-integrated.log`,
`strange-support-inventories-reconciled.log`, and
`strange-support-rules-integrated.log`. Full pack admission remains open.