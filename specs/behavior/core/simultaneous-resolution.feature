@core
Feature: Simultaneous Core effects
  Effects sharing a bold timing trigger wait for the first player to choose
  their resolution order.

  @behavior:rr:simultaneous-resolution:published-result
  @covers:behavior:card:01191:surge
  @covers:behavior:card:01191:exhaust-your-identity-card
  @rr:simultaneous-resolution @rr:surge.1 @rr:when-revealed-abilities.1
  @card:01191 @card:01103
  Scenario: The first player orders Surge before a printed When Revealed effect
    # "If two or more effects with the same bold timing trigger would resolve
    # simultaneously, the first player determines the order." Exhaustion's
    # Surge and printed When Revealed effect share that trigger. Although seat
    # 2 reveals it, seat 1 chooses Surge first; Shocker is dealt before the
    # remaining printed effect exhausts Iron Man.
    Given a canonical Core scene is dealt
      | campaign     | heroes              | seed |
      | rhino_expert | captain_marvel,iron_man | 842  |
    And seat 2 shows identity face 01029a
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When card 01191 copy 0 is revealed to seat 2
    Then seat 1 is asked to choose between 2 simultaneous effects
    And seat 1 is offered the "Surge" pending opportunity
    When seat 1 accepts the "Surge" pending opportunity
    Then card 01103 copy 0 is facedown in seat 2's encounter queue
    And card 01029a copy 0 is exhausted
    And card 01191 copy 0 is faceup on top of the encounter discard pile
