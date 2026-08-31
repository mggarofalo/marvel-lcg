@core
Feature: Identity defeat and player elimination
  Empty draw piles are not an elimination condition. Identity defeat is, and
  the ordered cleanup preserves the surviving Core game.

  @behavior:rr:player-elimination:published-result
  @covers:behavior:rr:defeat.2:published-result
  @covers:behavior:rr:player-elimination.step.1:published-result
  @covers:behavior:rr:player-elimination.step.2:published-result
  @covers:behavior:rr:player-elimination.step.4:published-result
  @covers:behavior:rr:player-elimination.step.5:published-result
  @covers:behavior:rr:player-elimination.6:published-result
  @rr:player-elimination @rr:defeat.2
  @rr:player-elimination.step.1 @rr:player-elimination.step.2
  @rr:player-elimination.step.4 @rr:player-elimination.step.5
  @rr:player-elimination.6
  Scenario: A defeated identity is eliminated while the other player continues
    # "A player is eliminated from the game if their identity is defeated."
    # "When a player is eliminated," pass the first-player token, move their
    # engaged minions to the next player, discard their owned cards, and remove
    # their play area; remaining players continue.
    Given a canonical Core scene is dealt
      | campaign | heroes                | seed |
      | rhino    | spider_man, iron_man | 305  |
    And card 01006 copy 0 is a support controlled by seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    When seat 1's identity is defeated
    Then seat 1 is eliminated
    And seat 2 is not eliminated
    And seat 2 has the first player token
    And card 01101 copy 0 is engaged with seat 2
    And card 01101 copy 0 has 1 damage
    And card 01001a copy 0 is removed from the game
    And card 01006 copy 0 is removed from the game
    And card 01006 copy 0 had a Discard event before an Eliminate event
    And the player order is 2
    And the per-player count is 2
    And the game is unfinished

  @behavior:rr:player-elimination.4:published-result
  @rr:player-elimination.4
  Scenario: Eliminating the last player ends the game in a loss
    # "If all players are eliminated, the game ends and the players lose."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    When seat 1's identity is defeated
    Then seat 1 is eliminated
    And the players lose the game
