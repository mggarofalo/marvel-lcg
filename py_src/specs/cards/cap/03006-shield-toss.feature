# Printed: "Hero Action (attack): Return Captain America's Shield to your hand
# → deal 4 damage to an enemy for each card discarded this way."
#
# The discard is a ranged cost with a legal floor of zero. Before MARVEL-138,
# the bot's ordinary minimum-target rule chose that floor every time, so Shield
# Toss returned the shield but discarded no cards and damaged no enemies. The
# payment prompt now says that its size is the effect; the shared bot planner
# selects the ceiling while ordinary effect targeting remains unchanged.

Feature: Shield Toss

  Background:
    Given the scenario is "rhino"
    And the hero is "captain_america"
    And I am in hero form
    And "Captain America's Shield" is in play
    And "Hydra Mercenary" is in play

  @card:03006
  Scenario: the ranged discard cost is paid above its zero floor
    Given my hand is "Shield Toss", "Fearless Determination", "Fearless Determination"

    When I choose "Play" on "Shield Toss" targeting "Hydra Mercenary"
    Then I am prompted to choose one
      | Pay cost Discard |

    When I choose "Pay cost Discard" targeting "Fearless Determination #1", "Fearless Determination #2"
    Then "Hydra Mercenary" is in the "EncounterDiscardPile"
    And "Rhino" has 0 damage
    And I have 1 cards in hand
    And "Captain America's Shield" is in the "HandsArea"
    And "Shield Toss" is in the "DiscardPile"
    And I am not prompted again
