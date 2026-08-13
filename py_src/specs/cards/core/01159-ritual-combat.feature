# Printed: "When Revealed: Discard the top card of the encounter deck. Then,
# choose to either deal X damage to your hero or place X threat on the main
# scheme. X is 1 more than the number of boost icons on the discarded encounter
# card."
#
# Two things have to be pinned and they are independent: which branch was taken,
# and what X was. X is computed before the question is asked and is baked into
# the option labels themselves, so the prompt table is the assertion that reads
# it -- an engine that miscounted boost icons would offer differently named
# options, whichever branch a scenario then took.
#
# So: two scenarios for the two branches at one value of X, and two more that
# move X. The encounter deck is written top-first, so the first card named is
# the one discarded and the second is there only so the deck is not left empty.
# Printed boost icons on the cards used:
#
#   Hard to Keep Down (01104)   no boost icons  -> X = 1
#   Hydra Mercenary (01101)     1 boost icon    -> X = 2
#   Usurp The Throne (01156)    3 boost icons   -> X = 4
#
# X = 1 is the case that pins the "1 more than": a card with no boost icons
# still deals damage, and an engine that read X as the icon count alone would do
# nothing at all there and would agree with every other scenario in this file.

Feature: Ritual Combat

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"

  @card:01159
  Scenario: the damage branch deals X to the hero and places no threat
    Given I am in hero form
    And the encounter deck is "Hydra Mercenary", "Sandman"
    And "Ritual Combat" is revealed

    Then I am prompted to choose one
      | deal 2 damage to your hero        |
      | place 2 threat on the main scheme |

    When I choose "deal 2 damage to your hero"
    Then I have 2 damage
    # The control on the other branch: an engine that resolved both would still
    # satisfy the damage assertion on its own.
    And the main scheme has 0 threat
    # The discard is a separate printed instruction from the choice, and it is
    # the top card of the deck rather than the treachery itself.
    And "Hydra Mercenary" is in the "EncounterDiscardPile"
    And "Sandman" is in the "EncounterDeck"
    And I am not prompted again

  @card:01159
  Scenario: the threat branch places X on the main scheme and leaves the hero alone
    Given I am in hero form
    And the encounter deck is "Hydra Mercenary", "Sandman"
    And "Ritual Combat" is revealed

    Then I am prompted to choose one
      | deal 2 damage to your hero        |
      | place 2 threat on the main scheme |

    When I choose "place 2 threat on the main scheme"
    Then the main scheme has 2 threat
    And I have 0 damage
    And "Hydra Mercenary" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01159
  Scenario: a discarded card with no boost icons still makes X one
    # The "+1". Nothing else in this file distinguishes X = icons from
    # X = icons + 1, because both branches scale together.
    Given I am in hero form
    And the encounter deck is "Hard to Keep Down", "Sandman"
    And "Ritual Combat" is revealed

    Then I am prompted to choose one
      | deal 1 damage to your hero        |
      | place 1 threat on the main scheme |

    When I choose "deal 1 damage to your hero"
    Then I have 1 damage
    And the main scheme has 0 threat
    And "Hard to Keep Down" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01159
  Scenario: X follows the boost icons of whatever card happened to be on top
    # Same transcript as the first scenario with one card swapped: three boost
    # icons instead of one, and every number moves by two. The pairing is what
    # makes this measure the boost count rather than a constant.
    Given I am in hero form
    And the encounter deck is "Usurp The Throne", "Sandman"
    And "Ritual Combat" is revealed

    Then I am prompted to choose one
      | deal 4 damage to your hero        |
      | place 4 threat on the main scheme |

    When I choose "place 4 threat on the main scheme"
    Then the main scheme has 4 threat
    And I have 0 damage
    And "Usurp The Throne" is in the "EncounterDiscardPile"
    And I am not prompted again
