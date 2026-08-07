# The basic actions every hero has, independent of any card's text. These live
# under specs/rules/ rather than specs/cards/ because they are rulebook
# behavior; Spider-Man is only the hero the transcript happens to use.

Feature: Basic actions

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  Scenario: changing form flips the identity to its hero side
    Given I am in alter-ego form

    When I change form
    Then I am in hero form
    And I am not prompted again

  Scenario: a basic attack deals the hero's ATK and exhausts them
    # Spider-Man's hero side is printed ATK 2. Rhino stage 1 has 14 hit points
    # against one player.
    Given I am in hero form

    When I attack "Rhino"
    Then "Rhino" has 12 health
    And "Rhino" has 2 damage
    And I am exhausted

  Scenario: a basic thwart removes the hero's THW from the main scheme
    # Spider-Man's hero side is printed THW 1.
    Given I am in hero form
    And the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat
    And I am exhausted

  Scenario: an alter-ego is not offered an attack
    Given I am in alter-ego form

    When I pass
    Then "Rhino" has 14 health
    And I am not in hero form
