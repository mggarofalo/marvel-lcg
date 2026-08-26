# Printed: "When Revealed: Each [[Drone]] minion engaged with your hero attacks.
# If no attack was made this way, put the top card of your deck into play
# facedown, engaged with you as a [[Drone]] minion."
# 1 boost icon.
#
# Three ways this resolves and they are three scenarios, because "if no attack
# was made" is reached from two different boards and the difference between them
# is the whole card:
#
#   drones engaged, hero form      they attack, and no drone is made
#   no drones, hero form           nothing attacks, so a drone is made
#   drones engaged, alter-ego      "engaged with your *hero*" -- there is no
#                                  hero, so nothing attacks and a drone is made
#                                  even though the board is full of drones
#
# The two drones in the first and third scenarios are made by the two Android
# Efficiency faces, one each, off the top of my deck. Two reveals of one face
# would be a `Given` acting on the same card twice, which the harness refuses;
# 01144a and 01144b are different cards printing the same When Revealed.
#
# Ultron Drones is in play throughout: a facedown drone has no printed statistics
# of its own, so without it there is nothing engaged to attack and nothing worth
# making.

Feature: Swarm Attack

  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"
    And "Ultron Drones" is in play

  @card:01147
  Scenario: every drone engaged with my hero attacks, and no new drone is made
    # A `Given`-time reveal runs the whole reveal pipeline, so the transcript
    # opens on the first drone's attack rather than on a `When` of its own.
    # Two defence prompts, one per drone: that there are exactly two is the
    # "each" in "each [[Drone]] minion engaged with your hero attacks".
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "01144a" is revealed
    And "01144b" is revealed
    And "Swarm Attack" is revealed

    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    # 2 damage, 1 from each drone's granted base ATK. The number is what says
    # both attacked rather than one of them twice or one of them at all.
    Then I have 2 damage
    # No third drone: two cards left my deck for the two Android Efficiency
    # reveals and nothing left it for this card, so the Genius under them is
    # still there.
    And "Genius" is in the "PlayerDeck"
    And I have 3 cards in my deck
    And I am not prompted again

  @card:01147
  Scenario: with nothing engaged with me it makes a drone instead
    # The other branch, reached the way the printed text describes: no attack was
    # made, so the top card of my deck stands up as a drone. No defence prompt at
    # all, which is itself the evidence that nothing attacked.
    Given I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "Swarm Attack" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And "Energy" is in the "PlayerDeck"
    And I have 4 cards in my deck
    And I have 0 damage
    And I am not prompted again

  @card:01147
  Scenario: an alter-ego has no hero for the drones to be engaged with, so a drone is made anyway
    # The same two drones as the first scenario, on the same decks, with the
    # identity in its other form. "Each [[Drone]] minion engaged with your hero"
    # names a hero, so nothing attacks -- and because nothing attacked, the
    # second sentence fires and a *third* drone is made off a board that already
    # had two.
    #
    # This is the scenario that separates "if no attack was made" from "if no
    # drone was engaged". An engine that read the condition as the second would
    # do nothing at all here.
    Given I am in alter-ego form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip"
    And "01144a" is revealed
    And "01144b" is revealed
    And "Swarm Attack" is revealed

    Then "Genius" is in the "EngagedEnemiesArea"
    And "Drone Minion #3" has 1 health
    And I have 2 cards in my deck
    And I have 0 damage
    And I am not prompted again
