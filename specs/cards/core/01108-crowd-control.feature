# Crowd Control. Printed:
#
#   "(Crisis Icon: While this scheme is in play, you cannot remove threat from
#    the main scheme.)"
#
# Printed 2 starting threat, boost 2, one crisis icon. No card script -- the
# starting threat and the icon are both implemented generically, which is what
# puts this card in the `stats_only` tier.
#
# ---------------------------------------------------------------------------
# Two things to say, and the first is the one only this card can say.
#
# The starting threat carries the per-hero star, so it is 2 at one hero and 4 at
# two. That is the card's own stat block and nothing else in the suite pins it.
#
# The icon is a rule rather than a stat, and specs/rules/damage-and-threat.feature
# already specifies it in general -- including a scenario on this very card,
# chosen there precisely because it has no script and so measures the icon and
# nothing else. What is added here is the pair that scenario does not make: the
# icon locks the *main* scheme only, so the same hero on the same board removes
# threat from Crowd Control itself. Without that, "you cannot remove threat"
# reads as "this card stops thwarting", which is not what it says.
#
# Spider-Man is printed THW 1, so every number below moves by exactly 1 or by
# nothing at all.

Feature: Crowd Control

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:01108
  Scenario: at one hero it enters play with its printed 2 threat
    Given "Crowd Control" is in play

    Then "Crowd Control" is in the "SideSchemesArea"
    And "Crowd Control" has 2 threat

  @card:01108
  Scenario: at two heroes the starred starting threat doubles
    Given the heroes are "spider_man", "captain_marvel"
    And "Crowd Control" is in play

    Then "Crowd Control" has 4 threat

  @card:01108
  Scenario: its crisis icon stops threat coming off the main scheme
    # The exhaust is asserted because the icon does not remove the option or
    # filter the target -- the engine lets the thwart happen and removes
    # nothing, so a player can spend a whole turn on it.
    Given "Crowd Control" is in play
    And the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 5 threat
    And I am exhausted

  @card:01108
  Scenario: the scheme carrying the icon can still be thwarted
    # The icon names the main scheme and only the main scheme. This is the same
    # hero, the same power and the same board as the scenario above, and here
    # the threat comes off.
    Given "Crowd Control" is in play

    When I thwart "Crowd Control"
    Then "Crowd Control" has 1 threat
    And I am exhausted

  @card:01108
  Scenario: with no crisis icon in play the same thwart lands
    # The control for the restriction. `I cannot remove threat` has to be
    # capable of failing, and on the same board without Crowd Control the main
    # scheme drops by Spider-Man's printed 1.
    Given the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat
    And I am exhausted
