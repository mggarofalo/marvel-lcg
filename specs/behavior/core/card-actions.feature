@core
Feature: Core card actions
  A player initiates a printed action, pays its costs, resolves each explicit
  choice, and observes the card's published effect.

  @behavior:card:01005:deal-8-damage-enemy
  @covers:behavior:rr:attack-player-ability-type.2:published-result
  @covers:behavior:rr:cost.3:published-result
  @covers:behavior:rr:event:published-result
  @covers:behavior:rr:initiating-abilities.step.1:published-result
  @covers:behavior:rr:initiating-abilities.step.3:published-result
  @covers:behavior:rr:initiating-abilities.step.5:published-result
  @covers:behavior:rr:initiating-abilities.step.6:published-result
  @covers:behavior:rr:initiating-abilities.step.7:published-result
  @covers:behavior:rr:play-put-into-play.2:published-result
  @covers:behavior:rr:player-turn.5:published-result
  @card:01005 @rr:attack-player-ability-type.2 @rr:cost.3 @rr:event
  @rr:initiating-abilities.step.1 @rr:initiating-abilities.step.3
  @rr:initiating-abilities.step.5 @rr:initiating-abilities.step.6
  @rr:initiating-abilities.step.7 @rr:play-put-into-play.2 @rr:player-turn.5
  Scenario: Swinging Web Kick pays, chooses an enemy, deals eight, and discards
    # "Hero Action (attack): Deal 8 damage to an enemy." An event is placed
    # faceup while it resolves, its resource cost is paid from hand, and after
    # the selected enemy takes eight damage the event enters its owner's pile.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 709  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01005 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01005 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01005 copy 0 is faceup in the resolving area
    And card 01094 copy 0 is offered by the pending action
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's discard pile
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 8 damage
    And card 01005 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01013:if-you-paid-for-card-using-energy-condition-met
  @covers:behavior:card:01013:deal-5-damage-enemy
  @card:01013
  Scenario: Photonic Blast draws after damage when paid with energy
    # "Deal 5 damage to an enemy. If you paid for this card using an energy
    # resource, draw 1 card."
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | captain_marvel | 710  |
    And seat 1 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01013 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
    When seat 1 initiates card 01013 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And seat 1 has 1 card in hand
    And card 01013 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01013:if-you-paid-for-card-using-energy-condition-not-met
  @covers:behavior:card:01013:deal-5-damage-enemy
  @card:01013
  Scenario: Photonic Blast does not draw when paid without energy
    # The conditional draw occurs only when an energy resource paid for the
    # event; physical and mental resources still pay its cost but not its rider.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 711  |
    And seat 1 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01013 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
    When seat 1 initiates card 01013 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And seat 1 has 0 cards in hand
    And card 01013 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01022:deal-1-damage-each-enemy
  @card:01022
  Scenario: Ground Stomp deals one damage to every enemy
    # "Hero Action: Deal 1 damage to each enemy." The singular action changes
    # both the villain and every engaged minion without a target prompt.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 712  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01022 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01022 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01094 copy 0 has 1 damage
    And card 01101 copy 0 has 1 damage
    And card 01022 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01054:deal-5-damage-enemy
  @card:01054
  Scenario: Uppercut deals five damage to its chosen enemy
    # "Hero Action (attack): Deal 5 damage to an enemy."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 713  |
    And seat 1 shows identity face 01019a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01054 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01054 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And card 01054 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01087:deal-3-damage-enemy
  @card:01087
  Scenario: Haymaker deals three damage to its chosen enemy
    # "Hero Action (attack): Deal 3 damage to an enemy."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 714  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01087 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01087 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 3 damage
    And card 01087 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01056:uses-3-attack-counters
  @covers:behavior:card:01056:enters-play-with-3-counters
  @covers:behavior:card:01056:when-those-are-gone-discard-card
  @covers:behavior:card:01056:exhaust-tac-team-and-remove-1-attack
  @covers:behavior:rr:uses-x-type:published-result
  @covers:behavior:rr:uses-x-type.1:published-result
  @card:01056 @rr:uses-x-type @rr:uses-x-type.1
  Scenario: Tac Team enters with three uses and discards after the third action
    # "Uses (3 attack counters)" places three counters as Tac Team enters play.
    # Each action exhausts it, spends exactly one counter, and deals two damage;
    # when the third counter is gone the support is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 715  |
    When card 01056 copy 0 enters play as a support controlled by seat 1
    Then card 01056 copy 0 has 3 attack counters
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 2 damage
    And card 01056 copy 0 has 2 attack counters
    And card 01056 copy 0 is exhausted
    When the end-of-player-phase ready step resolves
    Then card 01056 copy 0 is ready
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 4 damage
    And card 01056 copy 0 has 1 attack counter
    When the end-of-player-phase ready step resolves
    Then card 01056 copy 0 is ready
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 6 damage
    And card 01056 copy 0 is faceup on top of seat 1's discard pile
