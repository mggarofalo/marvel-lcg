# Printed: "Give to the Peter Parker player.
# You may flip to alter-ego form. Choose:
# - Exhaust Peter Parker -> remove Eviction Notice from the game.
# - Discard 1 card at random from your hand. This card gains surge. Discard this
#   obligation."
#
# An obligation resolves the moment it is given, so the transcript opens at the
# reveal rather than at a card being played and the first `Then` has no `When`
# before it.
#
# The reveal produces two decisions and they are not independent: "You may flip"
# is asked first, and whether it was taken decides the option set of the
# "Choose:" underneath -- the first bullet is conditional on being in alter-ego
# form, because only an alter-ego Peter Parker is there to exhaust. That coupling
# is why each path is its own scenario rather than one scenario with a table.
#
# The two bullets differ in three observable ways and all three are asserted on
# both sides: where the obligation ends up (out of the game, or in the encounter
# discard pile where a reshuffle can bring it back), whether the identity is
# exhausted, and whether a card left hand. Zone is the load-bearing one -- "not
# exhausted" and "hand unchanged" are both states the board was already in.
#
# "This card gains surge" is not asserted. A surged card stops in
# DealtEncounterCardsDeck when the reveal came from a `Given` rather than from a
# real villain phase, so the harness cannot see it -- the same gap
# specs/cards/core/01175-family-emergency.feature records.

Feature: Eviction Notice

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01165
  Scenario: exhausting Peter Parker removes the obligation from the game
    Given I am in alter-ego form
    And my hand is "Backflip", "Backflip", "Backflip"
    And "Eviction Notice" is revealed

    Then I am prompted to choose one
      | Exhaust Peter Parker → remove Eviction Notice from the game                      |
      | Discard 1 card at random from your hand. This card gains surge. Discard this obligation |

    When I choose "Exhaust Peter Parker → remove Eviction Notice from the game"
    Then "Eviction Notice" is in the "RemovedArea"
    And "Peter Parker" is exhausted
    # The controls on the other bullet: this branch costs no card and does not
    # put the obligation anywhere it could come back from.
    And I have 3 cards in hand
    And I am not prompted again

  @card:01165
  Scenario: the other bullet costs a card at random and discards the obligation
    Given I am in alter-ego form
    And my hand is "Backflip", "Backflip", "Backflip"
    And "Eviction Notice" is revealed

    Then I am prompted to choose one
      | Exhaust Peter Parker → remove Eviction Notice from the game                      |
      | Discard 1 card at random from your hand. This card gains surge. Discard this obligation |

    When I choose "Discard 1 card at random from your hand. This card gains surge. Discard this obligation"
    Then I have 2 cards in hand
    And I have 1 card in my discard pile
    # Discarded, not removed: this is the copy a reshuffle can deal again.
    And "Eviction Notice" is in the "EncounterDiscardPile"
    And "Peter Parker" is not exhausted
    And I am not prompted again

  @card:01165
  Scenario: flipping is what puts the exhaust bullet on the table
    # A hero is asked whether to flip before the "Choose:", and the second
    # prompt's option set is the assertion -- taking the flip is what makes the
    # first bullet legal. The hand is all copies of one card so that which copy
    # the random discard would have taken cannot matter to anything asserted.
    Given I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"
    And "Eviction Notice" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Flip to alter-ego form"
    Then I am prompted to choose one
      | Exhaust Peter Parker → remove Eviction Notice from the game                      |
      | Discard 1 card at random from your hand. This card gains surge. Discard this obligation |

    When I choose "Exhaust Peter Parker → remove Eviction Notice from the game"
    Then I am not in hero form
    And "Peter Parker" is exhausted
    And "Eviction Notice" is in the "RemovedArea"
    And I have 3 cards in hand
    And I am not prompted again

  @card:01165
  Scenario: declining the flip leaves only the random discard, and it resolves unasked
    # Staying in hero form makes the first bullet illegal, which leaves the
    # engine one option with no target to pick -- so the second bullet lands with
    # no further prompt rather than being offered as a one-row choice. The card
    # still leaves hand and the obligation is still discarded, so this is the
    # printed fall-through and not the resolution being skipped.
    Given I am in hero form
    And my hand is "Backflip", "Backflip", "Backflip"
    And "Eviction Notice" is revealed

    Then I am prompted to choose one
      | Flip to alter-ego form |
      | Cancel                 |

    When I choose "Cancel"
    Then I am in hero form
    And I have 2 cards in hand
    And I have 1 card in my discard pile
    And "Eviction Notice" is in the "EncounterDiscardPile"
    And "Spider-Man" is not exhausted
    And I am not prompted again
