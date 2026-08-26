# Printed: "Special: Choose a player. Deal 1 damage to the villain and to each
# enemy engaged with that player (2 damage instead if this is the final step of
# this sequence).
# (Play the "Wakanda Forever!" event to use this ability.)"
#
# A Special ability has no trigger of its own -- 01043 Wakanda Forever! is the
# only thing that fires it -- so every scenario here plays that event. Which
# means the two decision paths are the two positions this upgrade can occupy in
# the sequence: last, and not last.
#
# Tactical Genius is the second upgrade in the non-final scenario, not another
# damage dealer, so the two effects land on different parts of the board and
# cannot be confused for each other.
#
# "Choose a player" produces no prompt in a solo game -- one legal target is
# selected by the engine itself -- so the scenarios say `I am not prompted
# again` rather than naming a player.

Feature: Energy Daggers

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01046
  Scenario: as the final step it deals 2 to the villain and to each engaged enemy
    # Both, in one sweep. Hellcat is the control on "enemy": she is a character
    # the player controls, she is in play, and she takes nothing.
    Given my hand is "01043a", "Vibranium"
    And "Energy Daggers" is in play
    And "Shocker" is in play
    And "Hellcat" is in play

    When I play "01043a"
    Then "Rhino" has 2 damage
    And "Shocker" has 2 damage
    And "Hellcat" has 0 damage
    And I am not prompted again

  @card:01046
  Scenario: as a step that is not the final one it deals 1
    # The same board with Tactical Genius resolving after it, so Energy Daggers
    # is no longer the last step: 1 to each instead of 2. The scheme losing 2
    # threat is the other half of the same claim -- the upgrade that *is* last
    # gets the boosted number.
    Given my hand is "01043a", "Vibranium"
    And "Energy Daggers" is in play
    And "Tactical Genius" is in play
    And "Shocker" is in play
    And "Hellcat" is in play
    And the main scheme has 5 threat

    When I choose "Play" on "01043a" targeting "Energy Daggers", "Tactical Genius"
    Then "Rhino" has 1 damage
    And "Shocker" has 1 damage
    And "Hellcat" has 0 damage
    And the main scheme has 3 threat
    And I am not prompted again
