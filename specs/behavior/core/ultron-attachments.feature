@core
Feature: Core Ultron attachments
  Ultron attachments enter their printed host, apply their continuous or
  forced effects, and leave play only after their printed Hero Action is paid.

  @behavior:card:01141:attach-ultron
  @covers:behavior:card:01141:exhaust-your-hero-and-spend-mental-mental
  @card:01141
  Scenario: Program Transmitter attaches to Ultron and is discarded for mental resources
    # The revealed attachment enters on Ultron. Its Hero Action exhausts the
    # hero and spends two mental resources before discarding the attachment.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 982  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When card 01141 copy 0 is revealed to seat 1
    Then card 01141 copy 0 is attached to card 01134 copy 0
    When seat 1 initiates card 01141 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01001a copy 0 is exhausted
    And card 01141 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01141:after-ultron-schemes-place-1-threat-on
  @card:01141
  Scenario: Program Transmitter adds threat to every side scheme after Ultron schemes
    # After Ultron's scheme completes, Program Transmitter places one threat
    # on each side scheme independently of the threat placed on the main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 983  |
    And seat 1 shows identity face 01001b
    And card 01141 copy 0 is attached to card 01134 copy 0
    And card 01148 copy 0 is a side scheme in play
    And card 01148 copy 0 has 1 threat counter
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain schemes against seat 1 with every optional choice declined
    Then card 01148 copy 0 has 2 threat counters

  @behavior:card:01142:attach-ultron-drones-environment
  @covers:behavior:card:01142:spend-energy-mental-physical-resources-discard-card
  @card:01142
  Scenario: Upgraded Drones attaches to the environment and is discarded for three resources
    # The attachment enters on Ultron Drones. Its Hero Action spends one of
    # each resource type and discards it without exhausting the hero.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 984  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01142 copy 0 is revealed to seat 1
    Then card 01142 copy 0 is attached to card 01140 copy 0
    When seat 1 initiates card 01142 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01001a copy 0 is ready
    And card 01142 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01142:each-facedown-drone-minion-gets-1-atk
  @card:01142
  Scenario: Upgraded Drones increases a facedown Drone's attack and hit points
    # Ultron Drones supplies the facedown Drone's base ATK and hit points of
    # one. Upgraded Drones adds one to each characteristic.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 989  |
    And card 01142 copy 0 is attached to card 01140 copy 0
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    When the printed characteristics of card 01002 copy 0 are requested
    Then card 01002 copy 0 has modified ATK 2
    And card 01002 copy 0 has 2 remaining hit points

  @behavior:card:01152:attach-villain
  @covers:behavior:card:01152:after-villain-take-damage-give-it-tough
  @card:01152
  Scenario: Vibranium Armor gives the damaged villain tough
    # The revealed Armor attaches to the villain. After one damage lands on
    # Ultron, its forced response gives him one Tough status card.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 985  |
    And seat 1 shows identity face 01001a
    When card 01152 copy 0 is revealed to seat 1
    Then card 01152 copy 0 is attached to card 01134 copy 0
    When seat 1 uses their basic attack against card 01134 copy 0
    Then card 01134 copy 0 has 2 damage
    And card 01134 copy 0 has 1 tough status card

  @behavior:card:01152:exhaust-your-hero-and-spend-physical-physical
  @card:01152
  Scenario: Vibranium Armor is discarded for physical resources
    # The Hero Action exhausts the hero and spends two physical resources.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 986  |
    And seat 1 shows identity face 01001a
    And card 01152 copy 0 is attached to card 01134 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01090 | 0    |
    When seat 1 initiates card 01152 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
    Then card 01001a copy 0 is exhausted
    And card 01152 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01153:attach-villain
  @covers:behavior:card:01153:villain-gains-retaliate-1
  @card:01153
  Scenario: Concussion Blasters gives the villain retaliate
    # The revealed Blasters attaches to Ultron. Spider-Man's basic attack deals
    # two damage, then Retaliate 1 deals one damage back to Spider-Man.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 987  |
    And seat 1 shows identity face 01001a
    When card 01153 copy 0 is revealed to seat 1
    Then card 01153 copy 0 is attached to card 01134 copy 0
    When seat 1 uses their basic attack against card 01134 copy 0
    Then card 01134 copy 0 has 2 damage
    And card 01001a copy 0 has 1 damage

  @behavior:card:01153:exhaust-your-hero-and-spend-energy-energy
  @card:01153
  Scenario: Concussion Blasters is discarded for energy resources
    # The Hero Action exhausts the hero and spends two energy resources.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 988  |
    And seat 1 shows identity face 01001a
    And card 01153 copy 0 is attached to card 01134 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    When seat 1 initiates card 01153 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01001a copy 0 is exhausted
    And card 01153 copy 0 is faceup on top of the encounter discard pile
