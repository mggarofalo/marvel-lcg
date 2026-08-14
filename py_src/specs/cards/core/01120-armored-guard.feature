# Printed: "Guard. (While this minion is engaged with you, you cannot attack
# the villain.)"
# "Toughness. (This character enters play with a tough status card.)"
# Printed statistics: 3 hit points, ATK 1, SCH 0, boost 1.
#
# The card has no script at all: both keywords and every number come from
# `game/card/face/attribute/`, applied generically to any card that prints
# them. That is what "no script" means in this engine and why the spec
# campaign's denominator is larger than the number of scripted cards -- these
# are still claims, and this composition of them is only made by this card.
#
# `specs/rules/keywords.feature` pins Guard and Toughness as rules, on Hydra
# Mercenary and Sandman in the Rhino scenario. What is measured here is this
# card: its own statistics, and the two keywords on one minion at once, where
# they interact -- the guard cannot be cleared by the attack that would
# otherwise have cleared it, because the first attack is spent on the tough
# card.
#
# The second scenario is a control and not decoration. `I cannot attack` passes
# both when the action is refused and when it is not offered at all, so without
# a board on which the same step's subject *is* attackable, the first scenario
# is also satisfied by an engine that had forgotten how to attack.

Feature: Armored Guard

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"
    And I am in hero form

  @card:01120
  Scenario: it arrives engaged and tough, and puts the villain out of reach
    # Guard shows up in neither the option set nor any card's state -- the
    # engine enforces it by emptying the Attack option's legal targets -- so
    # `I cannot attack` is the only step that can see it, and the two attacks
    # that follow are what say the restriction is about the villain rather
    # than about attacking.
    #
    # Hellcat's printed ATK 1 is cancelled in full by the tough card, which is
    # the keyword being a cancel rather than a reduction; the hero's printed
    # ATK 2 then lands on a minion that is still standing, which is the reason
    # a 3 hit point guard is not simply removed by whoever wants to reach the
    # villain.
    Given "Armored Guard" is in play
    And "Hellcat" is in play

    Then "Armored Guard" is in the "EngagedEnemiesArea"
    And "Armored Guard" has 3 health
    And "Armored Guard" has 1 "attack"
    And "Armored Guard" has 0 "scheme"
    And "Armored Guard" has 1 "boost_const"
    And "Armored Guard" is tough
    And I cannot attack "Klaw"

    When I choose "attack" on "Hellcat" targeting "Armored Guard"
    Then "Armored Guard" has 0 damage
    And "Armored Guard" is not tough

    When I attack "Armored Guard"
    Then "Armored Guard" has 2 damage
    And "Armored Guard" is in play

  @card:01120
  Scenario: with no guard engaged the villain is attackable
    Given "Hellcat" is in play

    When I attack "Klaw"
    Then "Klaw" has 2 damage
