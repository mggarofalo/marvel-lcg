# Lock and Load.
#
# Printed: "Victory 0. When Defeated: Each player may search their deck and
# discard pile for a [[WEAPON]] upgrade with a cost of 3 or less and put it into
# play. (Shuffle.)"
#
# One of the six cards whose "may search" clause did nothing until the original investigation:
# `Search.PlayerCard(..., may=True)` widened the selector range to (0, max), and
# every automated player picks the minimum, so the search always came back
# empty. `SearchInternal` now offers the decline as its own option instead.
#
# ---------------------------------------------------------------------------
# What is not pinned here, and why.
#
# "with a cost of 3 or less" has no counter-example in the game: **no [[WEAPON]]
# upgrade in any released pack costs 4 or more** (the most expensive are the
# nine at cost 3). So the cost bound cannot be shown to bite, and a scenario
# claiming it does would be asserting something no board can reach. The trait
# and type bounds below are the ones a card can actually violate.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Lock and Load is a player side scheme; `"<id>" has 1 threat` leaves it one
# basic thwart from defeat, and Spider-Man prints THW 1. Psimitar and Domino's
# Pistol are [[WEAPON]] upgrades costing 2, from the same pack as the scheme.
# Tenacity is an upgrade of the same cost with no [[WEAPON]] trait, and Backflip
# is an event: between them they cover both halves of "a [[WEAPON]] upgrade".

Feature: Lock and Load

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:40019
  Scenario: the search puts the weapon upgrade into play
    Given my deck is "Psimitar", "Backflip", "Backflip"
    And "40019" is in play
    And "40019" has 1 threat

    When I thwart "40019"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Psimitar" is in the "UpgradesArea"
    And "40019" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40019
  Scenario: the deck is left alone when the search is declined
    Given my deck is "Psimitar", "Backflip", "Backflip"
    And "40019" is in play
    And "40019" has 1 threat

    When I thwart "40019"
    When I choose "Cancel"
    Then "Psimitar" is in the "PlayerDeck"
    And "40019" is in the "VictoryDisplay"
    And I am not prompted again

  @card:40019
  Scenario: only weapon upgrades are offered
    # Tenacity is an upgrade costing 2 with no [[WEAPON]] trait, so it fails the
    # trait half; Backflip is an event, so it fails the type half. Both sit in
    # the searched deck and neither may be offered.
    Given my deck is "Psimitar", "Domino's Pistol", "Tenacity", "Backflip"
    And "40019" is in play
    And "40019" has 1 threat

    When I thwart "40019"
    Then the legal targets for "Search" are
      | Psimitar         |
      | Domino's Pistol  |

    When I choose "Search" targeting "Domino's Pistol"
    Then "Domino's Pistol" is in the "UpgradesArea"
    And "Psimitar" is in the "PlayerDeck"
    And "Tenacity" is in the "PlayerDeck"
    And I am not prompted again

  @card:40019
  Scenario: nobody is asked when there is no weapon upgrade to find
    Given my deck is "Tenacity", "Backflip", "Backflip"
    And "40019" is in play
    And "40019" has 1 threat

    When I thwart "40019"
    Then "40019" is in the "VictoryDisplay"
    And "Tenacity" is in the "PlayerDeck"
    And I am not prompted again
