# Printed: "Play only if your identity has the [[Aerial]] trait.
# Hero Action (thwart): Choose X schemes. Remove 2 threat from each chosen
# scheme (3 threat instead if you overpaid for this card)."
#
# Printed cost: X -- spelled "-1" in the dataset, which is how marvelsdb writes
# an X cost. Resource icon: [[energy]]. Traits: AERIAL, THWART.
#
# ---------------------------------------------------------------------------
# The third X-cost card, and the one with a printed conditional that nothing
# could reach. "3 threat instead if you overpaid" needs a payment larger than
# the number of schemes chosen, and before MARVEL-135 the payment was always
# zero, so the branch was dead code on a shipped card.
#
# It also carried a defect of its own, which only became visible once it could
# be paid for: the script read X as `GetPaidResources().GetResourceIconTypes()`
# -- how many *kinds* of resource were spent, capped at four. That is Jubilee's
# mechanic, not this card's. Two [[mental]] resources bought one scheme rather
# than two, and "overpaid" meant "spent two colours". MARVEL-137.
#
# ## Falcon, and the pass
#
# Falcon (53001a) is the AERIAL identity used here. His Eagle-Eyed response
# fires after any card is played, so every scenario answers it with `I pass` --
# the beat is noise, and declining it is what keeps these scenarios about this
# card. Angel is the other core AERIAL hero and has the same shape of response;
# Spectrum's setup empties the hand before a transcript can fill it.
#
# ## What is paid, and what is chosen
#
# The transcript states the exact payment in resource icons. The runner still
# chooses the concrete filler cards in engine order, but it may not spend more
# or less than the named amount. X is 2 in every scenario below except the
# explicit-zero case. What changes is how many schemes are named, and that is
# what decides overpaying.
#
# Enhanced Spider-Sense (01004) is the filler -- one [[mental]], never playable
# here. The Break-In! starts at 0 threat and Breakin' & Takin' at 3, so both are
# set explicitly: a scheme that ends at 0 cannot say whether 2 or 3 came off it,
# and a defeated side scheme leaves the board entirely.

Feature: Everywhere All at Once

  Background:
    Given the scenario is "rhino"
    And the hero is "falcon"
    And I am in hero form
    And the main scheme has 5 threat
    And "Breakin' & Takin'" is in play
    And "Breakin' & Takin'" has 5 threat

  @card:58018
  Scenario: X schemes chosen for X paid removes 2 threat from each
    # Two paid, two named: not overpaid, so the printed 2 applies -- and it
    # applies to both, which is the "each chosen scheme" half.
    Given my hand is "Everywhere All at Once", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Play" on "Everywhere All at Once" paying 2 resources targeting "the main scheme", "Breakin' & Takin'"
    When I pass
    Then the main scheme has 3 threat
    And "Breakin' & Takin'" has 3 threat
    And I am not prompted again

  @card:58018
  Scenario: paying more than the schemes chosen removes 3 instead
    # The branch that was dead code. Same payment, one scheme named instead of
    # two, and the difference is one threat.
    Given my hand is "Everywhere All at Once", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Play" on "Everywhere All at Once" paying 2 resources targeting "the main scheme"
    When I pass
    Then the main scheme has 2 threat
    And "Breakin' & Takin'" has 5 threat
    And I am not prompted again

  @card:58018
  Scenario: two resources of one type are still two resources
    # MARVEL-137 in one scenario. Both fillers print [[mental]], so a reading of
    # X as "how many kinds of resource" makes this board X = 1: one scheme
    # chosen, not overpaid, 2 threat off the main scheme and none off the side
    # one. The reading that matches the printed card is the one above.
    #
    # The scenario is worth keeping rather than folding into the first because
    # the first passes under either reading whenever the fillers happen to
    # differ in colour, and an author picking fillers is not thinking about it.
    Given my hand is "Everywhere All at Once", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Play" on "Everywhere All at Once" paying 2 resources targeting "Breakin' & Takin'", "the main scheme"
    When I pass
    Then "Breakin' & Takin'" has 3 threat
    And the main scheme has 3 threat
    And I am not prompted again

  @card:58018
  Scenario: with nothing to spend no scheme is chosen and no threat moves
    # X = 0. The card is still playable -- a cost of X is affordable with an
    # empty hand -- and it is still discarded. A scheme has to be named because
    # the selector's floor is 1, and then `effect.targets[0:0]` drops it.
    Given my hand is "Everywhere All at Once", "Backflip"

    When I choose "Play" on "Everywhere All at Once" paying 0 resources targeting "the main scheme"
    When I pass
    Then the main scheme has 5 threat
    And "Breakin' & Takin'" has 5 threat
    And I have 1 cards in hand
    And "Everywhere All at Once" is in the "DiscardPile"
    And I am not prompted again

  @card:58018
  Scenario: an explicit overpayment leaves excess resources unspent
    # The two Enhanced Spider-Sense cards appear first and satisfy the named
    # amount. Backflip proves the runner does not fall back to spending every
    # resource offered by the hand.
    Given my hand is "Everywhere All at Once", "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Backflip"

    When I choose "Play" on "Everywhere All at Once" paying 2 resources targeting "the main scheme"
    When I pass
    Then the main scheme has 2 threat
    And "Breakin' & Takin'" has 5 threat
    And I have 1 cards in hand
    And I am not prompted again
