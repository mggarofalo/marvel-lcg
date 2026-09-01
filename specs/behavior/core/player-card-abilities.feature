@core
Feature: Core player card abilities
  Player cards resolve their printed Actions and constant modifiers from legal
  Core deals, with targets and resulting zones recorded in the transcript.

  @behavior:card:01035:exhaust-arc-reactor-ready-iron-man
  @card:01035
  Scenario: Arc Reactor exhausts to ready Iron Man
    # "Hero Action: Exhaust Arc Reactor → ready Iron Man."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 851  |
    And seat 1 shows identity face 01029a
    And card 01035 copy 0 is an upgrade attached to seat 1's identity
    And card 01029a copy 0 is exhausted
    When seat 1 initiates card 01035 copy 0's action without payment
    Then card 01035 copy 0 is exhausted
    And card 01029a copy 0 is ready

  @behavior:card:01036:you-get-6-hit-points
  @card:01036
  Scenario: Mark V Armor grants Iron Man six hit points
    # "You get +6 hit points." Tony Stark begins with nine hit points, so the
    # controlled upgrade raises his undamaged remaining total to fifteen.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 852  |
    When card 01036 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01029b copy 0 has 15 remaining hit points

  @behavior:card:01045:exhaust-golden-city-draw-2-cards
  @card:01045
  Scenario: The Golden City exhausts to draw two cards
    # "Alter-Ego Action: Exhaust The Golden City → draw 2 cards."
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 853  |
    And card 01045 copy 0 is a support controlled by seat 1
    And seat 1's hand is empty
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01041     | 0    |
      | 01042     | 0    |
    When seat 1 initiates card 01045 copy 0's action without payment
    Then card 01045 copy 0 is exhausted
    And card 01041 copy 0 is in seat 1's hand
    And card 01042 copy 0 is in seat 1's hand

  @behavior:card:01069:ready-ally
  @card:01069
  Scenario: Get Ready readies its chosen ally
    # "Action: Ready an ally."
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 854  |
    And card 01067 copy 0 is an ally controlled by seat 1
    And card 01067 copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01069 | 0    |
    When seat 1 initiates card 01069 copy 0's action without payment
    Then card 01067 copy 0 is offered by the pending action
    When seat 1 chooses card 01067 copy 0 for the pending action
    Then card 01067 copy 0 is ready
    And card 01069 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01086:heal-2-damage-from-any-character
  @card:01086
  Scenario: First Aid heals two damage from its chosen character
    # "Action: Heal 2 damage from any character."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 855  |
    And card 01001b copy 0 has 2 damage
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01086 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01086 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01001b copy 0 is offered by the pending action
    When seat 1 chooses card 01001b copy 0 for the pending action
    Then card 01001b copy 0 has 0 damage
    And card 01086 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01020:return-hellcat-your-hand
  @card:01020
  Scenario: Hellcat returns herself to her controller's hand
    # "Action: Return Hellcat to your hand."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 856  |
    And card 01020 copy 0 is an ally controlled by seat 1
    When seat 1 initiates card 01020 copy 0's action without payment
    Then card 01020 copy 0 is in seat 1's hand

  @behavior:card:01091:exhaust-avengers-mansion-choose-player
  @covers:behavior:card:01091:that-player-draws-1-card
  @card:01091
  Scenario: Avengers Mansion draws for the chosen player
    # "Action: Exhaust Avengers Mansion → choose a player. That player draws
    # 1 card." Captain Marvel chooses Spider-Man rather than herself.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 857  |
    And card 01091 copy 0 is a support controlled by seat 1
    And seat 2's hand is empty
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01002     | 0    |
    When seat 1 initiates card 01091 copy 0's action without payment
    Then card 01001b copy 0 is offered by the pending action
    When seat 1 chooses card 01001b copy 0 for the pending action
    Then card 01091 copy 0 is exhausted
    And card 01002 copy 0 is in seat 2's hand
