@core
Feature: Core defeat
  Reaching zero remaining hit points or zero side-scheme threat defeats the
  card, and a defeated villain stage advances without carrying excess damage.

  @behavior:rr:minion.2:published-result
  @covers:behavior:rr:defeat:published-result
  @covers:behavior:rr:defeat.1:published-result
  @covers:behavior:rr:damage.step.8:defeated-character-discarded
  @covers:behavior:rr:remaining-hit-points:published-result
  @covers:behavior:rr:remaining-hit-points.2:published-result
  @rr:minion.2 @rr:defeat @rr:defeat.1 @rr:damage.step.8
  @rr:remaining-hit-points @rr:remaining-hit-points.2
  Scenario: Exactly zero remaining hit points defeats and discards a minion
    # "If a minion has zero or fewer remaining hit points, it is defeated and
    # discarded."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 315  |
    And seat 1 shows identity face 01001a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    When seat 1 uses their basic attack against card 01101 copy 0
    Then card 01101 copy 0 has 0 remaining hit points
    And card 01101 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:side-scheme.2:published-result
  @covers:behavior:rr:defeat:published-result
  @covers:behavior:rr:defeat.1:published-result
  @covers:behavior:rr:leaves-play.1:published-result
  @covers:behavior:rr:leaves-play.2.3:published-result
  @covers:behavior:card:01107:place-additional-1-per-hero-threat-here
  @rr:side-scheme.2 @rr:defeat @rr:defeat.1
  @rr:leaves-play.1 @rr:leaves-play.2.3 @card:01107
  Scenario: Removing the final threat defeats and discards a side scheme
    # "A side scheme remains in play until there is no threat on it, which
    # causes it to be defeated and discarded."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 316  |
    And seat 1 shows identity face 01001a
    And card 01107 copy 0 is a side scheme in play
    And card 01107 copy 0 has 1 threat counter
    When seat 1 uses their basic thwart against card 01107 copy 0
    Then card 01107 copy 0 is faceup on top of the encounter discard pile
    And card 01107 copy 0 has 0 threat counters
    When card 01107 copy 0 is revealed to seat 1
    Then card 01107 copy 0 is in the villain's play area
    And card 01107 copy 0 has 3 threat counters

  @behavior:rr:villain-defeat:published-result
  @covers:behavior:rr:defeat:published-result
  @covers:behavior:rr:defeat.2:published-result
  @covers:behavior:rr:hit-points.2.2:published-result
  @covers:behavior:rr:villain-defeat.2:published-result
  @covers:behavior:rr:villain-defeat.3:published-result
  @covers:behavior:rr:villain-defeat.3.2:published-result
  @rr:villain-defeat @rr:defeat @rr:defeat.2 @rr:hit-points.2.2
  @rr:villain-defeat.2 @rr:villain-defeat.3 @rr:villain-defeat.3.2
  Scenario: Defeating a villain stage reveals the next stage without excess damage
    # "Remove the current stage of the villain deck from the game. The next
    # sequential stage ... is revealed." Excess damage does not carry over;
    # because both stages are titled Rhino, attachments carry to the new stage.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 317  |
    And seat 1 shows identity face 01001a
    And card 01094 copy 0 has 13 damage
    And card 01099 copy 0 is attached to card 01094 copy 0
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01094 copy 0 is removed from the game
    And card 01095 copy 0 is the faceup villain
    And card 01095 copy 0 has 0 damage
    And card 01099 copy 0 is attached to card 01095 copy 0
    And the game is unfinished

  @behavior:rr:winning-the-game:published-result
  @covers:behavior:card:01021:deal-x-damage-enemy
  @covers:behavior:card:01021:x-is-amount-damage-you-have-sustained
  @covers:behavior:card:01096:toughness
  @covers:behavior:card:01096:character-enter-play-with-tough-status-card
  @covers:behavior:card:01096:stun-each-hero
  @rr:winning-the-game @card:01021 @card:01096
  Scenario: Defeating the final villain stage makes the players win
    # "If the final villain stage is defeated, the players win the game."
    # Rhino II is first defeated with no excess carrying to Rhino III. Rhino
    # III stuns She-Hulk and gains Tough. Her first basic attack clears stun;
    # Tenacity readies her, and her second basic attack clears Tough. Gamma
    # Slam then deals her sustained 14 damage before the final Uppercut.
    Given a canonical Core scene is dealt
      | campaign     | heroes   | seed |
      | rhino_expert | she_hulk | 733  |
    And seat 1 shows identity face 01019a
    And card 01019a copy 0 has 14 damage
    And card 01095 copy 0 has 14 damage
    And card 01093 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01021 | 0    |
      | 01054 | 0    |
      | 01054 | 1    |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
      | 01052 | 0    |
      | 01052 | 1    |
      | 01055 | 0    |
      | 01055 | 1    |
    When seat 1 initiates card 01054 copy 0's action paying with these cards
      | card  | copy |
      | 01055 | 0    |
      | 01052 | 0    |
    Then card 01095 copy 0 is offered by the pending action
    When seat 1 chooses card 01095 copy 0 for the pending action
    Then card 01096 copy 0 is the faceup villain
    And card 01096 copy 0 has 0 damage
    And card 01096 copy 0 has 1 tough status card
    And card 01019a copy 0 has 1 stunned status card
    When seat 1 uses their basic attack against card 01096 copy 0
    Then card 01019a copy 0 is exhausted
    And card 01019a copy 0 has 0 stunned status cards
    And card 01096 copy 0 has 1 tough status card
    When seat 1 initiates card 01093 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
    Then card 01019a copy 0 is ready
    And card 01093 copy 0 is in seat 1's discard pile
    When seat 1 uses their basic attack against card 01096 copy 0
    Then card 01096 copy 0 has 0 damage
    And card 01096 copy 0 has 0 tough status cards
    When seat 1 initiates card 01021 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01096 copy 0 is offered by the pending action
    When seat 1 chooses card 01096 copy 0 for the pending action
    Then card 01096 copy 0 has 14 damage
    When seat 1 initiates card 01054 copy 1's action paying with these cards
      | card  | copy |
      | 01055 | 1    |
      | 01052 | 1    |
    Then card 01096 copy 0 is offered by the pending action
    When seat 1 chooses card 01096 copy 0 for the pending action
    Then the players win the game
