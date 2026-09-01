# Printed: "Action: Exhaust War Machine and deal 2 damage to him → deal 1
# damage to each enemy."
#
# ---------------------------------------------------------------------------
# Written for the original investigation, which was filed about Wakanda Forever! and turned out
# to be a population of two. Both cards print "each" and both spelled their
# selector `range=(1, "All")` -- "some or all" -- so a player could take the
# cost and then deal the damage to one enemy of their choosing. Self-play did
# exactly that on every board with more than one enemy, because
# `BotCommand.Build` submits `min_targets`.
#
# The card had no scenario at all before this, which is why the divergence
# survived a shipped core-set ally: nothing asserted the count, and the cost is
# paid either way so nothing looked wrong.
#
# The claim here is the *count*, so the transcripts name no targets and let the
# engine take what it must. Under `(1, "All")` the run deals its 1 damage to
# one enemy and the second assertion in each scenario fails.

Feature: War Machine

  Background:
    Given the scenario is "rhino"
    And the hero is "iron_man"
    And I am in hero form

  @card:01030
  Scenario: with the villain alone, each enemy is the villain
    # The degenerate board, and the control for the two below: one enemy means
    # "each enemy" and "an enemy" cannot be told apart, and this is what both
    # engines agree on. If this failed, nothing below would be about counting.
    Given "War Machine" is in play

    When I choose "Action" on "War Machine"
    Then "Rhino" has 1 damage
    And "War Machine" has 2 damage
    And "War Machine" is exhausted
    And I am not prompted again

  @card:01030
  Scenario: a minion in play is damaged too, without being named
    # Two enemies, no target list. The villain *and* the minion take 1 each.
    # Under a minimum of 1 only one of these two numbers is 1 and the other is
    # 0, whichever the engine happened to pick.
    Given "War Machine" is in play
    And "Shocker" is in play

    When I choose "Action" on "War Machine"
    Then "Rhino" has 1 damage
    And "Shocker" has 1 damage
    And I am not prompted again

  @card:01030
  Scenario: three enemies, three damaged, and the ally dealing it is not one
    # The third enemy is what makes this a claim about "each" rather than about
    # "two": an engine with a fixed maximum of 2 passes the scenario above and
    # fails this one.
    #
    # War Machine's own 2 is the cost and nothing more, which is "each *enemy*"
    # rather than "each character" -- he is a friendly character in play for the
    # whole resolution and the selector's filter has to exclude him.
    #
    # Hydra Bomber is the third rather than Sandman, and the reason is worth
    # the line: Sandman prints Toughness and enters play with a tough status
    # card, so his first point of damage is absorbed and he sits at 0 however
    # correct the count is. A scenario written against him fails while the
    # engine is right, which is the failure mode this whole file exists to
    # avoid. `specs/cards/core/01102-sandman.feature` is where that belongs.
    Given "War Machine" is in play
    And "Shocker" is in play
    And "Hydra Bomber" is in play

    When I choose "Action" on "War Machine"
    Then "Rhino" has 1 damage
    And "Shocker" has 1 damage
    And "Hydra Bomber" has 1 damage
    And "War Machine" has 2 damage
    And I am not prompted again
