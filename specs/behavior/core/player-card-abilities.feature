@core
Feature: Core player card abilities
  Player cards resolve their printed Actions and constant modifiers from legal
  Core deals, with targets and resulting zones recorded in the transcript.

  @behavior:card:01007:attach-minion
  @covers:behavior:card:01007:when-attached-minion-is-defeated-remove-3
  @card:01007
  Scenario: Spider-Tracer removes threat when its attached minion is defeated
    # Spider-Tracer is played attached to Hydra Mercenary. When Spider-Man's
    # basic attack defeats that minion, the Forced Interrupt chooses the main
    # scheme and removes three threat before the host leaves play.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 920  |
    And seat 1 shows identity face 01001a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01097b copy 0 has 3 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01007 | 0    |
      | 01085 | 0    |
    When seat 1 plays card 01007 copy 0 targeting card 01101 copy 0 paying with these cards
      | card  | copy |
      | 01085 | 0    |
    Then card 01007 copy 0 is attached to card 01101 copy 0
    When seat 1 begins their basic attack against card 01101 copy 0
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01097b copy 0 has 0 threat counters
    And card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01007 copy 0 is in seat 1's discard pile

  @behavior:card:01042:choose-up-3-different-cards-in-your-minimum
  @card:01042
  Scenario: Ancestral Knowledge shuffles its minimum one different card
    # "Up to 3" requires at least one choice. Selecting Vibranium moves that
    # exact card into the player deck while the two unselected cards remain in
    # the discard pile.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 921  |
    And seat 1 shows identity face 01040b
    And card 01044 copy 0 starts in seat 1's discard pile
    And card 01045 copy 0 starts in seat 1's discard pile
    And card 01046 copy 0 starts in seat 1's discard pile
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01042 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01042 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose between 1 and 3 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card  | copy |
      | 01044 | 0    |
    Then card 01044 copy 0 is in seat 1's player deck
    And card 01045 copy 0 is in seat 1's discard pile
    And card 01046 copy 0 is in seat 1's discard pile

  @behavior:card:01042:choose-up-3-different-cards-in-your-intermediate
  @card:01042
  Scenario: Ancestral Knowledge shuffles an intermediate two different cards
    # Selecting two different titles moves exactly those cards into the player
    # deck and leaves the third available discard unselected.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 922  |
    And seat 1 shows identity face 01040b
    And card 01044 copy 0 starts in seat 1's discard pile
    And card 01045 copy 0 starts in seat 1's discard pile
    And card 01046 copy 0 starts in seat 1's discard pile
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01042 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01042 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose between 1 and 3 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card  | copy |
      | 01044 | 0    |
      | 01045 | 0    |
    Then card 01044 copy 0 is in seat 1's player deck
    And card 01045 copy 0 is in seat 1's player deck
    And card 01046 copy 0 is in seat 1's discard pile

  @behavior:card:01042:choose-up-3-different-cards-in-your-maximum
  @card:01042
  Scenario: Ancestral Knowledge shuffles its maximum three different cards
    # Selecting three different titles moves all three exact cards into the
    # player deck before that deck is shuffled once.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 923  |
    And seat 1 shows identity face 01040b
    And card 01044 copy 0 starts in seat 1's discard pile
    And card 01045 copy 0 starts in seat 1's discard pile
    And card 01046 copy 0 starts in seat 1's discard pile
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01042 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01042 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose between 1 and 3 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card  | copy |
      | 01044 | 0    |
      | 01045 | 0    |
      | 01046 | 0    |
    Then card 01044 copy 0 is in seat 1's player deck
    And card 01045 copy 0 is in seat 1's player deck
    And card 01046 copy 0 is in seat 1's player deck

  @behavior:card:01018:max-1-per-player
  @card:01018
  Scenario: A player with Energy Channel cannot play another copy
    # One Energy Channel is already attached to Captain Marvel, satisfying Max
    # 1 per player; in a solo game the second copy therefore has no legal host.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 910  |
    And seat 1 shows identity face 01010a
    And card 01018 copy 0 is attached to card 01010a copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01018 | 1    |
      | 01088 | 0    |
    When seat 1 asks whether card 01018 copy 1 is available to play
    Then card 01018 copy 1 is unavailable to play

  @behavior:card:01055:double-number-resources-card-generates-while-paying
  @card:01055 @card:01057
  Scenario: The Power of Aggression alone pays a cost-two Aggression card
    # The Power of Aggression's one printed wild resource doubles while paying
    # for Combat Training, exactly meeting that Aggression card's cost of two.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 911  |
    And seat 1 shows identity face 01019a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01055 | 0    |
      | 01057 | 0    |
    When seat 1 plays card 01057 copy 0 targeting card 01019a copy 0 paying with these cards
      | card  | copy |
      | 01055 | 0    |
    Then card 01057 copy 0 is attached to card 01019a copy 0
    And card 01019a copy 0 has modified ATK 4

  @behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-not-met
  @card:01060
  Scenario: For Justice removes three threat without a mental payment
    # Energy pays the event's cost but is not mental, so For Justice removes
    # its base three threat and leaves one of the scheme's four threat.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 912  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 4 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01060 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01060 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01097b copy 0 has 1 threat counters

  @behavior:card:01060:remove-3-threat-from-scheme-4-threat-condition-met
  @card:01060
  Scenario: For Justice removes four threat with a mental payment
    # Genius pays the event's cost with mental resources, so For Justice
    # removes four threat instead of three.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 913  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 4 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01060 | 1    |
      | 01089 | 0    |
    When seat 1 initiates card 01060 copy 1's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01097b copy 0 has 0 threat counters

  @behavior:card:01062:double-number-resources-card-generates-while-paying
  @card:01062 @card:01065
  Scenario: The Power of Justice alone pays a cost-two Justice card
    # The Power of Justice's one printed wild resource doubles while paying for
    # Heroic Intuition, exactly meeting that Justice card's cost of two.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 914  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01062 | 0    |
      | 01065 | 0    |
    When seat 1 plays card 01065 copy 0 targeting card 01001a copy 0 paying with these cards
      | card  | copy |
      | 01062 | 0    |
    Then card 01065 copy 0 is attached to card 01001a copy 0
    And card 01001a copy 0 has modified THW 2

  @behavior:card:01063:max-1-per-player
  @card:01063
  Scenario: A player with Interrogation Room cannot play another copy
    # One Interrogation Room is already controlled by the sole player, so Max
    # 1 per player makes the second copy unavailable to play.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 915  |
    And card 01063 copy 0 is a support controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01063 | 1    |
      | 01088 | 0    |
    When seat 1 asks whether card 01063 copy 1 is available to play
    Then card 01063 copy 1 is unavailable to play

  @behavior:card:01063:after-you-defeat-minion-exhaust-interrogation-room
  @card:01063
  Scenario: Interrogation Room exhausts after its player defeats a minion
    # Spider-Man's basic attack defeats the already damaged Hydra Mercenary.
    # Interrogation Room's Response then exhausts and removes one threat from
    # the chosen main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 916  |
    And seat 1 shows identity face 01001a
    And card 01063 copy 0 is a support controlled by seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01097b copy 0 has 1 threat counter
    When seat 1 begins their basic attack against card 01101 copy 0
    Then seat 1 is offered the "Interrogation Room" pending opportunity
    When seat 1 accepts card 01063 copy 0's pending opportunity
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01063 copy 0 is exhausted
    And card 01097b copy 0 has 0 threat counters

  @behavior:card:01067:after-maria-hill-enters-play-each-player-multiple-players
  @card:01067
  Scenario: Maria Hill lets every player draw in a multiplayer game
    # Maria Hill's Response draws one card for each of the two players. Each
    # asserted card was fixed on that player's own deck before she entered.
    Given a canonical Core scene is dealt
      | campaign | heroes                      | seed |
      | rhino    | captain_marvel,spider_man   | 917  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01067 | 0    |
      | 01088 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01068     | 0    |
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01002     | 0    |
    When seat 1 plays card 01067 copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is offered the "Maria Hill" pending opportunity
    When seat 1 accepts card 01067 copy 0's pending opportunity
    Then card 01068 copy 0 is in seat 1's hand
    And card 01002 copy 0 is in seat 2's hand

  @behavior:card:01076:toughness
  @covers:behavior:card:01076:character-enters-play-with-tough-status-card
  @covers:behavior:card:01079:double-number-resources-card-generates-while-paying
  @card:01076 @card:01079
  Scenario: Luke Cage enters tough when Power of Protection pays part of his cost
    # Power of Protection doubles its printed wild while paying for Luke Cage;
    # with Energy's two resources that pays his cost four. Toughness gives him
    # one tough status card as he enters play.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 918  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01076 | 0    |
      | 01079 | 0    |
      | 01088 | 0    |
    When seat 1 plays card 01076 copy 0 paying with these cards
      | card  | copy |
      | 01079 | 0    |
      | 01088 | 0    |
    Then card 01076 copy 0 remains an ally controlled by seat 1
    And card 01076 copy 0 has 1 tough status card

  @behavior:card:01093:spend-physical-resource-and-discard-card-ready
  @card:01093
  Scenario: Tenacity spends a physical resource and discards to ready its hero
    # Tenacity's cost spends a physical resource and discards the upgrade; its
    # effect then readies the exhausted hero.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 919  |
    And seat 1 shows identity face 01001a
    And card 01093 copy 0 is attached to card 01001a copy 0
    And card 01001a copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01090 | 0    |
    When seat 1 initiates card 01093 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
    Then card 01001a copy 0 is ready
    And card 01093 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01028:she-hulk-gets-2-atk
  @covers:behavior:card:01028:after-she-hulk-attacks-discard-superhuman-strength
  @card:01028
  Scenario: Superhuman Strength increases an attack then discards and stuns
    # Superhuman Strength raises She-Hulk's printed ATK 3 by two. After that
    # attack ends, its Forced Response discards the upgrade and stuns Rhino.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 900  |
    And seat 1 shows identity face 01019a
    And card 01028 copy 0 is an upgrade attached to seat 1's identity
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01094 copy 0 has 5 damage
    And card 01094 copy 0 has 1 stunned status card
    And card 01028 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01031:for-each-printed-energy-resource-discarded-way-zero
  @card:01031
  Scenario: Repulsor Blast deals only its base damage with no energy resource
    # None of the five discarded cards prints an energy resource, so Repulsor
    # Blast deals only its one base attack damage.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 901  |
    And seat 1 shows identity face 01029a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01031 | 0    |
      | 01086 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01030     | 0    |
      | 01033     | 0    |
      | 01034     | 0    |
      | 01036     | 0    |
      | 01037     | 0    |
    When seat 1 initiates card 01031 copy 0's action paying with these cards
      | card  | copy |
      | 01086 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 1 damage

  @behavior:card:01031:for-each-printed-energy-resource-discarded-way-one
  @card:01031
  Scenario: Repulsor Blast adds two damage for one energy resource
    # Supersonic Punch is the only one of the five discarded cards with a
    # printed energy resource, adding two damage to the one base damage.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 902  |
    And seat 1 shows identity face 01029a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01031 | 0    |
      | 01086 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01032     | 0    |
      | 01033     | 0    |
      | 01034     | 0    |
      | 01036     | 0    |
      | 01037     | 0    |
    When seat 1 initiates card 01031 copy 0's action paying with these cards
      | card  | copy |
      | 01086 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 3 damage

  @behavior:card:01059:jessica-jones-gets-1-thw-for-each-zero
  @card:01059
  Scenario: Jessica Jones has printed thwart with no side scheme
    # With no side scheme in play, Jessica Jones receives no modifier and keeps
    # her printed THW 1.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 903  |
    And card 01059 copy 0 is an ally controlled by seat 1
    When the printed characteristics of card 01059 copy 0 are requested
    Then card 01059 copy 0 has modified THW 1

  @behavior:card:01059:jessica-jones-gets-1-thw-for-each-one
  @card:01059
  Scenario: Jessica Jones gains one thwart for one side scheme
    # One side scheme in play raises Jessica Jones from printed THW 1 to 2.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 904  |
    And card 01059 copy 0 is an ally controlled by seat 1
    And card 01107 copy 0 is a side scheme in play
    When the printed characteristics of card 01059 copy 0 are requested
    Then card 01059 copy 0 has modified THW 2

  @behavior:card:01059:jessica-jones-gets-1-thw-for-each-multiple
  @card:01059
  Scenario: Jessica Jones gains thwart for each of multiple side schemes
    # Two side schemes in play raise Jessica Jones from printed THW 1 to 3.
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets     | seed |
      | rhino    | spider_man | legions_of_hydra | 905  |
    And card 01059 copy 0 is an ally controlled by seat 1
    And card 01107 copy 0 is a side scheme in play
    And card 01180 copy 0 is a side scheme in play
    When the printed characteristics of card 01059 copy 0 are requested
    Then card 01059 copy 0 has modified THW 3

  @behavior:card:01065:play-under-any-player-s-control
  @covers:behavior:card:01065:your-hero-gets-1-thw
  @card:01065
  Scenario: Heroic Intuition may be played under another player's control
    # Spider-Man owns and plays Heroic Intuition on Captain Marvel. Captain
    # Marvel controls it and her printed THW 2 is increased to 3.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 906  |
    And seat 2 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01065 | 0    |
      | 01088 | 0    |
    When seat 1 plays card 01065 copy 0 targeting card 01010a copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01065 copy 0 is attached to card 01010a copy 0
    And card 01010a copy 0 has modified THW 3

  @behavior:card:01065:max-1-per-player
  @card:01065
  Scenario: A player with Heroic Intuition cannot receive another copy
    # The first copy already satisfies Max 1 per player. In this solo Core
    # game, the second owned copy therefore has no legal player to receive it.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 907  |
    And seat 1 shows identity face 01001a
    And card 01065 copy 0 is attached to card 01001a copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01065 | 1    |
      | 01088 | 0    |
    When seat 1 asks whether card 01065 copy 1 is available to play
    Then card 01065 copy 1 is unavailable to play

  @behavior:card:01081:play-under-any-player-s-control
  @covers:behavior:card:01081:your-hero-gets-1-def
  @card:01081
  Scenario: Armored Vest may be played under another player's control
    # Black Panther owns and plays Armored Vest on Captain Marvel. Captain
    # Marvel controls it and her printed DEF 1 is increased to 2.
    Given a canonical Core scene is dealt
      | campaign | heroes                       | seed |
      | rhino    | black_panther,captain_marvel | 908  |
    And seat 2 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01081 | 0    |
      | 01088 | 0    |
    When seat 1 plays card 01081 copy 0 targeting card 01010a copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01081 copy 0 is attached to card 01010a copy 0
    And card 01010a copy 0 has modified DEF 2

  @behavior:card:01081:max-1-per-player
  @card:01081
  Scenario: A player with Armored Vest cannot receive another copy
    # The first copy already satisfies Max 1 per player. In this solo Core
    # game, the second owned copy therefore has no legal player to receive it.
    Given a canonical Core scene is dealt
      | campaign | heroes       | seed |
      | rhino    | black_panther | 909  |
    And seat 1 shows identity face 01040a
    And card 01081 copy 0 is attached to card 01040a copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01081 | 1    |
      | 01088 | 0    |
    When seat 1 asks whether card 01081 copy 1 is available to play
    Then card 01081 copy 1 is unavailable to play

  @behavior:card:01002:after-you-play-black-cat-discard-top
  @covers:behavior:card:01002:add-each-card-with-printed-mental-resource
  @card:01002
  Scenario: Black Cat keeps only discarded cards with a printed mental resource
    # Playing Black Cat discards the next two cards. Enhanced Spider-Sense has
    # a printed mental resource and returns to hand; Backflip has a printed
    # physical resource and remains discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 896  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01089 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01004     | 0    |
      | 01003     | 0    |
    When seat 1 plays card 01002 copy 0 paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01002 copy 0 remains an ally controlled by seat 1
    And card 01004 copy 0 is in seat 1's hand
    And card 01003 copy 0 is in seat 1's discard pile

  @behavior:card:01011:after-spider-woman-enters-play-confuse-villain
  @card:01011
  Scenario: Spider-Woman may confuse the villain after entering play
    # Spider-Woman's optional Response resolves after she enters play and gives
    # the villain one confused status card.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 897  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01011 | 0    |
      | 01017 | 0    |
      | 01088 | 0    |
    When seat 1 plays card 01011 copy 0 paying with these cards
      | card  | copy |
      | 01017 | 0    |
      | 01088 | 0    |
    Then seat 1 is offered the "Spider-Woman" pending opportunity
    When seat 1 accepts card 01011 copy 0's pending opportunity
    Then card 01094 copy 0 has 1 confused status card
    And card 01011 copy 0 remains an ally controlled by seat 1

  @behavior:card:01019a:after-you-change-form-deal-2-damage
  @card:01019a
  Scenario: She-Hulk may deal two damage after changing to hero form
    # Changing from Jennifer Walters to She-Hulk opens Do You Even Lift?; its
    # chosen enemy takes two damage after the Response is accepted.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 898  |
    And seat 1 shows identity face 01019b
    When seat 1 changes form by flipping their identity
    Then seat 1 is offered the "Do You Even Lift?" pending opportunity
    When seat 1 accepts card 01019a copy 0's pending opportunity
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 2 damage
    And seat 1 is in hero form

  @behavior:card:01024:after-you-make-basic-attack-using-your
  @card:01024
  Scenario: One-Two Punch readies She-Hulk after her basic attack
    # She-Hulk's basic attack exhausts her and deals her printed three damage.
    # The response then pays one resource, discards One-Two Punch, and readies
    # her after that attack ends.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 899  |
    And seat 1 shows identity face 01019a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01024 | 0    |
      | 01088 | 0    |
    When seat 1 uses their basic attack against card 01094 copy 0 and accepts "One-Two Punch" paid with card 01088 copy 0
    Then card 01094 copy 0 has 3 damage
    And card 01019a copy 0 is ready
    And card 01024 copy 0 is in seat 1's discard pile

  @behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-not-met
  @card:01016
  Scenario: Captain Marvel's Helmet grants one defense without Aerial
    # Captain Marvel's Helmet grants +1 DEF while Captain Marvel lacks Aerial,
    # increasing her printed DEF 1 to 2.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 890  |
    And seat 1 shows identity face 01010a
    And card 01016 copy 0 is an upgrade attached to seat 1's identity
    When the printed characteristics of card 01010a copy 0 are requested
    Then card 01010a copy 0 has modified DEF 2

  @behavior:card:01016:captain-marvel-gets-1-def-2-def-condition-met
  @card:01016 @card:01017
  Scenario: Captain Marvel's Helmet grants two defense with Aerial
    # Cosmic Flight grants Captain Marvel Aerial, so the Helmet grants +2 DEF
    # instead and increases her printed DEF 1 to 3.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 891  |
    And seat 1 shows identity face 01010a
    And card 01016 copy 0 is an upgrade attached to seat 1's identity
    And card 01017 copy 0 is an upgrade attached to seat 1's identity
    When the printed characteristics of card 01010a copy 0 are requested
    Then card 01010a copy 0 has the AERIAL trait
    And card 01010a copy 0 has modified DEF 3

  @behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-not-met
  @card:01032
  Scenario: Supersonic Punch deals four damage without Aerial
    # Without Aerial, Supersonic Punch deals its base four attack damage to the
    # chosen enemy.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 892  |
    And seat 1 shows identity face 01029a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01032 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01032 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 4 damage
    And card 01032 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01032:deal-4-damage-enemy-8-damage-instead-condition-met
  @card:01032 @card:01039
  Scenario: Supersonic Punch deals eight damage with Aerial
    # Rocket Boots grants Iron Man Aerial for the phase, so Supersonic Punch
    # deals eight attack damage instead of four.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 893  |
    And seat 1 shows identity face 01029a
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01032 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01039 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01029a copy 0 has the AERIAL trait
    When seat 1 initiates card 01032 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 8 damage

  @behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-not-met
  @card:01038
  Scenario: Powered Gauntlets deals one damage without Aerial
    # Without Aerial, exhausting Powered Gauntlets deals one attack damage to
    # the chosen enemy.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 894  |
    And seat 1 shows identity face 01029a
    And card 01038 copy 0 is an upgrade attached to seat 1's identity
    When seat 1 initiates card 01038 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01038 copy 0 is exhausted
    And card 01094 copy 0 has 1 damage

  @behavior:card:01038:exhaust-powered-gauntlets-deal-1-damage-enemy-condition-met
  @card:01038 @card:01039
  Scenario: Powered Gauntlets deals two damage with Aerial
    # Rocket Boots grants Iron Man Aerial for the phase, so exhausting Powered
    # Gauntlets deals two attack damage instead of one.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 895  |
    And seat 1 shows identity face 01029a
    And card 01038 copy 0 is an upgrade attached to seat 1's identity
    And card 01039 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01089 | 0    |
    When seat 1 initiates card 01039 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01029a copy 0 has the AERIAL trait
    When seat 1 initiates card 01038 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01038 copy 0 is exhausted
    And card 01094 copy 0 has 2 damage

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

  @behavior:card:01084:after-entering-play-remove-two-threat
  @covers:behavior:card:01084:at-end-round-if-nick-fury-is-condition-met
  @card:01084
  Scenario: Nick Fury removes threat and leaves play at the end of the round
    # Nick Fury's first Forced Response option removes two threat from the
    # chosen scheme. If he remains in play, the delayed end-of-round effect
    # then discards him.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 888  |
    And card 01097b copy 0 has 2 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01084 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 plays card 01084 copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 for the pending action
    Then card 01097b copy 0 has 0 threat counters
    And card 01084 copy 0 remains an ally controlled by seat 1
    When the villain phase and round end
    Then card 01084 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01084:after-entering-play-deal-four-damage
  @card:01084
  Scenario: Nick Fury deals four damage after entering play
    # Nick Fury's third Forced Response option chooses one enemy and deals four
    # damage to that enemy.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 889  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01084 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 plays card 01084 copy 0 paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    # The remove-threat branch is ineligible while the scheme has no threat,
    # so the printed third branch is the second offered legal choice.
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 4 damage
    And card 01084 copy 0 remains an ally controlled by seat 1

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
