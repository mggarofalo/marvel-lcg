# Printed: "Guard.
# Forced Interrupt: When Advanced Ultron Drone is defeated, the engaged player
# puts the top card of their deck into play facedown, engaged with them as a
# [[Drone]] minion."
# Printed 4 hit points, 1 ATK, 1 SCH, 2 boost icons.
#
# The only DRONE minion in the set with a printed statistic line of its own,
# which is what makes it the control for every "facedown drone" restriction in
# the rest of the batch: it carries the DRONE trait and is engaged with a player,
# and it is still not what "each facedown [[Drone]] minion" means.
#
# Ultron Drones is in play in the defeat scenario because the drone the interrupt
# creates is facedown and has no printed hit points of its own. Without the
# environment it would enter play at 0 and be defeated in the same breath, and
# the scenario would read as though the interrupt had not fired.

Feature: Advanced Ultron Drone

  Background:
    Given the scenario is "ultron"
    And the hero is "spider_man"
    And I am in hero form
    And "Ultron Drones" is in play

  @card:01143
  Scenario: its guard puts the villain out of reach while it is engaged with me
    # Guard is enforced by emptying the Attack option's legal targets rather than
    # by removing the option, so neither the prompt table nor any card's state
    # can see it -- `I cannot attack` is the step that can (the original investigation).
    #
    # The second half is the control: the restriction is about the villain, not
    # about attacking, and without it an engine that had forgotten how to attack
    # anything would satisfy the first line.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Advanced Ultron Drone" is in play

    Then "Advanced Ultron Drone" is in the "EngagedEnemiesArea"
    And "Advanced Ultron Drone" has 4 health
    And I cannot attack "Ultron"

    When I attack "Advanced Ultron Drone"
    Then "Advanced Ultron Drone" has 2 damage
    And "Advanced Ultron Drone" is in play
    And I am not prompted again

  @card:01143
  Scenario: defeating it puts the top card of my deck into play as a drone
    # The interrupt. It is already at 2 damage, so Spider-Man's printed ATK 2
    # takes it to its printed 4 and defeats it.
    #
    # Aunt May is on top of my deck and is the card the interrupt is claimed to
    # take; the Backflip under it is asserted to still be in the deck, because a
    # scenario that only counted the deck down by one would pass against an
    # engine that reached anywhere into it.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Advanced Ultron Drone" is in play
    And "Advanced Ultron Drone" has 2 damage

    When I attack "Advanced Ultron Drone"
    # The minion itself is an encounter card, so it goes to the encounter discard
    # pile -- not to the player discard pile the facedown drone it just made will
    # eventually go to.
    Then "Advanced Ultron Drone" is in the "EncounterDiscardPile"
    And "Advanced Ultron Drone" is not in play
    # ...and in its place, the card that was on top of my deck, standing up as a
    # drone engaged with me under both of the names it now answers to.
    And "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And "Backflip #1" is in the "PlayerDeck"
    And I have 3 cards in my deck
    And I am not prompted again
