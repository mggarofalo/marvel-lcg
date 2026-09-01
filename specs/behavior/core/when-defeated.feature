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

  @behavior:rr:damage.step.6:published-result
  @covers:behavior:rr:would.1:published-result
  @covers:behavior:rr:forced.1:published-result
  @covers:behavior:card:01185:when-attached-minion-would-be-defeated-heal
  @rr:damage.step.6 @rr:would.1 @rr:forced.1 @card:01185
  Scenario: A forced would-be-defeated interrupt invalidates the defeat
    # Damage step 6 opens abilities that trigger "when [a character] would be
    # defeated." Biomechanical Upgrades heals all damage and discards itself,
    # so the minion is no longer imminently defeated.
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets      | seed |
      | rhino    | spider_man | the_doomsday_chair | 349 |
    And seat 1 shows identity face 01001a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01185 copy 0 is attached to card 01101 copy 0
    When seat 1 uses their basic attack against card 01101 copy 0
    Then card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 0 has 0 damage
    And card 01185 copy 0 is faceup on top of the encounter discard pile
