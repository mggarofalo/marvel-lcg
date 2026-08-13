# Printed: "Give to the Jennifer Walters player.
# You may flip to alter-ego form. Choose:
# - Exhaust Jennifer Walters -> remove Legal Work from the game.
# - Give the main scheme 1 acceleration token. Discard this obligation."
#
# An obligation resolves the moment it is revealed, so the transcript starts at
# the reveal rather than at a card being played. One reveal produces two
# decisions and they are not independent: the flip is offered first, and whether
# it was taken decides the option set of the "Choose:" that follows -- the first
# bullet is gated on being Jennifer Walters, which only the alter-ego side is.
# That coupling is why each path is its own scenario.
#
# The two bullets are told apart by three things at once, and every scenario
# below asserts all three, because any one of them alone is satisfied by an
# engine that resolved the other branch: where the obligation ends up
# (RemovedArea vs EncounterDiscardPile), whether the identity exhausted, and
# whether the main scheme gained an acceleration token. "Not exhausted" on its
# own is the state the board was already in.
#
# The two hero-form scenarios open with `When I pass`. That is not part of this
# card: `Given I am in hero form` flips She-Hulk during setup, and her printed
# response '"Do You Even Lift?"' triggers after a change into hero form, so the
# engine asks about it before the obligation gets a word in. Passing declines
# it. The alter-ego scenarios need no such beat because a game starts in
# alter-ego form and nothing was flipped.

Feature: Legal Work

  Background:
    Given the scenario is "rhino"
    And the hero is "she_hulk"

  @card:01160
  Scenario: exhausting Jennifer Walters removes the obligation from the game
    # No acceleration token is the load-bearing negative here. The obligation
    # leaving the game and the identity exhausting are both consistent with an
    # engine that ran the first bullet and then leaked the second one's effect.
    Given I am in alter-ego form
    And "Legal Work" is revealed

    Then I am prompted to choose one
      | Exhaust Jennifer Walters → remove Legal Work from the game         |
      | Give the main scheme 1 acceleration token. Discard this obligation |

    When I choose "Exhaust Jennifer Walters → remove Legal Work from the game"
    Then "Legal Work" is in the "RemovedArea"
    And "Jennifer Walters" is exhausted
    And "the main scheme" has 0 "acceleration_token" tokens
    And I am not prompted again

  @card:01160
  Scenario: the acceleration bullet discards the obligation instead of removing it
    # The other half of the same prompt. Discarded rather than removed is the
    # difference that matters to the rest of the game -- an obligation in the
    # encounter discard pile can be shuffled back in and revealed again.
    Given I am in alter-ego form
    And "Legal Work" is revealed

    Then I am prompted to choose one
      | Exhaust Jennifer Walters → remove Legal Work from the game         |
      | Give the main scheme 1 acceleration token. Discard this obligation |

    When I choose "Give the main scheme 1 acceleration token. Discard this obligation"
    Then "the main scheme" has 1 "acceleration_token" token
    And "Legal Work" is in the "EncounterDiscardPile"
    And "Jennifer Walters" is not exhausted
    And I am not prompted again

  @card:01160
  Scenario: taking the offered flip is what puts the exhaust bullet on the table
    # "You may flip" is a real decision and the engine asks it before the
    # "Choose:". The second prompt's option set is the assertion: flipping is
    # what makes the first bullet available at all.
    Given I am in hero form
    And "Legal Work" is revealed

    When I pass
    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Flip to alter-ego form"
    Then I am prompted to choose one
      | Exhaust Jennifer Walters → remove Legal Work from the game         |
      | Give the main scheme 1 acceleration token. Discard this obligation |

    When I choose "Exhaust Jennifer Walters → remove Legal Work from the game"
    Then I am not in hero form
    And "Jennifer Walters" is exhausted
    And "Legal Work" is in the "RemovedArea"
    And "the main scheme" has 0 "acceleration_token" tokens
    And I am not prompted again

  @card:01160
  Scenario: declining the flip leaves one bullet, and it resolves without a prompt
    # She-Hulk is not Jennifer Walters, so with the flip declined the first
    # bullet has no legal taker. One option and no target leaves the engine
    # nothing to ask about, which is why the acceleration token lands with no
    # second prompt at all -- `I am not prompted again` immediately after
    # `Cancel` is that claim, and it is the one a scenario that answered a
    # phantom second prompt would fail.
    Given I am in hero form
    And "Legal Work" is revealed

    When I pass
    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then I am in hero form
    And "She-Hulk" is not exhausted
    And "the main scheme" has 1 "acceleration_token" token
    And "Legal Work" is in the "EncounterDiscardPile"
    And I am not prompted again
