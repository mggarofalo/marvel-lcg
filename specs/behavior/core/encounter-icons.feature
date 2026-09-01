@core
Feature: Core encounter icons
  Core side schemes can add acceleration, crisis, or hazard icons while they
  remain in play. Each icon changes only the rules operation it names.

  @behavior:rr:acceleration-icon.1:published-result
  @covers:behavior:rr:acceleration-icon:published-result
  @rr:acceleration-icon @rr:acceleration-icon.1
  Scenario: An acceleration icon adds one threat during the villain phase
    # Each acceleration icon is a constant ability that places one additional
    # threat during the villain phase's Place Threat step.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 325  |
    And card 01109 copy 0 is a side scheme in play
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 3 threat counters

  @behavior:rr:acceleration-icon.2:published-result
  @rr:acceleration-icon.2
  Scenario: Defeating a side scheme removes its acceleration icon from play
    # An acceleration icon is removed from play by defeating the encounter
    # card on which it is printed.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 326  |
    And seat 1 shows identity face 01001a
    And card 01109 copy 0 is a side scheme in play
    And card 01109 copy 0 has 1 threat counter
    When seat 1 uses their basic thwart against card 01109 copy 0
    Then card 01109 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:acceleration-icon.3:published-result
  @covers:behavior:rr:acceleration-token.4:published-result
  @rr:acceleration-icon.3 @rr:acceleration-token.4 @card:01160
  Scenario: Removing an acceleration icon does not remove an acceleration token
    # "Acceleration icons are not considered acceleration tokens, and vice
    # versa." Defeating the card bearing the icon therefore leaves Legal
    # Work's acceleration token beside the main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 343  |
    And seat 1 shows identity face 01019a
    And card 01109 copy 0 is a side scheme in play
    And card 01109 copy 0 has 1 threat counter
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
    When card 01160 copy 0 is revealed to seat 1
    Then card 01160 copy 0 is in seat 1's play area
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then the main scheme has 1 acceleration token
    When seat 1 uses their basic thwart against card 01109 copy 0
    Then card 01109 copy 0 is faceup on top of the encounter discard pile
    And the main scheme has 1 acceleration token
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 2 threat counters

  @behavior:rr:crisis-icon.1:published-result
  @covers:behavior:rr:crisis-icon:published-result
  @rr:crisis-icon @rr:crisis-icon.1
  Scenario: A crisis icon prevents a player card from thwarting the main scheme
    # While a crisis icon is in play, player cards cannot remove threat from
    # the main scheme; the crisis side scheme remains a legal thwart target.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 327  |
    And seat 1 shows identity face 01001a
    And card 01108 copy 0 is a side scheme in play
    And card 01097b copy 0 has 3 threat counters
    When seat 1 asks for their basic thwart targets
    Then card 01097b copy 0 is unavailable as a target
    And card 01108 copy 0 is available as a target

  @behavior:rr:hazard-icon:published-result
  @covers:behavior:rr:hazard-icon.1:published-result
  @rr:hazard-icon @rr:hazard-icon.1
  Scenario: One hazard icon deals one additional encounter card total
    # Each hazard icon deals one additional encounter card during step three;
    # it does not deal one additional card per player.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 328  |
    And card 01107 copy 0 is a side scheme in play
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
      | 01101     | 1    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 1 is engaged with seat 1
    And seat 1 has 0 facedown encounter cards
