@core
Feature: Core villain phase
  The villain phase places scheme threat, resolves the villain activation, and
  then deals and reveals encounter cards in the published order.

  @behavior:rr:villain-phase:published-result
  @covers:behavior:rr:villain-phase.step.1:published-result
  @covers:behavior:rr:villain-phase.step.2.a:published-result
  @covers:behavior:rr:villain-phase.step.3:published-result
  @covers:behavior:rr:villain-phase.step.4:published-result
  @covers:behavior:rr:activation.1:published-result
  @covers:behavior:rr:activation.3:published-result
  @covers:behavior:rr:boost-boost-icon:published-result
  @covers:behavior:rr:boost-boost-icon.5:published-result
  @covers:behavior:rr:deal-deal-an-encounter-card:villain-phase-step-three
  @covers:behavior:rr:reveal:published-result
  @covers:behavior:rr:engage:published-result
  @covers:behavior:rr:scheme-enemy-activation:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.1:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.a:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.c:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.d:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.3:published-result
  @rr:villain-phase @rr:villain-phase.step.1 @rr:villain-phase.step.2.a
  @rr:villain-phase.step.3 @rr:villain-phase.step.4 @rr:activation.1
  @rr:activation.3 @rr:boost-boost-icon @rr:boost-boost-icon.5
  @rr:deal-deal-an-encounter-card @rr:reveal @rr:engage
  @rr:scheme-enemy-activation @rr:scheme-enemy-activation.step.1
  @rr:scheme-enemy-activation.step.2 @rr:scheme-enemy-activation.step.2.a
  @rr:scheme-enemy-activation.step.2.c @rr:scheme-enemy-activation.step.2.d
  @rr:scheme-enemy-activation.step.3
  Scenario: An alter-ego receives scheme threat before its encounter card is revealed
    # Step 1 places the main scheme's acceleration threat. The villain then
    # schemes with SCH plus boost icons; only afterward is one encounter card
    # dealt and revealed.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 318  |
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 4 threat counters
    And card 01103 copy 0 is faceup on top of the encounter discard pile
    And card 01101 copy 0 is engaged with seat 1
    And seat 1 has 0 facedown encounter cards
    And a Boost event was emitted before a Reveal event

  @behavior:rr:villain-phase.step.5:published-result
  @covers:behavior:rr:villain-phase.step.2:published-result
  @covers:behavior:rr:villain-phase.step.3:published-result
  @covers:behavior:rr:villain-phase.step.4:published-result
  @covers:behavior:rr:activation.1:published-result
  @covers:behavior:rr:deal-deal-an-encounter-card:villain-phase-step-three
  @covers:behavior:rr:reveal:published-result
  @covers:behavior:rr:in-player-order:published-result
  @covers:behavior:rr:in-player-order.2:published-result
  @rr:villain-phase.step.2 @rr:villain-phase.step.3
  @rr:villain-phase.step.4 @rr:villain-phase.step.5 @rr:activation.1
  @rr:deal-deal-an-encounter-card @rr:reveal @rr:in-player-order
  @rr:in-player-order.2
  Scenario: Two players resolve in clockwise order and pass the first player token
    # Each player resolves the villain activation in player order, receives one
    # encounter card, then reveals in that order before the token passes.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 319  |
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01105     | 0    |
      | 01101     | 0    |
      | 01101     | 1    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 4 threat counters
    And card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 1 is engaged with seat 2
    And seat 1 has 0 facedown encounter cards
    And seat 2 has 0 facedown encounter cards
    And seat 2 has the first player token
