@core
Feature: Core ownership and control
  Ownership is fixed by the legal deal; control determines which cards a
  player may use and pay with at the current game moment.

  @behavior:rr:cost.7:published-result
  @covers:behavior:rr:ownership-and-control.4:published-result
  @rr:cost.7 @rr:ownership-and-control.4 @card:01086 @card:01088
  Scenario: Another player's hand cannot pay your card's cost
    # A player paying a cost "must pay costs with cards and/or game elements
    # they control." Each player controls the cards in their own hand, so
    # Spider-Man's Energy cannot make Captain Marvel's First Aid playable.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 827  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01086 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01088 | 1    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 2 is offered a mulligan
    When seat 2 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 asks whether card 01086 copy 0 is available to play
    Then card 01086 copy 0 is unavailable to play
