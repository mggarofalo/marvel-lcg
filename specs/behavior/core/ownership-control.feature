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

  @behavior:rr:ownership-and-control.8:published-result
  @covers:behavior:rr:you-your.10:published-result
  @covers:behavior:card:01188:discard-upgrade-or-support-you-control
  @covers:behavior:card:01188:if-no-cards-were-discarded-way-card-condition-met
  @rr:ownership-and-control.8 @rr:you-your.10 @card:01188
  Scenario: Caught Off Guard cannot discard another player's support
    # "You control" refers only to cards in the resolving player's play area.
    # Seat 1 has no upgrade or support, so Caught Off Guard cannot select seat
    # 2's Avengers Mansion; no card is discarded and the treachery surges.
    Given a canonical Core scene is dealt
      | campaign | heroes                     | seed |
      | rhino    | spider_man,captain_marvel | 828  |
    And card 01091 copy 1 is a support controlled by seat 2
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01105     | 0    |
      | 01188     | 0    |
      | 01101     | 0    |
      | 01102     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01091 copy 1 remains a support controlled by seat 2
    And card 01102 copy 0 is engaged with seat 1
    And card 01101 copy 0 is engaged with seat 2
    And card 01188 copy 0 is faceup on top of the encounter discard pile
