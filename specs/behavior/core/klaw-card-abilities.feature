@core
Feature: Core Klaw card abilities
  Klaw scenario cards resolve from legal Core scenes according to their
  printed text and the shared reveal, attack, status, and attachment rules.

  @behavior:card:01114:search-encounter-deck-and-discard-pile-for
  @covers:behavior:card:01114:shuffle-encounter-deck
  @card:01114
  Scenario: Klaw II reveals The Immortal Klaw and shuffles the encounter deck
    # Defeating Klaw I reveals Klaw II. His When Revealed search reveals The
    # "Immortal" Klaw, then shuffles the searched encounter deck.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 951  |
    And seat 1 shows identity face 01001a
    And card 01113 copy 0 has 10 damage
    When seat 1 uses their basic attack against card 01113 copy 0
    Then card 01114 copy 0 is the faceup villain
    And card 01127 copy 0 is in the villain's play area

  @behavior:card:01114:when-klaw-attacks-give-him-1-additional
  @card:01114
  Scenario: Klaw II receives an additional boost card when he attacks
    # Klaw II's Forced Interrupt gives his attack one additional boost card,
    # so the activation resolves two boost cards rather than one.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 952  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
      | 01187     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then 2 cards were turned faceup as boost cards

  @behavior:card:01115:toughness
  @covers:behavior:card:01115:character-enters-play-with-tough-status-card
  @card:01115
  Scenario: Klaw III enters play with a tough status card
    # Defeating Klaw II reveals Klaw III. Toughness gives the newly entered
    # villain one Tough status card.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 953  |
    And seat 1 shows identity face 01001a
    And card 01114 copy 0 has 17 damage
    When seat 1 uses their basic attack against card 01114 copy 0
    Then card 01115 copy 0 is the faceup villain
    And card 01115 copy 0 has 1 tough status card

  @behavior:card:01115:when-klaw-attacks-give-him-1-additional
  @card:01115
  Scenario: Klaw III receives an additional boost card when he attacks
    # After Klaw III enters, his Forced Interrupt gives the next attack one
    # additional boost card.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 954  |
    And seat 1 shows identity face 01001a
    And card 01114 copy 0 has 17 damage
    And seat 1's hand is empty
    When seat 1 uses their basic attack against card 01114 copy 0
    Then card 01115 copy 0 is the faceup villain
    When the villain attacks seat 1 with every optional choice declined
    Then 2 cards were turned faceup as boost cards

  @behavior:card:01118:attach-klaw
  @covers:behavior:card:01118:spend-energy-mental-physical-resources-discard-card
  @card:01118
  Scenario: Sonic Converter attaches to Klaw and is discarded for three resources
    # The revealed Converter attaches to Klaw. Its Hero Action spends one
    # energy, mental, and physical resource and discards it.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 955  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01118 copy 0 is revealed to seat 1
    Then card 01118 copy 0 is attached to card 01113 copy 0
    When seat 1 initiates card 01118 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01118 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01119:attach-klaw
  @covers:behavior:card:01119:spend-energy-mental-physical-resources-discard-card
  @card:01119
  Scenario: Solid Sound Body attaches to Klaw and is discarded for three resources
    # The revealed Body attaches to Klaw. Its Hero Action spends one energy,
    # mental, and physical resource and discards it.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 956  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01119 copy 0 is revealed to seat 1
    Then card 01119 copy 0 is attached to card 01113 copy 0
    When seat 1 initiates card 01119 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01119 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01125:place-additional-1-per-hero-threat-here
  @card:01125
  Scenario: Defense Network adds one threat per player when revealed
    # At two players, Defense Network enters with two starting threat and its
    # When Revealed ability places two additional threat.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 957  |
    When card 01125 copy 0 is revealed to seat 1
    Then card 01125 copy 0 has 4 threat counters

  @behavior:card:01126:place-additional-1-per-hero-threat-here
  @card:01126
  Scenario: Illegal Arms Factory adds one threat per player when revealed
    # At two players, Illegal Arms Factory enters with three starting threat
    # and its When Revealed ability places two additional threat.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 958  |
    When card 01126 copy 0 is revealed to seat 1
    Then card 01126 copy 0 has 5 threat counters

  @behavior:card:01127:klaw-gets-10-hit-points
  @covers:behavior:card:01127:when-scheme-is-defeated-klaw-loses-those
  @card:01127
  Scenario: The Immortal Klaw grants ten hit points only while in play
    # Klaw I has twelve hit points. The side scheme raises that to twenty-two;
    # removing its final threat defeats it and Klaw returns to twelve.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 959  |
    And card 01127 copy 0 is a side scheme in play
    And card 01127 copy 0 has 1 threat counter
    When the printed characteristics of card 01113 copy 0 are requested
    Then card 01113 copy 0 has 22 remaining hit points
    When 1 threat is removed from card 01127 copy 0
    Then card 01127 copy 0 is faceup on top of the encounter discard pile
    And card 01113 copy 0 has 12 remaining hit points
