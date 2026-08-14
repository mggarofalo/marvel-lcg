# Printed (The Crimson Cowl, stage 1B): "When Revealed: Each player puts the top
# card of their deck into play facedown, engaged with them as a [[Drone]] minion."
# Printed 0 starting threat, 3 [star] to complete, 1 [star] acceleration.
#
# The scenario opens with this stage already in play and already revealed --
# 1A's setup line advances to it before the first turn. Its When Revealed
# therefore fired during setup, at a moment when a puzzle scene's player decks
# are still empty, so it took nothing off a deck and left nothing on the board.
# `Given "The Crimson Cowl" is revealed` runs it again against a stocked deck,
# which is the only way to see it: `CardFace.Reveal` has no idempotency check, so
# a second reveal genuinely re-runs the reveal pipeline rather than quietly doing
# nothing. That is a hazard the harness documentation warns about and is being
# used deliberately here.
#
# Ultron Drones is in play because a facedown drone has no printed statistics of
# its own and would otherwise be defeated the instant it entered play.

Feature: The Crimson Cowl 1B

  Background:
    Given the scenario is "ultron"
    And "Ultron Drones" is in play

  @card:01137b
  Scenario: revealing the stage takes the top card of my deck and stands it up as a drone
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "The Crimson Cowl" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    # The card under the top one is still in the deck: "the top card" is a claim
    # about position, and a deck count alone would pass against an engine that
    # reached anywhere into it.
    And "Energy" is in the "PlayerDeck"
    And I have 3 cards in my deck
    # The stage itself is unchanged by its own reveal: 0 threat placed, and the
    # printed 3 still to complete.
    And the main scheme has 0 threat
    And "the main scheme" has 3 "target_threat"
    And I am not prompted again

  @card:01137b
  Scenario: every player makes a drone, each off their own deck
    # The "each" in "each player puts the top card of their deck into play". The
    # solo scenario is equally consistent with an engine that resolved this once
    # for whoever revealed it, and the two decks lead with different cards so
    # that two drones off one deck would not read the same as one off each.
    Given the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And player 2's deck is "Pepper Potts", "Genius", "Genius"
    And "The Crimson Cowl" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Pepper Potts" is in the "EngagedEnemiesArea"
    And player 1 has 3 cards in their deck
    And player 2 has 2 cards in their deck
    # 6 to complete rather than 3: the printed target carries a [star] and is per
    # player, which is a claim about this face rather than about the drones.
    And "the main scheme" has 6 "target_threat"
    And I am not prompted again
