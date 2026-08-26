# Hard to Keep Down. Printed:
#
#   "When Revealed: Rhino heals 4 damage. If no damage was healed this way, this
#    card gains surge."
#
# ---------------------------------------------------------------------------
# Three branches, and the middle one is the reason there are three.
#
#   damage >= 4    the villain heals a full 4
#   0 < damage < 4  the villain heals what there is, and does NOT surge
#   damage == 0    nothing is healed, so the card surges
#
# The printed condition is "if no damage was healed", not "if 4 damage was not
# healed". A partial heal is still a heal, so the middle board is where an
# engine that read the condition as "the full amount" would surge and this
# scenario says it must not.
#
# ---------------------------------------------------------------------------
# Surge needs a real villain phase; a `Given`-time reveal cannot see it.
#
# A card surged from a `Given` stops in `DealtEncounterCardsDeck` and never
# reveals what it surged into, so both surge scenarios walk a round instead. The
# encounter deck is written top-first (MARVEL-82) and a villain activation takes
# two cards off it, so in `"Hydra Mercenary", "Hard to Keep Down", "Shocker"`
# the mercenary is the boost card, the treachery is the card revealed, and
# Shocker is what a surge reaches. Shocker entering play is the surge and
# nothing else, which is why the two villain-phase scenarios below differ in
# exactly one Given -- whether Rhino is damaged going in.
#
# The hero is Iron Man in alter-ego form: an alter-ego is schemed against rather
# than attacked, so no defence prompt interrupts the walk, and Shocker's own
# "deal 1 damage to each hero" finds no hero and cannot be confused with the
# villain's activation.

Feature: Hard to Keep Down

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:01104
  Scenario: a villain with damage to spare heals exactly 4 of it
    # 6 damage in, 2 damage out. The 2 is what makes this a heal of 4 rather
    # than a heal to full.
    Given "Rhino" has 6 damage
    And "Hard to Keep Down" is revealed

    Then "Rhino" has 2 damage
    And "Rhino" has 12 health
    And I am not prompted again

  @card:01104
  Scenario: less than 4 damage heals all of it and no more
    # The heal is capped by what is there, not by what is printed: 2 damage in,
    # 0 out, and the villain is back to its printed 14 rather than 16.
    Given "Rhino" has 2 damage
    And "Hard to Keep Down" is revealed

    Then "Rhino" has 0 damage
    And "Rhino" has 14 health
    And "Rhino" has 14 "max_health"
    And I am not prompted again

  @card:01104
  Scenario: an undamaged villain heals nothing, so the card surges
    Given the hero is "iron_man"
    And I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hard to Keep Down", "Shocker"

    When I pass
    Then "Rhino" has 0 damage
    And "Shocker" is in play
    And it is round 2

  @card:01104
  Scenario: a partial heal is still a heal, and does not surge
    # The control for the surge, and the third branch at the same time. The only
    # difference from the scenario above is the 2 damage on Rhino: it heals, so
    # nothing surges and Shocker is still sitting in the encounter deck at the
    # end of the round.
    Given the hero is "iron_man"
    And I am in alter-ego form
    And "Rhino" has 2 damage
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hard to Keep Down", "Shocker"

    When I pass
    Then "Rhino" has 0 damage
    And "Shocker" is not in play
    And "Shocker" is in the "EncounterDeck"
    And it is round 2
