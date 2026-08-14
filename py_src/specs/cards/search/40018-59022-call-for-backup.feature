# Call for Backup, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Victory 0. When Defeated: Each player may
# search their deck and discard pile for an ally and put it into play.
# (Shuffle.)"
#
# 40018 (NeXt Evolution) and 59022 (Hercules) are the reprint pair MARVEL-106
# confirmed cosmetic -- the two scripts differ only in local names. Both spell
# the opt-in as `Search.PlayerCard(..., may=True)`, and until MARVEL-112 that
# spelling could not be accepted by any automated player: `may` widened the
# selector to (0, max), and picking the minimum -- what `BotCommand.Build` and
# the engine's own forced-effect path both do -- meant taking no card at all.
# The prompt that did go out carried no name and no cancel button, so there was
# neither a "yes" to read nor a "no" to choose.
#
# `SearchInternal` now leaves the range alone and offers the decline as its own
# option, so the pair below is the shape every "may search" card gets:
#
#   | Search |
#   | Cancel |
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Call for Backup is a player side scheme, so `Given "<id>" is in play` puts it
# in the side-scheme area and `"<id>" has 1 threat` leaves it one basic thwart
# from defeat. Spider-Man prints THW 1. Black Cat and Hawkeye are allies;
# Backflip is an event and is what the filter has to leave behind.

Feature: Call for Backup

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:40018
  Scenario: 40018 offers the search and puts the ally into play
    Given my deck is "Black Cat", "Backflip", "Backflip"
    And "40018" is in play
    And "40018" has 1 threat

    When I thwart "40018"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Black Cat" is in the "AlliesArea"
    And "40018" is in the "VictoryDisplay"
    And I am not prompted again

  @card:59022
  Scenario: 59022 offers the search and puts the ally into play
    Given my deck is "Black Cat", "Backflip", "Backflip"
    And "59022" is in play
    And "59022" has 1 threat

    When I thwart "59022"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Black Cat" is in the "AlliesArea"
    And "59022" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40018
  Scenario: 40018 leaves the deck alone when the search is declined
    # "may". The scheme is defeated either way; what the player declines is the
    # search, not the trigger.
    Given my deck is "Black Cat", "Backflip", "Backflip"
    And "40018" is in play
    And "40018" has 1 threat

    When I thwart "40018"
    When I choose "Cancel"
    Then "Black Cat" is in the "PlayerDeck"
    And "40018" is in the "VictoryDisplay"
    And I am not prompted again

  @card:59022
  Scenario: 59022 leaves the deck alone when the search is declined
    Given my deck is "Black Cat", "Backflip", "Backflip"
    And "59022" is in play
    And "59022" has 1 threat

    When I thwart "59022"
    When I choose "Cancel"
    Then "Black Cat" is in the "PlayerDeck"
    And "59022" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40018
  @card:59022
  Scenario: the search offers every ally and nothing else
    # "for an ally". Two allies, so the transcript has to name the one it takes
    # -- and Backflip, an event sitting in the same deck, must not be offered.
    Given my deck is "Black Cat", "Hawkeye", "Backflip"
    And "40018" is in play
    And "40018" has 1 threat

    When I thwart "40018"
    Then the legal targets for "Search" are
      | Black Cat |
      | Hawkeye   |

    When I choose "Search" targeting "Hawkeye"
    Then "Hawkeye" is in the "AlliesArea"
    And "Black Cat" is in the "PlayerDeck"
    And I am not prompted again

  @card:40018
  @card:59022
  Scenario: the search reaches the discard pile as well as the deck
    # "their deck **and discard pile**" -- two zones, and a scenario that only
    # ever stocks the deck cannot tell them apart.
    Given my discard pile is "Black Cat", "Backflip"
    And my deck is "Backflip", "Backflip"
    And "40018" is in play
    And "40018" has 1 threat

    When I thwart "40018"
    When I choose "Search"
    Then "Black Cat" is in the "AlliesArea"
    And I am not prompted again

  @card:40018
  @card:59022
  Scenario: nobody is asked when there is no ally to find
    # The opt-in is a choice between two abilities, so an empty search has no
    # legal targets, `Selector.GetTargetRange` returns None, and the option is
    # filtered out before the prompt. Only "Cancel" is left, and a lone Cancel
    # resolves without asking. The player is asked exactly when there is
    # something to say yes to.
    Given my deck is "Backflip", "Backflip", "Backflip"
    And "40018" is in play
    And "40018" has 1 threat

    When I thwart "40018"
    Then "40018" is in the "VictoryDisplay"
    And I am not prompted again
