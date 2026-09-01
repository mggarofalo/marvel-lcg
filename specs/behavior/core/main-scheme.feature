@core
Feature: Core main scheme completion
  Threat completes a main-scheme stage at its target. A following stage starts
  from its own printed state; completing the final stage loses the game.

  @behavior:rr:main-scheme-main-scheme-deck.2:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.4:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.1:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.2:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.3:published-result
  @rr:main-scheme-main-scheme-deck.2
  @rr:main-scheme-main-scheme-deck.4
  @rr:main-scheme-main-scheme-deck.step.1
  @rr:main-scheme-main-scheme-deck.step.2
  @rr:main-scheme-main-scheme-deck.step.3
  Scenario: Completing a nonfinal main scheme advances without excess threat
    # At its target, the old stage is completed and removed. The next A-side's
    # When Revealed ability resolves, then its B-side enters with only its own
    # starting threat; excess threat does not carry over.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 329  |
    And card 01116b copy 0 has 5 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01121     | 0    |
    When 3 threat is placed on card 01116b copy 0
    Then card 01116b copy 0 is removed from the game
    And card 01117b copy 0 is the faceup main scheme
    And card 01117b copy 0 has 0 threat counters
    And card 01121 copy 0 is engaged with seat 1
    And the game is unfinished

  @behavior:rr:main-scheme-main-scheme-deck.2.1:published-result
  @rr:main-scheme-main-scheme-deck.2.1
  Scenario: Completing the final main scheme makes the villain win
    # If the villain completes the final stage of the main-scheme deck, the
    # villain wins immediately.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 330  |
    And card 01097b copy 0 has 6 threat counters
    When 1 threat is placed on card 01097b copy 0
    Then the villain wins the game
