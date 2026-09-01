# "I'm Tough". Printed:
#
#   "When Revealed: Give Rhino a tough status card. If Rhino already has a tough
#    status card, this card gains surge."
#
# The card is named with its quotation marks printed -- the name is literally
# `"I'm Tough"`. A quoted argument escapes those printed marks, so the step
# spelling is `"\"I'm Tough\""`.
#
# ---------------------------------------------------------------------------
# Two branches, decided by one question about the board.
#
#   Rhino is not tough    he becomes tough, and nothing surges
#   Rhino is already tough  nothing is given, and the card surges
#
# The condition is exclusive in the engine (`if villain and not villain.IsTough()`
# ... `else: GainSurge`) and exclusive in the printed text, so each scenario has
# to say what did *not* happen as well as what did. The first asserts that
# Shocker stayed in the encounter deck; the second that it came out.
#
# Both walk a real villain phase, because surge is invisible from a `Given`-time
# reveal -- a surged card stops in `DealtEncounterCardsDeck`. The deck is written
# top-first (the original investigation): the mercenary boosts the activation, 01105 is the card
# revealed, and Shocker is what a surge reaches. The two scenarios differ in one
# Given, whether Rhino starts tough, so Shocker's fate is the branch and nothing
# else.
#
# Iron Man in alter-ego form: an alter-ego is schemed against rather than
# attacked, so no defence prompt interrupts the walk and no damage total has to
# be explained.

Feature: "I'm Tough"

  Background:
    Given the scenario is "rhino"

  @card:01105
  Scenario: an untoughened villain is given the status and nothing surges
    Given the hero is "iron_man"
    And I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "01105", "Shocker"

    Then "Rhino" is not tough

    When I pass
    Then "Rhino" is tough
    And "Shocker" is not in play
    And "Shocker" is in the "EncounterDeck"
    And it is round 2

  @card:01105
  Scenario: a villain that is already tough gains nothing, so the card surges
    # Rhino is still tough at the end of the round, which is the half of this
    # that says the status was not taken and re-given: a re-give would look the
    # same on that assertion alone, but it would not have surged.
    Given the hero is "iron_man"
    And I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And "Rhino" is tough
    And the encounter deck is "Hydra Mercenary", "01105", "Shocker"

    When I pass
    Then "Rhino" is tough
    And "Shocker" is in play
    And it is round 2

  @card:01105
  Scenario: the tough status the card gives is a real one and absorbs an attack
    # What "a tough status card" is worth, on the cheapest board that shows it.
    # Spider-Man is printed ATK 2 and the villain takes none of it; the status
    # is spent doing that, so Rhino is no longer tough afterwards.
    #
    # Without this the two scenarios above are equally consistent with an engine
    # that set a flag nothing reads.
    Given the hero is "spider_man"
    And I am in hero form
    And "\"I'm Tough\"" is revealed

    Then "Rhino" is tough

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Rhino" has 14 health
    And "Rhino" is not tough
    And I am not prompted again
