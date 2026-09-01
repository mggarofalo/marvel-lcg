@core
Feature: Player-phase turns
  A real two-player game establishes player order and crosses the phase
  boundary only after both players have completed one turn.

  Background:
    Given a canonical Core scene is dealt
      | campaign | heroes                     | seed |
      | rhino    | spider_man,captain_marvel | 305  |

  @behavior:rr:player-phase:published-result
  @covers:behavior:rr:player-turn:published-result
  @rr:player-phase @rr:player-turn
  Scenario: Each player takes one turn in player order before the phase ends
    # "During the player phase, each player (in player order) takes one turn."
    # "During their turn, a player may perform the following options, in any
    # order." Declining the turn prompt performs no option and ends that
    # player's turn; the next prompt shows who may perform those options.
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan

    When seat 1 keeps every opening-hand card at mulligan
    Then seat 2 is offered a mulligan

    When seat 2 keeps every opening-hand card at mulligan
    Then seat 1 is the active player

    When seat 1 ends their turn
    Then seat 2 is the active player

    When seat 2 ends their turn
    Then seat 1 is offered the end-of-player-phase discard
