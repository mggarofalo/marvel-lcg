# Printed (Assault on NORAD, stage 2A): "When Revealed: Each player puts the top
# card of their deck into play facedown, engaged with them as a [[Drone]] minion.
# Advance to stage 2B."
#
# An A face is a transition rather than a board state: it is revealed, it
# resolves, and it advances in the same breath, so the only evidence it was ever
# in play is what it left behind. Two things, and both are asserted:
#
#   the drone       one per player, off that player's own deck
#   the advance     the main scheme afterwards is 2B, whose printed target is 10
#                   [star] against 1B's 3 [star]
#
# The first scenario reaches it the way a real game does, which is worth the
# longer transcript because nothing else in the suite proves this stage is
# reachable at all. The Crimson Cowl 1B is printed 3 to complete, and one villain
# phase places exactly that: 1 acceleration in step one, then Ultron's printed
# SCH 1 boosted by the single icon on the Hydra Mercenary taken off the top of
# the encounter deck as the boost card. Completing 1B reveals this stage, which
# resolves and advances, and the drone it made is engaged in time to activate
# later in the same step -- which is where the 1 threat on 2B comes from.
#
# The alter-ego form is deliberate: it keeps Ultron scheming rather than
# attacking, so his own Forced Response never fires and no decision from another
# card lands in the transcript.
#
# Tony Stark's hand size is 6, so the six cards written first are the ones drawn
# at the end of the turn and the seventh is what the drone is made from. Aunt May
# is written there so the drone has a printed identity worth naming.
#
# The second scenario reveals the stage directly, because "each player" needs two
# players and walking two heroes into round 2 puts a second hero's turn and a
# minion activation order in a transcript that is not about either. A direct
# reveal is an artificial board in one respect worth stating: the stage it
# advanced *from* is still in the main schemes area, because nothing completed
# it. Every assertion there names a card rather than "the main scheme" for that
# reason.

Feature: Assault on NORAD 2A

  @card:01138a
  Scenario: completing 1B reveals this stage, which makes a drone and advances to 2B
    Given the scenario is "ultron"
    And the hero is "iron_man"
    And I am in alter-ego form
    And my deck is "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Aunt May", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower", "Stark Tower"
    And the encounter deck is "Hydra Mercenary", "Crowd Control", "Hydra Mercenary", "Crowd Control", "Crowd Control", "Crowd Control"
    And "Ultron Drones" is in play

    When I pass

    # The stage that was completed is gone, and the one this card advanced to is
    # in play in its place -- 10 to complete rather than 1B's 3.
    Then "The Crimson Cowl" is in the "RemovedArea"
    And "the main scheme" has 10 "target_threat"
    And "the main scheme" has 2 "printed_stage"
    # The drone, made from the seventh card of my deck: the six above it were
    # drawn to hand size at the end of my turn.
    And "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And I have 9 cards in my deck
    And I have 6 cards in hand
    # 1 threat on the new stage, placed by that drone when it activated later in
    # the same villain phase. It is the drone being a real engaged enemy rather
    # than a card that merely left my deck.
    And the main scheme has 1 threat

  @card:01138a
  Scenario: every player makes a drone off their own deck
    # The "each". Both decks lead with a different card, so two drones off one
    # deck would not read the same as one off each.
    Given the scenario is "ultron"
    And the heroes are "iron_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And player 2's deck is "Pepper Potts", "Genius", "Genius"
    And "Ultron Drones" is in play
    And "01138a" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Pepper Potts" is in the "EngagedEnemiesArea"
    And player 1 has 3 cards in their deck
    And player 2 has 2 cards in their deck
    # The advance, on the card this scenario revealed: it is presenting 2B, whose
    # printed target carries a [star] and so is 20 for two players.
    And "01138b" has 20 "target_threat"
    And "01138b" has 2 "printed_stage"
    And I am not prompted again
