@core
Feature: Core Ultron villain stages and Drones
  Ultron's three stages and the Ultron Drones environment resolve their
  printed attack, search, characteristic, defeat, and Guard behavior.

  @behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-1
  @card:01134
  Scenario: Ultron I can place threat after attacking
    # After Ultron's attack resolves, the attacked player chooses the first
    # printed consequence and places one threat on the main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 995  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And card 01137b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01137b copy 0 has 1 threat counter
    And seat 1 has 1 facedown Drone minion

  @behavior:card:01134:after-ultron-attacks-you-choose-either-place-choice-2
  @card:01134
  Scenario: Ultron I can create a Drone after attacking
    # The second printed consequence puts the named top player-deck card into
    # play facedown as a second Drone instead of placing main-scheme threat.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 996  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And card 01137b copy 0 has 0 threat counters
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with every optional choice declined until a required decision
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01137b copy 0 has 0 threat counters
    And seat 1 has 2 facedown Drone minions

  @behavior:ruling:5afa90a922165fcc:stage-one-defender-receives-drone
  @ruling:5afa90a922165fcc @card:01134 @rr:defend-defense.5
  Scenario: Ultron I gives his after-attack Drone to the defending player
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 1118 |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And seat 2 has no facedown Drone minions
    And card 01011 copy 0 is an ally controlled by seat 2
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01012     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with card 01011 copy 0 defending
    Then option 2 is offered by the pending decision
    When seat 2 chooses option 2 for the pending encounter-card decision
    Then seat 1 has 0 facedown Drone minions
    And card 01012 copy 0 is engaged with seat 2

  @behavior:card:01135:when-ultron-attacks-you-put-top-card
  @covers:behavior:card:01135:until-end-his-attack-ultron-gets-1-one
  @covers:behavior:faq:01135:published-clarification-2
  @card:01135 @faq:01135
  Scenario: Ultron II creates and counts one Drone for an attack
    # With no Drone initially engaged, Ultron II creates one before calculating
    # his attack and receives +1 ATK for that one Drone until the attack ends.
    Given a canonical Core scene is dealt
      | campaign      | heroes     | seed |
      | ultron_expert | spider_man | 997  |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then seat 1 has 1 facedown Drone minion
    And card 01001a copy 0 has 3 damage

  @behavior:faq:01135:published-clarification-1
  @covers:behavior:ruling:3db19283592b0b90:original-target-receives-drone
  @faq:01135 @card:01135 @ruling:3db19283592b0b90
  Scenario: Ultron II gives the Drone to the original target before defense
    Given a canonical Core scene is dealt
      | campaign      | heroes                    | seed |
      | ultron_expert | spider_man,captain_marvel | 1001 |
    And seat 1 shows identity face 01001a
    And seat 1 has no facedown Drone minions
    And seat 2 has no facedown Drone minions
    And card 01011 copy 0 is an ally controlled by seat 2
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with card 01011 copy 0 defending
    Then card 01002 copy 0 is engaged with seat 1
    And seat 2 has 0 facedown Drone minions

  @behavior:card:01135:until-end-his-attack-ultron-gets-1-multiple
  @card:01135
  Scenario: Ultron II counts multiple engaged Drones for an attack
    # The setup Drone and the Drone created by Ultron's interrupt both count,
    # giving Ultron +2 ATK until this attack ends.
    Given a canonical Core scene is dealt
      | campaign      | heroes     | seed |
      | ultron_expert | spider_man | 998  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01142     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then seat 1 has 2 facedown Drone minions
    And card 01001a copy 0 has 4 damage

  @behavior:card:01136:search-encounter-deck-and-discard-pile-for
  @covers:behavior:card:01136:then-shuffle-encounter-deck
  @covers:behavior:card:01136:each-drone-minion-gets-1-atk-and
  @covers:behavior:card:01136:ultron-cannot-take-damage-while-drone-minion
  @covers:behavior:faq:01136:published-clarification-1
  @card:01136 @faq:01136
  Scenario: Ultron III reveals his Imperative and is protected by enhanced Drones
    # Defeating Ultron II reveals stage III. It searches for Ultron's Imperative,
    # reveals it to create two more Drones, and shuffles. A named Drone has two
    # ATK and hit points, and a second hero's attack cannot damage Ultron III.
    Given a canonical Core scene is dealt
      | campaign      | heroes                    | seed |
      | ultron_expert | spider_man,captain_marvel | 999  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    And card 01135 copy 0 has 42 damage
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    When seat 1 uses their basic attack against card 01135 copy 0
    Then card 01136 copy 0 is the faceup villain
    And card 01150 copy 0 is in play
    And seat 1 has 3 facedown Drone minions
    When the printed characteristics of card 01002 copy 0 are requested
    Then card 01002 copy 0 has modified ATK 2
    And card 01002 copy 0 has 2 remaining hit points
    When seat 2 uses their basic attack against card 01136 copy 0
    Then card 01136 copy 0 has 0 damage

  @behavior:card:01140:each-facedown-drone-minion-engaged-with-player
  @covers:behavior:card:01140:after-facedown-drone-minion-is-defeated-place
  @card:01140
  Scenario: A facedown Drone has base-one characteristics and returns to its owner's discard
    # Ultron Drones gives the named facedown player card base ATK and hit points
    # of one. Spider-Man's basic attack defeats it and its owner receives it
    # faceup in their discard pile.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1000 |
    And seat 1 shows identity face 01001a
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    When the printed characteristics of card 01002 copy 0 are requested
    Then card 01002 copy 0 has modified ATK 1
    And card 01002 copy 0 has 1 remaining hit points
    When seat 1 uses their basic attack against card 01002 copy 0
    Then card 01002 copy 0 is in seat 1's discard pile

  @behavior:card:01143:guard
  @covers:behavior:card:01143:when-advanced-ultron-drone-is-defeated-engaged
  @card:01143
  Scenario: Advanced Ultron Drone guards and replaces itself with a facedown Drone
    # Guard makes Ultron unavailable while the Advanced Drone is engaged. Its
    # forced interrupt creates a facedown Drone before the defeated minion leaves.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 1001 |
    And seat 1 shows identity face 01001a
    And card 01143 copy 0 is a minion engaged with seat 1
    And card 01143 copy 0 has 3 damage
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
    When seat 1 asks for their basic attack targets
    Then card 01143 copy 0 is available as a target
    And card 01134 copy 0 is unavailable as a target
    When seat 1 uses their basic attack against card 01143 copy 0
    Then card 01143 copy 0 is faceup on top of the encounter discard pile
    And seat 1 has 2 facedown Drone minions
