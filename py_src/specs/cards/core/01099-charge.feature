# Charge. Printed:
#
#   "Attach to Rhino.
#    [star] Forced Interrupt: When Rhino attacks, the attack gains overkill.
#    (Excess damage to an ally from this attack is dealt to that ally's
#    controller.) At the end of this attack, discard Charge."
#
# Printed ATK +3 (starred, so it is the attachment's contribution to the
# villain's attack) and boost 2.
#
# ---------------------------------------------------------------------------
# Three claims in one printed card, and the first two are measured on one
# transcript because they are two halves of one villain activation: the attack
# is bigger, and the card is gone afterwards.
#
# The arithmetic is 2 + 3 + 1. Rhino stage I is printed ATK 2; Charge adds its
# printed 3; and the activation is boosted by the top card of the encounter
# deck, a Hydra Mercenary printed boost 1. Boost is why every number in
# specs/rules/phase-structure.feature is one higher than the villain card, and
# it applies here for the same reason.
#
# ---------------------------------------------------------------------------
# Overkill is only visible when an ally defends, and it needs its own control.
#
# The keyword says nothing about a hero taking the attack -- the excess it moves
# is excess *over an ally's remaining hit points*. So the second pair of
# scenarios puts Black Cat, printed 2 hit points, in front of a 6-point attack
# and asks where the other 4 went. The control is the same board with no Charge
# attached: the attack is 3 rather than 6, Black Cat still dies, and the hero
# takes nothing, so the 4 in the first scenario is overkill and not "excess
# damage always rolls over".
#
# The engine asks one extra question in the overkill case -- `Simultaneous
# Overkill` -- which is a beat in the transcript rather than something the
# printed text names. It is answered rather than skipped, because the harness
# never answers a decision the transcript omits.

Feature: Charge

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

  @card:01099
  Scenario: it attaches to the villain, adds 3 to the attack, and is discarded at the end of it
    # Iron Man is printed 9 hit points and declines to defend, so all 6 land:
    # Rhino's printed 2, Charge's printed 3, and 1 for the boost card.
    Given "Charge" is in play
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    Then "Charge" is in the "UpgradesArea"

    When I pass
    When I pass
    Then I have 6 damage
    And "Charge" is not in play
    And "Charge" is in the "EncounterDiscardPile"
    And it is round 2

  @card:01099
  Scenario: excess damage over a defending ally is dealt to the hero
    # Black Cat is printed 2 hit points. The attack is 6, so she takes 2 and is
    # defeated, and overkill sends the other 4 to Iron Man rather than letting
    # it evaporate on the ally.
    Given "Charge" is in play
    And "Black Cat" is in play
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "Defense" on "Black Cat"
    When I choose "Simultaneous Overkill"
    Then I have 4 damage
    And "Black Cat" is not in play
    And "Black Cat" is in the "DiscardPile"

  @card:01099
  Scenario: without Charge the same ally soaks the whole attack
    # The control, and it is doing two jobs. The attack is 3 rather than 6,
    # which is the printed ATK +3 measured a second way; and the 1 point Black
    # Cat cannot absorb goes nowhere, which is what makes the 4 above overkill
    # rather than ordinary spillover.
    Given "Black Cat" is in play
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "Defense" on "Black Cat"
    Then I have 0 damage
    And "Black Cat" is not in play
