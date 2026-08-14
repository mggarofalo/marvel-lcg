# Shocker. Printed:
#
#   "When Revealed: Deal 1 damage to each hero."
#
# Printed 3 hit points, ATK 2, SCH 1, boost 2.
#
# ---------------------------------------------------------------------------
# Two scenarios, because "each hero" has exactly two answers on a board a
# scenario can build.
#
# The engine reads it as `Worlds.GetOnFieldHeroes`, and the printed word is
# "hero", not "player" and not "identity" -- so an alter-ego takes nothing. That
# is the second scenario, and it is the discriminating one: an engine that dealt
# the damage to every identity regardless of form passes the first and fails it.
#
# It is not three. A second player would show the "each" of "each hero" looping,
# but `I am in hero form` speaks for seat 1 only and no step puts another player
# into hero form, so a two-player board measures the same one hero and reads
# identically. Under Attack's file records the same limitation from the other
# side.
#
# The minion's own statistics are worth pinning next to the reveal because the
# reveal puts it into play: 3 hit points is what the card is *worth* attacking,
# and it is what tells "deal 1 to each hero" apart from "deal 1 to each
# character".

Feature: Shocker

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01103
  Scenario: revealing it deals 1 damage to the hero and puts the minion into play
    Given I am in hero form
    And "Shocker" is revealed

    Then I have 1 damage
    And "Shocker" is in play
    And "Shocker" is in the "EngagedEnemiesArea"
    And "Shocker" has 3 health
    And "Shocker" has 0 damage
    And I am not prompted again

  @card:01103
  Scenario: an alter-ego is not a hero and takes nothing
    # The control on the printed word. Peter Parker is the same identity in the
    # other form and is untouched, while the minion still arrives -- so the
    # reveal certainly happened and it was the damage clause that found no
    # target.
    Given I am in alter-ego form
    And "Shocker" is revealed

    Then I have 0 damage
    And I am not in hero form
    And "Shocker" is in play
    And "Shocker" has 3 health
    And I am not prompted again
