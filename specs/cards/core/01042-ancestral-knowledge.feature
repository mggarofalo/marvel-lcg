# Printed: "Alter-Ego Action: Choose up to 3 different cards in your discard
# pile and shuffle them into your deck."
#
# Three claims in one sentence, and they fail independently, so they are three
# scenarios plus the restriction that gates the action:
#
#   * "up to 3"       -- fewer than three is a legal answer
#   * "3"             -- the fourth card in the pile is not reached
#   * "different"     -- two copies of one title are one card
#   * "Alter-Ego Action" -- the option is absent in hero form
#
# The cards shuffled in cannot be named individually afterwards: "shuffle them
# into your deck" randomises the order, so what a scenario can say is which zone
# each card ended in and how many are in each. That is deliberate -- a scenario
# that pinned deck positions here would be pinning the RNG.
#
# `select_rule="DifferentCards"` enforces the printed restriction in both the
# Python engine and the browser. The engine rejects a selection containing two
# copies of one title; it does not trim the illegal selection to one copy. The
# current spec vocabulary can inspect individual legal targets and the target
# ceiling, but cannot assert that a combination of two otherwise legal targets
# is forbidden. `unit_test.test_select_rules` pins that relational rule directly:
#
#     # A: Can Ancestral Knowledge shuffle different versions of Wakanda Forever
#     #    into Black Panther's deck?
#     # Q: No. Cards with the same title are considered to be the same card for
#     #    the purpose of card abilities.

Feature: Ancestral Knowledge

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  @card:01042
  Scenario: three chosen cards leave the discard pile and the fourth stays
    # Four cards in the pile, three chosen. The discard ends at 3 rather than 1
    # because Ancestral Knowledge itself and the Vibranium that paid for it both
    # arrive there -- so the assertion that carries the claim is Vibranium Suit
    # still being in the pile, not the count.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit"

    When I choose "Play" on "01042" targeting "Panther Claws", "Tactical Genius", "Energy Daggers"
    Then I have 3 cards in my deck
    And "Vibranium Suit" is in the "DiscardPile"
    And "Panther Claws" is in the "PlayerDeck"
    And "Tactical Genius" is in the "PlayerDeck"
    And "Energy Daggers" is in the "PlayerDeck"
    And I have 3 cards in my discard pile
    And I am not prompted again

  @card:01042
  Scenario: "up to" allows fewer than three
    # The same board, one card chosen. Three cards stay in the pile, so an
    # engine that read "3" as a fixed number rather than a maximum fails here
    # while passing the scenario above.
    Given I am in alter-ego form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers", "Vibranium Suit"

    When I choose "Play" on "01042" targeting "Panther Claws"
    Then I have 1 cards in my deck
    And "Panther Claws" is in the "PlayerDeck"
    And "Tactical Genius" is in the "DiscardPile"
    And "Energy Daggers" is in the "DiscardPile"
    And "Vibranium Suit" is in the "DiscardPile"
    And I have 5 cards in my discard pile
    And I am not prompted again

  @card:01042
  Scenario: a hero is not offered the action at all
    # "Alter-Ego Action". The restriction is enforced by the option never
    # appearing, so the whole menu is asserted rather than just the absence --
    # the hero has an Attack and a Change Form and nothing else, and there is no
    # `Play` bound to the card sitting in hand.
    Given I am in hero form
    And my hand is "01042", "Vibranium"
    And my discard pile is "Panther Claws", "Tactical Genius", "Energy Daggers"

    Then I am prompted to choose one
      | Attack      |
      | Change Form |
