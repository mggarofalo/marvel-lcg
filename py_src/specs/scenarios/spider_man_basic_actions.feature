# Authored from printed card text, then validated against the Python engine.
# Nothing here is trusted until `python -m tools.spec.validate` says it passes.

Feature: Spider-Man basic actions

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"

  Scenario: A hero in alter-ego form can change to hero form
    Given "01001a" is in alter-ego form
    When the player changes form
    Then "01001a" is in the "HeroArea"
    And "01001a" has 0 damage

  Scenario: A basic attack deals the hero's ATK and exhausts them
    # Spider-Man's hero side is printed ATK 2. Rhino stage 1 has 14 hit points
    # against one player.
    Given "01001a" is in hero form
    When the player attacks "Rhino in VillainArea"
    Then "Rhino in VillainArea" has 12 health
    And "Rhino in VillainArea" has 2 damage
    And "01001a" is exhausted

  Scenario: A basic thwart removes the hero's THW from the main scheme
    # Spider-Man's hero side is printed THW 1.
    Given "01001a" is in hero form
    And "The Break-In!" has 5 threat
    When the player thwarts "The Break-In!"
    Then "The Break-In!" has 4 threat
    And "01001a" is exhausted

  Scenario: An alter-ego cannot attack
    # Peter Parker has no ATK, so the action is never offered. The scenario
    # asserts the board is untouched after the turn passes.
    Given "01001a" is in alter-ego form
    When the player passes
    Then "Rhino in VillainArea" has 14 health
