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
  @rr:villain-defeat @rr:defeat @rr:defeat.2 @rr:hit-points.2.2
  @rr:villain-defeat.2
  Scenario: Defeating a villain stage reveals the next stage without excess damage
    # "Remove the current stage of the villain deck from the game. The next
    # sequential stage ... is revealed." Excess damage does not carry over.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 317  |
    And seat 1 shows identity face 01001a
    And card 01094 copy 0 has 13 damage
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01094 copy 0 is removed from the game
    And card 01095 copy 0 is the faceup villain
    And card 01095 copy 0 has 0 damage
    And the game is unfinished
