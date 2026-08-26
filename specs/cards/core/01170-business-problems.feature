# Printed: "Give to the Tony Stark player.
# You may flip to alter-ego form. Choose:
# - Exhaust Tony Stark -> remove Business Problems from the game.
# - Exhaust each upgrade you control. Discard this obligation."
#
# An obligation resolves the moment it is revealed, so the transcript starts at
# the reveal rather than at a card being played. The reveal produces two
# decisions and they are not independent: the flip is offered first, and whether
# it was taken decides the option set of the "Choose:" that follows -- the first
# bullet is gated on being Tony Stark. That coupling is why each path is its own
# scenario.
#
# The second bullet takes no target: "each upgrade you control" is every one of
# them, so the discriminating question is not which upgrade was hit but how many
# and what else the engine reached for.

Feature: Business Problems

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"

  @card:01170
  Scenario: every upgrade exhausts, and nothing else does
    # Two upgrades, an ally and a support, so "each upgrade" has to be exactly
    # the two. An implementation that exhausted the first upgrade, or everything
    # in the play area, fails here; one that exhausted a single card would pass
    # a scenario with only one upgrade on the board.
    Given I am in alter-ego form
    And "Arc Reactor" is in play
    And "Mark V Helmet" is in play
    And "War Machine" is in play
    And "Stark Tower" is in play
    And "Business Problems" is revealed

    Then I am prompted to choose one
      | Exhaust Tony Stark → remove Business Problems from the game |
      | Exhaust each upgrade you control. Discard this obligation   |

    When I choose "Exhaust each upgrade you control. Discard this obligation"
    Then "Arc Reactor" is exhausted
    And "Mark V Helmet" is exhausted
    And "War Machine" is ready
    And "Stark Tower" is ready
    And "Tony Stark" is not exhausted
    And "Business Problems" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01170
  Scenario: exhausting Tony Stark removes the obligation from the game and leaves the upgrades ready
    # The two bullets differ in where the obligation ends up as well as in what
    # they cost: this one leaves the game, the other one is discarded and can be
    # shuffled back in. Both are asserted by zone for that reason.
    Given I am in alter-ego form
    And "Arc Reactor" is in play
    And "Business Problems" is revealed

    When I choose "Exhaust Tony Stark → remove Business Problems from the game"
    Then "Business Problems" is in the "RemovedArea"
    And "Tony Stark" is exhausted
    And "Arc Reactor" is ready
    And I am not prompted again

  @card:01170
  Scenario: declining the flip takes the exhaust option off the table
    # "You may flip" is a real decision, and the engine asks it first. A hero who
    # declines cannot take the first bullet -- Iron Man is not Tony Stark -- so
    # the "Choose:" comes back with exactly one option left, which is the
    # assertion.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Mark V Helmet" is in play
    And "Business Problems" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then I am prompted to choose one
      | Exhaust each upgrade you control. Discard this obligation |

    When I choose "Exhaust each upgrade you control. Discard this obligation"
    Then "Arc Reactor" is exhausted
    And "Mark V Helmet" is exhausted
    And I am in hero form
    And "Business Problems" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01170
  Scenario: flipping first is what makes the exhaust option available
    # The same board as above, answering the first decision the other way. The
    # option set of the second prompt is the assertion: flipping added the
    # bullet that declining removed.
    Given I am in hero form
    And "Arc Reactor" is in play
    And "Business Problems" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Flip to alter-ego form"
    Then I am prompted to choose one
      | Exhaust Tony Stark → remove Business Problems from the game |
      | Exhaust each upgrade you control. Discard this obligation   |

    When I choose "Exhaust Tony Stark → remove Business Problems from the game"
    Then I am not in hero form
    And "Tony Stark" is exhausted
    And "Arc Reactor" is ready
    And "Business Problems" is in the "RemovedArea"
    And I am not prompted again
