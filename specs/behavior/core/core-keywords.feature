@core
Feature: Core triggered keywords
  Core cards exercise surge, quickstrike, and retaliate at their published
  timing points rather than treating the printed keywords as static labels.

  @behavior:card:01121:surge
  @covers:behavior:card:01121:after-card-is-revealed-reveal-1-additional
  @covers:behavior:rr:surge:published-result
  @covers:behavior:rr:surge.1:published-result
  @covers:behavior:rr:surge.2:published-result
  @card:01121 @rr:surge @rr:surge.1 @rr:surge.2
  Scenario: Surge finishes Weapons Runner before revealing the additional card
    # "After this card is revealed, reveal 1 additional encounter card." The
    # original minion enters play first, then the surged card is revealed.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 716  |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01122     | 0    |
      | 01121     | 1    |
      | 01120     | 1    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01121 copy 1 is engaged with seat 1
    And card 01120 copy 1 is engaged with seat 1
    And the Reveal events moved these cards in order
      | card  | copy |
      | 01121 | 1    |
      | 01120 | 1    |

  @behavior:card:01167:quickstrike
  @covers:behavior:card:01167:after-minion-engages-your-hero-it-attacks
  @covers:behavior:rr:quickstrike:published-result
  @covers:behavior:rr:quickstrike.1:published-result
  @card:01167 @rr:quickstrike @rr:quickstrike.1
  Scenario: Quickstrike attacks when Vulture engages a hero
    # "After this minion engages a player, if that player is in hero form,
    # this minion attacks that player."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 717  |
    And seat 1 shows identity face 01001a
    When card 01167 copy 0 enters play as a minion engaged with seat 1
    Then card 01001a copy 0 has 3 damage
    And card 01167 copy 0 is engaged with seat 1

  @behavior:card:01040a:retaliate-1
  @covers:behavior:card:01040a:after-character-is-attacked-deal-1-damage
  @covers:behavior:rr:retaliate-x:published-result
  @covers:behavior:rr:retaliate-x.1:published-result
  @covers:behavior:rr:retaliate-x.2:published-result
  @card:01040a @rr:retaliate-x @rr:retaliate-x.1 @rr:retaliate-x.2
  Scenario: Retaliate damages the attacker after Black Panther is attacked
    # "After this character is attacked, deal 1 damage to the attacking
    # character." Retaliate resolves after the enemy attack finishes.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 718  |
    And seat 1 shows identity face 01040a
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01040a copy 0 has 2 damage
    And card 01094 copy 0 has 1 damage
