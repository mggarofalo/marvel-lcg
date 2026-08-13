# Printed (Assault on NORAD, stage 2B): "Forced Response: After placing threat
# here during step one of the villain phase, each player must choose to either
# place 2 threat here or put the top card of their deck into play facedown,
# engaged with them as a [[Drone]] minion."
# Printed 0 starting threat, 10 [star] to complete, 1 [star] acceleration.
#
# The hardest card in this batch to reach, and worth writing down how, because
# nothing in the step vocabulary sets a main scheme's stage. The board is walked
# into position instead:
#
#   round 1, step one   The Crimson Cowl 1B takes its printed 1 acceleration
#   round 1, step two   Ultron schemes his printed 1, boosted by the 1 icon on
#                       the Hydra Mercenary taken off the top of the encounter
#                       deck. 3 threat completes 1B, which advances to 2A, whose
#                       When Revealed puts the top card of each player's deck
#                       into play as a facedown [[Drone]] and advances to 2B
#   round 1, still      that drone is an engaged enemy now, so it activates
#     in step two       later in the same step and schemes for its base 1,
#                       putting the first threat on 2B
#   round 2, step one   2B takes its printed 1 acceleration -- and *that* is the
#                       threat placement this card's Forced Response follows
#
# So the choice arrives with 2 threat already on 2B: 1 from the drone's scheme in
# round 1 and 1 from round 2's step one. Every scenario asserts that 2 before
# answering, because a scenario that only checked the total afterwards could not
# tell the card's own 2 from what the board placed around it.
#
# The alter-ego form is deliberate throughout. It keeps Ultron scheming rather
# than attacking, which means his own Forced Response (01134) never fires and no
# decision from another card lands in this transcript.
#
# Two things about the board are not decoration:
#
#   Ultron Drones (01140) is put into play because it is what gives a facedown
#   [[Drone]] its base hit points. Without it a drone is defeated the instant it
#   enters play and the drone branch has nothing to show.
#
#   The encounter cards that get *revealed* are Crowd Control (01108), which is
#   the only core encounter card with no When Revealed text at all. A minion
#   revealed here would be a second enemy in the activation step, and the engine
#   then asks which order the minions activate in -- a prompt whose answer has to
#   name every minion, and a facedown drone has no printed name to give it. The
#   drone branch below has to name two drones for exactly that reason and does it
#   with the zone-and-ordinal form.

