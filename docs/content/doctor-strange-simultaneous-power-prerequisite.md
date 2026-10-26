# Doctor Strange simultaneous labelled-power prerequisite (MARVEL-827)

Base: `fb6f6a1dc2b54b174214a3a3ea078c4d504996f7`.
Branch: `feat/magic-simultaneous-power`. This document belongs to the commit that
contains the implementation and tests described below.

The prerequisite admits two bounded independent pairs within one labelled power:
constant attack damage to the chosen recipient plus discarding one top card of the
resolving player's deck, and constant threat removal from the chosen scheme plus
private inspection of one encounter card. Existing structural order, procedure and
power continuations retain the original label, target, modifiers and occurrence.
The production ability book and public content admission are unchanged.

A containing sequence supports only the receipt-dependent chosen-recipient tails
needed by Magic Blast and Astral Projection. Arbitrary subsequent deck-state reads
remain unsupported because an own-deck reset can change encounter-card state.
Projection advances the pair's mutation once, rather than ignoring its damage or
inventing a second damage instance while examining the observation.

## Authority

Printed facts: generated `datasets/cards/cards.json`, faces 09004 and 09003.
Current RR 1.8 `rr:and` explicitly says effects resolve simultaneously; `rr:and.1`
makes them independent and requires as much of each effect as possible. The existing
structural order prompt is retained, including either selected order.

Pre-1.5 rulings `ruling:e4ea1bf855e41aac` (5 damage and conditional 2 damage are
separate instances) and `ruling:d1f9f33e3ae4ba2f` (deck reset before event discard)
remain consistent with current `rr:damage`, `rr:event` and `rr:player-deck.1`.
Current `rr:event.5` modifies every instance of an event's damage. Printed resource
presence, including wild satisfying all three branches, follows the actual row and
`ruling:59bea1238c870653`; a double resource does not repeat a branch.

## Evidence

Test-local actual Magic Blast row on a nonpublic engine fixture: both selected
orders for physical, energy, mental and wild; exact discarded receipt surviving
final-card reshuffle; defeated minion receives no later branches and no replacement
target; one attack boundary; modifier applies to separate 6 and 3 damage receipts
and is consumed once; suspended damage causal metadata matches an uninterrupted
power control. The fixture does not establish Doctor Strange pack admission.

Synthetic Astral-shaped outer thwart body: remove 3 and privately inspect one,
then remove the exact recorded boost count, in both selected orders. Acknowledgment
leaves the inspected encounter card facedown and in its deck. The separately owned
scheme-cost prerequisite is required before authoring the complete actual row.

The final focused suite passed 35 tests, including existing labelled defeat,
dependency, continuation, attack identity and rule-citation classes. Restored
sources were rebuilt nonincrementally before the final focused run. Logs are in
`C:/Users/mggar/AppData/Local/Temp/`:

- `magic-red.log`: the original six actual-row cases fail labelled-power admission.
- `magic-final-build.log`: restored nonincremental build passes.
- `magic-final-focused.log`: 35 tests pass, zero failures or skips.
- `magic-mutant-reject.log`: compiled disabled recognizer fails 15 of 16 cases.
- `magic-mutant-wide.log`: compiled three-child admission fails the negative control.
- `magic-mutant-tail.log`: compiled permissive continuation guard fails because no
  exception is thrown for the unsupported deck-reading tail.
- `magic-commit.log`: normal commit-hook results, without bypass.

The temporary Astral full-tail probe (`magic-astral-full-probe.log`) failed its
old base-only threat assertion by observing the correct additional removal. The
permanent full-tail test replaces that probe and passes in the final suite.

## Limits and next integration proof

This is an engine prerequisite, not pack admission. Actual Astral choose-scheme
cost integration, Norman Osborn/Green Goblin same-physical-card flip, departed and
reentered target incarnation, and save/replay roundtrip are not proven here.
Independent review and combined integration gates remain with the delivery lead.
