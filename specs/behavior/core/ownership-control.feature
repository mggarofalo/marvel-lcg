@core
Feature: Core ownership and control
  Ownership is fixed by the legal deal; control determines which cards a
  player may use and pay with at the current game moment.

  @behavior:rr:ownership-and-control.2.1:published-result
  @covers:behavior:rr:upgrade.3.1:published-result
  @covers:behavior:card:01074:attach-ally
  @covers:behavior:card:01074:attached-ally-gets-1-thw-and-1
  @rr:ownership-and-control.2.1 @rr:upgrade.3.1 @card:01002 @card:01074
  Scenario: An attached upgrade follows the controller of its host without changing owner
    # "Upgrades attached to a card controlled by a player other than the
    # upgrade's owner are controlled by that other player." Captain Marvel may
    # attach her Inspired to the friendly Black Cat. Inspired remains owned by
    # Captain Marvel but is controlled by Spider-Man with its attached ally.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 829  |
    And card 01002 copy 0 is an ally controlled by seat 2
    And card 01074 copy 0 is attached to card 01002 copy 0
    When seat 2 asks for available card actions
    Then card 01074 copy 0 is owned by seat 1
    And card 01074 copy 0 is controlled by seat 2
    And card 01002 copy 0 has modified THW 2
    And card 01002 copy 0 has modified ATK 2

  @behavior:rr:action.1:published-result
  @rr:action.1 @card:01091 @card:01141
  Scenario: Card actions belong to their controller or to an encounter card
    # A player may trigger an Action only on a card they control or on an
    # encounter card. Seat 1 can pay Program Transmitter's encounter-card Hero
    # Action, but cannot trigger seat 2's controlled Avengers Mansion Action.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | captain_marvel,spider_man | 832  |
    And seat 1 shows identity face 01010a
    And card 01091 copy 1 is a support controlled by seat 2
    And card 01141 copy 0 is attached to card 01134 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When seat 1 asks for available card actions
    Then card 01141 copy 0's action is available
    And card 01091 copy 1's action is unavailable

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
