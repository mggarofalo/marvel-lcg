@core
Feature: Core Ultron side schemes
  Ultron side schemes create facedown Drones and resolve each player's printed
  choice from legal one-player and multiplayer scenes.

  @behavior:card:01148:each-player-puts-top-card-their-deck-one-player
  @covers:behavior:card:01148:place-1-threat-here-for-each-drone-one
  @card:01148
  Scenario: Drone Factory creates and counts one Drone in a one-player game
    # The prior setup Drone has left play. Drone Factory creates one Drone for
    # the only player, then adds one threat for that one Drone to its printed four.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 990  |
    And seat 1 has no facedown Drone minions
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    When card 01148 copy 0 is revealed to seat 1
    Then seat 1 has 1 facedown Drone minion
    And card 01148 copy 0 has 5 threat counters

  @behavior:card:01148:each-player-puts-top-card-their-deck-multiple-players
  @covers:behavior:card:01148:place-1-threat-here-for-each-drone-multiple
  @card:01148
  Scenario: Drone Factory creates and counts Drones for every player
    # Two setup Drones are already in play. Drone Factory creates one more for
    # each player, then adds four threat for all four Drones to its printed four.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 991  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01011     | 0    |
    When card 01148 copy 0 is revealed to seat 1
    Then seat 1 has 2 facedown Drone minions
    And seat 2 has 2 facedown Drone minions
    And card 01148 copy 0 has 8 threat counters

  @behavior:card:01150:first-player-puts-top-2-cards-their
  @card:01150
  Scenario: Ultron's Imperative gives two Drones to the first player
    # Seat 1 has the first-player token and one setup Drone. The side scheme
    # puts the next two player-deck cards into play as two additional Drones.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 992  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
    When card 01150 copy 0 is revealed to seat 2
    Then seat 1 has 3 facedown Drone minions
    And seat 2 has 1 facedown Drone minion

  @behavior:card:01151:each-player-chooses-either-place-2-threat-one-player
  @covers:behavior:card:01151:each-player-chooses-either-place-2-threat-choice-1
  @card:01151
  Scenario: One player chooses to place threat on Under Attack
    # The only player chooses the first printed consequence, adding two threat
    # to the side scheme's printed three instead of damaging their hero.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 993  |
    And seat 1 shows identity face 01001a
    When card 01151 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01151 copy 0 has 5 threat counters
    And card 01001a copy 0 has 0 damage

  @behavior:card:01151:each-player-chooses-either-place-2-threat-multiple-players
  @covers:behavior:card:01151:each-player-chooses-either-place-2-threat-choice-2
  @card:01151
  Scenario: Multiple players resolve different Under Attack choices
    # Seat 1 places two threat; seat 2 takes three hero damage. Each player
    # resolves one choice in player order.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 994  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    When card 01151 copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then option 2 is offered by the pending decision
    When seat 2 chooses option 2 for the pending encounter-card decision
    Then card 01151 copy 0 has 5 threat counters
    And card 01001a copy 0 has 0 damage
    And card 01010a copy 0 has 3 damage
