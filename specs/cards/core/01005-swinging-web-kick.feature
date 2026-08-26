# Printed: "Hero Action (attack): Deal 8 damage to an enemy." Cost 3.

Feature: Swinging Web Kick

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01005
  Scenario: it deals 8 damage to the chosen enemy
    Given I am in hero form
    And my hand is "Swinging Web Kick", "Backflip", "Backflip", "Webbed Up"

    When I play "Swinging Web Kick" targeting "Rhino"
    Then "Rhino" has 6 health
    And "Rhino" has 8 damage
    And I am not prompted again

  @card:01005
  Scenario: playing it discards the card and the resources that paid for it
    Given I am in hero form
    And my hand is "Swinging Web Kick", "Backflip", "Backflip", "Webbed Up"

    When I play "Swinging Web Kick" targeting "Rhino"
    Then "Swinging Web Kick" is in the "DiscardPile"
    And I have 0 cards in hand
    And I have 4 cards in my discard pile
