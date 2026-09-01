@core
Feature: Core status cards
  Statuses are physical cards with per-type limits. Tough replaces one damage
  instance, and Toughness creates a tough card only after entry into play.

  @behavior:rr:stun-stunned.2:published-result
  @covers:behavior:rr:status-cards:status-card-placement
  @rr:stun-stunned.2 @rr:status-cards
  Scenario: A stun ability gives the character a stunned status card
    # "If an ability 'stuns' a character, give that character a stunned status
    # card."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    When an ability stuns card 01094 copy 0
    Then card 01094 copy 0 has 1 stunned status card
    And card 01094 copy 0 is stunned

  @behavior:rr:status-cards.1:published-result @rr:status-cards.1
  Scenario: A character cannot receive a second status card of the same type
    # "A character cannot have more than one status card of each type at a
    # time."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And card 01094 copy 0 has a stunned status card
    When an ability stuns card 01094 copy 0
    Then card 01094 copy 0 has 1 stunned status card

  @behavior:rr:tough.2:published-result
  @covers:behavior:rr:tough.3:published-result
  @rr:tough.2 @rr:tough.3
  Scenario: Tough prevents the entire damage instance and is discarded
    # "Prevent all of that damage and discard a tough status card from that
    # character instead." The character is not considered to have taken damage.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And card 01094 copy 0 has a tough status card
    When card 01005 copy 0 deals 8 damage to card 01094 copy 0
    Then card 01094 copy 0 has 0 damage
    And card 01094 copy 0 has 0 tough status cards
    And 0 Damage events were emitted

  @behavior:rr:toughness:published-result
  @covers:behavior:rr:toughness.1:published-result
  @rr:toughness @rr:toughness.1
  Scenario: A character with Toughness gains tough after entering play
    # "Forced Response: After this character enters play, give it a tough
    # status card."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    When card 01102 copy 0 enters play as a minion engaged with seat 1
    Then card 01102 copy 0 is engaged with seat 1
    And card 01102 copy 0 has 1 tough status card
