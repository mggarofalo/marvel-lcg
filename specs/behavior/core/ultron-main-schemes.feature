@core
Feature: Core Ultron main schemes
  Ultron's main-scheme stages create Drones when revealed, require each player
  to resolve Assault on NORAD's villain-phase choice, and end the game only
  when the final stage reaches its target.

  @behavior:card:01137b:each-player-puts-top-card-their-deck-one-player
  @card:01137b
  Scenario: The Crimson Cowl creates one Drone in a one-player game
    # Stage 1B's setup reveal has already resolved in the canonical scene.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1016 |
    When the dealt Core scene is inspected
    Then card 01137b copy 0 is the faceup main scheme
    And seat 1 has 1 facedown Drone minion

  @behavior:card:01138a:each-player-puts-top-card-their-deck-one-player
  @covers:behavior:card:01138a:advance-stage-2b
  @card:01138a @card:01138b
  Scenario: Assault on NORAD creates one player's Drone and advances to stage 2B
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1017 |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01137b copy 0 is removed from the game
    And card 01138b copy 0 is the faceup main scheme
    And seat 1 has 2 facedown Drone minions

  @behavior:card:01138a:each-player-puts-top-card-their-deck-multiple-players
  @card:01138a @card:01138b
  Scenario: Assault on NORAD creates a Drone for every player
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1018 |
    And card 01137b copy 0 has 5 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    And seat 1 has 2 facedown Drone minions
    And seat 2 has 2 facedown Drone minions

  @behavior:card:01138b:after-placing-threat-here-during-step-one-choice-1
  @covers:behavior:card:01138b:after-placing-threat-here-during-step-one-one-player
  @card:01138b
  Scenario: One player places threat after villain phase step one
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1019 |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When villain phase 1 resolves with every optional choice declined until a required decision
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01138b copy 0 has 7 threat counters
    And seat 1 has 2 facedown Drone minions

  @behavior:card:01138b:after-placing-threat-here-during-step-one-choice-2
  @card:01138b
  Scenario: One player creates a Drone after villain phase step one
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1020 |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When villain phase 1 resolves with every optional choice declined until a required decision
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01138b copy 0 has 3 threat counters
    And seat 1 has 3 facedown Drone minions

  @behavior:card:01138b:after-placing-threat-here-during-step-one-multiple-players
  @card:01138b
  Scenario: Every player resolves an independent Assault on NORAD choice
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1021 |
    And card 01137b copy 0 has 5 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When villain phase 1 resolves with every optional choice declined until a required decision
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 1    |
      | 2    |
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then option 2 is offered by the pending decision
    When seat 2 chooses option 2 for the pending encounter-card decision
    Then card 01138b copy 0 has 8 threat counters
    And seat 1 has 2 facedown Drone minions
    And seat 2 has 3 facedown Drone minions

  @behavior:faq:01138:published-clarification-1
  @faq:01138 @card:01138b @card:01019b
  Scenario: Prevented step-one threat does not trigger Assault on NORAD
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | ultron   | she_hulk | 1024 |
    And seat 1 shows identity face 01019b
    And seat 1 has no facedown Drone minions
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01020     | 0    |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When villain phase 1 resolves accepting "I Object!"
    Then seat 1 has 1 facedown Drone minion

  @behavior:card:01139a:each-player-puts-top-card-their-deck-one-player
  @covers:behavior:card:01139a:advance-stage-3b
  @card:01139a @card:01139b
  Scenario: Countdown to Oblivion creates one player's Drone and advances to stage 3B
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1022 |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When 10 threat is placed on card 01138b copy 0
    Then card 01139b copy 0 is the faceup main scheme
    And seat 1 has 3 facedown Drone minions

  @behavior:card:01139a:each-player-puts-top-card-their-deck-multiple-players
  @card:01139a @card:01139b
  Scenario: Countdown to Oblivion creates a Drone for every player
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1023 |
    And card 01137b copy 0 has 5 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When 20 threat is placed on card 01138b copy 0
    Then card 01139b copy 0 is the faceup main scheme
    And seat 1 has 3 facedown Drone minions
    And seat 2 has 3 facedown Drone minions

  @behavior:card:01139b:threat-cannot-be-removed-from-scheme
  @covers:behavior:card:01139b:if-stage-is-completed-players-lose-game-condition-not-met
  @card:01139b
  Scenario: Countdown to Oblivion prevents threat removal below its target
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1024 |
    And seat 1 shows identity face 01001a
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When 10 threat is placed on card 01138b copy 0
    Then card 01139b copy 0 is the faceup main scheme
    When 2 threat is placed on card 01139b copy 0
    Then card 01139b copy 0 has 2 threat counters
    When seat 1 asks for their basic thwart targets
    Then card 01139b copy 0 is unavailable as a target
    And card 01139b copy 0 has 2 threat counters
    And the game is unfinished

  @behavior:card:01139b:if-stage-is-completed-players-lose-game-condition-met
  @card:01139b
  Scenario: Completing Countdown to Oblivion makes the villain win
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1025 |
    And card 01137b copy 0 has 2 threat counters
    When 1 threat is placed on card 01137b copy 0
    Then card 01138b copy 0 is the faceup main scheme
    When 10 threat is placed on card 01138b copy 0
    Then card 01139b copy 0 is the faceup main scheme
    When 5 threat is placed on card 01139b copy 0
    Then the villain wins the game
