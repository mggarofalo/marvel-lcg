# The shape of a round: the player phase, the villain phase, and the
# transitions between them. Rulebook behavior, so it lives under specs/rules/;
# Iron Man is only the hero the transcripts happen to use, chosen because his
# identity carries no interrupt and no response and so asks no questions of its
# own.
#
# MARVEL-23.
#
# ---------------------------------------------------------------------------
# Every phase scenario stocks both decks, and that is not decoration.
#
# A puzzle scene starts with an empty player deck and an empty encounter deck --
# that is the point of it, the board holds exactly what the scenario asks for
# and nothing else. But a *round* draws from both, so a scenario that ends a
# turn without stocking them does not walk the phases, it ends the game:
#
#   no player deck     the hero draws up to hand size at the end of their turn,
#                      cannot, and is eliminated -- the game ends in round 1
#                      with "All players were eliminated"
#   no encounter deck  the villain phase deals an encounter card, cannot, and
#                      the game ends with "There were no cards in either the
#                      encounter deck or the encounter discard pile"
#
# Both are the real rule applied to an artificial board, not engine defects.
# The filler is chosen to be inert: Pepper Potts is a Support that does nothing
# while it sits in a deck, and every encounter card is the same Hydra Mercenary
# so the boost value below is not a coincidence of the shuffle.
#
# ---------------------------------------------------------------------------
# Boost is why the numbers here are one higher than the villain card.
#
# The villain's activation is boosted: a card is dealt face down from the
# encounter deck and its boost icons add to the attack or the scheme. Rhino
# stage 1 is printed ATK 2 and SCH 1, and Hydra Mercenary is printed boost 1, so
# the attack is 3 and the scheme is 2. Both numbers below were authored as 2 and
# 1 and the engine rejected both; boost explains the two independent
# measurements with one rule, so the specs were wrong, not the engine.
#
# `When I pass` means two things and its position says which: at the turn menu
# it ends the turn, at a defence prompt it declines to defend. Both are the
# engine's cancel, which is why one step covers both.

Feature: Phase structure

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

  Scenario: a game begins in the player phase of round 1
    Given I am in hero form

    Then it is round 1
    And it is the player phase
    And it is the "Player Turn" phase
    And the game is not over
    And the main scheme has 0 threat

  Scenario: ending the last player's turn hands the round to the villain
    Given I am in hero form

    When I pass
    Then it is the villain phase

    When I pass
    Then it is round 2

  Scenario: the villain phase stops at enemy activation to ask for a defence
    # Deliberately the narrow assertion. "The villain phase" cannot tell threat
    # placement from enemy activation, and the order of those two is exactly the
    # sort of thing a port gets wrong.
    Given I am in hero form

    When I pass
    Then it is the "Enemy Activation" phase
    And I am prompted to choose one
      | Defense |

    When I pass
    Then it is round 2

  Scenario: a hero who ends their turn in hero form is attacked
    # Rhino stage 1 is printed ATK 2, boosted to 3 by Hydra Mercenary's 1 boost
    # icon. Iron Man is printed 9 hit points and declines to defend, so all of
    # it lands.
    Given I am in hero form

    When I pass
    When I pass
    Then I have 3 damage
    And it is round 2

  Scenario: an alter-ego is schemed against rather than attacked
    # The villain activates against each player: attack if they are in hero
    # form, scheme if they are in alter-ego form. An alter-ego is never asked to
    # defend, so ending the turn is the only beat the round needs.
    #
    # 3 threat: 1 for Rhino's printed SCH, 1 for the boost card, and 1 for the
    # main scheme's printed acceleration.
    Given I am in alter-ego form

    When I pass
    Then I have 0 damage
    And it is round 2
    And the main scheme has 3 threat

  Scenario: a hero form round accelerates the scheme without the villain scheming
    # The villain attacked rather than schemed, so the only threat placed is the
    # main scheme's own acceleration -- printed 1 on The Break-In! stage 1B.
    Given I am in hero form

    When I pass
    When I pass
    Then the main scheme has 1 threat

  Scenario: ending a turn with cards in hand asks to discard before drawing up
    # The end of a player's turn is "discard any number of cards, then draw up
    # to your hand size", and the engine only asks when there is something to
    # discard -- which is why round 1 of a puzzle scene never asks and round 2
    # always does. That asymmetry is the empty starting hand, not a rule.
    #
    # The prompt is forced: `End Phase` is the "discard nothing" answer, and a
    # transcript cannot pass its way past it. Alter-ego form keeps the
    # transcript about the end of the turn -- an alter-ego is schemed against
    # rather than attacked, so no defence interrupts the walk.
    Given I am in alter-ego form

    When I pass
    Then it is round 2

    When I pass
    Then it is the "Player Turn End" phase
    And it is the player phase

    When I choose "End Phase"
    Then it is round 3

  Scenario: the round comes back around a second time
    # Two defences in the second villain phase, not one. The encounter card
    # dealt during the first villain phase was a Hydra Mercenary, which enters
    # play engaged with the hero, and every engaged enemy activates -- so the
    # villain attacks and then the minion does. A transcript that expected one
    # defence would fail here, which is the point of writing the round out
    # rather than asserting the round number and calling it covered.
    Given I am in hero form

    When I pass
    When I pass
    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    Then it is round 3
    And it is the player phase
    And the game is not over
