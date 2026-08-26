# Printed (Countdown to Oblivion, stage 3A): "When Revealed: Each player puts the
# top card of their deck into play facedown, engaged with them as a [[Drone]]
# minion. Advance to stage 3B."
#
# The third A face in the set and the same sentence as the other two, which is
# exactly why it gets its own file rather than a note on 01138a's: the stage it
# advances to is different, and a stat assertion does not transfer between
# stages. 3B is printed 5 [star] to complete against 2B's 10 [star], and
# completing 3B loses the game.
#
# The stage is revealed directly. Reaching it the way a game does means placing
# 10 [star] threat on Assault on NORAD 2B, which is many rounds of a walked
# villain phase and would make the transcript about the board rather than about
# this card. The board that produces is artificial in one respect worth stating:
# the stage in play at the time is not completed and so is still in the main
# schemes area beside this one, which is why every assertion below names a card
# by id instead of saying "the main scheme".
#
# Ultron Drones is in play because a facedown drone has no printed statistics of
# its own and is defeated on entry without it.

Feature: Countdown to Oblivion 3A

  Background:
    Given the scenario is "ultron"
    And "Ultron Drones" is in play

  @card:01139a
  Scenario: it makes a drone off the top of my deck and advances to 3B
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "01139a" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And "Energy" is in the "PlayerDeck"
    And I have 3 cards in my deck
    # The advance. The card is presenting 3B, which is printed 5 to complete for
    # one player -- not 2B's 10 and not 1B's 3.
    And "01139b" has 5 "target_threat"
    And "01139b" has 3 "printed_stage"
    And "01139b" has 0 threat
    And I am not prompted again

  @card:01139a
  Scenario: every player makes a drone off their own deck
    # The "each". Both decks lead with a different card so that two drones off
    # one deck would not read the same as one off each.
    Given the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And player 2's deck is "Pepper Potts", "Genius", "Genius"
    And "01139a" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Pepper Potts" is in the "EngagedEnemiesArea"
    And player 1 has 3 cards in their deck
    And player 2 has 2 cards in their deck
    # 10 rather than 5: the printed target carries a [star] and is per player.
    And "01139b" has 10 "target_threat"
    And I am not prompted again
