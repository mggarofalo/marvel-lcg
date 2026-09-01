@core
Feature: Core Captain Marvel nemesis set
  Yon-Rogg's encounter set resolves from its printed Core text.

  @behavior:card:01176:place-additional-1per-hero-threat-here
  @card:01176
  Scenario: The Psyche-Magnitron adds one threat per hero
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 1078 |
    When card 01176 copy 0 is revealed to seat 1
    Then card 01176 copy 0 has 5 threat counters

  @behavior:card:01177:after-yon-rogg-attacks-place-1-threat
  @card:01177
  Scenario: Yon-Rogg places threat on The Psyche-Magnitron after attacking
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1079 |
    And seat 1 shows identity face 01010a
    And card 01010a copy 0 is exhausted
    And card 01176 copy 0 is a side scheme in play
    And card 01176 copy 0 has 1 threat counter
    And card 01177 copy 0 is a minion engaged with seat 1
    When card 01177 copy 0 attacks seat 1 with every optional choice declined
    Then card 01176 copy 0 has 2 threat counters

  @behavior:card:01178:surge
  @covers:behavior:card:01178:after-card-resolves-reveal-1-additional-encounter
  @covers:behavior:card:01178:place-1-threat-on-main-scheme
  @card:01178
  Scenario: Kree Manipulator places threat and surges
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1080 |
    And seat 1's hand is empty
    And card 01097b copy 0 has 0 threat counters
    When card 01178 copy 0 is revealed to seat 1
    Then seat 1 is asked to choose between 2 simultaneous effects
    And seat 1 is offered the "Surge" pending opportunity
    When seat 1 accepts the "Surge" pending opportunity
    Then card 01097b copy 0 has 1 threat counter
    And seat 1 has 1 facedown encounter card

  @behavior:card:01179:discard-each-energy-resource-from-your-hand
  @covers:behavior:card:01179:if-you-discarded-no-cards-way-card-condition-not-met
  @card:01179
  Scenario: Yon-Rogg's Treason discards every energy-resource card without surging
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1081 |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01085 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When card 01179 copy 0 is revealed to seat 1
    Then card 01085 copy 0 is in seat 1's discard pile
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's hand
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01179:if-you-discarded-no-cards-way-card-condition-met
  @card:01179
  Scenario: Yon-Rogg's Treason surges when no energy-resource card is discarded
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1082 |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When card 01179 copy 0 is revealed to seat 1
    Then card 01089 copy 0 is in seat 1's hand
    And seat 1 has 1 facedown encounter card
