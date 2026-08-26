# Generation X.
#
# Printed: "Victory 0. Each [[X-Men]] character gets +1 THW while making a basic
# thwart against this scheme. When Defeated: Each player may search their deck
# and discard pile for an identity-specific event and add it to their hand.
# (Shuffle.)"
#
# One of the six cards whose "may search" clause did nothing until MARVEL-112:
# `Search.PlayerCard(..., may=True)` widened the selector range to (0, max), and
# every automated player picks the minimum, so the search always came back
# empty. `SearchInternal` now offers the decline as its own option instead.
#
# Only the search clause is pinned here. The [[X-Men]] thwart bonus is a
# separate ability on the same card and was never affected; Spider-Man is not
# [[X-Men]], so the boards below do not engage it at all.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# The found event goes to hand, not into play, so the zone to assert is
# "HandsArea". Enhanced Spider-Sense and Swinging Web Kick are Spider-Man's own
# events; Haymaker is a basic event and is what the class filter has to leave
# behind.

Feature: Generation X

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:47016
  Scenario: the search adds the identity-specific event to hand
    Given my deck is "Enhanced Spider-Sense", "Haymaker", "Haymaker"
    And "47016" is in play
    And "47016" has 1 threat

    When I thwart "47016"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Enhanced Spider-Sense" is in the "HandsArea"
    And I have 4 cards in hand
    And "47016" is in the "VictoryDisplay"
    And I am not prompted again

  @card:47016
  Scenario: the deck is left alone when the search is declined
    Given my deck is "Enhanced Spider-Sense", "Haymaker", "Haymaker"
    And "47016" is in play
    And "47016" has 1 threat

    When I thwart "47016"
    When I choose "Cancel"
    Then "Enhanced Spider-Sense" is in the "PlayerDeck"
    And I have 3 cards in hand
    And "47016" is in the "VictoryDisplay"
    And I am not prompted again

  @card:47016
  Scenario: a basic event is not identity-specific
    Given my deck is "Enhanced Spider-Sense", "Swinging Web Kick", "Haymaker"
    And "47016" is in play
    And "47016" has 1 threat

    When I thwart "47016"
    Then the legal targets for "Search" are
      | Enhanced Spider-Sense |
      | Swinging Web Kick     |

    When I choose "Search" targeting "Swinging Web Kick"
    Then "Swinging Web Kick" is in the "HandsArea"
    And "Haymaker" is in the "PlayerDeck"
    And I am not prompted again

  @card:47016
  Scenario: nobody is asked when there is no identity-specific event to find
    Given my deck is "Haymaker", "Haymaker", "Haymaker"
    And "47016" is in play
    And "47016" has 1 threat

    When I thwart "47016"
    Then "47016" is in the "VictoryDisplay"
    And I have 3 cards in hand
    And I am not prompted again
