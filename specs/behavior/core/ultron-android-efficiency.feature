@core
Feature: Core Android Efficiency treacheries
  Each printed Android Efficiency face creates Drones for every player when
  revealed and offers its own resource-or-Drone choice as a boost ability.

  @behavior:card:01144a:each-player-puts-top-card-their-deck-one-player
  @card:01144a
  Scenario: Energy Android Efficiency creates one player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1026 |
    And seat 1's hand is empty
    When card 01144a copy 0 is revealed to seat 1
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01144a:each-player-puts-top-card-their-deck-multiple-players
  @card:01144a
  Scenario: Energy Android Efficiency creates every player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1027 |
    And seat 1's hand is empty
    And seat 2's hand is empty
    When card 01144a copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then seat 1 has 2 facedown Drone minions
    And seat 2 has 2 facedown Drone minions

  @behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-1
  @card:01144a
  Scenario: Energy Android Efficiency can spend an energy resource
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1028 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144a    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 paying with these cards for the pending encounter-card decision
      | card  | copy |
      | 01088 | 0    |
    Then card 01088 copy 0 is in seat 1's discard pile
    And seat 1 has 1 facedown Drone minion

  @behavior:card:01144a:choose-either-spend-energy-resource-or-put-choice-2
  @card:01144a
  Scenario: Energy Android Efficiency can create a Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1029 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144a    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01144b:each-player-puts-top-card-their-deck-one-player
  @card:01144b
  Scenario: Mental Android Efficiency creates one player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1030 |
    And seat 1's hand is empty
    When card 01144b copy 0 is revealed to seat 1
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01144b:each-player-puts-top-card-their-deck-multiple-players
  @card:01144b
  Scenario: Mental Android Efficiency creates every player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1031 |
    And seat 1's hand is empty
    And seat 2's hand is empty
    When card 01144b copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then seat 1 has 2 facedown Drone minions
    And seat 2 has 2 facedown Drone minions

  @behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-1
  @card:01144b
  Scenario: Mental Android Efficiency can spend a mental resource
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1032 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144b    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 paying with these cards for the pending encounter-card decision
      | card  | copy |
      | 01089 | 0    |
    Then card 01089 copy 0 is in seat 1's discard pile
    And seat 1 has 1 facedown Drone minion

  @behavior:card:01144b:choose-either-spend-mental-resource-or-put-choice-2
  @card:01144b
  Scenario: Mental Android Efficiency can create a Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1033 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144b    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01144c:each-player-puts-top-card-their-deck-one-player
  @card:01144c
  Scenario: Physical Android Efficiency creates one player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1034 |
    And seat 1's hand is empty
    When card 01144c copy 0 is revealed to seat 1
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01144c:each-player-puts-top-card-their-deck-multiple-players
  @card:01144c
  Scenario: Physical Android Efficiency creates every player's Drone
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1035 |
    And seat 1's hand is empty
    And seat 2's hand is empty
    When card 01144c copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then seat 1 has 2 facedown Drone minions
    And seat 2 has 2 facedown Drone minions

  @behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-1
  @card:01144c
  Scenario: Physical Android Efficiency can spend a physical resource
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1036 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01090 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144c    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 paying with these cards for the pending encounter-card decision
      | card  | copy |
      | 01090 | 0    |
    Then card 01090 copy 0 is in seat 1's discard pile
    And seat 1 has 1 facedown Drone minion

  @behavior:card:01144c:choose-either-spend-physical-resource-or-put-choice-2
  @card:01144c
  Scenario: Physical Android Efficiency can create a Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1037 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01090 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01144c    | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 has 2 facedown Drone minions
