@core
Feature: A player deck empties while cards are being drawn
  The player remains in the game, rebuilds the legal deck from their discard
  pile, receives one facedown encounter card, and finishes the instructed draw.

  Background:
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 303  |

  @behavior:rr:player-deck.2:published-result @rr:player-deck.1 @rr:player-deck.2 @rr:player-elimination
  Scenario: Drawing continues through the player-deck reset
    Given seat 1's player deck contains only these next cards
      | next card |
      | 01006     |
    When seat 1 draws 2 cards
    Then seat 1 has 7 cards in hand
    And seat 1 has 33 cards in their player deck
    And seat 1 has 1 facedown encounter card
    And seat 1 is not eliminated
    And the game is unfinished
