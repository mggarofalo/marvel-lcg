# Printed: "Hero Action: Choose one - draw 3 cards; deal 4 damage to an enemy;
# or remove 2 threat from a scheme."
#
# The card the format was designed around: a mid-resolution choice that a
# batched format cannot express, because the number and content of the prompts
# is behavior rather than something derivable from the printed text.

Feature: Nick Fury

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01084
  Scenario: damage is dealt to the chosen enemy, not the first one
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Deal 4 damage to an enemy" targeting "Shocker"
    Then "Shocker" has 4 damage
    And "Rhino" has 0 damage
    And I am not prompted again

  @card:01084
  Scenario: the option to remove threat is not offered when no scheme has any
    # The card is printed as a three-way choice. The engine offers two, because
    # "remove 2 threat" has no legal target with the main scheme at zero. That
    # the option set is state-dependent is exactly why the prompt is asserted.
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And "Shocker" is in play
    And the main scheme has 0 threat

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

  @card:01084
  Scenario: drawing puts three cards in hand
    Given I am in hero form
    And my hand is "Nick Fury", "Backflip", "Backflip", "Webbed Up", "Enhanced Spider-Sense"
    And my deck is "Backflip", "Backflip", "Backflip", "Enhanced Spider-Sense"

    When I play "Nick Fury"
    Then I am prompted to choose one
      | Draw 3 cards              |
      | Deal 4 damage to an enemy |

    When I choose "Draw 3 cards"
    Then I have 3 cards in hand
    And I have 1 card in my deck
    And I am not prompted again
