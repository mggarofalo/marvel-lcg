# The Break-In!, side 1B. Printed:
#
#   "If this stage is completed, the players lose the game."
#
# Printed 0 starting threat (fixed), 7 threat to complete per hero, and 1
# acceleration per hero.
#
# Three claims, three scenarios, and they are three different kinds of thing:
# the numbers at one hero, the same numbers at two (both targets carry the
# printed star, and a star is arithmetic rather than a constant), and the one
# printed sentence -- which is not about a number at all but about who wins.
#
# The starting threat is the odd one out and is asserted alongside the rest
# rather than on its own: it is printed *fixed*, so unlike the other two it must
# NOT move when a second player joins. A per-hero reading of the whole stat
# block passes the solo scenario and fails the two-hero one on that number
# alone.
#
# The last scenario ends the game, so nothing follows the beat that ends it.

Feature: The Break-In! (1B)

  Background:
    Given the scenario is "rhino"

  @card:01097b
  Scenario: at one hero the stage completes at 7 and accelerates by 1
    Given the hero is "spider_man"
    And I am in hero form

    Then the main scheme has 0 threat
    And "The Break-In!" has 7 "target_threat"
    And "The Break-In!" has 1 "escalation_threat"
    And "The Break-In!" has 0 "is_completed"

  @card:01097b
  Scenario: at two heroes both stars double and the starting threat does not
    # The discriminating board. 7 and 1 are printed per hero and go to 14 and 2;
    # the 0 is printed fixed and stays 0, which is the assertion that separates
    # "scale the stat block" from "scale the two stats that print a star".
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form

    Then the main scheme has 0 threat
    And "The Break-In!" has 14 "target_threat"
    And "The Break-In!" has 2 "escalation_threat"

  @card:01097b
  Scenario: completing the stage loses the game
    # The printed sentence. The board is one threat short of the target and the
    # alter-ego is schemed against rather than attacked: Rhino's printed SCH 1,
    # plus 1 for the Hydra Mercenary boost card, plus the stage's own printed 1
    # acceleration is 3, which takes 6 past 7.
    #
    # `the players have lost` is the assertion that matters. "The game is over"
    # is also true when the villain is defeated, so on its own it would pass
    # against an engine that had the outcome exactly backwards.
    Given the hero is "iron_man"
    And I am in alter-ego form
    And the main scheme has 6 threat
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then the game is over
    And the players have lost
