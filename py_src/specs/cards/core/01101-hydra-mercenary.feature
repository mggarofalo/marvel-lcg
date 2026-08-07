# Printed: "Guard. (While this minion is engaged with you, you cannot attack
# the villain.)"

Feature: Hydra Mercenary

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01101
  Scenario: it enters play with 3 hit points and no status
    Given "Hydra Mercenary" is in play
    Then "Hydra Mercenary" has 3 health
    And "Hydra Mercenary" is not tough
    And "Hydra Mercenary" is not stunned

  @card:01101
  Scenario: guard keeps the villain off the list of attack targets
    # With Guard in play the engine offers the minion as the only legal target,
    # so the attack needs no target named and the villain stays untouched.
    Given I am in hero form
    And "Hydra Mercenary" is in play

    When I choose "attack"
    Then "Hydra Mercenary" has 2 damage
    And "Rhino" has 0 damage
