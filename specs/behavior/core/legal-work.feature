@core
Feature: Legal Work
  She-Hulk's obligation is given to her player and presents its optional form
  change before the player chooses one of its two consequences.

  @behavior:card:01160:you-may-flip-alter-ego-form-declined
  @covers:behavior:card:01160:give-jennifer-walters-player
  @covers:behavior:card:01160:printed-effect-01
  @covers:behavior:card:01160:give-main-scheme-1-acceleration-token
  @covers:behavior:card:01160:discard-obligation
  @covers:behavior:rr:acceleration-token.2:published-result
  @card:01160 @rr:acceleration-token.2
  Scenario: Legal Work can accelerate the main scheme without changing form
    # "Card effects may instruct the players to add an acceleration token to
    # play." Legal Work says, "Give the main scheme 1 acceleration token.
    # Discard this obligation."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 341  |
    And seat 1 shows identity face 01019a
    When card 01160 copy 0 is revealed to seat 1
    Then card 01160 copy 0 is in seat 1's play area
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then the main scheme has 1 acceleration token
    And card 01160 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01160:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01160:exhaust-jennifer-walters-remove-legal-work-from
  @card:01160
  Scenario: Legal Work can exhaust Jennifer Walters and leave the game
    # Legal Work says, "You may flip to alter-ego form" and "Exhaust Jennifer
    # Walters → remove Legal Work from the game."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 342  |
    And seat 1 shows identity face 01019a
    When card 01160 copy 0 is revealed to seat 1
    Then card 01160 copy 0 is in seat 1's play area
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then seat 1 is in alter-ego form
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01019b copy 0 is exhausted
    And card 01160 copy 0 is removed from the game
