@core
Feature: A player deck empties while cards are being drawn
  The player remains in the game, rebuilds the legal deck from their discard
  pile, receives one facedown encounter card, and finishes the instructed draw.

  Background:
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 303  |

  @behavior:rr:player-deck.1:empty-with-discard @rr:player-deck.1 @rr:deal-deal-an-encounter-card
  Scenario: The final draw immediately rebuilds a player deck and deals an encounter card
    # "If a player deck empties, the player shuffles their discard pile to make
    # a new deck. That player immediately deals themself one facedown encounter card."
    Given seat 1's player deck contains only these next cards
      | next card | copy |
      | 01006     | 0    |
    When seat 1 draws 1 card
    Then seat 1 has 6 cards in hand
    And seat 1 has 34 cards in their player deck
    And seat 1 has 0 cards in their discard pile
    And seat 1 has 1 facedown encounter card
    And a Shuffle event with trigger player deck empty was emitted
    And seat 1 is not eliminated

  @behavior:rr:player-deck.2:published-result @rr:player-deck.1 @rr:player-deck.2 @rr:player-elimination
  Scenario: Drawing continues through the player-deck reset
    # "The player continues to draw cards up to the specified number."
    Given seat 1's player deck contains only these next cards
      | next card | copy |
      | 01006     | 0    |
    When seat 1 draws 2 cards
    Then seat 1 has 7 cards in hand
    And seat 1 has 33 cards in their player deck
    And seat 1 has 1 facedown encounter card
    And seat 1 is not eliminated
    And the game is unfinished

  @behavior:rr:player-deck.3:published-result @rr:player-deck.1 @rr:player-deck.3
  Scenario: Discarding from a deck stops at the reset boundary
    # "No further cards are discarded from the newly shuffled deck."
    Given seat 1's player deck contains only these next cards
      | next card | copy |
      | 01006     | 0    |
    When seat 1 discards the top 5 cards of their player deck
    Then seat 1 has 35 cards in their player deck
    And seat 1 has 0 cards in their discard pile
    And seat 1 has 1 facedown encounter card
    And a Shuffle event with trigger player deck empty was emitted

  @behavior:rr:player-deck.4:published-result @rr:player-deck.4 @rr:discard.1
  Scenario: An empty deck waits for the first discarded card before resetting
    # "The deck does not reset until there is at least one card in the player's
    # discard pile, then the player deals themself one facedown encounter card."
    Given seat 1's player deck contains only these next cards with all other deck cards in hand
      | next card | copy |
      | 01006     | 0    |
    When seat 1 draws 1 card
    Then seat 1 has 0 cards in their player deck
    And seat 1 has 0 cards in their discard pile
    And seat 1 has 0 facedown encounter cards
    And seat 1 is not eliminated
    When seat 1 discards card 01006 copy 0 from hand
    Then seat 1 has 1 card in their player deck
    And seat 1 has 0 cards in their discard pile
    And seat 1 has 1 facedown encounter card
    And a Shuffle event with trigger player deck empty was emitted
