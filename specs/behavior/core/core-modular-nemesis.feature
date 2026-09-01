@core
Feature: Core modular nemesis sets
  Legions of Hydra and The Doomsday Chair resolve from their printed Core text.

  @behavior:rr:first-player.1:published-result
  @covers:behavior:rr:first-player.2:published-result
  @rr:first-player.1 @rr:first-player.2 @card:01185
  Scenario: The first player breaks an encounter attachment target tie
    # Biomechanical Upgrades does not name a resolving player. Two minions
    # share the highest printed hit points, so the first player chooses its
    # target even though the encounter card was revealed to the second player.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | modular sets       | seed |
      | rhino    | spider_man,captain_marvel | the_doomsday_chair | 1110 |
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 1 is a minion engaged with seat 2
    When card 01185 copy 0 is revealed to seat 2
    Then card 01101 copy 0 is offered by the pending action
    And card 01101 copy 1 is offered by the pending action
    When seat 1 chooses card 01101 copy 1 for the pending action
    Then card 01185 copy 0 is attached to card 01101 copy 1

  @behavior:card:01180:if-madame-hydra-is-not-in-play-condition-met
  @covers:behavior:card:01180:place-2-additional-threat-here-for-each-one
  @card:01180 @card:01181
  Scenario: Legions of Hydra finds Madame Hydra before counting Hydra enemies
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1083 |
    When card 01180 copy 0 is revealed to seat 1
    Then card 01181 copy 0 is engaged with seat 1
    And card 01180 copy 0 has 5 threat counters

  @behavior:card:01180:if-madame-hydra-is-not-in-play-condition-not-met
  @covers:behavior:card:01180:place-2-additional-threat-here-for-each-multiple
  @card:01180 @card:01181 @card:01182
  Scenario: Legions of Hydra counts every Hydra enemy already in play
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1084 |
    And card 01181 copy 0 is a minion engaged with seat 1
    And card 01182 copy 0 is a minion engaged with seat 1
    And card 01182 copy 1 is a minion engaged with seat 1
    When card 01180 copy 0 is revealed to seat 1
    Then card 01181 copy 0 is engaged with seat 1
    And card 01180 copy 0 has 9 threat counters

  @behavior:card:01181:after-madame-hydra-schemes-or-attacks-place
  @card:01180 @card:01181
  Scenario: Madame Hydra places threat after attacking
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1085 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And card 01180 copy 0 is a side scheme in play
    And card 01181 copy 0 is a minion engaged with seat 1
    When card 01181 copy 0 attacks seat 1 with every optional choice declined until a required decision
    Then card 01180 copy 0 is offered by the pending action
    When seat 1 chooses card 01180 copy 0 for the pending action
    Then card 01180 copy 0 has 5 threat counters

  @behavior:ruling:3893e8921b39cf65:madame-hydra-one-legions-scheme
  @ruling:3893e8921b39cf65 @card:01180 @card:01181
  Scenario: Madame Hydra chooses one of two Legions schemes for her threat
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1117 |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And card 01180 copy 0 is a side scheme in play
    And card 01180 copy 1 is a side scheme in play
    And card 01181 copy 0 is a minion engaged with seat 1
    When card 01181 copy 0 attacks seat 1 with every optional choice declined until a required decision
    Then card 01180 copy 0 is offered by the pending action
    And card 01180 copy 1 is offered by the pending action
    When seat 1 chooses card 01180 copy 1 for the pending action
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01180 copy 0 has 3 threat counters
    And card 01180 copy 1 has 5 threat counters

  @behavior:card:01181:after-madame-hydra-schemes-or-attacks-place
  @card:01180 @card:01181
  Scenario: Madame Hydra places threat after scheming
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 1086 |
    And seat 1 shows identity face 01001b
    And card 01180 copy 0 is a side scheme in play
    And card 01181 copy 0 is a minion engaged with seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
      | 01102     | 0    |
    When villain phase 1 resolves with every optional choice declined until a required decision
    Then card 01180 copy 0 is offered by the pending action
    When seat 1 chooses card 01180 copy 0 for the pending action
    Then card 01180 copy 0 has 5 threat counters

  @behavior:card:01183:if-m-o-d-o-k-is-condition-met
  @card:01183 @card:01184
  Scenario: The Doomsday Chair finds M.O.D.O.K. outside play
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets       | seed |
      | rhino    | spider_man | the_doomsday_chair | 1087 |
    When card 01183 copy 0 is revealed to seat 1
    Then card 01184 copy 0 is engaged with seat 1

  @behavior:card:01183:if-m-o-d-o-k-is-condition-not-met
  @card:01183 @card:01184
  Scenario: The Doomsday Chair does not search when M.O.D.O.K. is in play
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets       | seed |
      | rhino    | spider_man | the_doomsday_chair | 1088 |
    And card 01184 copy 0 is a minion engaged with seat 1
    When card 01183 copy 0 is revealed to seat 1
    Then card 01184 copy 0 is engaged with seat 1

  @behavior:card:01184:retaliate-2
  @covers:behavior:card:01184:after-character-is-attacked-deal-2-damage
  @card:01184
  Scenario: M.O.D.O.K. retaliates against an attacking hero
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets       | seed |
      | rhino    | spider_man | the_doomsday_chair | 1089 |
    And seat 1 shows identity face 01001a
    And card 01184 copy 0 is a minion engaged with seat 1
    When seat 1 uses their basic attack against card 01184 copy 0
    Then card 01184 copy 0 has 2 damage
    And card 01001a copy 0 has 2 damage

  @behavior:card:01185:surge
  @covers:behavior:card:01185:attach-minion-with-highest-printed-hit-points
  @card:01184 @card:01185
  Scenario: Biomechanical Upgrades attaches to the highest printed hit points and surges
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets       | seed |
      | rhino    | spider_man | the_doomsday_chair | 1090 |
    And card 01103 copy 0 is a minion engaged with seat 1
    And card 01184 copy 0 is a minion engaged with seat 1
    When card 01185 copy 0 is revealed to seat 1
    Then card 01185 copy 0 is attached to card 01184 copy 0
    And seat 1 has 1 facedown encounter card

  @behavior:faq:01185:published-clarification-1
  @faq:01185 @card:01185 @card:01140
  Scenario: Biomechanical Upgrades can attach to a facedown Drone
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets        | seed |
      | ultron   | spider_man | the_doomsday_chair | 1092 |
    And seat 1 has no facedown Drone minions
    And card 01002 copy 0 is a facedown Drone minion engaged with seat 1
    When card 01185 copy 0 is revealed to seat 1
    Then card 01185 copy 0 is attached to card 01002 copy 0
    And seat 1 has 1 facedown encounter card
