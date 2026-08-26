# Printed: "Klaw gets +10 hit points. (When this scheme is defeated, Klaw loses
# those hit points.)"
# Printed statistics: 3 starting threat per hero, acceleration icon.
#
# The parenthesis is the second half of the ability and the reason this is two
# scenarios rather than one: a grant that is never taken back is
# indistinguishable from a grant that is, on any board where the scheme stays
# in play.
#
# What "loses those hit points" does to a villain who has already been hit is
# the thing worth measuring, and it is not obvious from the printed text: the
# damage stays where it is and the maximum comes down around it. Klaw at 5
# damage of 22 is at 7 of 12 the moment the scheme goes, which is one point
# short of the defeat this engine would otherwise have to resolve.

Feature: The "Immortal" Klaw

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01127
  Scenario: while it is in play Klaw stands on 10 more hit points
    # Klaw stage I prints 12 per hero, so 22 at one hero is the printed 12 plus
    # this scheme's 10 and nothing else.
    Given I am in hero form
    And "01127" is in play

    Then "01127" is in the "SideSchemesArea"
    And "01127" has 3 threat
    And "01127" has 1 "acceleration_icon"
    And "Klaw" has 22 "max_health"
    And "Klaw" has 22 health

  @card:01127
  Scenario: defeating the scheme takes the hit points back and leaves the damage
    # Captain Marvel's printed THW 2 clears the last 2 threat, the scheme is
    # defeated and discarded, and Klaw drops to his own 12. The 5 damage he was
    # already carrying does not move: 17 of 22 becomes 7 of 12.
    #
    # That the damage survives is what separates "loses 10 hit points" from
    # "is reset", and a scenario on an undamaged villain could not tell them
    # apart.
    Given I am in hero form
    And "01127" is in play
    And "01127" has 2 threat
    And "Klaw" has 5 damage

    Then "Klaw" has 22 "max_health"
    And "Klaw" has 17 health
    And "Klaw" has 5 damage

    When I thwart "01127"
    Then "01127" is not in play
    And "01127" is in the "EncounterDiscardPile"
    And "Klaw" has 12 "max_health"
    And "Klaw" has 7 health
    And "Klaw" has 5 damage

  @card:01127
  Scenario: the starting threat is per hero and the grant is not
    # `3*` against a flat +10. Two heroes double the threat to 6 and leave the
    # hit points alone -- Klaw stage I is 12 per hero, so 24 plus the same 10.
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form
    And "01127" is in play

    Then "01127" has 6 threat
    And "Klaw" has 34 "max_health"
