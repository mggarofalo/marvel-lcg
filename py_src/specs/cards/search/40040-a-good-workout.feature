# A Good Workout.
#
# Printed: "Hero Action (attack): Deal 4 damage to an enemy and discard the top
# card of your deck. For each resource icon discarded this way, deal 1
# additional damage to an enemy."
#
# This is here for the helper, not for the card. `PlayerAction.DiscardDeckTopCard`
# indexed `DiscardDeckTopCards(1, ...)[0]` unguarded, so on a board with nothing
# to discard it raised `IndexError` mid-resolution and the engine's broad
# handlers swallowed it (MARVEL-119). Twenty card scripts call it; 01050 Hulk is
# the one the bug was found on, and its scenario in specs/cards/core/ can only
# catch a regression through the ERROR verdict `Log.HasError` produces, because
# every branch of Hulk's response is conditional on the discarded card and the
# abort lands on the same board as the correct resolution.
#
# This card is the counter-example, and it is why the guard is worth a second
# scenario outside the core shard: the 4 damage is *not* conditional on the
# discard. Before the fix the event was paid for, the discard raised, and
# `DealDamage` was never reached -- Rhino took 0 and two resources were gone.
# That is a board assertion, so this scenario fails without depending on the
# log-demotion machinery, which MARVEL-65 showed can itself silently stop
# working.
#
# ---------------------------------------------------------------------------
# Board notes.
#
# A puzzle scene starts with no player deck and no discard pile, which is
# exactly the state under test: both empty, so there is nothing to reshuffle and
# nothing to discard. No `my deck is` here, deliberately.
#
# The event costs 2, so the hand carries two more cards to pay with -- which is
# why the discard pile holds three cards afterwards and none before. Rhino is
# the only enemy on the board, so the engine picks the target for the additional
# damage itself and asks nothing.
#
# The engine deals `4 + icons` as one instruction rather than the printed card's
# "4, then 1 additional per icon" -- with an empty deck the icon count is 0 and
# the two readings coincide, which is the other reason this board is the right
# one to pin the helper on.

Feature: A Good Workout

  Background:
    Given the scenario is "rhino"
    And the hero is "domino"
    And I am in hero form
    And my hand is "A Good Workout", "Backflip", "Backflip"

  @card:40040
  Scenario: an empty deck discards nothing and the damage is still dealt
    When I play "A Good Workout" targeting "Rhino"
    Then "Rhino" has 4 damage
    And I have 0 cards in my deck
    And I have 3 cards in my discard pile
    And I am not prompted again
