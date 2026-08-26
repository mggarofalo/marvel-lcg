# The Break-In!, side 1A. Printed:
#
#   "Contents: Rhino (I) and Rhino (II). (Rhino (II) and Rhino (III) instead for
#    expert mode.) Rhino and Standard encounter sets. One modular encounter set
#    (recommended: Bomb Scare).
#    Setup: Advance to stage 1B."
#
# Side A of a main scheme is a setup instruction, and all but one line of it is
# about what to put in the box before the game starts. The engine has no text
# for this id at all (`text_comparison: engine_missing`) and 01097a and 01097b
# name one card object, two faces -- `"01097a"` resolves to the same card in the
# main scheme area that `"01097b"` does.
#
# So there is exactly one clause a scenario can measure, and it is the last one:
# "Setup: Advance to stage 1B". The board a game opens on is stage 1B, not 1A --
# side B is the side in play, at its own printed 0 starting threat, before
# anybody has taken a turn.
#
# One scenario, and the tier plans for one. The contents clause is measured by
# the villain deck holding stages I and II, which 01095's file already exercises
# by defeating stage I and finding stage II behind it; asserting it a second
# time here would be the same claim about the same setup under a second tag.

Feature: The Break-In! (1A)

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  @card:01097a
  Scenario: the game opens on side B, in play, with no threat on it
    # `printed_stage` is 1 either way -- 1A and 1B are two sides of stage 1, so
    # the stage number is not what distinguishes them. What does is that the
    # card in the main scheme area is carrying side B's numbers: 0 starting
    # threat, and a target and an acceleration that only side B prints.
    Then "01097a" is in the "MainSchemesArea"
    And the main scheme has 0 threat
    And "The Break-In!" has 1 "printed_stage"
    And "The Break-In!" has 7 "target_threat"
    And it is round 1
    And the game is not over
