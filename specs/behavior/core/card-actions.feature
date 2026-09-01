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
