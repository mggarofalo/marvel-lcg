@core
Feature: Core Iron Man nemesis set
  Whiplash and the Iron Man nemesis encounter cards resolve from their printed
  Core text.

  @behavior:card:01171:place-additional-1-per-hero-threat-here
  @card:01171
  Scenario: Imminent Overload adds one threat per hero
    Given a canonical Core scene is dealt
      | campaign | heroes                   | seed |
      | rhino    | iron_man,captain_marvel | 1067 |
    When card 01171 copy 0 is revealed to seat 1
    Then card 01171 copy 0 has 5 threat counters

  @behavior:card:01172:retaliate-1-after-character-is-attacked-deal
  @card:01172
  Scenario: Whiplash retaliates after the hero attacks him
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1068 |
    And seat 1 shows identity face 01029a
    And card 01172 copy 0 is a minion engaged with seat 1
    When seat 1 uses their basic attack against card 01172 copy 0
    Then card 01172 copy 0 has 1 damage
    And card 01029a copy 0 has 1 damage

  @behavior:card:01173:choose-either-deal-1-damage-your-hero-zero
  @card:01173
  Scenario: Electric Whip Attack does nothing when no upgrade is controlled
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1069 |
    And seat 1 shows identity face 01029a
    When card 01173 copy 0 is revealed to seat 1
    Then card 01029a copy 0 has 0 damage
    And no opportunity is pending

  @behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-1
  @covers:behavior:card:01173:choose-either-deal-1-damage-your-hero-one
  @card:01173
  Scenario: Electric Whip Attack deals one damage for one controlled upgrade
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1070 |
    And seat 1 shows identity face 01029a
    And card 01035 copy 0 is an upgrade attached to seat 1's identity
    When card 01173 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01029a copy 0 has 1 damage
    And card 01035 copy 0 remains attached to seat 1's identity

  @behavior:card:01173:choose-either-deal-1-damage-your-hero-choice-2
  @covers:behavior:card:01173:choose-either-deal-1-damage-your-hero-multiple
  @card:01173
  Scenario: Electric Whip Attack discards the chosen upgrade among multiple
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1071 |
    And seat 1 shows identity face 01029a
    And card 01035 copy 0 is an upgrade attached to seat 1's identity
    And card 01038 copy 0 is an upgrade attached to seat 1's identity
    When card 01173 copy 0 is revealed to seat 1
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01035 copy 0 is offered by the pending action
    And card 01038 copy 0 is offered by the pending action
    When seat 1 chooses card 01035 copy 0 for the pending action
    Then card 01035 copy 0 is in seat 1's discard pile
    And card 01038 copy 0 remains attached to seat 1's identity
    And card 01029a copy 0 has 0 damage

  @behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-met
  @card:01173
  Scenario: Electric Whip Attack discards an upgrade during an undefended attack
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1072 |
    And seat 1 shows identity face 01029a
    And card 01029a copy 0 is exhausted
    And card 01035 copy 0 is an upgrade attached to seat 1's identity
    And these cards are next on the encounter deck
      | next card | copy |
      | 01173     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then card 01035 copy 0 is offered by the pending action
    When seat 1 chooses card 01035 copy 0 for the pending action
    Then card 01035 copy 0 is in seat 1's discard pile

  @behavior:card:01173:if-villain-is-making-undefended-attack-choose-condition-not-met
  @card:01173
  Scenario: Electric Whip Attack leaves upgrades alone during a defended attack
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1073 |
    And seat 1 shows identity face 01029a
    And card 01035 copy 0 is an upgrade attached to seat 1's identity
    And these cards are next on the encounter deck
      | next card | copy |
      | 01173     | 0    |
    When the villain attacks seat 1 with card 01029a copy 0 defending
    Then card 01035 copy 0 remains attached to seat 1's identity

  @behavior:card:01174:each-player-discards-top-5-cards-their-one-player
  @covers:behavior:card:01174:for-each-printed-energy-resource-player-discards-zero
  @card:01174
  Scenario: Electromagnetic Backlash discards five and deals zero without energy resources
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1074 |
    And seat 1 shows identity face 01029a
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01030     | 0    |
      | 01031     | 0    |
      | 01033     | 0    |
      | 01034     | 0    |
      | 01036     | 0    |
    When card 01174 copy 0 is revealed to seat 1
    Then seat 1 has 5 cards in their discard pile
    And card 01029a copy 0 has 0 damage

  @behavior:card:01174:for-each-printed-energy-resource-player-discards-one
  @card:01174
  Scenario: Electromagnetic Backlash deals one damage for one printed energy resource
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1075 |
    And seat 1 shows identity face 01029a
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01030     | 0    |
      | 01031     | 0    |
      | 01033     | 0    |
      | 01034     | 0    |
      | 01035     | 0    |
    When card 01174 copy 0 is revealed to seat 1
    Then card 01029a copy 0 has 1 damage

  @behavior:card:01174:for-each-printed-energy-resource-player-discards-multiple
  @card:01174
  Scenario: Electromagnetic Backlash counts every printed energy resource
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1076 |
    And seat 1 shows identity face 01029a
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01030     | 0    |
      | 01031     | 0    |
      | 01033     | 0    |
      | 01035     | 0    |
      | 01088     | 0    |
    When card 01174 copy 0 is revealed to seat 1
    Then card 01029a copy 0 has 3 damage

  @behavior:card:01174:each-player-discards-top-5-cards-their-multiple-players
  @card:01174
  Scenario: Electromagnetic Backlash discards five cards from every player's deck
    Given a canonical Core scene is dealt
      | campaign | heroes                   | seed |
      | rhino    | iron_man,captain_marvel | 1077 |
    And seat 1 shows identity face 01029a
    And seat 2 shows identity face 01010a
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01030     | 0    |
      | 01031     | 0    |
      | 01033     | 0    |
      | 01034     | 0    |
      | 01036     | 0    |
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01011     | 0    |
      | 01012     | 0    |
      | 01013     | 0    |
      | 01015     | 0    |
      | 01016     | 0    |
    When card 01174 copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then seat 1 has 5 cards in their discard pile
    And seat 2 has 5 cards in their discard pile
