# Klaw, stage II. Printed: "When Revealed: Search the encounter deck and
# discard pile for The "Immortal" Klaw and reveal it. Shuffle the encounter
# deck."
# "[star] Forced Interrupt: When Klaw attacks, give him 1 additional boost card
#  for this activation."
# Printed statistics: 18 hit points per hero, ATK 1, SCH 2.
#
# The search names two zones and the branch is which one holds the card -- or
# neither, which is the third path and the one that says the villain does not
# simply arrive with +10 hit points on principle. A stat assertion does not
# carry between villain stages, so the hit points, attack and scheme values are
# pinned here on their own card even though stages I and III run the same
# script file for their shared interrupt.
#
# ---------------------------------------------------------------------------
# How the transcripts reach stage II.
#
# A puzzle scene starts on stage I with the rest of the villain deck behind it,
# so the honest way to reveal stage II is to defeat stage I. Klaw stage I has
# 12 hit points against one hero; 11 damage from a `Given` plus Captain
# Marvel's printed ATK 2 defeats him, the villain advances, and stage II's When
# Revealed runs as part of that advance.
#
# The side scheme is named "01127" rather than by name throughout: its printed
# name The "Immortal" Klaw carries double quotes, which a card reference in
# this format has no way to spell.

Feature: Klaw (II)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01114
  Scenario: revealing stage II pulls The "Immortal" Klaw out of the encounter deck
    # The whole reveal, and the printed statistics of the stage it arrives on.
    # 28 hit points is 18 printed plus the 10 the side scheme grants while it
    # is in play, so that number is also the evidence that the search found
    # something and revealed it rather than merely discarding it.
    Given I am in hero form
    And "Klaw" has 11 damage
    And the encounter deck is "01127", "Armored Guard"

    When I attack "Klaw"
    Then "Klaw" has 2 "printed_stage"
    And "Klaw" has 1 "attack"
    And "Klaw" has 2 "scheme"
    And "01127" is in the "SideSchemesArea"
    And "01127" has 3 threat
    And "Klaw" has 28 health

  @card:01114
  Scenario: the discard pile is searched as well as the deck
    # "Search the encounter deck and discard pile" is two zones, and an engine
    # that read only the first passes the scenario above and fails this one.
    # The encounter deck here holds nothing but a minion, so the only copy in
    # the game is the one already in the discard pile.
    Given I am in hero form
    And "Klaw" has 11 damage
    And the encounter discard pile is "01127"
    And the encounter deck is "Armored Guard", "Armored Guard"

    When I attack "Klaw"
    Then "01127" is in the "SideSchemesArea"
    And "01127" has 3 threat
    And "Klaw" has 28 health

  @card:01114
  Scenario: with nothing to find the stage arrives on its printed hit points
    # The control, and the reason the 28 above is worth writing down. Neither
    # zone holds the side scheme, so the search finds nothing, nothing is
    # revealed, and Klaw stage II stands on the 18 hit points he prints.
    Given I am in hero form
    And "Klaw" has 11 damage
    And the encounter deck is "Armored Guard", "Armored Guard"

    When I attack "Klaw"
    Then "Klaw" has 2 "printed_stage"
    And "Klaw" has 18 health
    And "Klaw" has 0 damage
