# Printed: "Hero Action (thwart): Remove 2 threat from a scheme. Then, if you
# have the [[Aerial]] trait, remove 2 threat from a different scheme."
#
# Two claims stacked on one card: an unconditional removal, and a second one
# gated on a trait the hero does not print. Captain Marvel is Avenger/Soldier;
# Cosmic Flight (01017) is what grants her Aerial, so the same transcript with
# and without that upgrade is the whole conditional.
#
# "A different scheme" is the other claim, and it is only observable when there
# is more than one scheme on the board -- which is why three of the four
# scenarios below put a side scheme in play and why one of them puts two.
#
# A note on the option label. The follow-up removal is offered as an option
# named "Play", bound to Crisis Interdiction while it sits in the processing
# area, rather than as anything resembling the printed sentence. Labels come
# from the Python card script (`ForChoiceAbility("")`), not from printed text,
# so the scenario asserts what the engine calls it. See spec-harness.md,
# "Option labels" -- the C# port has to expose the same string.

Feature: Crisis Interdiction

  Background:
    Given the scenario is "rhino"
    And the hero is "captain_marvel"

  @card:01012
  Scenario: without Aerial only the named scheme loses threat
    # The control for every scenario below it. Two schemes are on the board and
    # both carry threat, so the play has to name one; only that one moves.
    Given I am in hero form
    And my hand is "Crisis Interdiction", "Photonic Blast", "Energy Absorption"
    And the main scheme has 3 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 3 threat

    When I play "Crisis Interdiction" targeting "the main scheme"
    Then the main scheme has 1 threat
    And "Breakin' & Takin'" has 3 threat
    And I am not prompted again

  @card:01012
  Scenario: Aerial takes 2 more off the only other scheme, without asking
    # Cosmic Flight is the only difference from the scenario above, and it moves
    # the side scheme from 3 to 1. The second removal has exactly one legal
    # target, so the engine selects it itself and never asks -- naming it would
    # be noise, and "I am not prompted again" is the assertion that says so.
    Given I am in hero form
    And my hand is "Crisis Interdiction", "Photonic Blast", "Energy Absorption"
    And "Cosmic Flight" is in play
    And the main scheme has 3 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 3 threat

    When I play "Crisis Interdiction" targeting "the main scheme"
    Then the main scheme has 1 threat
    And "Breakin' & Takin'" has 1 threat
    And I am not prompted again

  @card:01012
  Scenario: Aerial asks which other scheme when two could take it
    # The mid-resolution choice. Threat leaves the scheme the transcript named
    # and the third scheme is untouched -- the assertion that would pass
    # regardless under a first-match resolver is the one about Breakin' & Takin'
    # still holding 3.
    Given I am in hero form
    And my hand is "Crisis Interdiction", "Photonic Blast", "Energy Absorption"
    And "Cosmic Flight" is in play
    And the main scheme has 3 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 3 threat
    And "Bomb Scare" is in play
    And "Bomb Scare" has 3 threat

    When I play "Crisis Interdiction" targeting "the main scheme"
    Then I am prompted to choose one
      | Play |

    When I choose "Play" targeting "Bomb Scare"
    Then the main scheme has 1 threat
    And "Bomb Scare" has 1 threat
    And "Breakin' & Takin'" has 3 threat
    And I am not prompted again

  @card:01012
  Scenario: Aerial cannot take the second 2 threat off the same scheme
    # "A different scheme", pinned. The main scheme is the only scheme in play
    # and it holds enough threat to absorb 4, so an engine that read the second
    # sentence as "remove 2 more threat" would leave it at 1. It leaves it at 3,
    # and asks nothing, because the only other candidate is excluded.
    Given I am in hero form
    And my hand is "Crisis Interdiction", "Photonic Blast", "Energy Absorption"
    And "Cosmic Flight" is in play
    And the main scheme has 5 threat

    When I play "Crisis Interdiction"
    Then the main scheme has 3 threat
    And I am not prompted again
