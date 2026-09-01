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

  @behavior:card:01010b:choose-player-draw-1-card
  @covers:behavior:card:01010b:limit-once-per-round-within-limit
  @covers:behavior:card:01010b:limit-once-per-round-limit-reached
  @card:01010b @rr:limit
  Scenario: Commander draws for the chosen player and is then spent for the round
    # "Action: Choose a player to draw 1 card. (Limit once per round.)"
    # Carol chooses Spider-Man rather than herself. After that player's draw,
    # the same printed action is absent for the remainder of the round.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 844  |
    And seat 1 shows identity face 01010b
    And seat 2's hand is empty
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01003     | 0    |
    When seat 1 asks for available card actions
    Then card 01010b copy 0's action is available
    When seat 1 initiates card 01010b copy 0's action without payment
    Then card 01001b copy 0 is offered by the pending action
    When seat 1 chooses card 01001b copy 0 for the pending action
    Then seat 2 has 1 card in hand
    When seat 1 asks for available card actions
    Then card 01010b copy 0's action is unavailable

  @behavior:card:01029a:you-get-1-hand-size-for-each-zero
  @covers:behavior:card:01029a:you-get-1-hand-size-for-each-one
  @covers:behavior:card:01029a:you-get-1-hand-size-for-each-multiple
  @card:01029a @card:01035 @card:01036 @card:01037
  Scenario: Iron Man hand size counts Tech upgrades and stops at seven
    # "You get +1 hand size for each Tech upgrade you control (to a maximum
    # hand size of 7)." With none his printed hand size is one; one Tech makes
    # it two, and enough Tech upgrades stop at the stated maximum.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 845  |
    When seat 1 changes form by flipping their identity
    Then card 01029a copy 0 has modified HS 1
    When card 01035 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 2
    When card 01036 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 3
    When card 01037 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 4
    When card 01038 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 5
    When card 01038 copy 1 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 6
    When card 01039 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 7
    When card 01039 copy 1 enters play as an upgrade controlled by seat 1
    Then card 01029a copy 0 has modified HS 7

  @behavior:card:01029b:look-at-top-3-cards-your-deck
  @covers:behavior:card:01029b:add-1-your-hand-and-discard-others
  @covers:behavior:card:01029b:limit-once-per-round-within-limit
  @covers:behavior:card:01029b:limit-once-per-round-limit-reached
  @covers:behavior:faq:01029a:published-clarification-1
  @card:01029b @rr:limit @faq:01029a
  Scenario: Futurist keeps one of the top three and is then spent for the round
    # "Look at the top 3 cards of your deck. Add 1 to your hand and discard
    # the others. (Limit once per round.)" The offer preserves deck order;
    # Tony selects the middle card and the other two enter his discard pile.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 846  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01031     | 0    |
      | 01031     | 1    |
      | 01031     | 2    |
      | 01032     | 0    |
    When seat 1 asks for available card actions
    Then card 01029b copy 0's action is available
    When seat 1 initiates card 01029b copy 0's action without payment
    Then card 01031 copy 0 is offered by the pending action
    And card 01031 copy 1 is offered by the pending action
    And card 01031 copy 2 is offered by the pending action
    When seat 1 chooses card 01031 copy 1 for the pending action
    Then card 01031 copy 1 is in seat 1's hand
    And seat 1's discard pile has these cards from top to bottom
      | card  | copy |
      | 01031 | 2    |
      | 01031 | 0    |
    And seat 1's player deck has card 01032 on top
    When seat 1 asks for available card actions
    Then card 01029b copy 0's action is unavailable

  @behavior:ruling:1ce7600f9b96309a:published-clarification
  @ruling:1ce7600f9b96309a @card:01029b @rr:player-deck.1
  Scenario: Futurist looks at only the two cards remaining before the deck resets
    # Looking does not empty the deck. Tony sees the two cards that actually
    # remain, chooses one, discards the other, and only then resets the empty
    # deck and receives the facedown encounter card.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1111 |
    And seat 1's player deck contains only these next cards
      | next card | copy |
      | 01031     | 0    |
      | 01031     | 1    |
    When seat 1 initiates card 01029b copy 0's action without payment
    Then card 01031 copy 0 is offered by the pending action
    And card 01031 copy 1 is offered by the pending action
    When seat 1 chooses card 01031 copy 0 for the pending action
    Then card 01031 copy 0 is in seat 1's hand
    And seat 1 has 1 facedown encounter card
    And card 01031 copy 1 is in seat 1's player deck
