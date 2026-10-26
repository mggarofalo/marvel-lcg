# Doctor Strange final support rows â€” MARVEL-819

Base `22d678049b5c6b646c7c69453b2d8fe80d57207b`, integrating the independently
reviewed owner-deck, physical status-host and exact upgrade-host prerequisites.
This packet appends actual **09013 Clea**, **09019 The Night Nurse**, and
**09039 Iron Man (Tony Stark, Leadership ally)**: **675 rows, 30/40 Doctor
Strange faces**. All 672 predecessor raw JSON objects are unchanged. SHA-256 of
their concatenated raw object text is
`36a87b708901993b6779b8687dcd40c95adf04c405fab055f625c4893aecdd3aa`.
No engine, setup, source-rule dataset, public admission or UI changes are included.

Authority is the exact generated `datasets/cards/cards.json` record for each face.
Clea's optional Interrupt runs before defeat and shuffles the same physical ally
into her owner's deck. RR 1.8 `rr:ownership-and-control.7.2` sends a departing
foreign-controlled card to its owner's equivalent area, or removes it when that
owner has left the game. The Night Nurse has three medical Uses, pays exhaustion
and one counter, chooses a hero, heals one and discards exactly one physical
status on that hero. Current `rr:and.1` resolves each independent effect as far as
possible. Local `faq:09019` and `ruling:ad2db09c24f9a27f` require discarding Tough
too; that answer agrees with current RR 1.8. Iron Man's constant reduces the cost
to play each upgrade on this exact ally by one, including foreign ownership and
control; RR initiation steps 3â€“5 assess the selected host before payment.
The existing DSL spellings and ordering are engine choices.

`NightNurseStatusHostTests.Board` now uses the actual book instead of appending a
duplicate 09019. All its effect-order, physical-status, last-counter, eligibility,
rejected-answer and paid-prompt query-purity assertions remain. The actual played
support pays one resource and enters ready with three medical Uses. The independent
starting-counter inventory includes the expected medical Uses rather than deriving
its expectation from the new row.

`DoctorStrangeSupportPrerequisiteTests` uses actual Clea for defeat acceptance and
decline under both same and foreign control. Deliberately alternate Night Nurse
programs and unknown-operation probes explicitly replace their production row in
the synthetic fixture. Unknown spellings are still rejected for the specified word,
without weakening duplicate-row admission. Global authored inventories are 675/30;
the Unflappable assertion remains bound to index 671 and its no-counter check.
The complete independent Doctor Strange face set includes all three additions.

`DoctorStrangeIronManCardTests` plays actual Inspired and Enraged through the real
root prompt onto either foreign Iron Man for zero or an ordinary ally for a paid
resource. It checks host-specific prices before payment, invalid free ordinary-host
payment, exact attachment identity, ownership/control and actual printed attack /
thwart bonuses. A second actual-play test proves floor zero, repeated discounts,
two real attachment bonuses together, an unaffected ordinary upgrade and drained
continuations. No empty upgrade programs substitute for production bonuses.

`DoctorStrangeCleaCardTests` compares the accepted actual defeat path with the exact
manually prepared owner-deck shuffle, checking physical deck order, all RNG words,
index and consumption, the unaffected controller deck, one movement and complete
agenda drainage. Decline, actual eliminated-owner and direct nondefeat discard
controls retain their distinct expected outcomes.

## Open engine finding

The actual eliminated-owner **decline** case fails on this base: ordinary defeat
cleanup sends Clea into the eliminated owner's discard, and `PlayerDeck.Reset`
then moves her to that player's empty deck. Expected destination is RemovedArea.
The accepted Interrupt already removes her correctly. The failing probe is
`DoctorStrangeCleaCardTests.DefeatInterruptUsesExactOwnerDeckAndSeededShuffle`
with `accept: false, eliminated: true`; its assertion is preserved. The lead owns
this separate engine repair. This packet is not accepted for integration until
that finding and the final combined checks are resolved.

## Verification

Evidence is outside the repository under `C:/Users/mggar/Source/strange-final-*`:

- `cards.log`: offline generated cards and search match the generator.
- `mutant-clea.log`: a valid actual-row ownerDeck â†’ yourDeck mutant fails the
  foreign-control owner assertion (one failure, eight passes).
- `mutant-nurse.log`: a valid actual-row exhaust-source â†’ exhaust-hero mutant
  fails source exhaustion (six failures, thirteen passes).
- `mutant-iron.log`: deleting Iron Man's Constant leaves a valid empty program
  and fails actual host pricing (five failures). An earlier unsupported-operation
  experiment was rejected at lowering and is excluded from mutation evidence.
- `restored-build.log`: exact candidate book restored, then nonincremental Release
  build succeeds with zero warnings/errors before final affected verification.
- `affected-final.log` and `results/strange-final-affected-final.trx`: whole related
  classes, all Doctor Strange classes, migrated Black Widow inventories, ability
  data/program/lowering and rule citations; final outcome is recorded below.

Focused tests and mutations do not establish full Content, native, second-platform,
complete-pack or public-product acceptance. Normal commit hooks are retained.

Final bounded verification: **243 passed / 244 total**, with the sole failure
being the preserved eliminated-owner decline finding above. Both inventory
corrections pass. No assertions were removed to accommodate that failure.
