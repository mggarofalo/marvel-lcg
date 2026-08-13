# Printed: "[star] Forced Response: After Ultron attacks you, choose to either
# place 1 threat on the main scheme or put the top card of your deck into play
# facedown, engaged with you as a [[Drone]] minion."
# Ultron (I) is printed ATK 2 [star], SCH 1, 17 [star] hit points.
#
# The only card in this batch whose ability cannot be reached from a `Given`:
# the trigger is Ultron's attack, so every scenario here walks a real villain
# phase. Iron Man is the hero because his identity carries no interrupt and no
# response of its own -- Spider-Man's Spider-Sense fires before the villain
# activates and puts a decision in the transcript that has nothing to do with
# Ultron. Both decks are stocked because a round that cannot draw or cannot deal
# an encounter card ends the game instead of walking the phases.
#
# Three numbers recur and each is the printed rule applied to this board:
#
#   3 damage    printed ATK 2 plus the 1 boost icon on the Hydra Mercenary that
#               was taken off the top of the encounter deck as the boost card
#   1 threat    The Crimson Cowl 1B is printed with 1 acceleration, placed in
#               step one of the villain phase before Ultron activates
#   7 in deck   the 8 cards stocked, less the 1 drawn at the end of my turn
#
# The Ultron Drones environment (01140) is put into play by every scenario that
# takes the drone branch, and that is not decoration: it is the card that gives a
# facedown [[Drone]] its base 1 hit point, it is part of the printed setup of
# this scenario, and a puzzle scene does not deal set-aside cards. Without it the
# drone enters play with 0 hit points and is defeated in the same breath, which
# is a true statement about an incomplete board rather than about this card.

Feature: Ultron

  # The decks are stocked per scenario rather than in the Background: a `Given`
  # that stocks a deck adds to it, so a Background list and a scenario list would
  # stack into one deck of both and the boost card would not be the one written.
  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"

  @card:01134
  Scenario: the threat branch places 1 on the main scheme and takes nothing off my deck
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Ultron Drones" is in play

    When I pass
    When I pass
    Then I am prompted to choose one
      | Place 1 threat on the main scheme                                            |
      | Put the top card of your deck into play facedown, engaged with you as a Drone minion |

    When I choose "Place 1 threat on the main scheme"
    # 2 threat: the 1 placed in step one for the printed acceleration, plus this.
    Then the main scheme has 2 threat
    # The control on the other branch. A card leaving the deck is the only thing
    # that branch does that this one does not, and the deck size is where it
    # shows -- the drone itself has no printed name to assert a zone against.
    And I have 7 cards in my deck
    # The attack happened, which is what the response is printed to follow.
    And I have 3 damage
    And I am not prompted again

  @card:01134
  Scenario: the drone branch takes the top card of my deck and stands it up as an enemy
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Ultron Drones" is in play

    When I pass
    When I pass
    Then I am prompted to choose one
      | Place 1 threat on the main scheme                                            |
      | Put the top card of your deck into play facedown, engaged with you as a Drone minion |

    When I choose "Put the top card of your deck into play facedown, engaged with you as a Drone minion"
    # The drone enters play engaged with me during Ultron's activation, so it
    # activates in the same enemy-activation step and I am asked to defend a
    # second time. That second prompt is itself evidence the card became an
    # engaged enemy rather than merely leaving my deck.
    When I pass

    # 4 damage: 3 from Ultron, 1 from the drone's printed ATK. This is the
    # assertion that separates "a card entered play as a minion" from "a card was
    # discarded", which is what happens on a board with no Ultron Drones.
    Then I have 4 damage
    And I have 6 cards in my deck
    # The card that left my deck, named the only way a facedown drone can be
    # named -- by the deck card underneath it, narrowed by the zone it reached.
    # The ref fails as unresolvable if no Pepper Potts is standing in the
    # engaged-enemies area, which is what makes this an assertion rather than a
    # restatement.
    And "Pepper Potts in EngagedEnemiesArea" is in play
    # The control on the other branch: this one places no threat, so the main
    # scheme is left holding only step one's acceleration.
    And the main scheme has 1 threat
    And I am not prompted again

  @card:01134
  Scenario: the threat can only go on the main scheme, not on a side scheme in play
    # "place 1 threat on the main scheme" is a printed restriction, and it is
    # invisible to the prompt table -- the option is one row whether it has one
    # legal target or three. Under Attack is in play with its printed 3 starting
    # threat and is not offered, and still reads 3 after the branch resolves.
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Ultron Drones" is in play
    And "Under Attack" is in play

    When I pass
    When I pass
    Then the legal targets for "Place 1 threat on the main scheme" are
      | The Crimson Cowl |
    And I cannot choose "Place 1 threat on the main scheme" targeting "Under Attack"

    When I choose "Place 1 threat on the main scheme"
    Then the main scheme has 2 threat
    And "Under Attack" has 3 threat
    And I am not prompted again

  @card:01134
  Scenario: an alter-ego is schemed against rather than attacked, so nothing is offered
    # The control for the trigger itself. "After Ultron attacks you" is the
    # condition, and a villain activating against an alter-ego schemes instead --
    # so the whole choice never happens. Without this scenario the three above
    # are equally consistent with an engine that offered the choice after any
    # villain activation.
    #
    # The boost card is deliberately a Hard to Keep Down, which has no boost
    # icons. With a boosted 2-threat scheme the villain phase would place exactly
    # the 3 that completes The Crimson Cowl 1B and the main scheme would advance
    # out from under the assertion; 2 leaves it in play and makes the number
    # readable. Boost cards are discarded without being revealed, so nothing on
    # that card resolves.
    Given I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hard to Keep Down", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Ultron Drones" is in play

    When I pass
    Then I am not prompted again
    # 2 threat: 1 acceleration in step one, 1 for Ultron's printed SCH, 0 boost.
    And the main scheme has 2 threat
    # No attack, so no damage and no response -- and my deck is untouched, which
    # is the drone branch not having happened either.
    And I have 0 damage
    And I have 6 cards in my deck
