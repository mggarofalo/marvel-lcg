@core
Feature: End-of-player-phase hand and ready steps
  The legal Core scene makes each branch visible without inventing a deck,
  changing ownership, or resolving an unrelated card effect.

  Background:
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |

  @behavior:rr:end-of-player-phase.step.1:optional-at-or-below
  @rr:end-of-player-phase.step.1
  Scenario: A hand below its limit may keep every card
    # "Each player may discard any number of cards from their hand."
    Given seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
    When seat 1 keeps every card during the optional end-of-player-phase discard
    Then seat 1 has 3 cards in hand
    And the game is unfinished

  @behavior:rr:end-of-player-phase.step.1:mandatory-above-limit
  @covers:behavior:rr:hand-size:above-limit
  @rr:end-of-player-phase.step.1 @rr:hand-size
  Scenario: An overfull hand must discard to its limit
    # "[Each player] must discard down to their hand size if they have more
    # cards than their hand size."
    Given seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
      | 01005 | 0    |
      | 01006 | 0    |
      | 01007 | 0    |
      | 01008 | 0    |
    When seat 1 chooses these cards for the end-of-player-phase discard
      | card  | copy |
      | 01002 | 0    |
    Then seat 1 has 6 cards in hand
    And card 01002 copy 0 is in seat 1's discard pile

  @behavior:rr:end-of-player-phase.step.2:below-limit
  @covers:behavior:rr:hand-size:below-limit
  @covers:behavior:rr:hand-size.1:one-at-a-time
  @rr:end-of-player-phase.step.2 @rr:hand-size @rr:hand-size.1
  Scenario: A hand below its limit draws up to its current size
    # "Each player simultaneously draws up to their hand size."
    # "When drawing up to their hand size, a player draws cards one at a time,
    # checking after each card is drawn whether they are at their hand size."
    Given seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
    When the end-of-player-phase draw step resolves
    Then seat 1 has 6 cards in hand
    And 3 Draw events were emitted

  @behavior:rr:end-of-player-phase.step.2:at-limit
  @covers:behavior:rr:hand-size:at-limit
  @rr:end-of-player-phase.step.2 @rr:hand-size
  Scenario: A hand at its limit draws nothing
    # "Each player simultaneously draws up to their hand size."
    Given seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
      | 01005 | 0    |
      | 01006 | 0    |
      | 01007 | 0    |
    When the end-of-player-phase draw step resolves
    Then seat 1 has 6 cards in hand
    And 0 Draw events were emitted

  @behavior:rr:end-of-player-phase.step.3:ready-all-in-play
  @covers:behavior:rr:ready.1:instruction
  @rr:end-of-player-phase.step.3 @rr:ready.1
  Scenario: Player and encounter cards ready together
    # "Each player simultaneously readies all of their cards. Ready each
    # exhausted encounter card."
    Given card 01001a copy 0 is exhausted
    And card 01094 copy 0 is exhausted
    When the end-of-player-phase ready step resolves
    Then card 01001a copy 0 is ready
    And card 01094 copy 0 is ready
    And 2 Ready events were emitted
