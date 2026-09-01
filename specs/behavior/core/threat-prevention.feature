@core
Feature: Core threat prevention
  Imminent threat is changed during its interrupt window before any remaining
  amount is placed on the scheme.

  @behavior:rr:prevent.2:published-result
  @covers:behavior:card:01019b:when-threat-would-be-placed-on-scheme
  @covers:behavior:card:01019b:limit-once-per-round-within-limit
  @covers:behavior:card:01019b:limit-once-per-round-limit-reached
  @rr:prevent.2 @card:01019b
  Scenario: I Object reduces an imminent threat assignment before placement
    # "When threat is prevented, reduce the amount of threat being assigned
    # before it is placed on the scheme." Jennifer Walters prevents one of the
    # three assigned threat, so only the remaining two reach the main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 830  |
    And seat 1 shows identity face 01019b
    When 3 threat is assigned to the main scheme for seat 1 accepting "I Object!"
    Then card 01097b copy 0 has 2 threat counters
    When 2 threat begins assignment to the main scheme for seat 1
    Then no opportunity is pending
    And card 01097b copy 0 has 4 threat counters

  @behavior:rr:you-your.2:published-result
  @covers:behavior:card:01061:when-any-amount-threat-would-be-placed
  @rr:you-your.2 @card:01061
  Scenario: Great Responsibility applies damage to the resolving player's identity
    # If a card says "you" take damage, the resolving player applies that
    # damage to their identity's hit point dial. Great Responsibility replaces
    # all three assigned threat, so none is placed and Spider-Man takes three.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 831  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01061 | 0    |
    When 3 threat is assigned to the main scheme for seat 1 accepting "Great Responsibility"
    Then card 01097b copy 0 has 0 threat counters
    And card 01001a copy 0 has 3 damage
    And card 01061 copy 0 is faceup on top of seat 1's discard pile
