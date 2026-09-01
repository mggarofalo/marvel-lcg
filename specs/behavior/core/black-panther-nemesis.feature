@core
Feature: Core Black Panther nemesis set
  Black Panther's set-aside nemesis cards follow their printed Core text once
  they enter a legal game state.

  @behavior:card:01157:killmonger-cannot-take-damage-from-black-panther
  @card:01157
  Scenario: Killmonger prevents damage from a Black Panther upgrade
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 1049 |
    And card 01157 copy 0 is a minion engaged with seat 1
    And card 01046 copy 0 is an upgrade attached to seat 1's identity
    When card 01046 copy 0 deals 1 damage to card 01157 copy 0
    Then card 01157 copy 0 has 0 damage

  @behavior:card:01158:surge-after-card-resolves-reveal-1-additional
  @covers:behavior:card:01158:give-villain-and-each-minion-engaged-with
  @card:01158
  Scenario: Heart-Shaped Herb toughens every enemy engaged with the player and surges
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 1050 |
    And seat 1 shows identity face 01040b
    And seat 1's hand is empty
    And card 01157 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    When card 01158 copy 0 is revealed to seat 1
    Then seat 1 is asked to choose between 2 simultaneous effects
    And seat 1 is offered the "Surge" pending opportunity
    When seat 1 accepts the "Surge" pending opportunity
    Then card 01094 copy 0 has a tough status card
    And card 01157 copy 0 has a tough status card
    And card 01101 copy 0 has a tough status card
    And seat 1 has 1 facedown encounter card

  @behavior:card:01158:give-villain-tough-status-card
  @card:01158
  Scenario: Heart-Shaped Herb gives the villain tough as a boost ability
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 1051 |
    And seat 1 shows identity face 01040a
    And card 01041 copy 0 is an ally controlled by seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01158     | 0    |
    When the villain attacks seat 1 with card 01041 copy 0 defending
    Then card 01094 copy 0 has a tough status card

  @behavior:card:01159:discard-top-card-encounter-deck
  @covers:behavior:card:01159:then-choose-either-deal-x-damage-your-choice-1
  @card:01159
  Scenario: Ritual Combat adds one to a zero-boost card and damages the hero
    Given a canonical Core scene is dealt
      | campaign     | heroes        | seed |
      | rhino_expert | black_panther | 1052 |
    And seat 1 shows identity face 01040a
    And seat 1's hand is empty
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
    When card 01159 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01040a copy 0 has 1 damage
    And card 01097b copy 0 has 0 threat counters
    And the encounter discard pile has 2 cards

  @behavior:card:01159:then-choose-either-deal-x-damage-your-choice-2
  @card:01159
  Scenario: Ritual Combat can place its computed amount as threat
    Given a canonical Core scene is dealt
      | campaign     | heroes        | seed |
      | rhino_expert | black_panther | 1053 |
    And seat 1 shows identity face 01040a
    And seat 1's hand is empty
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01188     | 0    |
    When card 01159 copy 1 is revealed to seat 1
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01040a copy 0 has 0 damage
    And card 01097b copy 0 has 2 threat counters

  @behavior:card:01159:x-is-1-more-than-number-boost
  @card:01159
  Scenario: Ritual Combat scales X with multiple printed boost icons
    Given a canonical Core scene is dealt
      | campaign     | heroes        | seed |
      | rhino_expert | black_panther | 1054 |
    And seat 1 shows identity face 01040a
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01193     | 0    |
    When card 01159 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01040a copy 0 has 4 damage
