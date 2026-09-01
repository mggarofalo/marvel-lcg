@core
Feature: Core When Defeated timing
  A defeated card resolves its When Defeated ability while its hosted cards
  are still discoverable, then leaves play.

  @behavior:rr:when-defeated-abilities.2.1:published-result
  @covers:behavior:rr:when-defeated-abilities.1:published-result
  @covers:behavior:rr:when-defeated-abilities.2:published-result
  @covers:behavior:card:01166:each-player-places-random-card-from-their-multiple-players
  @covers:behavior:card:01166:return-each-facedown-card-here-its-owner
  @rr:when-defeated-abilities.1 @rr:when-defeated-abilities.2
  @rr:when-defeated-abilities.2.1 @card:01166
  Scenario: Highway Robbery returns its hosted cards before leaving play
    # "A defeated card leaves play after its When Defeated ability is
    # resolved." Highway Robbery can still find both facedown cards and return
    # each one to its owner's hand before the side scheme is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes              | seed |
      | rhino    | spider_man,she_hulk | 348  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01006 | 0    |
    And seat 2's hand contains exactly these cards
      | card  | copy |
      | 01020 | 0    |
    When card 01166 copy 0 is revealed to seat 1
    Then card 01006 copy 0 is facedown attached to card 01166 copy 0
    And card 01020 copy 0 is facedown attached to card 01166 copy 0
    When 99 threat is removed from card 01166 copy 0
    Then card 01006 copy 0 is in seat 1's hand
    And card 01020 copy 0 is in seat 2's hand
    And card 01166 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:damage.step.6:published-result
  @covers:behavior:rr:would.1:published-result
  @covers:behavior:rr:forced.1:published-result
  @covers:behavior:card:01185:when-attached-minion-would-be-defeated-heal
  @rr:damage.step.6 @rr:would.1 @rr:forced.1 @card:01185
  Scenario: A forced would-be-defeated interrupt invalidates the defeat
    # Damage step 6 opens abilities that trigger "when [a character] would be
    # defeated." Biomechanical Upgrades heals all damage and discards itself,
    # so the minion is no longer imminently defeated.
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets      | seed |
      | rhino    | spider_man | the_doomsday_chair | 349 |
    And seat 1 shows identity face 01001a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01185 copy 0 is attached to card 01101 copy 0
    When seat 1 uses their basic attack against card 01101 copy 0
    Then card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 0 has 0 damage
    And card 01185 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:damage.step.7:published-result
  @covers:behavior:card:01182:deal-engaged-player-encounter-card
  @covers:behavior:card:01182:guard
  @covers:behavior:card:01182:while-minion-is-engaged-with-you-you
  @covers:behavior:ruling:2c98d8b065c9e8b0:published-clarification
  @rr:damage.step.7 @card:01182 @ruling:2c98d8b065c9e8b0
  Scenario: A defeated minion resolves its When Defeated ability before discard
    # Damage step 7 resolves abilities that trigger "when [a character] is
    # defeated" before step 8 discards that character. Hydra Soldier therefore
    # deals its engaged player an encounter card before leaving play.
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets    | seed |
      | rhino    | spider_man | legions_of_hydra | 350 |
    And seat 1 shows identity face 01001a
    And card 01182 copy 0 is a minion engaged with seat 1
    And card 01182 copy 0 has 2 damage
    And the encounter deck contains only these next cards with all other deck cards in the encounter discard pile
      | next card | copy |
      | 01180     | 0    |
    When seat 1 asks for their basic attack targets
    Then card 01094 copy 0 is unavailable as a target
    And card 01182 copy 0 is available as a target
    When seat 1 uses their basic attack against card 01182 copy 0
    Then card 01182 copy 0 is faceup on top of the encounter discard pile
    And seat 1 has 1 facedown encounter card
    And card 01180 copy 0 is facedown in seat 1's encounter queue
    And card 01182 copy 0 is faceup on top of the encounter discard pile
    And the main scheme has 1 acceleration token

  @behavior:rr:damage.step.9:published-result
  @covers:behavior:card:01052:after-your-hero-attacks-and-defeats-enemy
  @covers:behavior:rr:you-your.6:published-result
  @rr:damage.step.9 @rr:you-your.6 @card:01052
  Scenario: A response observes the enemy defeated by the completed attack
    # Damage step 9 opens abilities that trigger "after [a character]
    # defeats". Chase Them Down responds after the attack defeats the minion
    # and removes two threat from the chosen scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | she_hulk | 351  |
    And seat 1 shows identity face 01019a
    And card 01097b copy 0 has 3 threat counters
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01052 | 0    |
      | 01088 | 0    |
    When seat 1 uses their basic attack against card 01101 copy 0 and accepts "Chase Them Down" targeting card 01097b copy 0 paid with card 01088 copy 0
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01097b copy 0 has 1 threat counter
    And card 01052 copy 0 is in seat 1's discard pile
    And card 01088 copy 0 is in seat 1's discard pile

  @behavior:rr:you-your.15:published-result
  @rr:you-your.15 @card:01083 @card:01052
  Scenario: An ally defeating a minion is not the controlling hero defeating it
    # An ally performs its own attack. Mockingbird's defeat of the minion does
    # not satisfy Chase Them Down's "your hero attacks and defeats" condition.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 838  |
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 2 damage
    And card 01097b copy 0 has 3 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01052 | 0    |
      | 01088 | 0    |
    When card 01083 copy 0 uses its basic attack against card 01101 copy 0
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01097b copy 0 has 3 threat counters
    And card 01052 copy 0 is in seat 1's hand
