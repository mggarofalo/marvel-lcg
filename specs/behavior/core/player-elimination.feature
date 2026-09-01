@core
Feature: Identity defeat and player elimination
  Empty draw piles are not an elimination condition. Identity defeat is, and
  the ordered cleanup preserves the surviving Core game.

  @behavior:rr:player-elimination:published-result
  @covers:behavior:rr:defeat.2:published-result
  @covers:behavior:rr:hit-points.2.1:published-result
  @covers:behavior:rr:player-elimination.step.1:published-result
  @covers:behavior:rr:player-elimination.step.2:published-result
  @covers:behavior:rr:player-elimination.step.4:published-result
  @covers:behavior:rr:player-elimination.step.5:published-result
  @covers:behavior:rr:player-elimination.5.1:published-result
  @covers:behavior:rr:player-elimination.6:published-result
  @covers:behavior:rr:player-s-play-area.6:published-result
  @covers:behavior:rr:per-player-icon:published-result
  @covers:behavior:rr:per-player-icon.1:published-result
  @rr:player-elimination @rr:defeat.2 @rr:hit-points.2.1
  @rr:player-elimination.5.1
  @rr:player-elimination.step.1 @rr:player-elimination.step.2
  @rr:player-elimination.step.4 @rr:player-elimination.step.5
  @rr:player-elimination.6 @rr:player-s-play-area.6
  @rr:per-player-icon @rr:per-player-icon.1
  Scenario: A defeated identity is eliminated while the other player continues
    # "A player is eliminated from the game if their identity is defeated."
    # "When a player is eliminated," pass the first-player token, move their
    # engaged minions to the next player, discard their owned cards, and remove
    # their play area; remaining players continue.
    Given a canonical Core scene is dealt
      | campaign | heroes                | seed |
      | rhino    | spider_man, iron_man | 305  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 has 6 damage
    And card 01006 copy 0 is a support controlled by seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then seat 1 is eliminated
    And seat 1's play area is removed
    And seat 2 is not eliminated
    And seat 2 has the first player token
    And card 01101 copy 0 is engaged with seat 2
    And card 01101 copy 0 has 1 damage
    And card 01001a copy 0 is removed from the game
    And card 01006 copy 0 is removed from the game
    And card 01006 copy 0 had a Discard event before an Eliminate event
    And the player order is 2
    And the per-player count is 2
    And the attack has ended
    And the game is unfinished

  @behavior:rr:player-elimination.3:published-result
  @covers:behavior:rr:player-elimination.step.3:published-result
  @rr:player-elimination.3 @rr:player-elimination.step.3
  @card:01002 @card:01074
  Scenario: A foreign-owned upgrade returns to its surviving owner's discard pile
    # For each card in the eliminated player's play area not owned by that
    # player, "place each other card in its owner's discard pile." Captain
    # Marvel owns Inspired while Spider-Man controls it with Black Cat.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 840  |
    And card 01002 copy 0 is an ally controlled by seat 2
    And card 01074 copy 0 is attached to card 01002 copy 0
    When seat 2's identity is defeated
    Then seat 2 is eliminated
    And card 01074 copy 0 is faceup on top of seat 1's discard pile
    And seat 1 is not eliminated
    And the game is unfinished

  @behavior:rr:player-elimination.5:published-result
  @covers:behavior:card:01154:deal-1-damage-each-friendly-character
  @rr:player-elimination.5 @card:01154
  Scenario: A revealed ability finishes after eliminating its resolving player
    # "If a player is eliminated partway through the resolution of an ability,
    # resolve the entire ability." Concussive Blast defeats Captain Marvel
    # first, then continues through the friendly characters still in the game.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | modular sets | seed |
      | rhino    | captain_marvel,iron_man | under_attack | 841  |
    And seat 1 shows identity face 01010a
    And seat 2 shows identity face 01029a
    And card 01010a copy 0 has 11 damage
    And card 01030 copy 0 is an ally controlled by seat 2
    When card 01154 copy 0 is revealed to seat 1
    Then seat 1 is eliminated
    And seat 2 is not eliminated
    And card 01029a copy 0 has 1 damage
    And card 01030 copy 0 has 1 damage
    And card 01154 copy 0 is removed from the game
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

  @behavior:ruling:895d2e4fcd40b0a2:published-clarification
  @ruling:895d2e4fcd40b0a2 @rr:player-elimination.step.2 @rr:villain-phase.step.2.b
  Scenario: Transferred minions attack the next player during that player's activation step
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 1126 |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    And card 01001a copy 0 has 9 damage
    And card 01010a copy 0 is exhausted
    And card 01094 copy 0 has a stunned status card
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01103 copy 0 is a minion engaged with seat 1
    And seat 1's hand is empty
    And seat 2's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 1    |
    When villain phase 1 resolves with every optional choice declined until a required decision
    Then seat 1 is asked to order 2 cards for the pending action
    When seat 1 orders these cards for the pending action
      | card  | copy |
      | 01101 | 0    |
      | 01103 | 0    |
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01101 copy 0 is engaged with seat 2
    And card 01103 copy 0 is engaged with seat 2
    And card 01010a copy 0 has 5 damage
