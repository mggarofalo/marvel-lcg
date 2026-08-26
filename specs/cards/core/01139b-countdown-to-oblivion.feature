# Printed (Countdown to Oblivion, stage 3B): "Threat cannot be removed from this
# scheme.
# If this stage is completed, the players lose the game"
# Printed 0 starting threat, 5 [star] to complete, 1 [star] acceleration.
#
# A restriction with no observable side effect of its own, which is the shape of
# assertion this suite exists to make: the thwart is still offered, this scheme
# is still a legal target for it, the action is still spent -- and the threat
# does not move. Nothing about the option set or any card's state changes, so
# only the number before and after says anything.
#
# The control matters more than usual here. "Threat did not go down" is also true
# of an engine that had forgotten how to thwart at all, so the second scenario is
# the same hero on the same board removing threat from a side scheme.
#
# The stage is reached by revealing 3A, which resolves and advances to this face.
# That leaves the stage it advanced from in the main schemes area beside this
# one, because nothing completed it -- so every assertion names a card by id
# rather than saying "the main scheme".

Feature: Countdown to Oblivion 3B

  Background:
    Given the scenario is "ultron"
    And the hero is "iron_man"
    And I am in hero form
    And "Ultron Drones" is in play

  @card:01139b
  Scenario: thwarting this stage removes none of its threat
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "01139a" is revealed
    And "01139b" has 4 threat

    When I thwart "01139b"
    # Iron Man is printed THW 2, so an unrestricted thwart would leave 2 here.
    Then "01139b" has 4 threat
    # The action was spent doing it: this is a restriction on the threat moving,
    # not on the thwart being made.
    And I am exhausted
    And I am not prompted again

  @card:01139b
  Scenario: the same hero can still remove threat from a scheme that does not say this
    # The control. Without it the scenario above is equally satisfied by an
    # engine that had forgotten how to thwart, or by a board where the hero could
    # not act at all.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "01139a" is revealed
    And "01139b" has 4 threat
    And "Invasive AI" is in play
    And "Invasive AI" has 4 threat

    When I thwart "Invasive AI"
    # The same printed THW 2, on a scheme that carries no such line.
    Then "Invasive AI" has 2 threat
    And "01139b" has 4 threat
    And I am not prompted again

  @card:01139b
  Scenario: completing this stage loses the game
    # The second printed line. The threat is placed by setup rather than by a
    # walked villain phase -- reaching 5 threat on this stage in play would take
    # several rounds of acceleration and would be a scenario about the phase
    # structure -- but the completion check it triggers is the game's own.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts"
    And "01139a" is revealed
    And "01139b" has 5 threat

    Then the game is over
    And the players lost
