# Printed: "When Revealed: Each player chooses to either place 2 threat here or
# deal 3 damage to their hero."
# Printed starting threat 3, boost 3, one crisis icon.
#
# Two clauses are worth separating and the card text runs them together. The
# choice is one; "each player" is the other, and a solo scenario cannot see it
# at all -- a one-hero board looks identical whether the engine loops or resolves
# once. So the first two scenarios take the two branches on a solo board and the
# last two put a second player behind them.
#
# The side scheme's own starting threat is 3 and it is printed fixed, so it is 3
# at one player and 3 at two. Every threat number below is that 3 plus what the
# card placed, which is what makes the totals discriminating rather than just
# large.
#
# The second player is in alter-ego form and there is no step that would put
# them in hero form -- `I am in hero form` speaks for the first player only. That
# turns out to be the interesting board rather than a limitation: an alter-ego
# controls no hero, so "deal 3 damage to their hero" has no legal target for
# them, the engine is left holding one option with one target and resolves it
# without asking. The threat total is how that is observed, since the harness
# never sees a prompt for it.

Feature: Under Attack

  Background:
    Given the scenario is "rhino"

  @card:01151
  Scenario: the threat branch places 2 on the side scheme and leaves the hero untouched
    Given the hero is "spider_man"
    And I am in hero form
    And "Under Attack" is revealed

    Then I am prompted to choose one
      | Place 2 threat here         |
      | Deal 3 damage to their hero |

    When I choose "Place 2 threat here"
    Then "Under Attack" has 5 threat
    # The control on the other branch. Without it an engine that placed the
    # threat *and* dealt the damage would satisfy the line above.
    And I have 0 damage
    And I am not prompted again

  @card:01151
  Scenario: the damage branch hits the hero and adds no threat
    Given the hero is "spider_man"
    And I am in hero form
    And "Under Attack" is revealed

    Then I am prompted to choose one
      | Place 2 threat here         |
      | Deal 3 damage to their hero |

    When I choose "Deal 3 damage to their hero"
    Then I have 3 damage
    # Still the printed starting 3: this branch places nothing.
    And "Under Attack" has 3 threat
    And I am not prompted again

  @card:01151
  Scenario: the card resolves for the second player as well as for me
    # 7 threat is 3 printed + 2 that I placed + 2 the second player placed. A
    # scenario that only looked at my own choice would read 5 here and pass
    # against an engine that had forgotten the "each player" clause entirely.
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And "Under Attack" is revealed

    Then I am prompted to choose one
      | Place 2 threat here         |
      | Deal 3 damage to their hero |

    When I choose "Place 2 threat here"
    Then "Under Attack" has 7 threat
    And I have 0 damage
    And "Captain Marvel" has 0 damage
    And I am not prompted again

  @card:01151
  Scenario: the second player's resolution is their own and not a copy of mine
    # I take the damage branch and the total still moves by exactly the 2 the
    # second player placed, so the two resolutions are independent choices over
    # the same pair of options rather than one choice applied twice.
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And "Under Attack" is revealed

    When I choose "Deal 3 damage to their hero"
    Then I have 3 damage
    And "Under Attack" has 5 threat
    # The damage was dealt to *my* hero. The second player is an alter-ego and
    # could not have been a legal target for it in either resolution.
    And "Captain Marvel" has 0 damage
    And I am not prompted again
