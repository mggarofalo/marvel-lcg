# Printed: "Retaliate 1. (After this character is attacked, deal 1 damage to
# the attacking character.)"
#
# Printed statistics: 11 hit points, ATK 2, THW 2, DEF 2, hand size 5.
#
# Black Panther has no script -- the tier is `stats_only`, and everything the
# card does comes from `game/card/face/attribute/` applied to the printed
# numbers. So the numbers *are* the specification, and the two basic powers are
# the only way to read ATK and THW back out of the engine.
#
# Retaliate 1 is deliberately not repeated here. `specs/rules/keywords.feature`
# already pins it with this hero, in both directions -- the villain takes 1 back
# after attacking him, and nothing comes back when he is the one attacking. Those
# scenarios carry no `@card:` tag, so they do not credit 01040a in
# `tools.spec.coverage`, but writing the same transcript again to move a number
# would be padding, not evidence.
#
# A basic power exhausts the hero, so ATK and THW cannot share one transcript.
# That is why these are two scenarios rather than one with two assertions.

Feature: Black Panther

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01040a
  Scenario: a basic attack deals his printed ATK 2, and he has 11 hit points
    # Rhino stage 1 is printed 14 hit points against one player, so 2 damage is
    # visible as both "2 damage" and "12 health" -- the second is the one that
    # would survive an engine that recorded damage without subtracting it.
    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And "Rhino" has 12 health
    And "me" has 11 health
    And I have 0 damage
    And I am exhausted
    And I am not prompted again

  @card:01040a
  Scenario: a basic thwart removes his printed THW 2
    # 2, not 1. Spider-Man's THW is 1 and `specs/rules/basic-actions.feature`
    # pins that, so this scenario is the one that says the number belongs to the
    # hero rather than to the basic action.
    Given the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 3 threat
    And I am exhausted
    And I am not prompted again

  @card:01040a
  Scenario: the end phase draws him back up to his printed hand size of 5
    # Hand size is printed on the identity and differs between the two sides --
    # 5 on Black Panther, 6 on T'Challa -- so this scenario and the matching one
    # in 01040b-tchalla.feature are each other's control.
    #
    # The deck and the encounter deck both have to be stocked or the round does
    # not finish: a puzzle scene starts with neither, and an empty player deck
    # eliminates the hero in round 1.
    Given my deck is "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium", "Vibranium"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then I have 5 cards in hand
    And I have 3 cards in my deck
    And it is round 2
