# Printed: "Action: Spend a [energy] resource -> choose THW or ATK. Until the
# end of the phase, Vision gets +2 to the chosen power. (Limit once per round.)"
#
# The choice is between two numbers on the same card, which makes it the easiest
# kind of card to write a hollow spec for: after either branch Vision is still
# in play, still ready, still 3 hit points, and the only thing that moved is a
# stat. So every scenario here asserts *both* powers, not just the one it
# chose -- the branch that was not taken is the assertion that a spec claiming
# "+2 to the chosen power" can actually fail on.
#
# Asserting the stat is not enough on its own either, because "Vision has 3
# thwart" is a claim about a number the engine printed rather than about
# anything it did. Each branch is therefore also spent: the boosted power is
# used, and the threat removed or the damage dealt is asserted against the two
# control scenarios at the bottom, which play the same board without taking the
# Action at all.
#
# Three things about an ally activation that are Vision's stats rather than this
# ability, and are asserted so a change to either would not slip past: an ally
# exhausts to thwart or attack, and Vision's printed 1 consequential damage
# lands on it when it does.
#
# The cost is pinned by hand size. Vision's Action costs one [energy] resource,
# and the two scenarios that take it drop from four cards to three while the two
# controls stay at four. Without that a scenario passes on an engine that
# granted the bonus for free.

Feature: Vision

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01068
  Scenario: the Action is not offered when its energy cost cannot be paid
    # Affordability belongs to this option on this board, not to the hand by
    # itself: generators and discounts in play would also count. Here the only
    # resources are mental, so Vision's energy cost cannot be met and the turn
    # menu must omit the Action instead of offering a choice it later refuses.
    Given I am in hero form
    And "Vision" is in play
    And my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    Then I am not offered "Action" on "Vision"

  @card:01068
  Scenario: the THW branch raises thwart by 2 and leaves attack alone
    # The follow-up option table is the "(Limit once per round.)" assertion.
    # The menu that comes back after the Action has resolved carries Attack,
    # Thwart and Change Form for the hero and Attack and Thwart for Vision --
    # and no Action, because it has already been used this round. Written out in
    # full because an option table is only an assertion if it is complete.
    Given I am in hero form
    And "Vision" is in play
    And my hand is "Energy", "Backflip", "Backflip", "Backflip"
    And the main scheme has 5 threat

    When I choose "Action" on "Vision"
    Then I am prompted to choose one
      | THW |
      | ATK |

    When I choose "THW"
    Then "Vision" has 3 "thwart"
    And "Vision" has 2 "attack"
    And I have 3 cards in hand
    And I am prompted to choose one
      | Attack      |
      | Thwart      |
      | Change Form |
      | Attack      |
      | Thwart      |

    When I choose "Thwart" on "Vision"
    Then the main scheme has 2 threat
    And "Rhino" has 0 damage
    And "Vision" is exhausted
    And "Vision" has 1 damage

  @card:01068
  Scenario: the ATK branch raises attack by 2 and leaves thwart alone
    Given I am in hero form
    And "Vision" is in play
    And my hand is "Energy", "Backflip", "Backflip", "Backflip"
    And the main scheme has 5 threat

    When I choose "Action" on "Vision"
    Then I am prompted to choose one
      | THW |
      | ATK |

    When I choose "ATK"
    Then "Vision" has 4 "attack"
    And "Vision" has 1 "thwart"
    And I have 3 cards in hand

    When I choose "Attack" on "Vision"
    Then "Rhino" has 4 damage
    And the main scheme has 5 threat
    And "Vision" is exhausted
    And "Vision" has 1 damage

  @card:01068
  Scenario: without the Action Vision thwarts for its printed 1
    # The control for the THW branch. Same board, same hand -- the Action is
    # offered and simply not taken, which is what the option table below says --
    # so 3 threat removed above is attributable to this ability and not to
    # Vision being a 3-thwart ally all along.
    Given I am in hero form
    And "Vision" is in play
    And my hand is "Energy", "Backflip", "Backflip", "Backflip"
    And the main scheme has 5 threat

    Then I am prompted to choose one
      | Attack      |
      | Thwart      |
      | Change Form |
      | Action      |
      | Attack      |
      | Thwart      |
    And "Vision" has 1 "thwart"
    And "Vision" has 2 "attack"

    When I choose "Thwart" on "Vision"
    Then the main scheme has 4 threat
    And I have 4 cards in hand
    And "Vision" is exhausted

  @card:01068
  Scenario: without the Action Vision attacks for its printed 2
    # The control for the ATK branch, and the reason it is a separate scenario:
    # an ally exhausts to activate, so one board cannot both thwart and attack.
    Given I am in hero form
    And "Vision" is in play
    And my hand is "Energy", "Backflip", "Backflip", "Backflip"
    And the main scheme has 5 threat

    Then I am prompted to choose one
      | Attack      |
      | Thwart      |
      | Change Form |
      | Action      |
      | Attack      |
      | Thwart      |

    When I choose "Attack" on "Vision"
    Then "Rhino" has 2 damage
    And I have 4 cards in hand
    And "Vision" is exhausted
