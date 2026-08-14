# Printed: "Special (thwart): Remove 1 threat from a scheme (2 threat instead if
# this is the final step of this sequence).
# (Play the "Wakanda Forever!" event to use this ability.)"
#
# Two decision paths, the last step and a step that is not the last, both
# reached through 01043 Wakanda Forever! -- the only card that fires a Special.
#
# "a scheme" is a choice, so both scenarios put a side scheme on the board
# alongside the main one and both schemes carry threat. Threat leaves the scheme
# the transcript named and the other keeps what it had.

Feature: Tactical Genius

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01048
  Scenario: as the final step it removes 2 threat from the chosen scheme
    Given my hand is "01043a", "Vibranium"
    And "Tactical Genius" is in play
    And the main scheme has 5 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 5 threat

    When I play "01043a"
    Then the legal targets for "Special" are
      | The Break-In!     |
      | Breakin' & Takin' |

    When I choose "Special" targeting "Breakin' & Takin'"
    Then "Breakin' & Takin'" has 3 threat
    And the main scheme has 5 threat
    And I am not prompted again

  @card:01048
  Scenario: as a step that is not the final one it removes 1
    # Panther Claws resolves after it, so Tactical Genius is no longer last: 1
    # threat instead of 2. Rhino taking the boosted 4 is the other end of the
    # same sequence, and the main scheme keeping its 5 is still the claim that
    # the threat came off the scheme that was named.
    Given my hand is "01043a", "Vibranium"
    And "Tactical Genius" is in play
    And "Panther Claws" is in play
    And the main scheme has 5 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 5 threat

    When I choose "Play" on "01043a" targeting "Tactical Genius", "Panther Claws"
    When I choose "Special" targeting "Breakin' & Takin'"
    Then "Breakin' & Takin'" has 4 threat
    And the main scheme has 5 threat
    And "Rhino" has 4 damage
    And I am not prompted again
