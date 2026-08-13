# Printed: "Give to the T'Challa player.
# You may flip to alter-ego form. Choose:
# - Exhaust T'Challa -> remove Affairs of State from the game.
# - Choose and discard a BLACK PANTHER upgrade you control. Discard this
#   obligation."
#
# An obligation resolves the moment it is revealed, so the transcript starts at
# the reveal rather than at a card being played. Two decisions come out of one
# reveal and they are not independent: the flip is offered first, and whether it
# was taken is what decides the option set of the second. That coupling is the
# reason each path is its own scenario -- a batched format would record the
# final board and lose which question produced it.
#
# The printed "Choose:" is a two-way choice, but neither bullet is always
# available. The first is gated on being T'Challa, and the second needs a
# BLACK PANTHER upgrade to discard. With neither available the rules fall
# through to discarding the obligation, which is the last scenario here.

Feature: Affairs of State

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  @card:01155
  Scenario: the chosen upgrade is discarded, not the other one
    Given I am in alter-ego form
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And "Affairs of State" is revealed

    Then I am prompted to choose one
      | Exhaust T'Challa → remove Affairs of State from the game                        |
      | Choose and discard a BLACK PANTHER upgrade you control. Discard this obligation |

    When I choose "Choose and discard a BLACK PANTHER upgrade you control. Discard this obligation" targeting "Panther Claws"
    Then "Panther Claws" is not in play
    And "Tactical Genius" is in play
    And "T'Challa" is not exhausted
    And "Affairs of State" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01155
  Scenario: exhausting T'Challa removes the obligation from the game and spares the upgrade
    # The two bullets differ in where the obligation ends up as well as in what
    # it costs: this one leaves the game, the other one is discarded and can be
    # shuffled back in. Both are asserted by zone for that reason.
    Given I am in alter-ego form
    And "Panther Claws" is in play
    And "Affairs of State" is revealed

    When I choose "Exhaust T'Challa → remove Affairs of State from the game"
    Then "Affairs of State" is in the "RemovedArea"
    And "T'Challa" is exhausted
    And "Panther Claws" is in play
    And I am not prompted again

  @card:01155
  Scenario: declining the flip takes the exhaust option off the table
    # "You may flip" is a real decision, and the engine asks it before the
    # "Choose:" -- so a hero who declines cannot take the first bullet, because
    # Black Panther is not T'Challa. The second prompt asserts exactly one
    # option for that reason.
    Given I am in hero form
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And "Affairs of State" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then I am prompted to choose one
      | Choose and discard a BLACK PANTHER upgrade you control. Discard this obligation |

    When I choose "Choose and discard a BLACK PANTHER upgrade you control. Discard this obligation" targeting "Tactical Genius"
    Then "Tactical Genius" is not in play
    And "Panther Claws" is in play
    And I am in hero form
    And "Affairs of State" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01155
  Scenario: an upgrade without the BLACK PANTHER trait cannot be fed to the obligation
    # Armored Vest is an upgrade the player controls and is not a legal target,
    # so with the flip declined neither printed bullet can be taken. That the
    # Vest survives is the claim -- a scenario with no upgrade at all would pass
    # whether or not the trait restriction were implemented.
    #
    # Where the obligation itself ends up is deliberately not asserted. The
    # engine falls through to discarding it, and no bullet on the card says to;
    # "Discard this obligation" is printed as part of the second bullet only.
    # Pinning the fall-through here would turn a question into a trusted answer.
    Given I am in hero form
    And "Armored Vest" is in play
    And "Affairs of State" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then "Armored Vest" is in play
    And "T'Challa" is not exhausted
    And I am in hero form
    And I am not prompted again
