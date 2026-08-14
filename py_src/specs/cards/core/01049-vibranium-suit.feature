# Printed: "Special (attack): Move 1 damage from your hero to an enemy (2 damage
# instead if this is the final step of this sequence).
# (Play the "Wakanda Forever!" event to use this ability.)"
#
# "Move" is what makes this three scenarios rather than the two the plan
# budgets. The other Black Panther upgrades deal or remove; this one takes
# damage off the hero and puts the same amount on an enemy, so there is a third
# path the printed number cannot reach on its own: the hero has less damage than
# the ability would move. `min(hero.GetLostHealth(), damage)` is the engine's
# spelling of it, and it is the correct rule -- damage that is not on the hero
# cannot be moved off him -- but nothing in the two boosted/unboosted scenarios
# would notice if it were dropped.
#
# 01043 Wakanda Forever! is the only card that fires a Special, so every
# scenario plays it.

Feature: Vibranium Suit

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01049
  Scenario: as the final step it moves 2 damage to the chosen enemy
    # The hero goes from 3 damage to 1 and the enemy the transcript named goes
    # from 0 to 2 -- one number leaving one card and arriving on another, which
    # is the difference between "move" and "deal". Rhino at 0 says the enemy was
    # chosen rather than found.
    Given my hand is "01043a", "Vibranium"
    And "Vibranium Suit" is in play
    And "Shocker" is in play
    And "me" has 3 damage

    When I play "01043a"
    Then the legal targets for "Special" are
      | Rhino   |
      | Shocker |

    When I choose "Special" targeting "Shocker"
    Then "Shocker" has 2 damage
    And "Rhino" has 0 damage
    And I have 1 damage
    And I am not prompted again

  @card:01049
  Scenario: as a step that is not the final one it moves 1
    # Tactical Genius resolves after it, so the suit is no longer last: the hero
    # goes from 5 damage to 4 and Rhino takes the 1. The scheme losing 2 threat
    # is the other end of the sequence.
    Given my hand is "01043a", "Vibranium"
    And "Vibranium Suit" is in play
    And "Tactical Genius" is in play
    And "me" has 5 damage
    And the main scheme has 5 threat

    When I choose "Play" on "01043a" targeting "Vibranium Suit", "Tactical Genius"
    Then "Rhino" has 1 damage
    And I have 4 damage
    And the main scheme has 3 threat
    And I am not prompted again

  @card:01049
  Scenario: it moves no more damage than the hero is carrying
    # The final step, so the printed number is 2 -- and the hero has 1 damage,
    # so 1 is what moves. An engine that dealt the boosted 2 to the enemy would
    # put Rhino at 2 here, and an engine that healed the hero by 2 would have
    # taken his health above his printed 11.
    Given my hand is "01043a", "Vibranium"
    And "Vibranium Suit" is in play
    And "me" has 1 damage

    When I play "01043a"
    Then "Rhino" has 1 damage
    And I have 0 damage
    And "me" has 11 health
    And I am not prompted again
