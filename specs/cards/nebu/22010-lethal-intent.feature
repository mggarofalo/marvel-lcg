# Printed: "Hero Action: Choose up to X [[technique]] upgrades you control.
# Resolve each of their "Special" abilities (in the order of your choice)."
#
# Printed cost: X. Resource icon: [[mental]].
#
# ---------------------------------------------------------------------------
# The second of the three X-cost cards, and the one that shows X is not only a
# number of damage. Here it is a number of *abilities*, and the card resolved
# none of them until MARVEL-135: the planner paid nothing for a variable cost,
# `Effect.GetCostX` reported 0, and `effect.targets[:0]` is empty.
#
# ## Why the upgrades are played rather than placed
#
# `Given "<card>" is in play` would be the obvious setup and it cannot be used.
# Nebula's own Combat Protocols (22001a) is a **Forced** Response on turn begin
# that resolves the Special of every [[technique]] upgrade she controls and then
# discards them -- so an upgrade on the board when the turn starts is gone
# before the transcript's first `When`, and its Special has already fired.
#
# So the board is built the way a player would build it: an empty hand of
# upgrades, played this turn, after Combat Protocols has found nothing to do.
# The absence of a Combat Protocols beat below is itself the claim that it found
# nothing -- the harness fails a scenario that leaves a prompt unanswered.
#
# ## What X is, without a step that states it
#
# The runner spends everything the option offers. So X is whatever is left in
# hand when Lethal Intent is played, and the two arms below differ by exactly
# one filler card. Enhanced Spider-Sense (01004) is the filler: one [[mental]],
# and never playable here.
#
# The two upgrades are chosen because their Specials are visible in different
# places and neither needs a decision of its own -- Unyielding Persistence
# (22006) makes Nebula tough, Cutthroat Ambition (22004) removes 3 threat from
# a scheme, and with one scheme on the board there is nothing to ask about.
#
# The Break-In! starts at 0 threat, so the background puts 5 on it: "removed 3"
# and "removed nothing" both read as 0 against an empty scheme, and the whole
# point of these scenarios is telling those two apart.

Feature: Lethal Intent

  Background:
    Given the scenario is "rhino"
    And the hero is "nebula"
    And I am in hero form
    And the main scheme has 5 threat

  @card:22010
  Scenario: two resources spent resolves both chosen upgrades
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Unyielding Persistence", "Cutthroat Ambition", "Lethal Intent", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Play" on "Unyielding Persistence"
    When I choose "Play" on "Cutthroat Ambition"
    When I choose "Play" on "Lethal Intent" targeting "Unyielding Persistence", "Cutthroat Ambition"
    Then "me" is tough
    And the main scheme has 2 threat
    And "Unyielding Persistence" is in play
    And "Cutthroat Ambition" is in play
    And I have 0 cards in hand
    And I am not prompted again

  @card:22010
  Scenario: one resource spent resolves only the first upgrade named
    # One filler fewer, so X is 1, and the second named upgrade is dropped by
    # `effect.targets[:cost]` after being selected. Both stay in play -- Lethal
    # Intent resolves a Special, it does not spend the upgrade.
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Unyielding Persistence", "Cutthroat Ambition", "Lethal Intent", "Enhanced Spider-Sense"

    When I choose "Play" on "Unyielding Persistence"
    When I choose "Play" on "Cutthroat Ambition"
    When I choose "Play" on "Lethal Intent" targeting "Unyielding Persistence", "Cutthroat Ambition"
    Then "me" is tough
    And the main scheme has 5 threat
    And "Cutthroat Ambition" is in play
    And I have 0 cards in hand
    And I am not prompted again

  @card:22010
  Scenario: the same one resource resolves whichever upgrade is named first
    # The scenario above with the two names swapped and nothing else changed,
    # so "the first one" is pinned as the transcript's order rather than as
    # play order or object order. The threat moves and the tough does not.
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Unyielding Persistence", "Cutthroat Ambition", "Lethal Intent", "Enhanced Spider-Sense"

    When I choose "Play" on "Unyielding Persistence"
    When I choose "Play" on "Cutthroat Ambition"
    When I choose "Play" on "Lethal Intent" targeting "Cutthroat Ambition", "Unyielding Persistence"
    Then "me" is not tough
    And the main scheme has 2 threat
    And I have 0 cards in hand
    And I am not prompted again

  @card:22010
  Scenario: with nothing left to spend the card resolves no Special at all
    # Exactly enough in hand to play the two upgrades and the card itself, so
    # X is 0. This is what every game did with Lethal Intent before MARVEL-135:
    # the card was played, it was discarded, and nothing happened.
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Unyielding Persistence", "Cutthroat Ambition", "Lethal Intent"

    When I choose "Play" on "Unyielding Persistence"
    When I choose "Play" on "Cutthroat Ambition"
    When I choose "Play" on "Lethal Intent" targeting "Unyielding Persistence", "Cutthroat Ambition"
    Then "me" is not tough
    And the main scheme has 5 threat
    And "Lethal Intent" is in the "DiscardPile"
    And I have 0 cards in hand
    And I am not prompted again
