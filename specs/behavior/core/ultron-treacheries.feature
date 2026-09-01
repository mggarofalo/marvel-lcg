@core
Feature: Core Ultron treacheries
  Ultron treacheries resolve attack, scheme, healing, deck-discard, and Drone
  branches from legal states with zero, one, and multiple qualifying results.

  @behavior:card:01145:ultron-schemes
  @covers:behavior:card:01145:discard-top-card-your-deck-for-each-one
  @card:01145
  Scenario: Rage of Ultron discards one card after a one-threat scheme
    # Ultron I schemes for one with a zero-icon boost. Rage then discards one
    # player-deck card for the one threat placed by that activation.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1002 |
    And seat 1 shows identity face 01001b
    And card 01137b copy 0 has 0 threat counters
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then card 01137b copy 0 has 1 threat counter
    And card 01002 copy 0 is in seat 1's discard pile
    And card 01003 copy 0 is in seat 1's player deck

  @behavior:card:01145:discard-top-card-your-deck-for-each-zero
  @card:01145
  Scenario: Rage of Ultron discards nothing after a confused scheme
    # Confused replaces Ultron's scheme, so the activation places no threat and
    # Rage discards no player-deck cards.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1003 |
    And seat 1 shows identity face 01001b
    And card 01134 copy 0 has a confused status card
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then card 01137b copy 0 has 0 threat counters
    And card 01002 copy 0 is in seat 1's player deck

  @behavior:card:01145:discard-top-card-your-deck-for-each-multiple
  @card:01145
  Scenario: Rage of Ultron discards multiple cards after a large scheme
    # Ultron II's two scheme plus a three-icon boost places five threat, so Rage
    # discards exactly the next five player-deck cards.
    Given a canonical Core scene is dealt
      | campaign      | heroes     | seed |
      | ultron_expert | spider_man | 1004 |
    And seat 1 shows identity face 01001b
    And card 01137b copy 0 has 0 threat counters
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
      | 01004     | 0    |
      | 01005     | 0    |
      | 01006     | 0    |
      | 01007     | 0    |
      | 01008     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01151     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01137b copy 0 has 5 threat counters
    And seat 1 has 2 facedown Drone minions
    And card 01003 copy 0 is in seat 1's discard pile
    And card 01004 copy 0 is in seat 1's discard pile
    And card 01005 copy 0 is in seat 1's discard pile
    And card 01006 copy 0 is in seat 1's discard pile
    And card 01007 copy 0 is in seat 1's discard pile
    And card 01008 copy 0 is in seat 1's player deck

  @behavior:card:01145:ultron-attacks-you
  @covers:behavior:card:01145:discard-top-card-your-deck-for-each-zero-2
  @card:01145
  Scenario: Rage of Ultron discards nothing after a fully defended attack
    # Spider-Man's defense reduces Ultron I's zero-boost attack to zero, so Rage
    # discards no player-deck cards for attack damage.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1005 |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 chooses card 01001a copy 0 for the pending action
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01001a copy 0 has 0 damage
    And card 01002 copy 0 is in seat 1's player deck

  @behavior:card:01145:discard-top-card-your-deck-for-each-one-2
  @card:01145
  Scenario: Rage of Ultron discards one card after one attack damage
    # A two-icon boost makes Ultron's attack four. Spider-Man defends for three,
    # takes one damage, and Rage discards one player-deck card.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1006 |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01148     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 chooses card 01001a copy 0 for the pending action
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01001a copy 0 has 1 damage
    And card 01002 copy 0 is in seat 1's discard pile
    And card 01003 copy 0 is in seat 1's player deck

  @behavior:card:01145:discard-top-card-your-deck-for-each-multiple-2
  @card:01145
  Scenario: Rage of Ultron discards multiple cards after attack damage
    # Undefended Ultron I deals two damage with a zero-icon boost, so Rage
    # discards the next two player-deck cards.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1007 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
      | 01004     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When card 01145 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01001a copy 0 has 2 damage
    And card 01002 copy 0 is in seat 1's discard pile
    And card 01003 copy 0 is in seat 1's discard pile
    And card 01004 copy 0 is in seat 1's player deck

  @behavior:card:01146:ultron-heals-2-damage-for-each-drone-zero
  @covers:behavior:card:01146:if-no-damage-was-healed-way-card-condition-met
  @card:01146
  Scenario: Drone's Command surges when no Drone can heal Ultron
    # With no Drone engaged with the revealing player, Ultron heals zero. The
    # treachery therefore gains surge and deals the next encounter card facedown.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1008 |
    And seat 1 has no facedown Drone minions
    And card 01134 copy 0 has 4 damage
    And these cards are next on the encounter deck
      | next card | copy |
      | 01148     | 0    |
    When card 01146 copy 0 is revealed to seat 1
    Then card 01134 copy 0 has 4 damage
    And card 01148 copy 0 is facedown in seat 1's encounter queue

  @behavior:card:01146:ultron-heals-2-damage-for-each-drone-one
  @covers:behavior:card:01146:if-no-damage-was-healed-way-card-condition-not-met
  @card:01146
  Scenario: Drone's Command heals two for one engaged Drone
    # One engaged Drone heals two damage from Ultron, so the surge condition is false.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1009 |
    And card 01134 copy 0 has 4 damage
    When card 01146 copy 0 is revealed to seat 1
    Then card 01134 copy 0 has 2 damage

  @behavior:card:01146:ultron-heals-2-damage-for-each-drone-multiple
  @card:01146
  Scenario: Drone's Command heals for multiple engaged Drones
    # Two named Drones engaged with the player heal four total damage.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1010 |
    And seat 1 has no facedown Drone minions
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    And card 01003 copy 0 is a facedown Drone minion engaged with seat 1
    And card 01134 copy 0 has 6 damage
    When card 01146 copy 0 is revealed to seat 1
    Then card 01134 copy 0 has 2 damage

  @behavior:card:01146:ultron-heals-1-damage-for-each-drone-zero
  @card:01146
  Scenario: Drone's Command boost heals zero with no engaged Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1011 |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And card 01134 copy 0 has 3 damage
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01146     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then card 01134 copy 0 has 3 damage

  @behavior:card:01146:ultron-heals-1-damage-for-each-drone-one
  @card:01146
  Scenario: Drone's Command boost heals one for one engaged Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1012 |
    And seat 1 shows identity face 01001a
    And card 01134 copy 0 has 3 damage
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01146     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then card 01134 copy 0 has 2 damage

  @behavior:card:01146:ultron-heals-1-damage-for-each-drone-multiple
  @card:01146
  Scenario: Drone's Command boost heals for multiple engaged Drones
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1013 |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    And card 01003 copy 0 is a facedown Drone minion engaged with seat 1
    And card 01134 copy 0 has 4 damage
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01146     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then card 01134 copy 0 has 2 damage

  @behavior:card:01147:each-drone-minion-engaged-with-your-hero
  @covers:behavior:card:01147:if-no-attack-was-made-way-put-condition-not-met
  @card:01147
  Scenario: Swarm Attack makes each engaged Drone attack
    # The setup Drone and one named second Drone each reach their own defender
    # decision. Because attacks were made, Swarm Attack creates no third Drone.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1014 |
    And seat 1 shows identity face 01001a
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    And seat 1's hand is empty
    When card 01147 copy 0 is revealed to seat 1
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 declines the pending opportunity
    Then seat 1 has 2 facedown Drone minions

  @behavior:card:01147:if-no-attack-was-made-way-put-condition-met
  @card:01147
  Scenario: Swarm Attack creates a Drone when none attack
    # With no engaged Drone, no attack is made and the named top player-deck
    # card enters play facedown as one Drone.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1015 |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    When card 01147 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then seat 1 has 1 facedown Drone minion
