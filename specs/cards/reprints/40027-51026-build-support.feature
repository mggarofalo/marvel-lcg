# Build Support, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Victory 0. When Defeated: Each player may
# search their deck and discard pile for a support with a cost of 3 or less and
# put it into play. (Shuffle.)"
#
# 40027 (Mutant Genesis / NeXt Evolution) and 51026 (Black Panther) are one
# printed card with two script files. They disagreed about the opt-in, and only
# one of the two spellings works at all:
#
#   * 51026 asked with an explicit `Player.MayChooseOneAbility` around a
#     mandatory `Search.PlayerCard`, and put the found support into play.
#   * 40027 passed `may=True` to `Search.PlayerCard`, which is the obvious
#     spelling and **silently finds nothing**. `may` widens the selector range
#     to (0, max); `EffectChecker.UpdateLegalTargets` then reports a target
#     range of (0, 0) and `PlayerAction.ChoiceAndSpellEffect` auto-resolves the
#     choice with no targets rather than asking anyone. No prompt is recorded
#     and the card does nothing.
#
# So the reprint was right and the original was the broken one. 40027 now uses
# the same shape. The two files also used to differ over `cost_equal_or_less=3`
# versus `cost_less_than=3`, which was never a difference -- both compiled to
# `printed_cost.val > 3`. the original investigation deleted the misnamed spelling, so both now
# read `cost_equal_or_less=3`, which is what "a cost of 3 or less" prints.
#
# The same `may=True` no-op affects six other cards -- see the report on
# the original investigation.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Build Support is a player side scheme, so `Given "<id>" is in play` puts it in
# the side-scheme area and `"<id>" has 1 threat` sets it one basic thwart from
# defeat. Spider-Man prints THW 1. Aunt May is a Support costing 1; Avengers
# Mansion costs 4 and is what the boundary scenario turns on.

Feature: Build Support

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:40027
  Scenario: 40027 searches the deck and puts the support into play
    Given my deck is "Aunt May", "Backflip", "Backflip"
    And "40027" is in play
    And "40027" has 1 threat

    When I thwart "40027"
    Then I am prompted to choose one
      | Search their deck and discard pile for a support with a cost of 3 or less and put it into play |
      | Cancel                                                                                        |

    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Aunt May" is in the "SupportsArea"
    And "40027" is in the "VictoryDisplay"
    And I am not prompted again

  @card:51026
  Scenario: 51026 searches the deck and puts the support into play
    Given my deck is "Aunt May", "Backflip", "Backflip"
    And "51026" is in play
    And "51026" has 1 threat

    When I thwart "51026"
    Then I am prompted to choose one
      | Search their deck and discard pile for a support with a cost of 3 or less and put it into play |
      | Cancel                                                                                        |

    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Aunt May" is in the "SupportsArea"
    And "51026" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40027
  Scenario: 40027 leaves the deck alone when the search is declined
    # "may". The card is defeated either way; what the player declines is the
    # search, not the trigger.
    Given my deck is "Aunt May", "Backflip", "Backflip"
    And "40027" is in play
    And "40027" has 1 threat

    When I thwart "40027"
    When I choose "Cancel"
    Then "Aunt May" is in the "PlayerDeck"
    And "40027" is in the "VictoryDisplay"
    And I am not prompted again

  @card:51026
  Scenario: 51026 leaves the deck alone when the search is declined
    Given my deck is "Aunt May", "Backflip", "Backflip"
    And "51026" is in play
    And "51026" has 1 threat

    When I thwart "51026"
    When I choose "Cancel"
    Then "Aunt May" is in the "PlayerDeck"
    And "51026" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40027
  Scenario: 40027 finds the support in the discard pile as well as the deck
    # "their deck **and discard pile**" -- two zones, and a scenario that only
    # ever stocks the deck cannot tell them apart.
    Given my discard pile is "Aunt May", "Backflip"
    And my deck is "Backflip", "Backflip"
    And "40027" is in play
    And "40027" has 1 threat

    When I thwart "40027"
    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Aunt May" is in the "SupportsArea"
    And I am not prompted again

  @card:51026
  Scenario: 51026 finds the support in the discard pile as well as the deck
    Given my discard pile is "Aunt May", "Backflip"
    And my deck is "Backflip", "Backflip"
    And "51026" is in play
    And "51026" has 1 threat

    When I thwart "51026"
    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Aunt May" is in the "SupportsArea"
    And I am not prompted again

  @card:40027
  @card:51026
  Scenario: a support costing 3 is inside the limit
    # The pair below is the boundary. "3 or less" is inclusive, so Pepper Potts
    # at cost 3 comes out and Avengers Mansion at cost 4 does not. Either
    # scenario alone is satisfied by an off-by-one in the other direction.
    Given my deck is "Pepper Potts", "Backflip", "Backflip"
    And "40027" is in play
    And "40027" has 1 threat

    When I thwart "40027"
    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Pepper Potts" is in the "SupportsArea"
    And I am not prompted again

  @card:40027
  @card:51026
  Scenario: a support costing 4 is outside it
    Given my deck is "Avengers Mansion", "Backflip", "Backflip"
    And "40027" is in play
    And "40027" has 1 threat

    When I thwart "40027"
    When I choose "Search their deck and discard pile for a support with a cost of 3 or less and put it into play"
    Then "Avengers Mansion" is in the "PlayerDeck"
    And I am not prompted again
