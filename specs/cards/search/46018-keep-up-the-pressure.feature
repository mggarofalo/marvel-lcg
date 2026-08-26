# Keep Up the Pressure.
#
# Printed: "Victory 0. When Defeated: Each player may search their deck and
# discard pile for an [[Attack]] event and add it to their hand. (Shuffle.)
# Until the end of the phase, each [[Attack]] event deals 1 additional damage."
#
# One of the six cards whose "may search" clause did nothing until MARVEL-112:
# `Search.PlayerCard(..., may=True)` widened the selector range to (0, max), and
# every automated player picks the minimum, so the search always came back
# empty. `SearchInternal` now offers the decline as its own option instead.
#
# Only the search clause is pinned here. The damage rider is a separate ability
# that was never affected by the `may` bug, and it registers whether or not the
# search happens -- the last scenario is what says so.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# This card adds the found event to the hand rather than putting it into play,
# so the zone to assert is "HandsArea". Swinging Web Kick and Haymaker are
# [[Attack]] events; Backflip is an event with no [[Attack]] trait and is what
# the trait filter has to leave behind.

Feature: Keep Up the Pressure

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"

  @card:46018
  Scenario: the search adds the attack event to hand
    Given my deck is "Swinging Web Kick", "Backflip", "Backflip"
    And "46018" is in play
    And "46018" has 1 threat

    When I thwart "46018"
    Then I am prompted to choose one
      | Search |
      | Cancel |

    When I choose "Search"
    Then "Swinging Web Kick" is in the "HandsArea"
    And I have 4 cards in hand
    And "46018" is in the "VictoryDisplay"
    And I am not prompted again

  @card:46018
  Scenario: the deck is left alone when the search is declined
    Given my deck is "Swinging Web Kick", "Backflip", "Backflip"
    And "46018" is in play
    And "46018" has 1 threat

    When I thwart "46018"
    When I choose "Cancel"
    Then "Swinging Web Kick" is in the "PlayerDeck"
    And I have 3 cards in hand
    And "46018" is in the "VictoryDisplay"
    And I am not prompted again

  @card:46018
  Scenario: only attack events are offered
    # Backflip is an event without the [[Attack]] trait, and it sits in the same
    # searched deck.
    Given my deck is "Swinging Web Kick", "Haymaker", "Backflip"
    And "46018" is in play
    And "46018" has 1 threat

    When I thwart "46018"
    Then the legal targets for "Search" are
      | Swinging Web Kick |
      | Haymaker          |

    When I choose "Search" targeting "Haymaker"
    Then "Haymaker" is in the "HandsArea"
    And "Swinging Web Kick" is in the "PlayerDeck"
    And I am not prompted again

  @card:46018
  Scenario: nobody is asked when there is no attack event to find
    Given my deck is "Backflip", "Backflip", "Backflip"
    And "46018" is in play
    And "46018" has 1 threat

    When I thwart "46018"
    Then "46018" is in the "VictoryDisplay"
    And I have 3 cards in hand
    And I am not prompted again
