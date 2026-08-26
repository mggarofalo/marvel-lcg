# Rhino, stage I. Printed with no ability text at all: a stat block and two
# traits. Stage I is 14 hit points per hero, ATK 2, SCH 1.
#
# The star on the printed hit points is the whole card. "14" on its own is a
# constant an engine can hardcode; "14 per hero" is arithmetic, and it is the
# one thing about this card that can be got wrong. So the two scenarios are the
# same board at one hero and at two, and the second is what makes the first
# worth writing down.
#
# ATK 2 and SCH 1 are deliberately NOT re-asserted here. They are already pinned
# by specs/rules/phase-structure.feature, which walks a villain phase against
# this exact villain and measures both -- 3 damage in hero form and 3 threat in
# alter-ego form, each being the printed value plus a boost card plus, for the
# scheme, the main scheme's acceleration. Writing those transcripts a second
# time under a @card tag would raise the coverage number without adding a claim.
#
# `health` is remaining hit points and `max_health` is the printed total, which
# is why the damaged board below asserts both: an engine that scaled the
# printed total but forgot to scale the pool, or vice versa, differs from this
# in exactly one of the two numbers.

Feature: Rhino (I)

  Background:
    Given the scenario is "rhino"
    And I am in hero form

  @card:01094
  Scenario: at one hero the villain has its printed 14 hit points
    # Spider-Man is printed ATK 2, so the attack leaves 2 damage on a 14-point
    # pool and 12 remaining. The pair says the damage went on the villain
    # rather than the villain's printed total coming down.
    Given the hero is "spider_man"

    Then "Rhino" has 14 health
    And "Rhino" has 14 "max_health"
    And "Rhino" has 0 damage

    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And "Rhino" has 12 health
    And "Rhino" has 14 "max_health"
    And I am not prompted again

  @card:01094
  Scenario: at two heroes it has 28, which is the printed star and not a constant
    # The discriminating board. 14 is per hero, so a second player doubles it.
    # An engine that read the printed number literally reads 14 here.
    Given the heroes are "spider_man", "captain_marvel"

    Then "Rhino" has 28 health
    And "Rhino" has 28 "max_health"
    And "Rhino" has 0 damage
