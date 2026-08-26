# Printed: "Special (attack): Deal 2 damage to an enemy (4 damage instead if
# this is the final step of this sequence).
# (Play the "Wakanda Forever!" event to use this ability.)"
#
# Two decision paths: the last step of the sequence, and a step that is not the
# last. 01043 Wakanda Forever! is the only thing that fires a Special, so both
# scenarios go through it.
#
# "an enemy" is a choice, and both scenarios put a second enemy on the board so
# that it is one. Shocker is used rather than Hydra Mercenary because Hydra
# Mercenary has Guard -- with a Guard minion engaged the villain is not a legal
# target for an attack-labelled ability, the choice collapses to one, and the
# engine stops asking. That is correct behaviour and it would have made these
# scenarios silently weaker.
#
# Shocker is put into play rather than revealed, so his "When Revealed: deal 1
# damage to each hero" does not fire; the hero taking 0 damage says so.

Feature: Panther Claws

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01047
  Scenario: as the final step it deals 4 to the chosen enemy
    # The damage goes where the transcript pointed it. Rhino at 0 is the
    # assertion that would fail against an engine that hit the first enemy it
    # found, and the target list is the printed noun stated directly.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Shocker" is in play

    When I play "01043a"
    Then the legal targets for "Special" are
      | Rhino   |
      | Shocker |
    And I cannot choose "Special" targeting "me"

    When I choose "Special" targeting "Shocker"
    Then "Shocker" has 4 damage
    And "Rhino" has 0 damage
    And I have 0 damage
    And I am not prompted again

  @card:01047
  Scenario: as a step that is not the final one it deals 2
    # Tactical Genius resolves after it, so Panther Claws is no longer last: 2
    # instead of 4, and the 2 threat removed from the scheme is the other end of
    # the same sequence.
    Given my hand is "01043a", "Vibranium"
    And "Panther Claws" is in play
    And "Tactical Genius" is in play
    And "Shocker" is in play
    And the main scheme has 5 threat

    When I choose "Play" on "01043a" targeting "Panther Claws", "Tactical Genius"
    When I choose "Special" targeting "Shocker"
    Then "Shocker" has 2 damage
    And "Rhino" has 0 damage
    And the main scheme has 3 threat
    And I am not prompted again
