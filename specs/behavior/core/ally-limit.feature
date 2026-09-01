@core
Feature: Core ally limit
  A player may play another ally at the printed limit, then must choose an ally
  they control to discard before the entering ally finishes resolving.

  @behavior:rr:ally-limit:published-result
  @covers:behavior:rr:choose-option.2:published-result
  @covers:behavior:rr:choose-option.2.2:published-result
  @rr:ally-limit @rr:choose-option.2 @rr:choose-option.2.2
  Scenario: Playing a fourth ally requires one controlled ally to be discarded
    # "Each player is permitted to control a maximum of three allies in play at
    # any given time." Nick Fury remains playable while three allies are in
    # play; after he is played, the mandatory choice returns the player to
    # three before Nick Fury's enters-play response resolves.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 734  |
    And card 01066 copy 0 is an ally controlled by seat 1
    And card 01067 copy 0 is an ally controlled by seat 1
    And card 01068 copy 0 is an ally controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01084 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 asks whether card 01084 copy 0 is available to play
    Then card 01084 copy 0 is available to play
    When seat 1 plays card 01084 copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01066 copy 0 is offered by the pending action
    When seat 1 chooses card 01066 copy 0 for the pending action
    Then card 01066 copy 0 is in seat 1's discard pile
    And card 01084 copy 0 remains an ally controlled by seat 1
    And option 1 is not offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 has 3 cards in hand
