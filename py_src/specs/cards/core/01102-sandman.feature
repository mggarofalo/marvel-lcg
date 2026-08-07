# Printed: "Toughness. (This character enters play with a tough status card.)"

Feature: Sandman

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01102
  Scenario: it enters play with a tough status card
    Given "Sandman" is in play
    Then "Sandman" is tough
    And "Sandman" has 0 damage

  @card:01102
  Scenario: the tough status absorbs an attack instead of hit points
    Given I am in hero form
    And "Sandman" is in play

    When I attack "Sandman"
    Then "Sandman" has 0 damage
    And "Sandman" is not tough
    And I am not prompted again
