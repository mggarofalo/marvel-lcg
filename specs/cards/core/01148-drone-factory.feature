# Printed: "When Revealed: Each player puts the top card of their deck into play
# facedown, engaged with them as a [[Drone]] minion. Place 1 threat here for each
# [[Drone]] minion in play."
# Printed 4 starting threat with no [star], 2 boost icons, 1 acceleration icon.
#
# Two counts that are easy to conflate and are not the same:
#
#   the drones it makes    one per player, off that player's own deck
#   the drones it counts   every [[Drone]] minion *in play*, including ones that
#                          were already there and ones it did not make
#
# The middle scenario is the only one that separates them. It adds an Advanced
# Ultron Drone -- a DRONE minion that was on the board before the reveal -- and
# the threat placed goes up by one, which an engine counting "the drones this
# card created" would not do.
#
# The starting threat carries no [star], so unlike Invasive AI and Ultron's
# Imperative it is 4 whatever the player count, and the two-player scenario says
# so rather than leaving it implied.

Feature: Drone Factory

  Background:
    Given the scenario is "ultron"
    And "Ultron Drones" is in play

  @card:01148
  Scenario: one drone off my deck, and one threat for it
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "Drone Factory" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Energy" is in the "PlayerDeck"
    And I have 3 cards in my deck
    # 5: the printed 4 it enters play with, plus 1 for the single drone in play.
    And "Drone Factory" has 5 threat
    And I am not prompted again

  @card:01148
  Scenario: the threat counts drones that were already in play, not only the ones it made
    # Same board with one card added. Advanced Ultron Drone is a DRONE minion
    # engaged with me before the reveal, so "each [[Drone]] minion in play" is
    # two and the threat placed is two.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "Advanced Ultron Drone" is in play
    And "Drone Factory" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    # 6 against the 5 above, and the one card of difference is a minion this
    # card did not create.
    And "Drone Factory" has 6 threat
    And I am not prompted again

  @card:01148
  Scenario: every player makes a drone off their own deck, and the starting threat does not double
    # The "each" in "each player puts the top card of their deck into play". The
    # two decks lead with different cards so that two drones made from one deck
    # would not read the same as one from each.
    Given the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And player 2's deck is "Pepper Potts", "Genius", "Genius"
    And "Drone Factory" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Pepper Potts" is in the "EngagedEnemiesArea"
    And player 1 has 3 cards in their deck
    And player 2 has 2 cards in their deck
    # 6 = the printed 4, which has no [star] and so does not scale with the
    # player count, plus 1 for each of the two drones now in play.
    And "Drone Factory" has 6 threat
    And I am not prompted again
