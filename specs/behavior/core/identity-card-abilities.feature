@core
Feature: Core identity card abilities
  Identity abilities are exercised from the printed form that owns them and
  retain their printed limits for the round.

  @behavior:card:01001b:generate-mental-resource
  @covers:behavior:card:01001b:limit-once-per-round-within-limit
  @covers:behavior:card:01001b:limit-once-per-round-limit-reached
  @card:01001b @card:01086 @rr:limit @rr:resource-ability.1
  Scenario: Scientist pays one cost and is then spent for the round
    # "Resource: Generate a [mental] resource. (Limit once per round.)"
    # Peter Parker pays the first First Aid's generic cost without leaving
    # play. A second legal First Aid still has a damaged target, but Scientist
    # cannot pay it again during the same round.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 843  |
    And seat 1 shows identity face 01001b
    And card 01001b copy 0 has 4 damage
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01086 | 0    |
      | 01063 | 0    |
    When seat 1 initiates card 01086 copy 0's action paying with these cards
      | card   | copy |
      | 01001b | 0    |
    Then card 01001b copy 0 is offered by the pending action
    When seat 1 chooses card 01001b copy 0 for the pending action
    Then card 01001b copy 0 has 2 damage
    And card 01001b copy 0 is in play
    When seat 1 asks whether card 01063 copy 0 is available to play
    Then card 01063 copy 0 is unavailable to play
