# Secret Rendezvous, stage 2A. Printed: "When Revealed: Discard cards from the
# encounter deck until a minion is discarded. Put that minion into play engaged
# with the first player. Advance to stage 2B"
#
# The same two-branch search 1B prints, plus a third sentence 1B does not have:
# 2A is a transitional face that never sits on the board, so every scenario
# that reveals it also has to see it leave. That advance is what the two
# scenarios below assert alongside the branch they are each about, because it
# happens on both paths and an engine could plausibly tie it to the minion.
#
# ---------------------------------------------------------------------------
# How the transcripts reveal it.
#
# 2A is in the main scheme deck at the start of a klaw game and arrives when
# stage 1B completes, so the honest way to reveal it is to complete 1B: `Given
# the main scheme has 6 threat` is a real threat placement through
# `RunPuzzle`, 6 is 1B's per-hero completion threshold at one hero, and the
# advance chain runs from there. The encounter deck is stocked *before* the
# threat step, because Given steps apply in the order they are written and 2A
# reads that deck the moment it is revealed.

Feature: Secret Rendezvous (2A)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01117a
  Scenario: completing 1B reveals 2A, which digs out a minion and advances
    # All three printed sentences on one board. Two cards are discarded to
    # reach the minion, the minion arrives engaged, the card under it is
    # untouched, and the main scheme is left showing 2B -- printed stage 2,
    # completing at 8 threat rather than 1B's 6.
    Given I am in hero form
    And the encounter deck is "Sonic Boom", "Klaw's Vengeance", "Armored Guard", "Armored Guard"
    And the main scheme has 6 threat

    Then "Sonic Boom" is in the "EncounterDiscardPile"
    And "Klaw's Vengeance" is in the "EncounterDiscardPile"
    And "Armored Guard #1" is in the "EngagedEnemiesArea"
    And "Armored Guard #2" is in the "EncounterDeck"
    And "the main scheme" has 2 "printed_stage"
    And "the main scheme" has 8 "target_threat"
    And the main scheme has 0 threat

  @card:01117a
  Scenario: with no minion to find it still advances
    # The branch, and the reason the advance is asserted twice. The deck holds
    # a treachery and a side scheme and runs out without producing a minion, so
    # nothing is put into play -- and stage 2B arrives anyway, because
    # "Advance to stage 2B" is not conditional on the search having found
    # anything.
    #
    # The spare Armored Guard is in the encounter discard pile, which the
    # printed text does not search, so "nothing entered play" is a claim about
    # where the engine looked rather than about an empty board.
    Given I am in hero form
    And the encounter discard pile is "Armored Guard"
    And the encounter deck is "Sonic Boom", "Klaw's Vengeance"
    And the main scheme has 6 threat

    Then "Armored Guard" is not in play
    And "the main scheme" has 2 "printed_stage"
    And "the main scheme" has 8 "target_threat"
    And the main scheme has 0 threat
