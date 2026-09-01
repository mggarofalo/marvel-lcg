@core
Feature: Core She-Hulk nemesis set
  Titania and her encounter cards resolve from their printed Core text.

  @behavior:card:01161:place-additional-1-per-hero-threat-here
  @card:01161
  Scenario: Personal Challenge adds one threat per hero
    Given a canonical Core scene is dealt
      | campaign | heroes                   | seed |
      | rhino    | she_hulk,captain_marvel | 1055 |
    And seat 1 shows identity face 01019a
    When card 01161 copy 0 is revealed to seat 1
    Then card 01161 copy 0 has 5 threat counters

  @behavior:card:01162:x-is-equal-titania-s-remaining-hit
  @card:01162
  Scenario: Titania's attack equals her remaining hit points
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 1056 |
    And card 01162 copy 0 is a minion engaged with seat 1
    And card 01162 copy 0 has 2 damage
    When the dealt Core scene is inspected
    Then card 01162 copy 0 has modified ATK 4

  @behavior:card:01164:titania-attacks-your-hero
  @covers:behavior:card:01164:if-titania-did-not-attack-heal-all-condition-not-met
  @card:01164
  Scenario: Titania's Fury attacks without healing or surging in hero form
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 1057 |
    And seat 1 shows identity face 01019a
    And card 01019a copy 0 is exhausted
    And seat 1's hand is empty
    And card 01162 copy 0 is a minion engaged with seat 1
    And card 01162 copy 0 has 2 damage
    When card 01164 copy 0 is revealed to seat 1
    Then card 01019a copy 0 has 4 damage
    And card 01162 copy 0 has 2 damage
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01164:if-titania-did-not-attack-heal-all-condition-met
  @card:01164
  Scenario: Titania's Fury heals Titania and surges when no attack can occur
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 1058 |
    And seat 1 shows identity face 01019b
    And seat 1's hand is empty
    And card 01162 copy 0 is a minion engaged with seat 1
    And card 01162 copy 0 has 3 damage
    When card 01164 copy 1 is revealed to seat 1
    Then card 01162 copy 0 has 0 damage
    And seat 1 has 1 facedown encounter card
