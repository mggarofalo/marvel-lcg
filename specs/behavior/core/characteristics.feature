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
