@core
Feature: Core character calculations
  Printed values and active modifiers combine into the public characteristics
  used by the rules engine.

  @behavior:card:01039:you-get-1-hit-point
  @covers:behavior:rr:maximum-hit-points:published-result
  @covers:behavior:rr:sustained-damage.1:published-result
  @card:01039 @rr:maximum-hit-points @rr:sustained-damage.1
  Scenario: Rocket Boots increases maximum hit points before sustained damage
    # Maximum hit points are the printed value plus active "gets" modifiers.
    # Sustained damage is then subtracted from that maximum: Tony Stark's
    # printed 9, plus Rocket Boots' 1, minus 2 sustained damage leaves 8.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 806  |
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And card 01029b copy 0 has 2 damage
    When the dealt Core scene is inspected
    Then card 01029b copy 0 has 8 remaining hit points
    And card 01039 copy 0 remains attached to seat 1's identity

  @behavior:rr:attachment.1:published-result
  @rr:attachment.1 @card:01141
  Scenario: A character attachment modifies the attached villain's scheme value
    # Program Transmitter is attached to Ultron and prints SCH +1. Ultron's
    # printed SCH 1 plus the attachment and two boost icons places four threat.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 808  |
    And card 01141 copy 0 is attached to card 01134 copy 0
    And card 01137b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01143     | 0    |
    When the villain schemes against seat 1 with every optional choice declined
    Then card 01137b copy 0 has 4 threat counters

  @behavior:rr:modifiers.6.1:published-result
  @covers:behavior:rr:modifiers:published-result
  @rr:modifiers @rr:modifiers.6.1 @card:01099
  Scenario: A statistic reverts when the modifier granting it expires
    # When an active statistic modifier expires, the statistic "reverts to the
    # value it would have without the modifier." Charge gives Rhino +3 ATK for
    # the first attack and discards itself before the second attack.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 814  |
    And seat 1 shows identity face 01001a
    And card 01099 copy 0 is attached to card 01094 copy 0
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01105     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 5 damage
    And card 01099 copy 0 is faceup on top of the encounter discard pile
    When the villain attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 7 damage

  @behavior:rr:lasting-effects.4:published-result
  @covers:behavior:rr:lasting-effects.3:published-result
  @covers:behavior:card:01070:choose-player
  @covers:behavior:card:01070:each-character-that-player-controls-gets-1
  @rr:lasting-effects.3 @rr:lasting-effects.4 @card:01070
  Scenario: A character entering after Lead from the Front receives its lasting bonus
    # A card that enters play after a lasting effect is created "is still
    # affected by that lasting effect." Spider-Woman enters after the chosen
    # player receives Lead from the Front's phase-long +1 ATK and +1 THW.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 815  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01070 | 0    |
      | 01088 | 0    |
      | 01011 | 0    |
      | 01014 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is in alter-ego form
    When seat 1 takes their voluntary form change
    Then seat 1 changed from alter-ego to hero form
    When seat 1 initiates card 01070 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01010a copy 0 is offered by the pending action
    When seat 1 chooses card 01010a copy 0 for the pending action
    Then card 01070 copy 0 is in seat 1's discard pile
    When seat 1 plays card 01011 copy 0 paying with these cards
      | card  | copy |
      | 01014 | 0    |
    Then card 01011 copy 0 remains an ally controlled by seat 1
    When card 01011 copy 0 uses its basic attack against card 01094 copy 0
    Then card 01094 copy 0 has 3 damage
    When the player phase ends
    Then card 01010a copy 0 is ready
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01094 copy 0 has 5 damage

  @behavior:card:01039:exhaust-rocket-boots-and-spend-mental-resource
  @covers:behavior:rr:mental-resource.2:published-result
  @covers:behavior:rr:lasting-effects.1:published-result
  @covers:behavior:rr:lasting-effects.2:published-result
  @covers:behavior:rr:lasting-effects.5:published-result
  @covers:behavior:rr:gains:published-result
  @covers:behavior:rr:end-of-player-phase.step.4:published-result
  @covers:behavior:rr:form-change-form.2:retains-lasting-effects
  @card:01039 @rr:mental-resource.2
  @rr:lasting-effects.1 @rr:lasting-effects.2 @rr:lasting-effects.5
  @rr:end-of-player-phase.step.4
  @rr:gains
  @rr:form-change-form.2
  Scenario: Rocket Boots spends mental and grants Aerial after its action resolves
    # The mental resource pays the printed ability cost. Its Aerial grant is a
    # lasting effect, so it remains active after Rocket Boots finishes resolving.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 809  |
    And seat 1 shows identity face 01029a
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When seat 1 initiates card 01039 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01039 copy 0 is exhausted
    And card 01089 copy 0 is in seat 1's discard pile
    And card 01029a copy 0 has the AERIAL trait
    When seat 1 changes form by flipping their identity
    Then seat 1 changed from hero to alter-ego form
    And card 01029b copy 0 has the AERIAL trait
    When the player phase ends
    Then card 01029b copy 0 does not have the AERIAL trait
