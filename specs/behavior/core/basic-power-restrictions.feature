@core
Feature: Core basic power restrictions
  Legal-target and recovery affordances expose only basic powers that can
  change the current Core game state.

  @behavior:rr:guard:published-result
  @covers:behavior:rr:guard.1:published-result
  @covers:behavior:rr:attack-player-ability-type.1.1:published-result
  @covers:behavior:rr:attack-player-ability-type.4:published-result
  @rr:guard @rr:guard.1 @rr:attack-player-ability-type.1.1
  @rr:attack-player-ability-type.4
  Scenario: Guard removes the villain but not the guarding minion from attack targets
    # "The engaged player cannot attack any villain." Hero and ally attacks can
    # otherwise target any enemy.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 310  |
    And seat 1 shows identity face 01001a
    And card 01101 copy 0 is a minion engaged with seat 1
    When seat 1 asks for their basic attack targets
    Then card 01094 copy 0 is unavailable as a target
    And card 01101 copy 0 is available as a target

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
