# Preemptive Strike, printed once and scripted twice.
#
# Printed (both ids, byte-identical): "Hero Interrupt (defense): When a boost
# card is turned face up while the villain attacks, cancel all boost icons
# ([boost]) on that card. Then deal 1 damage to the villain for each boost icon
# cancelled this way."
#
# 05014 (Ms. Marvel) and 38015 (Rogue) are one printed card with two script
# files -- `cards/pack/msm/05014.py` and `cards/pack/rogue/38015.py`. Ten of the
# 318 reprints are like this and six of the ten disagreed in behaviour; see
# docs/spec-campaign.md, "A reprint is not a second card of work". Every
# scenario below is written twice, once per id, with the same board and the same
# assertions, so a future edit to one file and not the other fails a test
# instead of sitting unread.
#
# ---------------------------------------------------------------------------
# Why the damage is 1 here.
#
# Hydra Mercenary carries a single boost icon. Rhino prints ATK 2, so an
# unanswered activation deals 3 and a cancelled one deals 2 -- which is what the
# control scenario at the bottom pins. The villain takes 1, one per icon
# cancelled.
#
# The transcript walks a real villain phase because a boost card is never
# revealed and no `Given` can put one face up. Spider-Sense and the defense
# declaration are declined explicitly: the harness never answers a decision the
# transcript omits.

Feature: Preemptive Strike

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

  @card:05014
  Scenario: 05014 cancels the boost icon and deals 1 damage to the villain
    Given my hand is "05014", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "05014"
    # 1 damage to the villain, one per cancelled icon, and the hero takes
    # Rhino's printed 2 rather than 3 -- the icon is gone from the attack as
    # well as paid out as damage. Either assertion alone would pass on an engine
    # that did only half the card.
    Then "Rhino" has 1 damage
    And I have 2 damage
    And I am not prompted again

  @card:38015
  Scenario: 38015 cancels the boost icon and deals 1 damage to the villain
    Given my hand is "38015", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    Then I am prompted to choose one
      | Play |

    When I choose "Play" on "38015"
    Then "Rhino" has 1 damage
    And I have 2 damage
    And I am not prompted again

  @card:05014
  @card:38015
  Scenario: declining the interrupt leaves the boost icon on the attack
    # The control. Without it "Rhino has 1 damage / I have 2 damage" above is
    # consistent with an engine that never boosted the attack in the first
    # place. Here the interrupt is offered and declined, the icon stands, and
    # the hero takes 3.
    Given my hand is "05014", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    When I pass
    Then "Rhino" has 0 damage
    And I have 3 damage
    And I am not prompted again
