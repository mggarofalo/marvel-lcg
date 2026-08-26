# Printed: "Give to the Carol Danvers player.
# You may flip to alter-ego form. Choose:
# - Exhaust Carol Danvers -> remove Family Emergency from the game.
# - You are stunned. This card gains surge. Discard this obligation."
#
# An obligation resolves the moment it is revealed, so the transcript starts at
# the reveal rather than at a card being played. The reveal produces two
# decisions and they are not independent: the flip is offered first, and whether
# it was taken decides the option set of the "Choose:" that follows -- the first
# bullet is gated on being Carol Danvers. That coupling is why each path is its
# own scenario.
#
# Unlike the other two core obligations this one has no fall-through: the second
# bullet needs nothing on the board, so it is always available and a hero who
# declines the flip simply takes it.
#
# "This card gains surge" is not asserted anywhere below. A puzzle scene has no
# encounter deck to deal the extra card out of, and the vocabulary has no step
# for the size of one either -- see the report on this file.

Feature: Family Emergency

  Background:
    Given the scenario is "rhino"
    And the hero is "captain_marvel"

  @card:01175
  Scenario: exhausting Carol Danvers removes the obligation from the game without stunning her
    # The two bullets differ in where the obligation ends up as well as in what
    # they cost: this one leaves the game, the other one is discarded and can be
    # shuffled back in. Both are asserted by zone for that reason, and the stun
    # is asserted absent so this scenario cannot pass on the other branch.
    Given I am in alter-ego form
    And "Family Emergency" is revealed

    Then I am prompted to choose one
      | Exhaust Carol Danvers → remove Family Emergency from the game   |
      | You are stunned. This card gains surge. Discard this obligation |

    When I choose "Exhaust Carol Danvers → remove Family Emergency from the game"
    Then "Family Emergency" is in the "RemovedArea"
    And "Carol Danvers" is exhausted
    And "Carol Danvers" is not stunned
    And I am not prompted again

  @card:01175
  Scenario: the other bullet stuns the identity and discards the obligation instead
    Given I am in alter-ego form
    And "Family Emergency" is revealed

    When I choose "You are stunned. This card gains surge. Discard this obligation"
    Then "Carol Danvers" is stunned
    And "Carol Danvers" is not exhausted
    And "Family Emergency" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01175
  Scenario: flipping first is what makes the exhaust option available
    # "You may flip" is a real decision and the engine asks it before the
    # "Choose:". The second prompt's option set is the assertion: flipping is
    # what puts the first bullet on the table.
    Given I am in hero form
    And "Family Emergency" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Flip to alter-ego form"
    Then I am prompted to choose one
      | Exhaust Carol Danvers → remove Family Emergency from the game   |
      | You are stunned. This card gains surge. Discard this obligation |

    When I choose "Exhaust Carol Danvers → remove Family Emergency from the game"
    Then I am not in hero form
    And "Carol Danvers" is exhausted
    And "Carol Danvers" is not stunned
    And "Family Emergency" is in the "RemovedArea"
    And I am not prompted again

  @card:01175
  Scenario: declining the flip leaves only the stun, and the hero takes it
    # Captain Marvel is not Carol Danvers, so the first bullet has no legal
    # taker. One option and one target leaves the engine nothing to ask about,
    # which is why the stun lands with no second prompt.
    Given I am in hero form
    And "Family Emergency" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then I am in hero form
    And "Captain Marvel" is stunned
    And "Captain Marvel" is not exhausted
    And "Family Emergency" is in the "EncounterDiscardPile"
    And I am not prompted again
