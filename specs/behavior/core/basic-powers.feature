@core
Feature: Core basic powers
  Heroes and allies exhaust to attack or thwart, and alter-egos exhaust to
  recover. Ally consequential damage resolves after the chosen basic power.

  @behavior:rr:attack-player-ability-type.1:published-result
  @covers:behavior:rr:exhausted.1:published-result
  @covers:behavior:rr:target.2.1:published-result
  @rr:attack-player-ability-type.1 @rr:exhausted.1 @rr:target.2.1
  Scenario: A hero exhausts and deals its ATK with a basic attack
    # "A character must exhaust to use this power. This deals damage equal to
    # the character's ATK value to the enemy."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 305  |
    And seat 1 shows identity face 01001a
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01001a copy 0 is exhausted
    And card 01094 copy 0 has 2 damage

  @behavior:rr:ally.2:published-result
  @covers:behavior:rr:ally.3:published-result
  @covers:behavior:rr:ally.5:published-result
  @covers:behavior:rr:consequential-damage:published-result
  @covers:behavior:rr:damage.2:tracked-by-damage-tokens
  @covers:behavior:rr:attack-player-ability-type.step.9:published-result
  @rr:ally.2 @rr:ally.3 @rr:ally.5 @rr:consequential-damage
  @rr:attack-player-ability-type.step.9 @rr:damage.2
  Scenario: An ally attacks while its identity remains ready and takes consequential damage
    # "After an ally attacks, it takes consequential damage equal to the
    # number of consequential damage icons beneath its ATK field."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 306  |
    And card 01083 copy 0 is an ally controlled by seat 1
    When card 01083 copy 0 uses its basic attack against card 01094 copy 0
    Then card 01083 copy 0 is exhausted
    And card 01083 copy 0 has 1 damage
    And card 01094 copy 0 has 1 damage
    And card 01001b copy 0 is ready

  @behavior:rr:thwart.1:published-result
  @covers:behavior:rr:threat:published-result
  @rr:thwart.1 @rr:threat
  Scenario: A hero exhausts and removes its THW with a basic thwart
    # "A character must exhaust to use this power. This removes threat equal
    # to the character's THW value from the scheme."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 307  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 3 threat counters
    When seat 1 uses their basic thwart against card 01097b copy 0
    Then card 01001a copy 0 is exhausted
    And card 01097b copy 0 has 2 threat counters

  @behavior:rr:consequential-damage.1:published-result
  @covers:behavior:rr:ally.2:published-result
  @covers:behavior:rr:ally.3:published-result
  @covers:behavior:rr:ally.5:published-result
  @covers:behavior:rr:thwart.1:published-result
  @rr:consequential-damage.1 @rr:ally.2 @rr:ally.3 @rr:ally.5 @rr:thwart.1
  Scenario: An ally takes consequential damage after its basic thwart resolves
    # "Consequential damage is dealt to an ally after resolving abilities that
    # are triggered by the ally attacking or thwarting."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 308  |
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01097b copy 0 has 3 threat counters
    When card 01083 copy 0 uses its basic thwart against card 01097b copy 0
    Then card 01097b copy 0 has 2 threat counters
    And card 01083 copy 0 has 1 damage
    And card 01083 copy 0 is exhausted
    And card 01001b copy 0 is ready
    And a Thwart event was emitted before a Consequential_Damage event

  @behavior:rr:recover-recovery:published-result
  @covers:behavior:rr:heal:published-result
  @covers:behavior:rr:heal.1:published-result
  @rr:recover-recovery @rr:heal @rr:heal.1
  Scenario: An alter-ego exhausts and cannot heal beyond maximum hit points
    # "The player exhausts their alter-ego and heals a number of hit points
    # equal to their REC value." A heal can only reach maximum hit points.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 309  |
    And card 01001b copy 0 has 2 damage
    When seat 1 uses their basic recovery
    Then card 01001b copy 0 has 0 damage
    And card 01001b copy 0 is exhausted
