# Klaw, stage I. Printed: "[star] Forced Interrupt: When Klaw attacks, give him
# 1 additional boost card for this activation."
# Printed statistics: 12 hit points per hero, ATK 0, SCH 2.
#
# One ability with one condition in it -- "when Klaw attacks" -- so the two
# transcripts below are an activation that is an attack and an activation that
# is not. The card prints ATK 0, which means the only damage the villain ever
# deals at this stage comes out of the boost cards, and so the number of boost
# cards is the whole card.
#
# `12*` is two claims, not one: 12 hit points and 12 *per hero*. A one-hero
# board cannot tell those apart, so the second scenario is played two-handed
# and the number doubles.
#
# The stage-III card runs this same script file for the shared interrupt, but a
# stat assertion does not carry between stages -- different hit points, attack
# and scheme values -- so 01115 is specced separately.
#
# ---------------------------------------------------------------------------
# Reading the encounter deck in these scenarios.
#
# Decks are written top-first, and this engine's villain phase takes cards off
# in this order: the escalation threat is placed first, then the villain
# activates and draws its boost cards, then an encounter card is dealt to the
# player and revealed. So in "Armored Guard" x4 against an attacking Klaw the
# first two are boost cards, the third is the encounter card that enters play,
# and the fourth is untouched -- which is exactly the shape the assertions
# below read, one card per zone.

Feature: Klaw (I)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

  @card:01113
  Scenario: an attacking Klaw spends two boost cards, not one
    # Both boost cards reach the encounter discard pile and the card behind
    # them is still in the deck, so the count is read off the board rather than
    # inferred from the damage. The damage says the same thing a second way:
    # Armored Guard prints 1 boost icon and Klaw prints ATK 0, so 2 damage is
    # two boost cards and nothing else.
    Given I am in hero form
    And the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard", "Armored Guard"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 2 damage
    And "Klaw" has 12 health
    And "Klaw" has 0 "attack"
    And "Klaw" has 2 "scheme"
    And "Armored Guard #1" is in the "EncounterDiscardPile"
    And "Armored Guard #2" is in the "EncounterDiscardPile"
    And "Armored Guard #3" is in the "EngagedEnemiesArea"
    And "Armored Guard #4" is in the "EncounterDeck"
    And it is round 2

  @card:01113
  Scenario: a scheming Klaw spends one, because scheming is not attacking
    # The control. An alter-ego is schemed against rather than attacked, so
    # "when Klaw attacks" is false and the activation takes the single boost
    # card every villain activation takes: the first card is spent, the second
    # is the encounter card dealt afterwards, and the third never moves.
    #
    # 4 threat says the same thing arithmetically -- Klaw's printed SCH 2, one
    # boost icon, and the main scheme's printed escalation 1. A second boost
    # card would have made it 5.
    Given I am in alter-ego form
    And the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard", "Armored Guard"

    When I pass
    Then the main scheme has 4 threat
    And "Armored Guard #1" is in the "EncounterDiscardPile"
    And "Armored Guard #2" is in the "EngagedEnemiesArea"
    And "Armored Guard #3" is in the "EncounterDeck"
    And "Armored Guard #4" is in the "EncounterDeck"
    And it is round 2

  @card:01113
  Scenario: the printed hit points are per hero
    # `12*`. Two heroes at the table and the villain stands up on 24, which is
    # the reading a one-hero board cannot distinguish from a flat 12. The
    # attack and scheme values carry no star and do not move.
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form

    Then "Klaw" has 24 health
    And "Klaw" has 0 "attack"
    And "Klaw" has 2 "scheme"
    And "Klaw" has 1 "printed_stage"
