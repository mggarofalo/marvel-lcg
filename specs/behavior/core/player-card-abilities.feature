@core
Feature: Core player card abilities
  Player cards resolve their printed Actions and constant modifiers from legal
  Core deals, with targets and resulting zones recorded in the transcript.

  @behavior:card:01068:choose-thw-plus-two-until-end-phase
  @covers:behavior:card:01068:limit-once-per-round-within-limit
  @covers:behavior:card:01068:limit-once-per-round-limit-reached
  @card:01068
  Scenario: Vision chooses a temporary thwart increase once per round
    # Vision spends one energy resource and chooses THW, raising his printed
    # THW 1 by two until the end of the phase. His once-per-round action is no
    # longer available after that resolution.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 877  |
    And card 01068 copy 0 is an ally controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01012 | 0    |
    When seat 1 asks for available card actions
    Then card 01068 copy 0's action is available
    When seat 1 initiates card 01068 copy 0's action paying with these cards
      | card  | copy |
      | 01012 | 0    |
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01068 copy 0 has modified THW 3
    And card 01068 copy 0 has modified ATK 2
    When seat 1 asks for available card actions
    Then card 01068 copy 0's action is unavailable

  @behavior:card:01068:choose-atk-plus-two-until-end-phase
  @card:01068
  Scenario: Vision chooses a temporary attack increase
    # Choosing ATK instead leaves Vision's printed THW 1 unchanged and raises
    # his printed ATK 2 by two until the end of the phase.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 878  |
    And card 01068 copy 0 is an ally controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01017 | 0    |
    When seat 1 initiates card 01068 copy 0's action paying with these cards
      | card  | copy |
      | 01017 | 0    |
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01068 copy 0 has modified THW 1
    And card 01068 copy 0 has modified ATK 4

  @behavior:card:01083:after-mockingbird-enters-play-stun-enemy
  @card:01083
  Scenario: Mockingbird responds to entering play by stunning an enemy
    # After Mockingbird enters play, her optional Response chooses Rhino and
    # gives that enemy a stunned status card.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 882  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01050 | 0    |
      | 01083 | 0    |
      | 01088 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 plays card 01083 copy 0 paying with these cards
      | card  | copy |
      | 01050 | 0    |
      | 01088 | 0    |
    Then seat 1 is offered the "Mockingbird" pending opportunity
    When seat 1 accepts card 01083 copy 0's pending opportunity
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 1 stunned status card
    And card 01083 copy 0 remains an ally controlled by seat 1

  @behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-not-met
  @card:01037
  Scenario: Mark V Helmet removes threat from one scheme without Aerial
    # Without the Aerial trait, the Helmet's thwart action chooses one scheme
    # and removes one threat only from that scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 884  |
    And seat 1 shows identity face 01029a
    And card 01037 copy 0 is an upgrade attached to seat 1's identity
    And card 01107 copy 0 is a side scheme in play
    And card 01097b copy 0 has 1 threat counter
    And card 01107 copy 0 has 1 threat counter
    When seat 1 initiates card 01037 copy 0's action without payment
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01037 copy 0 is exhausted
    And card 01097b copy 0 has 0 threat counters
    And card 01107 copy 0 has 1 threat counter

  @behavior:card:01037:exhaust-mark-v-helmet-remove-1-threat-condition-met
  @card:01037 @card:01039
  Scenario: Mark V Helmet removes threat from every scheme with Aerial
    # Rocket Boots grants Iron Man Aerial until the end of the phase. The
    # Helmet therefore removes one threat from both thwartable schemes.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 885  |
    And seat 1 shows identity face 01029a
    And card 01037 copy 0 is an upgrade attached to seat 1's identity
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And card 01107 copy 0 is a side scheme in play
    And card 01097b copy 0 has 1 threat counter
    And card 01107 copy 0 has 1 threat counter
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When seat 1 initiates card 01039 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01029a copy 0 has the AERIAL trait
    And card 01039 copy 0 is exhausted
    When seat 1 initiates card 01037 copy 0's action without payment
    Then card 01037 copy 0 is exhausted
    And card 01097b copy 0 has 0 threat counters
    And card 01107 copy 0 has 0 threat counters

  @behavior:card:01017:when-captain-marvel-would-take-damage-discard
  @card:01017
  Scenario: Cosmic Flight discards to prevent three imminent damage
    # Cosmic Flight's Hero Interrupt discards the upgrade before damage is
    # applied and prevents three of the five damage, leaving two to be taken.
    Given a canonical Core scene is dealt
      | campaign | heroes         | modular sets     | seed |
      | rhino    | captain_marvel | legions_of_hydra | 886  |
    And seat 1 shows identity face 01010a
    And card 01017 copy 0 is an upgrade attached to seat 1's identity
    And these cards are next on the encounter deck
      | next card | copy |
      | 01180     | 0    |
    When the villain attacks seat 1 accepting "Cosmic Flight"
    Then card 01010a copy 0 has 2 damage
    And card 01017 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01008:when-those-are-gone-discard-card
  @card:01008
  Scenario: Web-Shooter leaves play when its final web counter is spent
    # Removing the last of Web-Shooter's three uses generates its wild resource
    # and then discards the upgrade because no web counters remain.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 887  |
    And seat 1 shows identity face 01001a
    And card 01008 copy 0 is an upgrade attached to seat 1's identity
    And card 01008 copy 0 has 1 web counter
    When seat 1 uses card 01008 copy 0's resource ability
    Then card 01008 copy 0 generated G resources
    And card 01008 copy 0 is faceup on top of seat 1's discard pile

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

  @behavior:card:01015:exhaust-alpha-flight-station-choose-and-discard-condition-met
  @card:01015
  Scenario: Alpha Flight Station draws two for Carol Danvers
    # "Draw 1 card (draw 2 cards instead if you are Carol Danvers)." Carol
    # discards one card as the cost, then receives the altered two-card draw.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 858  |
    And card 01015 copy 0 is a support controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
      | 01014     | 1    |
    When seat 1 initiates card 01015 copy 0's action discarding these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01015 copy 0 is exhausted
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01014 copy 0 is in seat 1's hand
    And card 01014 copy 1 is in seat 1's hand

  @behavior:card:01026:exhaust-superhuman-law-division-and-spend-mental
  @card:01026
  Scenario: Superhuman Law Division spends mental to remove two threat
    # "Alter-Ego Action: Exhaust Superhuman Law Division and spend a [mental]
    # resource → remove 2 threat from a scheme."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 859  |
    And card 01026 copy 0 is a support controlled by seat 1
    And card 01097b copy 0 has 3 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When seat 1 initiates card 01026 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01026 copy 0 is exhausted
    And card 01097b copy 0 has 1 threat counter

  @behavior:card:01033:exhaust-pepper-potts-generate-resources-top-card
  @card:01033
  Scenario: Pepper Potts generates the top discard card's resources
    # "Resource: Exhaust Pepper Potts → generate the resources of the top card
    # in your discard pile." Energy's two printed resources produce YY.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 860  |
    And card 01033 copy 0 is a support controlled by seat 1
    And card 01088 copy 0 starts in seat 1's discard pile
    When seat 1 uses card 01033 copy 0's resource ability
    Then card 01033 copy 0 generated YY resources
    And card 01033 copy 0 is exhausted

  @behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-accepted
  @card:01006
  Scenario: Aunt May exhausts to heal Peter Parker
    # "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter Parker."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 861  |
    And card 01006 copy 0 is a support controlled by seat 1
    And card 01001b copy 0 has 4 damage
    When seat 1 initiates card 01006 copy 0's action without payment
    Then card 01006 copy 0 is exhausted
    And card 01001b copy 0 has 0 damage

  @behavior:card:01006:exhaust-aunt-may-heal-4-damage-from-declined
  @card:01006
  Scenario: Declining Aunt May leaves Peter Parker damaged
    # The unforced Action is optional. Asking for legal Actions exposes Aunt
    # May without resolving it, so neither its cost nor effect occurs.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 862  |
    And card 01006 copy 0 is a support controlled by seat 1
    And card 01001b copy 0 has 4 damage
    When seat 1 asks for available card actions
    Then card 01006 copy 0's action is available
    And card 01006 copy 0 is ready
    And card 01001b copy 0 has 4 damage

  @behavior:card:01034:exhaust-stark-tower-choose-player
  @covers:behavior:card:01034:that-player-returns-topmost-tech-upgrade-in
  @card:01034
  Scenario: Stark Tower returns the chosen player's topmost Tech upgrade
    # "That player returns the topmost Tech upgrade in their discard pile to
    # their hand." A non-Tech card remains above the selected Web-Shooter, and
    # the lower Web-Shooter remains below it.
    Given a canonical Core scene is dealt
      | campaign | heroes              | seed |
      | rhino    | iron_man,spider_man | 863  |
    And card 01034 copy 0 is a support controlled by seat 1
    And card 01008 copy 0 starts in seat 2's discard pile
    And card 01008 copy 1 starts in seat 2's discard pile
    And card 01006 copy 0 starts in seat 2's discard pile
    When seat 1 initiates card 01034 copy 0's action without payment
    Then card 01001b copy 0 is offered by the pending action
    When seat 1 chooses card 01001b copy 0 for the pending action
    Then card 01034 copy 0 is exhausted
    And card 01008 copy 1 is in seat 2's hand
    And card 01008 copy 0 is in seat 2's discard pile
    And card 01006 copy 0 is in seat 2's discard pile
