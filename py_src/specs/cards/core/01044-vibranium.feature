# Printed: no ability text. Two wild resource icons.
#
# A resource card's whole behaviour is what it pays for, and the harness never
# chooses payment -- `BotCommand.BuildPayment` does, and it spends the fewest
# cards that cover the cost. That makes the icon count observable from two
# sides, and it takes both to pin it at exactly 2:
#
#   * a cost of 2 paid by one Vibranium and nothing else says the card is worth
#     at least 2. One icon would leave the play unpayable and the scenario would
#     fail as spec-wrong.
#   * a cost of 3 that consumes *both* Vibranium says it is worth no more than
#     2. Three icons would cover the cost with one card and leave the other in
#     hand.
#
# What is not pinned here is *wild*. Every cost in the core set is generic, so
# an icon's colour only becomes visible against a cost that names one -- Sonic
# Boom's `Cost("YBR")` is the nearest -- and no step in the catalogue reads a
# card's icons directly.

Feature: Vibranium

  Background:
    Given the scenario is "rhino"
    And the hero is "black_panther"
    And I am in hero form

  @card:01044
  Scenario: one Vibranium pays a cost of 2 on its own
    # Panther Claws is printed cost 2. The hand holds nothing else that could
    # contribute, so the upgrade reaching play at all is the assertion; the
    # empty hand and the single discarded card say no second resource was found
    # from somewhere.
    Given my hand is "Panther Claws", "Vibranium"

    When I play "Panther Claws"
    Then "Panther Claws" is in the "UpgradesArea"
    And "Vibranium" is in the "DiscardPile"
    And I have 0 cards in hand
    And I have 1 cards in my discard pile

  @card:01044
  Scenario: a cost of 3 takes both Vibranium
    # Helicarrier is printed cost 3, so two Vibranium is 4 icons for a 3 cost
    # and one of them is partly wasted -- which is the point. Payment spends the
    # fewest cards it can, proven by the scenario above where two Vibranium in
    # hand would have left one behind. Here neither is left behind, so neither
    # was worth 3 on its own.
    Given my hand is "Helicarrier", "Vibranium", "Vibranium"

    When I play "Helicarrier"
    Then "Helicarrier" is in the "SupportsArea"
    And I have 0 cards in hand
    And I have 2 cards in my discard pile
