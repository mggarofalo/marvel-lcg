# Printed: "Hero Action: Stun X Enemies."
#
# Printed cost: X. Resource icon: [[energy]]. Trait: SUPERPOWER.
#
# ---------------------------------------------------------------------------
# The first scenarios this card has ever had, because until MARVEL-135 it could
# not be made to do anything.
#
# X is not a number the engine holds. `Cost.FromText` reads "X" as zero -- it is
# not a digit string, so `ResRBYGA.FromText` falls through to a=0 -- and the
# card is played for a cost of 0. What X *is* is measured afterwards, by
# `Effect.GetCostX`: resources paid minus cost owed. Pay two, X is two.
#
# So a planner that pays the least it legally can pays nothing, and "Stun X
# Enemies" stuns none. That is what the bot and the spec harness both did, and
# the fix was to give the cost a name -- `Variable` in the option's rule list --
# and have the planner spend against it. `unit_test/test_bot.py` pins the
# planner; these scenarios pin what the card does with what it is given.
#
# **The transcript does not state the payment, and does not need to.** The
# runner spends everything the option offers, in engine order, so the hand *is*
# the payment: three cards in hand, one of them this card, means X is 2. That is
# why every scenario below sets the hand exactly. MARVEL-136 asks for a step
# that states an amount directly; these show what can be pinned without one.
#
# Always Be Running (14003) is the filler. It is in Quicksilver's own deck, it
# prints one [[energy]], and it is never playable here -- its own Hero Action
# needs a target this board does not offer -- so it is a resource and nothing
# else. Two of them is a payment of 2; one is a payment of 1.
#
# Hydra Mercenary is the second enemy. The villain alone cannot tell "stun the
# targets" from "stun everything", and a minion that is already engaged needs no
# setup step to reach.

Feature: Speed Cyclone

  Background:
    Given the scenario is "rhino"
    And the hero is "quicksilver"
    And I am in hero form
    And "Hydra Mercenary" is in play

  @card:14006
  Scenario: two resources spent stuns two enemies
    Given my hand is "Speed Cyclone", "Always Be Running", "Always Be Running"

    When I choose "Play" on "Speed Cyclone" targeting "Rhino", "Hydra Mercenary"
    Then "Rhino" is stunned
    And "Hydra Mercenary" is stunned
    And I have 0 cards in hand
    And "Speed Cyclone" is in the "DiscardPile"
    And I am not prompted again

  @card:14006
  Scenario: one resource spent stuns only the first enemy named
    # The floor and the ceiling of MARVEL-133 in one board. The card asks for
    # `range=(1, "All")` because a selector cannot be bounded by a cost that has
    # not been paid yet, so both enemies are legal targets and both are chosen
    # -- and then `effect.targets[:cost]` throws the second one away.
    #
    # Nothing about that is visible to the player, which is the defect: the
    # engine accepted a selection it had already decided not to use. Pinned
    # here so the day it is bounded properly, this scenario is what says the
    # behaviour changed.
    Given my hand is "Speed Cyclone", "Always Be Running"

    When I choose "Play" on "Speed Cyclone" targeting "Rhino", "Hydra Mercenary"
    Then "Rhino" is stunned
    And "Hydra Mercenary" is not stunned
    And I am not prompted again

  @card:14006
  Scenario: one resource spent stuns whichever enemy is named first
    # The same hand and the same board as above with the two targets swapped,
    # so the slice is pinned as "the order they were chosen in" rather than as
    # "the villain first" or "whatever the engine had at index 0".
    Given my hand is "Speed Cyclone", "Always Be Running"

    When I choose "Play" on "Speed Cyclone" targeting "Hydra Mercenary", "Rhino"
    Then "Hydra Mercenary" is stunned
    And "Rhino" is not stunned
    And I am not prompted again

  @card:14006
  Scenario: with nothing to spend X is zero and the card stuns nothing
    # X = 0 is a legal choice, and this is the board where it is the only one.
    # The card is still played and still discarded -- it is not withheld for
    # being unaffordable, because a cost of X is affordable with an empty hand.
    #
    # This is exactly what every game did before MARVEL-135, on every board.
    Given my hand is "Speed Cyclone"

    When I choose "Play" on "Speed Cyclone" targeting "Rhino"
    Then "Rhino" is not stunned
    And "Hydra Mercenary" is not stunned
    And "Speed Cyclone" is in the "DiscardPile"
    And I am not prompted again
