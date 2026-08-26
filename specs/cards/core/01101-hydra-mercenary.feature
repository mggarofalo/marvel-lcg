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

  @card:01101
  Scenario: two copies are told apart by the order the scenario created them
    # The duplicate-name case (MARVEL-42). Create both, then address them by
    # ordinal. "#1" is the first one the encounter-deck step listed, and it
    # stays "#1" after it moves into play -- the ordinal counts creation order,
    # not position in a zone.
    Given I am in hero form
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #2" is in play
    And "Hydra Mercenary #1" has 2 damage

    Then "Hydra Mercenary #1" has 1 health
    And "Hydra Mercenary #2" has 3 health
    And "Hydra Mercenary #1" is in the "EngagedEnemiesArea"
    And "Hydra Mercenary #2" is in the "EngagedEnemiesArea"

  @card:01101
  Scenario: damage lands on the copy the transcript named
    # Two identical guards; the attack has to be told which. This is the
    # assertion that would silently pass under a first-match resolver.
    Given I am in hero form
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #2" is in play

    When I attack "Hydra Mercenary #2"
    Then "Hydra Mercenary #2" has 2 damage
    And "Hydra Mercenary #1" has 0 damage
