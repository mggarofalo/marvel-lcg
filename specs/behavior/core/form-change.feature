@core
Feature: Hero and alter-ego form changes
  An identity is one physical card. Flipping it changes the active face and
  does not replace the character or clear state that the Core game can retain.

  @behavior:rr:form-change-form.1:flip-identity
  @covers:behavior:rr:form-change-form:hero-or-alter-ego
  @covers:behavior:rr:identity:face-indicates-form
  @covers:behavior:rr:identity.1:starts-alter-ego
  @covers:behavior:rr:form-change-form.2:retains-damage
  @covers:behavior:rr:form-change-form.2:retains-status-cards
  @covers:behavior:rr:form-change-form.2:retains-attached-cards
  @covers:behavior:rr:form-change-form.2:retains-readiness
  @covers:behavior:rr:remaining-hit-points.1:published-result
  @rr:form-change-form.1 @rr:form-change-form @rr:identity
  @rr:form-change-form.2 @rr:identity.1 @rr:remaining-hit-points.1
  Scenario: Flipping an identity changes only its form
    # "[A player changes] form by flipping their identity card."
    # "When a player changes form, only the form changes. The character retains
    # their sustained damage, status cards, ... attached cards, ... and current
    # state (ready or exhausted)."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 305  |
    And card 01029a copy 0 has 4 damage
    And card 01029a copy 0 is exhausted
    And card 01029a copy 0 has a stunned status card
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    When seat 1 changes form by flipping their identity
    Then seat 1 is in hero form
    And seat 1 changed from alter-ego to hero form
    And card 01029a copy 0 has 4 damage
    And card 01029a copy 0 is exhausted
    And card 01029a copy 0 has a stunned status card
    And card 01039 copy 0 remains attached to seat 1's identity
    And card 01029a copy 0 has 6 remaining hit points

  @behavior:rr:form-change-form.1:voluntary-window-and-limit
  @rr:form-change-form.1
  Scenario: A voluntary form change is offered once during the player's turn
    # "Once each round, during their turn, each player is permitted to change
    # form." Taking that permission removes it from the same turn's options.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 809  |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is in alter-ego form
    When seat 1 asks whether a voluntary form change is available
    Then a voluntary form change is available
    When seat 1 takes their voluntary form change
    Then seat 1 changed from alter-ego to hero form
    When seat 1 asks whether a voluntary form change is available
    Then a voluntary form change is unavailable

  @behavior:rr:form-change-form.3:published-result
  @covers:behavior:card:01025:change-your-form-flip-your-identity-card
  @rr:form-change-form.3 @card:01025
  Scenario: A form change caused by Split Personality preserves the voluntary change
    # "If a card ability causes a player to change forms, it does not count
    # against" the voluntary form change. Split Personality flips She-Hulk.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 810  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01025 | 0    |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is in alter-ego form
    When seat 1 initiates card 01025 copy 0's action paying with these cards
      | card  | copy |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
    Then seat 1 is in hero form
    When seat 1 takes their voluntary form change
    Then seat 1 changed from hero to alter-ego form

  @behavior:rr:form-change-form.4:published-result
  @covers:behavior:card:01015:exhaust-alpha-flight-station-choose-and-discard-condition-not-met
  @covers:behavior:rr:identity.2:published-result
  @rr:form-change-form.4 @rr:identity.2 @card:01015
  Scenario: An alter-ego title does not match the identity in hero form
    # While a player is in hero form, abilities that interact with their
    # alter-ego do not interact with their identity. Captain Marvel is not
    # Carol Danvers, so Alpha Flight Station draws one card rather than two.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 811  |
    And seat 1 shows identity face 01010a
    And card 01015 copy 0 is a support controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
      | 01014     | 1    |
    When seat 1 initiates card 01015 copy 0's action discarding these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01015 copy 0 is exhausted
    And card 01088 copy 0 is in seat 1's discard pile
    And seat 1 has 1 card in hand

  @behavior:rr:form-change-form.5:published-result
  @covers:behavior:card:01017:captain-marvel-gains-aerial-trait
  @covers:behavior:rr:identity.2:published-result
  @rr:form-change-form.5 @rr:identity.2 @card:01017
  Scenario: A hero title does not match the identity in alter-ego form
    # While a player is in alter-ego form, abilities that interact with their
    # hero do not interact with their identity. Cosmic Flight grants Aerial to
    # Captain Marvel, not to the Carol Danvers face of the identity.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 812  |
    And seat 1 shows identity face 01010b
    And card 01017 copy 0 is an upgrade attached to seat 1's identity
    When the dealt Core scene is inspected
    Then card 01010b copy 0 does not have the AERIAL trait
    When seat 1 changes form by flipping their identity
    Then card 01010a copy 0 has the AERIAL trait

  @behavior:rr:form-change-form.7:published-result
  @covers:behavior:card:01009:hero-form-only
  @rr:form-change-form.7 @card:01009
  Scenario: A hero-form-only card is playable only while the hero face is active
    # Cards with "[type] form only" can only be played by a player in that
    # form. Webbed Up is not offered to Peter Parker and is offered after he
    # changes to Spider-Man.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 813  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01009 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is in alter-ego form
    When seat 1 asks whether card 01009 copy 0 is available to play
    Then card 01009 copy 0 is unavailable to play
    When seat 1 takes their voluntary form change
    Then seat 1 changed from alter-ego to hero form
    When seat 1 asks whether card 01009 copy 0 is available to play
    Then card 01009 copy 0 is available to play
