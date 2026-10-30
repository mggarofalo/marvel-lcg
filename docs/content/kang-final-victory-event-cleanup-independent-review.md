# Independent review of terminal cleanup repair

Reviewed `876b65bb4b4fbcfe3984af27c28f558e86690328`, based on `3166260c521bd8ae5f5551aad109d77dbba07c1cb`. **Accept the terminal cleanup repair for its reviewed scope.** The shared event cleanup check prevents a terminal event from being discarded after victory; power completion and ordered entry batches stop before subsequent observable work. The three corresponding synthetic controls also assert normal nonterminal completion. These are generic interpreter tests, not additional Kang card text.

The pre-feature labelled-power fixture's two cases genuinely passed on `e7e3e745658521d69ed3e9dec72644880a9c7126`. Its separate diagnostic revealed that both were already `PlayersLose / EncounterCardsExhausted` while the prior engine continued to satisfy the original assertions. The corrected fixture adds one player-deck card, explicitly asserts `Outcome.Unfinished`, and retains all original assertions. The baseline and diagnostic logs support this distinction; the old test did not fail on the old base.

The source's combined compiled omission mutant fails all three terminal observations and passes the three controls; it establishes the boundaries together rather than independent guard necessity. The restored nonincremental build reports zero warnings and errors, and the selected regression reports 256 passing cases. Detailed paths and earlier failed or diagnostic attempts are recorded in [the repair evidence](kang-final-victory-event-cleanup-repair.md).

An independent Release run built current sources and passed 28 tests in `PlayersWinEventCleanupTests`, `PlayersWinChoiceTests`, `PlayersWinInstructionTests`, `KangCentralTransitionTests`, and `LabelledPowerDefeatContinuationTests`, including the original event failure and both Kang III modes. Log: `C:/Users/mggar/Source/evidence/victory-cleanup-independent-final.log`.

Direct `playersWin` options remain explicitly unsupported before a prompt. This review does not claim full managed, native, second-platform, public-admission or complete-pack coverage.
