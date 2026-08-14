# Printed: "Alter-Ego Action: Exhaust The Golden City -> draw 2 cards."
#
# Two decision paths, which is what the `imperative` plan budgets and also what
# the card has: the action, and the restriction that decides whether it is on
# the menu at all. The exhaust cost is not a third -- it is asserted inside the
# first, both as the card's state afterwards and as the reason the action does
# not come back around.
#
# The deck is stocked with Vibranium on purpose. A resource card is not
# playable, so the two cards drawn add nothing to the menu; anything playable
# would appear as a `Play` option and the option table would then be failing for
# a reason that has nothing to do with this card.

Feature: The Golden City

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  @card:01045
  Scenario: the action draws 2 and exhausts the support
    # Two cards, and the support exhausted -- then the menu comes back with the
    # action gone, because its cost can no longer be paid. That last table is
    # what says "exhaust" is a cost rather than a decoration.
    Given I am in alter-ego form
    And my deck is "Vibranium", "Vibranium", "Vibranium"
    And "The Golden City" is in play

    When I choose "Alter-Ego Action" on "The Golden City"
    Then I have 2 cards in hand
    And I have 1 cards in my deck
    And "The Golden City" is exhausted
    And I am prompted to choose one
      | Change Form |

  @card:01045
  Scenario: a hero is not offered the action
    # "Alter-Ego Action". The same board on the other side of the identity: the
    # support is in play and ready, and the menu has no action bound to it.
    Given I am in hero form
    And my deck is "Vibranium", "Vibranium", "Vibranium"
    And "The Golden City" is in play

    Then "The Golden City" is not exhausted
    And I am prompted to choose one
      | Attack      |
      | Change Form |
