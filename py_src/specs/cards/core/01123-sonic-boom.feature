# Printed: "When Revealed: Either spend [energy] [mental] [physical] resources
# or exhaust each character you control."
# "[star] Boost: If this activation deals damage to you, exhaust your hero."
#
# A treachery whose When Revealed is a choice, which makes the option set the
# thing worth pinning. Both branches are asserted against the *other* branch's
# effect not happening, because "exhaust each character you control" and "spend
# three resources" are both invisible to an assertion that only looks at one.

Feature: Sonic Boom

  Background:
    Given the scenario is "klaw"
    And the hero is "spider_man"

  @card:01123
  Scenario: the exhaust branch exhausts the ally as well as the hero
    # "each character you control" is the claim -- a scenario with only a hero
    # in play would pass on an engine that exhausted just the hero.
    Given I am in hero form
    And "Black Cat" is in play
    And my hand is "Backflip", "Backflip", "Backflip", "Backflip", "Backflip"
    And "Sonic Boom" is revealed

    Then I am prompted to choose one
      | Spend [[energy]][[mental]][[physical]] |
      | Exhaust each character you control     |

    When I choose "Exhaust each character you control"
    Then I am exhausted
    And "Black Cat" is exhausted

  @card:01123
  Scenario: paying the three resources leaves every character ready
    Given I am in hero form
    And "Black Cat" is in play
    And my hand is "Haymaker", "Enhanced Spider-Sense", "Backflip", "Backflip", "Backflip"
    And "Sonic Boom" is revealed

    Then I am prompted to choose one
      | Spend [[energy]][[mental]][[physical]] |
      | Exhaust each character you control     |

    When I choose "Spend [[energy]][[mental]][[physical]]"
    Then I am not exhausted
    And "Black Cat" is not exhausted
    # Without this the scenario passes on an engine that resolved nothing at
    # all: "not exhausted" is the state the board was already in. Three
    # resources spent is three cards out of a five-card hand.
    And I have 2 cards in hand
