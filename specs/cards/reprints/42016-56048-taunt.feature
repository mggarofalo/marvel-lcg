# Taunt, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Hero Action: The villain attacks you.
# Other characters cannot defend against this attack. Draw 3 cards."
#
# 42016 (Angel) and 56048 (Civil War) are one printed card with two script
# files, and they disagreed on both clauses.
#
# **Which villain.** 42016 declared `SetTarget(Villain)` and attacked whatever
# the player picked; 56048 calls `Worlds.FindVillain(effect)`. In a one-villain
# game these are indistinguishable -- a lone legal target is auto-selected and
# no prompt appears -- but "the villain" is a *defined term* in this engine, not
# a card type. `Scenario.GetVillain` sends `Message.GettingVillain`, and the
# Sinister Six scenario (27100a) answers it with the villain holding the active
# counter, which is how its printed rules define the phrase. Targeting by card
# type bypasses that hook and lets a player Taunt a villain the scenario says is
# not "the villain"; in the Wrecking Crew it also turns one printed card into a
# four-way prompt. 01078 Get Behind Me!, 15030 Bait and Switch and 19003 "Fight
# Me, Coward!" all print "the villain attacks you" and all use the lookup.
#
# **Cannot defend.** 42016 passed `AttackProperty(other_characters_cannot_defend
# =True)`; 56048 passes the `other_characters_cannot_defend=True` keyword to
# `DoAttackYou`. Both spellings exist and they are not the same restriction. The
# property field is read in exactly one place -- the condition on the basic
# defense ability -- so it stops a non-attacked character being *declared* as
# defender and nothing else. The keyword registers
# `AbilityFactory.UnitCannotDefend(..., cannot_trigger_defense_ability=True)`,
# which is the engine's whole model of "cannot defend": as
# unit_test/test_cannot_defend.py puts it, "'Cannot defend' is two
# restrictions, not one" -- being declared as defender, and reaching a
# defense-labeled ability. Nine other cards use that factory; nothing else in
# the engine reads the property field.
#
# 42016 now runs the same body as 56048.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# Rhino prints ATK 2 and the boost card is a Hydra Mercenary carrying one boost
# icon, so an undefended Taunt lands 3 on the hero. Spider-Sense is declined
# explicitly; the harness never answers a decision the transcript omits. The
# hand arithmetic is 3 cards, minus Taunt, minus the one that paid for it, plus
# the three it draws.

Feature: Taunt

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"

  @card:42016
  Scenario: 42016 makes the villain attack, only the hero may defend, and draws 3
    Given my hand is "42016", "Pepper Potts", "Pepper Potts"
    And "Black Cat" is in play

    When I play "42016"
    When I pass
    # One row, not two. Black Cat is a ready ally the hero controls and would
    # otherwise be offered as a defender -- see the control below.
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 3 damage
    And "Black Cat" is not exhausted
    And I have 4 cards in hand

  @card:56048
  Scenario: 56048 makes the villain attack, only the hero may defend, and draws 3
    Given my hand is "56048", "Pepper Potts", "Pepper Potts"
    And "Black Cat" is in play

    When I play "56048"
    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 3 damage
    And "Black Cat" is not exhausted
    And I have 4 cards in hand

  @card:42016
  @card:56048
  Scenario: an ordinary card-driven villain attack still lets the ally defend
    # The control, and the whole reason the single-row table above means
    # anything. "Fight Me, Coward!" (19003) is the same shape -- a Hero Action
    # event whose effect is `DoAttackYou` -- with no cannot-defend clause, and
    # its defense window offers two rows: the hero and Black Cat.
    Given my hand is "19003", "Pepper Potts", "Pepper Potts"
    And "Black Cat" is in play

    When I play "19003" targeting "me"
    When I pass
    Then I am prompted to choose one
      | Defense |
      | Defense |
