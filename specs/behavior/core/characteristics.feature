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
