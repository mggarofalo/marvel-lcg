Feature: Rhino encounter set minions

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"

  Scenario: Sandman enters play with a tough status card
    # Printed: "Toughness. (This character enters play with a tough status card.)"
    Given "01102" is in play
    Then "Sandman" is tough
    And "Sandman" has 0 damage

  Scenario: A tough status absorbs an attack instead of hit points
    Given "01001a" is in hero form
    And "01102" is in play
    When the player attacks "Sandman"
    Then "Sandman" has 0 damage
    And "Sandman" is not tough

  Scenario: Hydra Mercenary enters play with no status and 3 hit points
    Given "01101" is in play
    Then "Hydra Mercenary" has 3 health
    And "Hydra Mercenary" is not tough
    And "Hydra Mercenary" is not stunned
