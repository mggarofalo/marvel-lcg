@core
Feature: Core Spider-Man nemesis set
  Vulture's encounter set resolves from its printed Core text without
  borrowing cards from another identity's deck.

  @behavior:card:01166:each-player-places-random-card-from-their-one-player
  @card:01166
  Scenario: Highway Robbery places a random hand card facedown on the scheme
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1059 |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    When card 01166 copy 0 is revealed to seat 1
    Then card 01088 copy 0 is facedown attached to card 01166 copy 0

  @behavior:card:01168:stun-your-hero
  @covers:behavior:card:01168:if-vulture-is-in-play-card-gains-condition-not-met
  @card:01168
  Scenario: Sweeping Swoop stuns the hero without surging when Vulture is absent
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1060 |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    When card 01168 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has a stunned status card
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01168:if-vulture-is-in-play-card-gains-condition-met
  @card:01168
  Scenario: Sweeping Swoop surges when Vulture is in play
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1061 |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And card 01167 copy 0 is a minion engaged with seat 1
    When card 01168 copy 1 is revealed to seat 1
    Then card 01001a copy 0 has a stunned status card
    And seat 1 has 1 facedown encounter card

  @behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-met
  @card:01168
  Scenario: Sweeping Swoop stuns the friendly character damaged by its activation
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1062 |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01168     | 0    |
    When the villain attacks seat 1 with card 01083 copy 0 defending
    Then card 01083 copy 0 has a stunned status card

  @behavior:card:01168:if-activation-deals-damage-friendly-character-stun-condition-not-met
  @card:01168
  Scenario: Sweeping Swoop does not stun when tough prevents all activation damage
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1063 |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01083 copy 0 has a tough status card
    And these cards are next on the encounter deck
      | next card | copy |
      | 01168     | 0    |
    When the villain attacks seat 1 with card 01083 copy 0 defending
    Then card 01083 copy 0 has 0 stunned status cards

  @behavior:card:01169:discard-1-card-at-random-from-each-one-player
  @covers:behavior:card:01169:place-1-threat-on-main-scheme-for-one
  @card:01169
  Scenario: Vulture's Plans discards one player's only card and counts its resource type
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1064 |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And card 01097b copy 0 has 0 threat counters
    When card 01169 copy 0 is revealed to seat 1
    Then card 01088 copy 0 is in seat 1's discard pile
    And card 01097b copy 0 has 1 threat counter

  @behavior:card:01169:discard-1-card-at-random-from-each-multiple-players
  @covers:behavior:card:01169:place-1-threat-on-main-scheme-for-multiple
  @card:01169
  Scenario: Vulture's Plans discards from every player and counts distinct resource types
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 1065 |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01089 | 1    |
    And card 01097b copy 0 has 0 threat counters
    When card 01169 copy 0 is revealed to seat 1
    Then card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 1 is in seat 2's discard pile
    And card 01097b copy 0 has 2 threat counters

  @behavior:card:01169:place-1-threat-on-main-scheme-for-zero
  @card:01169
  Scenario: Vulture's Plans places no threat when no resource type is discarded
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1066 |
    And seat 1's hand is empty
    And card 01097b copy 0 has 0 threat counters
    When card 01169 copy 0 is revealed to seat 1
    Then card 01097b copy 0 has 0 threat counters