Feature: Assault on NORAD

  # Stocked per scenario, not in the Background: a `Given` that stocks a deck
  # adds to it rather than replacing it, so two lists would interleave into one
  # deck and the boost card would not be the card written first.
  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"

  @card:01138b
  Scenario: the threat branch places 2 more on the scheme and leaves my deck alone
    Given I am in alter-ego form
    And my deck is "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower"
    And the encounter deck is "Hydra Mercenary", "Crowd Control", "Hydra Mercenary", "Crowd Control", "Crowd Control", "Crowd Control"
    And "Ultron Drones" is in play

    When I pass
    When I pass
    When I choose "End Phase"

    # Round 2, step one has just placed the acceleration. This is the state the
    # Forced Response is being asked from.
    Then the main scheme has 2 threat
    And I have 9 cards in my deck
    And I am prompted to choose one
      | Place 2 threat here                                                              |
      | Put the top card of their deck into play facedown, engaged with them as a DRONE minion |

    When I choose "Place 2 threat here"
    # 7 by the end of the villain phase: the 2 above, the 2 this branch placed,
    # 2 more when Ultron schemes his boosted 1, and 1 when the drone that came in
    # with stage 2A schemes against an alter-ego.
    Then the main scheme has 7 threat
    # The control on the other branch: no card left my deck, so the count is the
    # same 9 it was at the prompt.
    And I have 9 cards in my deck
    And I am not prompted again

  @card:01138b
  Scenario: the drone branch takes the top card of my deck instead of placing threat
    Given I am in alter-ego form
    And my deck is "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower"
    And the encounter deck is "Hydra Mercenary", "Crowd Control", "Hydra Mercenary", "Crowd Control", "Crowd Control", "Crowd Control"
    And "Ultron Drones" is in play

    When I pass
    When I pass
    When I choose "End Phase"

    Then the main scheme has 2 threat
    And I have 9 cards in my deck
    And I am prompted to choose one
      | Place 2 threat here                                                              |
      | Put the top card of their deck into play facedown, engaged with them as a DRONE minion |

    When I choose "Put the top card of their deck into play facedown, engaged with them as a DRONE minion"

    # Observed at the minion-order prompt, which is the first decision after the
    # branch resolved. 4 rather than the threat branch's 6 at the same point:
    # this branch placed nothing, and the 2 that did arrive is Ultron's boosted
    # scheme. A card left my deck and is standing in the engaged-enemies area --
    # the second one there, the first having arrived with stage 2A.
    Then the main scheme has 4 threat
    And I have 8 cards in my deck
    And "Stark Tower #2 in EngagedEnemiesArea" is in play

    # A facedown drone has no printed name, so it is named by the deck card
    # underneath it, narrowed by zone and by the order the Given created them.
    When I choose "Minion Activates Order" targeting "Stark Tower #1 in EngagedEnemiesArea", "Stark Tower #2 in EngagedEnemiesArea"
    # 6, not the threat branch's 7: this branch put 2 fewer on the scheme and one
    # more drone on the board, and a drone schemes for 1 against an alter-ego.
    Then the main scheme has 6 threat
    And I have 8 cards in my deck
    And I am not prompted again

  @card:01138b
  Scenario: the threat goes on this scheme, not on a side scheme sitting beside it
    # "place 2 threat here" is a printed restriction and the prompt table cannot
    # see it -- the option is one row whether it has one legal target or three.
    # Crowd Control was revealed in round 1 and is in play with its printed 2
    # threat; it is not offered, and it still reads 2 once the branch resolves.
    Given I am in alter-ego form
    And my deck is "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower"
    And the encounter deck is "Hydra Mercenary", "Crowd Control", "Hydra Mercenary", "Crowd Control", "Crowd Control", "Crowd Control"
    And "Ultron Drones" is in play

    When I pass
    When I pass
    When I choose "End Phase"
    Then the legal targets for "Place 2 threat here" are
      | Assault on NORAD |
    And I cannot choose "Place 2 threat here" targeting "Crowd Control"

    When I choose "Place 2 threat here"
    Then the main scheme has 7 threat
    # By the end of the villain phase a second Crowd Control has been revealed,
    # so the one this scenario is about is named by zone and creation order.
    And "Crowd Control #1 in SideSchemesArea" has 2 threat
    And I am not prompted again

  @card:01138b
  Scenario: step one placing threat on a different main scheme offers nothing
    # The control for the trigger. This is the same board and the same villain
    # phase, with one card changed: the boost card has no boost icons, so Ultron
    # schemes for 1 instead of 2, The Crimson Cowl 1B stops at 2 of its printed
    # 3 and never advances. Step one placed threat on the main scheme exactly as
    # it does above and no choice is offered, so the response belongs to stage 2B
    # and not to threat placement in general.
    Given I am in alter-ego form
    And my deck is "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower"
    And the encounter deck is "Hard to Keep Down", "Crowd Control", "Hard to Keep Down", "Crowd Control", "Crowd Control", "Crowd Control"
    And "Ultron Drones" is in play

    When I pass
    Then I am not prompted again
    # 2 threat on the stage that is still in play: 1 acceleration in step one,
    # 1 for Ultron's unboosted printed SCH.
    And the main scheme has 2 threat
    # No drone was ever created, so nothing came off my deck: 16 stocked less the
    # 6 drawn up to hand size.
    And I have 10 cards in my deck
