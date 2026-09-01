@core
Feature: Core interrupt and response windows
  A triggering occurrence opens one ordered interrupt window before it happens
  and one ordered response window afterward.

  @behavior:rr:interrupt.2:published-result
  @covers:behavior:rr:interrupt.2.1:published-result
  @covers:behavior:rr:interrupt.1:published-result
  @covers:behavior:rr:first-player.4:published-result
  @covers:behavior:rr:triggering-condition.1.1:published-result
  @covers:behavior:card:01085:when-villain-schemes-reduce-amount-threat-placed
  @rr:interrupt.1 @rr:interrupt.2 @rr:interrupt.2.1 @rr:first-player.4
  @rr:triggering-condition.1.1 @card:01085
  Scenario: Two controlled copies interrupt one threat assignment in player order
    # Multiple copies of an Interrupt may each trigger from the same condition.
    # The first player receives the first opportunity and each seat is offered
    # only the Emergency in that player's hand. Each prevents one of three.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | she_hulk,captain_marvel | 832  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01085 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01085 | 1    |
    When 3 threat begins assignment to the main scheme for seat 1
    Then seat 1 is offered the "Emergency" pending opportunity
    And card 01085 copy 0 is offered by the pending action
    And card 01085 copy 1 is not offered by the pending action
    When seat 1 accepts card 01085 copy 0's pending opportunity
    Then seat 2 is offered the "Emergency" pending opportunity
    And card 01085 copy 1 is offered by the pending action
    When seat 2 accepts card 01085 copy 1's pending opportunity
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then no opportunity is pending
    And card 01097b copy 0 has 1 threat counters
    And card 01085 copy 0 is in seat 1's discard pile
    And card 01085 copy 1 is in seat 2's discard pile

  @behavior:rr:interrupt.4:published-result
  @rr:interrupt.4 @card:01061 @card:01085
  Scenario: A replacement closes further interrupts to the original condition
    # If an Interrupt replaces the imminent condition, no further Interrupt can
    # trigger against the original. Great Responsibility replaces all threat,
    # so seat 2 never receives an Emergency opportunity for that assignment.
    Given a canonical Core scene is dealt
      | campaign | heroes                     | seed |
      | rhino    | spider_man,captain_marvel | 833  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01061 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01085 | 1    |
    When 3 threat begins assignment to the main scheme for seat 1
    Then seat 1 is offered the "Great Responsibility" pending opportunity
    When seat 1 accepts card 01061 copy 0's pending opportunity
    Then no opportunity is pending
    And card 01097b copy 0 has 0 threat counters
    And card 01001a copy 0 has 3 damage
    And card 01085 copy 1 is in seat 2's hand

  @behavior:rr:interrupt.5:published-result
  @rr:interrupt.5 @card:01085
  Scenario: Passing closes the interrupt window for that occurrence
    # Once every player declines further Interrupts, that instance's window is
    # closed. The threat is placed and the declined Emergency remains in hand.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 834  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01085 | 0    |
    When 2 threat begins assignment to the main scheme for seat 1
    Then seat 1 is offered the "Emergency" pending opportunity
    When seat 1 declines the pending opportunity
    Then no opportunity is pending
    And card 01097b copy 0 has 2 threat counters
    And card 01085 copy 0 is in seat 1's hand

  @behavior:rr:response.2:published-result
  @covers:behavior:rr:response.2.1:published-result
  @covers:behavior:rr:triggering-condition.2:published-result
  @rr:response.2 @rr:response.2.1 @rr:triggering-condition.2 @card:01052
  Scenario: Two copies respond to the same hero attack
    # Multiple copies of a Response may each trigger from the same condition.
    # She-Hulk defeats Shocker once; each Chase Them Down resolves once and
    # removes two threat from the same scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 835  |
    And seat 1 shows identity face 01019a
    And card 01103 copy 0 is a minion engaged with seat 1
    And card 01097b copy 0 has 5 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01052 | 0    |
      | 01052 | 1    |
    When seat 1 begins their basic attack against card 01103 copy 0
    Then the pending occurrence combines WhenDamageDealt and WhenCardDefeated
    Then card 01052 copy 0 is offered by the pending action
    And card 01052 copy 1 is offered by the pending action
    When seat 1 accepts card 01052 copy 0's pending opportunity
    Then card 01052 copy 1 is offered by the pending action
    When seat 1 accepts card 01052 copy 1's pending opportunity
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then no opportunity is pending
    And card 01097b copy 0 has 1 threat counters
    And card 01052 copy 0 is in seat 1's discard pile
    And card 01052 copy 1 is in seat 1's discard pile

  @behavior:rr:triggering-condition.1:published-result
  @covers:behavior:rr:response.1:published-result
  @rr:triggering-condition.1 @rr:response.1 @card:01066
  Scenario: One Hawkeye response resolves only once for one minion entry
    # Each Response can trigger only once per occurrence. Hawkeye's controller
    # receives the opportunity, spends one arrow, and cannot spend another
    # arrow against the same minion entering play.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 836  |
    And card 01066 copy 0 is an ally controlled by seat 1
    When card 01103 copy 0 is revealed to seat 2
    Then seat 1 is offered the "Hawkeye" pending opportunity
    And card 01066 copy 0 is offered by the pending action
    When seat 1 accepts card 01066 copy 0's pending opportunity
    Then no opportunity is pending
    And card 01066 copy 0 has 3 arrow counters
    And card 01103 copy 0 has 2 damage

  @behavior:rr:response.4:published-result
  @rr:response.4 @card:01066
  Scenario: Passing closes the response window for that occurrence
    # Once every player declines further Responses, that instance's response
    # window is closed. Hawkeye keeps all four arrows and deals no damage.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 837  |
    And card 01066 copy 0 is an ally controlled by seat 1
    When card 01103 copy 0 is revealed to seat 1
    Then seat 1 is offered the "Hawkeye" pending opportunity
    When seat 1 declines the pending opportunity
    Then no opportunity is pending
    And card 01066 copy 0 has 4 arrow counters
    And card 01103 copy 0 has 0 damage
