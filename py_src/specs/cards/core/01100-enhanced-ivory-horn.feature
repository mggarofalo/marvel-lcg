# Enhanced Ivory Horn. Printed:
#
#   "Attach to Rhino.
#    Hero Action: Spend [physical][physical][physical] resources -> discard this
#    card"
#
# Printed ATK +1 and boost 2.
#
# The `declarative` tier plans one scenario and this card has two things it
# does, from two ability factories: it attaches itself to the villain when it
# enters play, and it offers the hero a way to pay it off. They are not two
# readings of one behaviour -- an engine could implement either without the
# other -- so there are two scenarios rather than one with two assertions.
#
# The printed ATK +1 belongs with the first. An attachment on the villain that
# nothing reads is indistinguishable from an attachment in the discard pile, so
# "it attached" is only worth asserting next to a number that moved because it
# did: Rhino's activation is 2 + 1 + 1, the printed ATK, the horn, and the
# Hydra Mercenary boost card.
#
# The cost is three *physical* resources and the hand pays it with Strength
# (printed 2 physical) and Vibranium (printed 2 wild), because no core-set card
# generates three physical on its own. The option is named `Hero_Action` by the
# engine, from the ability type rather than from any printed label.

Feature: Enhanced Ivory Horn

  Background:
    Given the scenario is "rhino"

  @card:01100
  Scenario: it attaches to the villain and adds 1 to the villain's attack
    Given the hero is "iron_man"
    And I am in hero form
    And "Enhanced Ivory Horn" is in play
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    Then "Enhanced Ivory Horn" is in the "UpgradesArea"

    When I pass
    When I pass
    Then I have 4 damage
    And "Enhanced Ivory Horn" is in play
    And it is round 2

  @card:01100
  Scenario: the hero action discards it for three physical resources
    # The horn leaves the villain and goes to the encounter discard pile, and
    # the hero is not exhausted for it -- the printed cost is resources, not a
    # basic power.
    Given the hero is "spider_man"
    And I am in hero form
    And "Enhanced Ivory Horn" is in play
    And my hand is "Strength", "Vibranium"

    When I choose "Hero Action" on "Enhanced Ivory Horn"
    Then "Enhanced Ivory Horn" is not in play
    And "Enhanced Ivory Horn" is in the "EncounterDiscardPile"
    And I have 0 cards in hand
    And I am not exhausted
    And I am not prompted again
