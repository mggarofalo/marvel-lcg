# Printed: "When Revealed: Choose to either take 2 damage or place 1 threat on
# the main scheme."
#
# An encounter card whose reveal stops and asks the player a question, so the
# transcript starts inside the Given: `"Hydra Bomber" is revealed` runs the
# reveal pipeline, and the first decision the policy answers is the card's own
# choice rather than the turn menu.
#
# Two printed branches, so two scenarios carry the branches and two more pin
# things a batched format would lose: that "take 2 damage" means the identity
# and not the minion or the hero form, and that a second copy asks a second
# time rather than the two reveals collapsing into one.

Feature: Hydra Bomber

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01110
  Scenario: taking the damage puts it on the identity, not on the minion
    # "Take 2 damage" is printed without a subject, and there are two units it
    # could plausibly mean. The assertions say which: the hero has 2 and the
    # Bomber still has all 2 of its health.
    Given I am in hero form
    And "Hydra Bomber" is revealed

    Then I am prompted to choose one
      | Take 2 damage                     |
      | Place 1 threat on the main scheme |

    When I choose "Take 2 damage"
    Then I have 2 damage
    And "Hydra Bomber" has 0 damage
    And "Hydra Bomber" has 2 health
    And the main scheme has 0 threat
    And I am not prompted again

  @card:01110
  Scenario: placing the threat leaves the hero undamaged
    # The other branch of the same prompt. The main scheme goes to 1 and the
    # hero stays at 0 -- the second assertion is what makes this scenario
    # discriminating rather than a restatement of the first.
    Given I am in hero form
    And "Hydra Bomber" is revealed

    Then I am prompted to choose one
      | Take 2 damage                     |
      | Place 1 threat on the main scheme |

    When I choose "Place 1 threat on the main scheme"
    Then the main scheme has 1 threat
    And I have 0 damage
    And I am not prompted again

  @card:01110
  Scenario: an alter-ego is asked the same question and takes the same damage
    # The choice is not gated on form, and the damage follows the identity
    # through the flip. Both options are still offered, which is the part a
    # spec that only checked the damage total would miss.
    Given I am in alter-ego form
    And "Hydra Bomber" is revealed

    Then I am prompted to choose one
      | Take 2 damage                     |
      | Place 1 threat on the main scheme |

    When I choose "Take 2 damage"
    Then I have 2 damage
    And I am not in hero form
    And I am not prompted again

  @card:01110
  Scenario: two copies ask twice, and the answers are independent
    # Two reveals, two prompts, answered differently. A single shared
    # resolution would land both effects the same way; here the hero takes 2
    # from the first copy and the main scheme takes 1 from the second.
    Given I am in hero form
    And the encounter deck is "Hydra Bomber", "Hydra Bomber"
    And "Hydra Bomber #1" is revealed
    And "Hydra Bomber #2" is revealed

    When I choose "Take 2 damage"
    When I choose "Place 1 threat on the main scheme"
    Then I have 2 damage
    And the main scheme has 1 threat
    And I am not prompted again
