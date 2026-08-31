@core
Feature: The encounter deck empties
  The reset timing depends on the effect that consumed the final card, but
  every successful reset places one acceleration token on the main scheme.

  Background:
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |

  @behavior:rr:encounter-deck.1:empty-with-discard @covers:behavior:rr:acceleration-token.1:published-result
  @rr:encounter-deck.1 @rr:acceleration-token.1
  Scenario: Emptying the encounter deck immediately resets it and accelerates the scheme
    # "The encounter discard pile is immediately shuffled to create a new
    # encounter deck. When this occurs, place an acceleration token."
    Given the encounter deck contains only these next cards with all other deck cards in the encounter discard pile
      | next card | copy |
      | 01186     | 0    |
    When the top 1 card of the encounter deck is discarded
    Then the encounter discard pile has 0 cards
    And the main scheme has 1 acceleration token
    And a Reset event was emitted before a Shuffle event
    And the game is unfinished

  @behavior:rr:encounter-deck.2:published-result @rr:encounter-deck.2 @rr:discard.4
  Scenario: A specified discard stops when the original encounter deck empties
    # "Do not continue the discard effect with the newly shuffled encounter deck."
    Given the encounter deck contains only these next cards with all other deck cards in the encounter discard pile
      | next card | copy |
      | 01186     | 0    |
    When the top 5 cards of the encounter deck are discarded
    Then the encounter discard pile has 0 cards
    And the main scheme has 1 acceleration token
    And the game is unfinished

  @behavior:rr:encounter-deck.3:published-result
  @covers:behavior:rr:deal-deal-an-encounter-card:ability-deal-facedown-queued
  @rr:encounter-deck.3 @rr:deal-deal-an-encounter-card
  Scenario: Dealing the final encounter card finishes after the reset
    # "That effect finishes resolving after the encounter deck has been reset."
    Given the encounter deck contains only these next cards with all other deck cards in the encounter discard pile
      | next card | copy |
      | 01186     | 0    |
    When seat 1 is dealt 1 encounter card
    Then seat 1 has 1 facedown encounter card
    And card 01186 copy 0 is facedown in seat 1's encounter queue
    And the encounter discard pile has 0 cards
    And the main scheme has 1 acceleration token
    And a Reset event was emitted before a Deal event
    And the game is unfinished

  @behavior:rr:encounter-deck.4:published-result @rr:encounter-deck.4
  Scenario: Empty encounter draw and discard piles make the players lose
    # "If this happens, the players lose."
    Given the encounter deck contains only these next cards with all other deck cards dealt facedown to seat 1
      | next card | copy |
      | 01186     | 0    |
    When seat 1 is dealt 1 encounter card
    Then the encounter deck has 0 cards
    And the encounter discard pile has 0 cards
    And the players lose the game
