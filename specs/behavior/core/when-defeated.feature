@core
Feature: Core When Defeated timing
  A defeated card resolves its When Defeated ability while its hosted cards
  are still discoverable, then leaves play.

  @behavior:rr:when-defeated-abilities.2.1:published-result
  @covers:behavior:rr:when-defeated-abilities.1:published-result
  @covers:behavior:rr:when-defeated-abilities.2:published-result
  @covers:behavior:card:01166:each-player-places-random-card-from-their-multiple-players
  @covers:behavior:card:01166:return-each-facedown-card-here-its-owner
  @rr:when-defeated-abilities.1 @rr:when-defeated-abilities.2
  @rr:when-defeated-abilities.2.1 @card:01166
  Scenario: Highway Robbery returns its hosted cards before leaving play
    # "A defeated card leaves play after its When Defeated ability is
    # resolved." Highway Robbery can still find both facedown cards and return
    # each one to its owner's hand before the side scheme is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes              | seed |
      | rhino    | spider_man,she_hulk | 348  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01006 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01020 | 0    |
    When card 01166 copy 0 is revealed to seat 1
    Then card 01006 copy 0 is facedown attached to card 01166 copy 0
    And card 01020 copy 0 is facedown attached to card 01166 copy 0
    When 99 threat is removed from card 01166 copy 0
    Then card 01006 copy 0 is in seat 1's hand
    And card 01020 copy 0 is in seat 2's hand
    And card 01166 copy 0 is faceup on top of the encounter discard pile
