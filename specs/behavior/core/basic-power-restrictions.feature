@core
Feature: Core basic power restrictions
  Legal-target and recovery affordances expose only basic powers that can
  change the current Core game state.

  @behavior:rr:guard:published-result
  @covers:behavior:rr:guard.1:published-result
  @covers:behavior:rr:attack-player-ability-type.1.1:published-result
  @covers:behavior:rr:attack-player-ability-type.4:published-result
  @covers:behavior:rr:ability.8.2:constant-active-while-in-play
  @covers:behavior:rr:ability.9:condition-met
  @covers:behavior:rr:ability.9:condition-not-met
  @covers:behavior:rr:exhausted.2:published-result
  @covers:behavior:card:01051:after-tigra-attacks-and-defeats-minion-heal
  @covers:behavior:faq:01051:published-clarification-1
  @covers:behavior:rr:ability.step.4.b:published-result
  @covers:behavior:rr:attack-player-ability-type.step.8:published-result
  @covers:behavior:rr:in-play-and-out-of-play.12:published-result
  @covers:behavior:rr:target.3.8:published-result
  @covers:behavior:card:01101:guard
  @covers:behavior:card:01101:while-minion-is-engaged-with-you-you
  @rr:guard @rr:guard.1 @rr:attack-player-ability-type.1.1
  @rr:attack-player-ability-type.4 @rr:ability.8.2 @rr:ability.9 @rr:exhausted.2
  @card:01051 @card:01101 @faq:01051 @rr:ability.step.4.b @rr:attack-player-ability-type.step.8
  @rr:in-play-and-out-of-play.12 @rr:target.3.8
  Scenario: Guard applies only while the guarding minion remains in play
    # "While this minion is engaged with you, you cannot attack the villain."
    # The constant condition is true while Hydra Mercenary is engaged; after
    # Black Cat defeats it, the condition is false and the villain is legal.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | she_hulk | 310  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01101 copy 0 is exhausted
    And card 01051 copy 0 is an ally controlled by seat 1
    And card 01051 copy 0 has 1 damage
    When seat 1 asks for their basic attack targets
    Then card 01094 copy 0 is unavailable as a target
    And card 01101 copy 0 is available as a target
    When card 01051 copy 0 uses its basic attack against card 01101 copy 0 and accepts the Tigra opportunity
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01051 copy 0 has 1 damage
    When seat 1 asks for their basic attack targets
    Then card 01094 copy 0 is available as a target

  @behavior:rr:thwart.1.1:published-result @rr:thwart.1.1
  Scenario: A scheme with no threat is not a basic thwart target
    # "A character can only initiate a basic thwart if there is a scheme with
    # at least one threat for the character to remove."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 311  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 0 threat counters
    When seat 1 asks for their basic thwart targets
    Then card 01097b copy 0 is unavailable as a target

  @behavior:rr:recover-recovery.1:published-result @rr:recover-recovery.1
  Scenario: An undamaged alter-ego cannot perform a basic recovery
    # "An identity that has no damage to heal cannot perform a basic recovery."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 312  |
    When seat 1 asks whether basic recovery is available
    Then basic recovery is unavailable
