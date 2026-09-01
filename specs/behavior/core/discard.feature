@core
Feature: Player and encounter discards
  Discard destinations depend on ownership and card class, while a multi-card
  deck discard preserves the sequence produced by the single effect.

  @behavior:rr:discard.1:published-result
  @covers:behavior:rr:discard-pile.1:published-result
  @covers:behavior:rr:ownership-and-control.7.4:published-result
  @rr:discard.1 @rr:discard-pile.1 @rr:ownership-and-control.7.4
  Scenario: A discarded player card goes faceup to its owner's pile
    # "If a player card is discarded, it is placed faceup on top of the owning
    # player's discard pile."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
    When seat 1 discards card 01002 copy 0 from hand
    Then card 01002 copy 0 is faceup on top of seat 1's discard pile

  @behavior:rr:discard.2:published-result
  @covers:behavior:rr:discard-pile.1:published-result
  @rr:discard.2 @rr:discard-pile.1
  Scenario: A discarded encounter card goes faceup to the encounter pile
    # "If an encounter card is discarded, it is placed faceup on top of the
    # encounter discard pile."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When the top 1 card of the encounter deck is discarded
    Then card 01101 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:discard.4:published-result
  @covers:behavior:rr:player-deck.1:published-result
  @rr:discard.4 @rr:player-deck.1
  Scenario: A singular effect discards deck cards one at a time without reordering them
    # "Place those cards in the appropriate discard pile one at a time (without
    # changing the order)."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
      | 01004     | 0    |
    When seat 1 discards the top 3 cards of their player deck
    Then 3 Discard events were emitted
    And seat 1's discard pile has these cards from top to bottom
      | card  | copy |
      | 01004 | 0    |
      | 01003 | 0    |
      | 01002 | 0    |
