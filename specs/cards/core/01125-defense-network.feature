# Printed: "When Revealed: Place an additional 1 [per_hero] threat here."
# Printed statistics: 2 starting threat, boost 2, crisis icon.
#
# The card is two numbers that behave differently and one script line that
# joins them: a starting threat that is *fixed* and an additional threat that
# is *per hero*. A one-hero board reads both as 1 and cannot tell "2 + 1 per
# hero" from "3 per hero" or from a flat 3, so the second scenario is the one
# that makes the first one mean anything.
#
#   one hero    2 + 1  =  3      three readings agree
#   two heroes  2 + 2  =  4      "3 per hero" says 6, a flat 3 says 3
#
# `Given "<card>" is revealed` runs the reveal pipeline, so the When Revealed
# fires during setup and the board is already settled when the `Then`s run.
# There is no transcript because there is no decision: the card asks nothing.
#
# The crisis icon is asserted as a printed value only. What the icon *does* --
# threat cannot be removed from the main scheme while it is in play -- is
# rulebook behaviour and is specced on Under Attack in
# `specs/rules/damage-and-threat.feature`; restating it here would be one claim
# written twice against two cards.

Feature: Defense Network

  Background:
    Given the scenario is "klaw"

  @card:01125
  Scenario: it arrives on its printed threat plus one for the hero
    Given the hero is "captain_marvel"
    And I am in hero form
    And "Defense Network" is revealed

    Then "Defense Network" is in the "SideSchemesArea"
    And "Defense Network" has 3 threat
    And "Defense Network" has 1 "crisis"

  @card:01125
  Scenario: only the additional threat is per hero, not the printed 2
    # 4, not 6. The starting threat stays where it is printed and the single
    # additional threat is the only part that counts heroes -- which is the
    # distinction the printed card makes by putting the [per_hero] icon on one
    # of the two numbers and not the other.
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form
    And "Defense Network" is revealed

    Then "Defense Network" has 4 threat
    And "Defense Network" has 1 "crisis"
