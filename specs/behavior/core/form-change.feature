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
