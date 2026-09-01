@core
Feature: Core main scheme completion
  Threat completes a main-scheme stage at its target. A following stage starts
  from its own printed state; completing the final stage loses the game.

  @behavior:rr:main-scheme-main-scheme-deck.2:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.4:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.1:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.2:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.step.3:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.5:published-result
  @covers:behavior:card:01117a:discard-cards-from-encounter-deck-until-minion
  @covers:behavior:card:01117a:put-that-minion-into-play-engaged-with
  @covers:behavior:card:01117a:advance-stage-2b
  @covers:behavior:card:01117b:if-stage-is-completed-players-lose-game-condition-not-met
  @card:01117a @card:01117b
  @rr:main-scheme-main-scheme-deck.2
  @rr:main-scheme-main-scheme-deck.4
  @rr:main-scheme-main-scheme-deck.step.1
  @rr:main-scheme-main-scheme-deck.step.2
  @rr:main-scheme-main-scheme-deck.step.3
  @rr:main-scheme-main-scheme-deck.5
  Scenario: Completing a nonfinal main scheme advances without excess threat
    # At its target, the old stage is completed and removed. The next A-side's
    # When Revealed ability resolves, then its B-side enters with only its own
    # starting threat; excess threat does not carry over.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 329  |
    And card 01116b copy 0 has 5 threat counters
    And the main scheme has 1 acceleration token
    And these cards are next on the encounter deck
      | next card | copy |
      | 01121     | 0    |
    When 3 threat is placed on card 01116b copy 0
    Then card 01116b copy 0 is removed from the game
    And card 01117b copy 0 is the faceup main scheme
    And card 01117b copy 0 has 0 threat counters
    And the main scheme has 1 acceleration token
    And card 01121 copy 0 is engaged with seat 1
    And the game is unfinished

  @behavior:card:01117b:if-stage-is-completed-players-lose-game-condition-met
  @card:01117b
  Scenario: Completing Secret Rendezvous makes the villain win
    # Secret Rendezvous is the final Klaw main-scheme stage. Reaching its
    # target of eight completes it and applies its printed loss instruction.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 950  |
    And card 01116b copy 0 has 5 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01121     | 0    |
    When 1 threat is placed on card 01116b copy 0
    Then card 01117b copy 0 is the faceup main scheme
    When 8 threat is placed on card 01117b copy 0
    Then the villain wins the game

  @behavior:rr:main-scheme-main-scheme-deck.2.1:published-result
  @covers:behavior:card:01097b:if-stage-is-completed-players-lose-game-condition-met
  @card:01097b
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

  @behavior:rr:main-scheme-main-scheme-deck.6:published-result
  @rr:main-scheme-main-scheme-deck.6
  Scenario: A main scheme cannot be discarded from play
    # "Main scheme cards cannot be discarded from play."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 344  |
    When an effect attempts to discard card 01097b copy 0
    Then card 01097b copy 0 is the faceup main scheme
    And 0 Discard events were emitted
