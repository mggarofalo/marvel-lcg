@core
Feature: Core play areas
  Every physical card occupies one rules-defined game area and one play area.

  @behavior:rr:play-area.1:published-result
  @covers:behavior:rr:play-area.2:published-result
  @covers:behavior:rr:play-area.3:published-result
  @covers:behavior:rr:player-s-play-area.2:published-result
  @covers:behavior:rr:player-s-play-area.3:published-result
  @covers:behavior:rr:player-s-play-area.4:published-result
  @covers:behavior:rr:villain-s-play-area.1:published-result
  @covers:behavior:rr:villain-s-play-area.2:published-result
  @covers:behavior:rr:villain-s-play-area.3:published-result
  @covers:behavior:rr:villain-s-play-area.4:published-result
  @rr:play-area.1 @rr:play-area.2 @rr:play-area.3
  @rr:player-s-play-area.2 @rr:player-s-play-area.3
  @rr:player-s-play-area.4 @rr:villain-s-play-area.1
  @rr:villain-s-play-area.2 @rr:villain-s-play-area.3
  @rr:villain-s-play-area.4
  Scenario: Core encounter cards occupy exactly their prescribed play areas
    # Engaged minions and obligations belong to a player's play area.
    # Environments, side schemes, and attachments on villain-area cards belong
    # to the villain's play area, and no card belongs to both.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 807  |
    And card 01143 copy 0 is a minion engaged with seat 1
    And card 01165 copy 0 is an obligation in seat 1's play area
    And card 01148 copy 0 is a side scheme in play
    And card 01141 copy 0 is attached to card 01134 copy 0
    When the dealt Core scene is inspected
    Then card 01143 copy 0 is in seat 1's play area
    And card 01143 copy 0 is not in the villain's play area
    And card 01165 copy 0 is in seat 1's play area
    And card 01165 copy 0 is not in the villain's play area
    And card 01140 copy 0 is in the villain's play area
    And card 01140 copy 0 is not in seat 1's play area
    And card 01148 copy 0 is in the villain's play area
    And card 01148 copy 0 is not in seat 1's play area
    And card 01141 copy 0 is in the villain's play area
    And card 01141 copy 0 is not in seat 1's play area
