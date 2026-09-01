@core
Feature: Core Standard and Expert encounter cards
  The Standard and Expert encounter sets resolve from their printed Core card
  text in legal scenario scenes.

  @behavior:card:01186:villain-schemes
  @card:01186
  Scenario: Advance makes the villain scheme
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1038 |
    And seat 1 shows identity face 01001b
    And seat 1's hand is empty
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 1    |
    When card 01186 copy 0 is revealed to seat 1
    Then card 01097b copy 0 has 1 threat counter

  @behavior:card:01187:card-gains-surge
  @card:01187
  Scenario: Assault surges in alter-ego form
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1039 |
    And seat 1 shows identity face 01001b
    When card 01187 copy 0 is revealed to seat 1
    Then seat 1 has 1 facedown encounter card

  @behavior:card:01187:villain-attacks-you
  @card:01187
  Scenario: Assault makes the villain attack in hero form
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1040 |
    And seat 1 shows identity face 01010a
    And card 01010a copy 0 is exhausted
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 1    |
    When card 01187 copy 0 is revealed to seat 1
    Then card 01010a copy 0 has 2 damage

  @behavior:card:01188:if-no-cards-were-discarded-way-card-condition-not-met
  @card:01188
  Scenario: Caught Off Guard surges when there is nothing eligible to discard
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1041 |
    And seat 1's hand is empty
    When card 01188 copy 0 is revealed to seat 1
    Then seat 1 has 1 facedown encounter card

  @behavior:faq:01036:published-clarification-1
  @faq:01036 @card:01036 @card:01188
  Scenario: Losing Mark V Armor at zero unmodified hit points defeats Iron Man
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1050 |
    And card 01036 copy 0 is an upgrade attached to seat 1's identity
    And card 01029b copy 0 has 9 damage
    When card 01188 copy 0 is revealed to seat 1
    Then card 01036 copy 0 is offered by the pending action
    When seat 1 chooses card 01036 copy 0 for the pending action
    Then seat 1 is eliminated

  @behavior:faq:01039:published-clarification-1
  @faq:01039 @card:01039 @card:01188
  Scenario: Losing Rocket Boots at zero unmodified hit points defeats Iron Man
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 1051 |
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And card 01029b copy 0 has 9 damage
    When card 01188 copy 0 is revealed to seat 1
    Then card 01039 copy 0 is offered by the pending action
    When seat 1 chooses card 01039 copy 0 for the pending action
    Then seat 1 is eliminated

  @behavior:card:01189:card-gains-surge
  @card:01189
  Scenario: Gang-Up surges in alter-ego form
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1042 |
    And seat 1 shows identity face 01001b
    When card 01189 copy 0 is revealed to seat 1
    Then seat 1 has 1 facedown encounter card

  @behavior:card:01189:villain-and-each-minion-engaged-with-you
  @card:01189
  Scenario: Gang-Up makes the villain and every engaged minion attack
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 1043 |
    And seat 1 shows identity face 01010a
    And card 01010a copy 0 is exhausted
    And card 01101 copy 0 is a minion engaged with seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 1    |
    When card 01189 copy 0 is revealed to seat 1
    Then card 01010a copy 0 has 3 damage

  @behavior:card:01190:reveal-your-set-aside-nemesis-minion-and
  @covers:behavior:card:01190:reveal-your-set-aside-nemesis-side-scheme
  @covers:behavior:card:01190:shuffle-rest-your-set-aside-nemesis-encounter
  @covers:behavior:card:01190:if-your-nemesis-minion-does-not-enter-condition-not-met
  @card:01190
  Scenario: Shadow of the Past brings Spider-Man's nemesis set into the game
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1044 |
    And seat 1 shows identity face 01001b
    And seat 1's hand is empty
    When card 01190 copy 0 is revealed to seat 1
    Then card 01167 copy 0 is engaged with seat 1
    And card 01166 copy 0 is in play
    And card 01168 copy 0 is in the encounter deck
    And card 01168 copy 1 is in the encounter deck
    And card 01169 copy 0 is in the encounter deck
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01190:if-your-nemesis-minion-does-not-enter-condition-met
  @covers:behavior:faq:01190:published-clarification-1
  @card:01190 @faq:01190
  Scenario: Shadow of the Past surges when the nemesis minion is already in play
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 1045 |
    And seat 1 shows identity face 01001b
    And card 01167 copy 0 is a minion engaged with seat 1
    And seat 1's hand is empty
    When card 01190 copy 0 is revealed to seat 1
    Then seat 1 has 1 facedown encounter card
    And card 01166 copy 0 is in play

  @behavior:card:01192:place-4-threat-on-each-side-scheme
  @covers:behavior:card:01192:if-there-are-no-side-schemes-in-condition-not-met
  @card:01192
  Scenario: Masterplan places four threat on every side scheme in play
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 1046 |
    And card 01107 copy 0 is a side scheme in play
    And card 01107 copy 0 has 1 threat counter
    When card 01192 copy 0 is revealed to seat 1
    Then card 01107 copy 0 has 5 threat counters

  @behavior:card:01192:if-there-are-no-side-schemes-in-condition-met
  @covers:behavior:card:01192:reveal-that-side-scheme
  @covers:behavior:ruling:e3c98d687be651de:published-clarification
  @card:01192 @ruling:e3c98d687be651de
  Scenario: Masterplan discards until and reveals a side scheme when none is in play
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 1047 |
    And the encounter deck contains only these next cards with all other deck cards in the encounter discard pile
      | next card | copy |
      | 01101     | 0    |
      | 01107     | 0    |
    When card 01192 copy 0 is revealed to seat 1
    Then the main scheme has 1 acceleration token
    And card 01107 copy 0 is in play

  @behavior:card:01193:surge
  @covers:behavior:card:01193:reveal-top-card-encounter-deck
  @card:01193
  Scenario: Under Fire reveals the top encounter card and surges
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 1048 |
    And seat 1 shows identity face 01001b
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
      | 01101     | 0    |
    When card 01193 copy 0 is revealed to seat 1
    Then seat 1 is asked to choose between 2 simultaneous effects
    And seat 1 is offered the "Surge" pending opportunity
    When seat 1 accepts the "Surge" pending opportunity
    Then card 01101 copy 0 is engaged with seat 1
    And card 01186 copy 0 is facedown in seat 1's encounter queue
