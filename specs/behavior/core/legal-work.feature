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
  @covers:behavior:rr:ability.7.1:published-result
  @covers:behavior:rr:may:published-result
  @covers:behavior:rr:obligation.1:published-result
  @covers:behavior:rr:obligation.4:published-result
  @covers:behavior:rr:obligation.6:published-result
  @covers:behavior:rr:reveal.4:published-result
  @covers:behavior:rr:reveal.4.1:published-result
  @covers:behavior:rr:reveal.step.1:published-result
  @covers:behavior:rr:reveal.step.2:published-result
  @covers:behavior:rr:reveal.step.3:published-result
  @card:01160 @rr:acceleration-token.2 @rr:ability.7.1 @rr:may
  @rr:obligation.1 @rr:obligation.4 @rr:obligation.6
  @rr:reveal.4 @rr:reveal.4.1 @rr:reveal.step.1 @rr:reveal.step.2
  @rr:reveal.step.3
  Scenario: Legal Work can accelerate the main scheme without changing form
    # "Card effects may instruct the players to add an acceleration token to
    # play." Legal Work says, "Give the main scheme 1 acceleration token.
    # Discard this obligation."
    Given a canonical Core scene is dealt
      | campaign | heroes              | seed |
      | rhino    | spider_man,she_hulk | 341  |
    And seat 2 shows identity face 01019a
    When card 01160 copy 0 is revealed to seat 1
    Then card 01160 copy 0 is in seat 2's play area
    When seat 2 chooses option 2 for the pending encounter-card decision
    Then seat 2 is in hero form
    When seat 2 chooses option 2 for the pending encounter-card decision
    Then the main scheme has 1 acceleration token
    And card 01160 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01160:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01160:exhaust-jennifer-walters-remove-legal-work-from
  @covers:behavior:rr:removed-from-the-game:published-result
  @covers:behavior:rr:you-your.4:published-result
  @card:01160 @rr:removed-from-the-game @rr:you-your.4
  Scenario: Legal Work can exhaust Jennifer Walters and leave the game
    # Legal Work says, "You may flip to alter-ego form" and "Exhaust Jennifer
    # Walters → remove Legal Work from the game."
    Given a canonical Core scene is dealt
      | campaign | heroes              | seed |
      | rhino    | spider_man,she_hulk | 342  |
    And seat 2 shows identity face 01019a
    When card 01160 copy 0 is revealed to seat 1
    Then card 01160 copy 0 is in seat 2's play area
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then seat 2 is in alter-ego form
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then card 01019b copy 0 is exhausted
    And card 01160 copy 0 is removed from the game

  @behavior:card:01155:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01155:give-t-challa-player
  @covers:behavior:card:01155:printed-effect-01
  @covers:behavior:card:01155:exhaust-t-challa-remove-affairs-state-from
  @card:01155
  Scenario: Affairs of State can exhaust T'Challa and leave the game
    # Affairs of State says to give it to the T'Challa player, permits that
    # player to flip, and offers “Exhaust T'Challa → remove Affairs of State
    # from the game.”
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,black_panther  | 862  |
    And seat 2 shows identity face 01040a
    When card 01155 copy 0 is revealed to seat 1
    Then card 01155 copy 0 is in seat 2's play area
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then seat 2 is in alter-ego form
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then card 01040b copy 0 is exhausted
    And card 01155 copy 0 is removed from the game

  @behavior:card:01155:you-may-flip-alter-ego-form-declined
  @covers:behavior:card:01155:choose-and-discard-black-panther-upgrade-you
  @covers:behavior:card:01155:discard-obligation
  @card:01155
  Scenario: Affairs of State can discard a controlled Black Panther upgrade
    # Declining the optional form change leaves T'Challa in hero form. The
    # second printed choice discards the selected controlled Black Panther
    # upgrade, then discards Affairs of State.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 863  |
    And seat 1 shows identity face 01040a
    When card 01046 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01046 copy 0 is in seat 1's play area
    When card 01155 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01046 copy 0 is offered by the pending action
    When seat 1 chooses card 01046 copy 0 for the pending action
    Then card 01046 copy 0 is in seat 1's discard pile
    And card 01155 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01165:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01165:give-peter-parker-player
  @covers:behavior:card:01165:printed-effect-01
  @covers:behavior:card:01165:exhaust-peter-parker-remove-eviction-notice-from
  @card:01165
  Scenario: Eviction Notice can exhaust Peter Parker and leave the game
    # Eviction Notice says to give it to the Peter Parker player, permits that
    # player to flip, and offers “Exhaust Peter Parker → remove Eviction Notice
    # from the game.”
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 864  |
    And seat 2 shows identity face 01001a
    When card 01165 copy 0 is revealed to seat 1
    Then card 01165 copy 0 is in seat 2's play area
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then seat 2 is in alter-ego form
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then card 01001b copy 0 is exhausted
    And card 01165 copy 0 is removed from the game

  @behavior:card:01165:you-may-flip-alter-ego-form-declined
  @covers:behavior:card:01165:discard-1-card-at-random-from-your
  @covers:behavior:card:01165:card-gains-surge
  @covers:behavior:card:01165:discard-obligation
  @card:01165
  Scenario: Eviction Notice discards from hand and gains surge
    # With exactly one card in hand, the random discard has one deterministic
    # result. The chosen consequence also gives Eviction Notice surge before
    # the obligation is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 865  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01165 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01002 copy 0 is in seat 1's discard pile
    And card 01101 copy 0 is facedown in seat 1's encounter queue
    And card 01165 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01170:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01170:give-tony-stark-player
  @covers:behavior:card:01170:printed-effect-01
  @covers:behavior:card:01170:exhaust-tony-stark-remove-business-problems-from
  @card:01170
  Scenario: Business Problems can exhaust Tony Stark and leave the game
    # Business Problems says to give it to the Tony Stark player, permits that
    # player to flip, and offers “Exhaust Tony Stark → remove Business Problems
    # from the game.”
    Given a canonical Core scene is dealt
      | campaign | heroes                  | seed |
      | rhino    | spider_man,iron_man     | 866  |
    And seat 2 shows identity face 01029a
    When card 01170 copy 0 is revealed to seat 1
    Then card 01170 copy 0 is in seat 2's play area
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then seat 2 is in alter-ego form
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then card 01029b copy 0 is exhausted
    And card 01170 copy 0 is removed from the game

  @behavior:card:01170:you-may-flip-alter-ego-form-declined
  @covers:behavior:card:01170:exhaust-each-upgrade-you-control
  @covers:behavior:card:01170:discard-obligation
  @card:01170
  Scenario: Business Problems exhausts every controlled upgrade
    # Declining the optional form change leaves Iron Man in hero form. The
    # second printed choice exhausts each upgrade he controls before Business
    # Problems is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 867  |
    And seat 1 shows identity face 01029a
    When card 01035 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01035 copy 0 is in seat 1's play area
    When card 01036 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01036 copy 0 is in seat 1's play area
    When card 01170 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01035 copy 0 is exhausted
    And card 01036 copy 0 is exhausted
    And card 01170 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01175:you-may-flip-alter-ego-form-accepted
  @covers:behavior:card:01175:give-carol-danvers-player
  @covers:behavior:card:01175:printed-effect-01
  @covers:behavior:card:01175:exhaust-carol-danvers-remove-family-emergency-from
  @card:01175
  Scenario: Family Emergency can exhaust Carol Danvers and leave the game
    # Family Emergency says to give it to the Carol Danvers player, permits
    # that player to flip, and offers “Exhaust Carol Danvers → remove Family
    # Emergency from the game.”
    Given a canonical Core scene is dealt
      | campaign | heroes                      | seed |
      | rhino    | spider_man,captain_marvel   | 868  |
    And seat 2 shows identity face 01010a
    When card 01175 copy 0 is revealed to seat 1
    Then card 01175 copy 0 is in seat 2's play area
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then seat 2 is in alter-ego form
    When seat 2 chooses option 1 for the pending encounter-card decision
    Then card 01010b copy 0 is exhausted
    And card 01175 copy 0 is removed from the game

  @behavior:card:01175:you-may-flip-alter-ego-form-declined
  @covers:behavior:card:01175:you-are-stunned
  @covers:behavior:card:01175:card-gains-surge
  @covers:behavior:card:01175:discard-obligation
  @card:01175
  Scenario: Family Emergency stuns Carol Danvers and gains surge
    # Declining the optional form change leaves Captain Marvel in hero form.
    # The second choice stuns her and gives the obligation surge before it is
    # discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 869  |
    And seat 1 shows identity face 01010a
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01175 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then seat 1 is in hero form
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01010a copy 0 has 1 stunned status card
    And card 01101 copy 0 is facedown in seat 1's encounter queue
    And card 01175 copy 0 is faceup on top of the encounter discard pile
