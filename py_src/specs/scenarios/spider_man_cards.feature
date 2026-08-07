Feature: Spider-Man player cards

  Background:
    Given the scenario "rhino"
    And the hero "spider_man"

  Scenario: Swinging Web Kick deals 8 damage to an enemy
    # Printed: "Hero Action (attack): Deal 8 damage to an enemy." Cost 3, so
    # three other cards are in hand to pay for it.
    Given "01001a" is in hero form
    And the hand contains "01005", "01003", "01003", "01009"
    When the player plays "01005" targeting "Rhino in VillainArea"
    Then "Rhino in VillainArea" has 6 health
    And "01005" is in the "DiscardPile"
    And the player has 0 cards in hand

  Scenario: Playing a card discards the resources that paid for it
    Given "01001a" is in hero form
    And the hand contains "01005", "01003", "01003", "01009"
    When the player plays "01005" targeting "Rhino in VillainArea"
    Then the player has 4 cards in the discard pile

  Scenario Outline: A basic attack is the same however much damage is already there
    Given "01001a" is in hero form
    And "Rhino in VillainArea" has <damage> damage
    When the player attacks "Rhino in VillainArea"
    Then "Rhino in VillainArea" has <health> health

    Examples:
      | damage | health |
      | 0      | 12     |
      | 3      | 9      |
      | 7      | 5      |
