@core
Feature: Canonical Core setup
  A behavioral scene begins from the printed Core deal before any transcript
  arranges a boundary state. The deal itself is therefore executable authority.

  @behavior:rr:appendix-ii-setup.step.1:published-result
  @covers:behavior:rr:appendix-ii-setup.step.2:published-result
  @covers:behavior:rr:appendix-ii-setup.step.3:published-result
  @covers:behavior:rr:appendix-ii-setup.step.4:published-result
  @covers:behavior:rr:appendix-ii-setup.step.5:published-result
  @covers:behavior:rr:appendix-ii-setup.step.6:published-result
  @covers:behavior:rr:appendix-ii-setup.step.8:published-result
  @covers:behavior:rr:appendix-ii-setup.step.9:published-result
  @covers:behavior:rr:appendix-ii-setup.step.10:published-result
  @covers:behavior:rr:appendix-ii-setup.step.12.a:published-result
  @covers:behavior:rr:appendix-ii-setup.step.12.b:published-result
  @covers:behavior:rr:appendix-ii-setup.step.14:published-result
  @covers:behavior:rr:in-play-and-out-of-play.2:published-result
  @covers:behavior:rr:in-play-and-out-of-play.6:published-result
  @covers:behavior:rr:in-play-and-out-of-play.9:published-result
  @covers:behavior:rr:in-play-and-out-of-play.11:published-result
  @covers:behavior:rr:in-play-and-out-of-play.13:published-result
  @covers:behavior:rr:modes-of-play.1:published-result
  @covers:behavior:rr:first-player:published-result
  @rr:appendix-ii-setup.step.1 @rr:appendix-ii-setup.step.2
  @rr:appendix-ii-setup.step.3 @rr:appendix-ii-setup.step.4
  @rr:appendix-ii-setup.step.5 @rr:appendix-ii-setup.step.6
  @rr:appendix-ii-setup.step.8
  @rr:appendix-ii-setup.step.9 @rr:appendix-ii-setup.step.10
  @rr:appendix-ii-setup.step.12.a @rr:appendix-ii-setup.step.12.b
  @rr:appendix-ii-setup.step.14
  @rr:in-play-and-out-of-play.2 @rr:in-play-and-out-of-play.6
  @rr:in-play-and-out-of-play.9 @rr:in-play-and-out-of-play.11
  @rr:in-play-and-out-of-play.13
  @rr:modes-of-play.1
  @rr:first-player
  Scenario: The printed Spider-Man and Rhino deal reaches its opening state
    # Setup selects identities, sets their hit points, chooses a first player,
    # sets aside obligations and nemesis sets, selects the scenario, sets the
    # villain's hit points, creates the encounter deck, and draws opening hands.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 801  |
    When the dealt Core scene is inspected
    Then the game has 1 player
    And seat 1 is in alter-ego form
    And card 01001b copy 0 is in play
    And seat 1's identity face 01001a is out of play
    And seat 1 has the first player token
    And seat 1 has 6 cards in hand
    And seat 1 has 34 cards in their player deck
    And seat 1's player deck has card 01065 on top
    And card 01001b copy 0 has 10 remaining hit points
    And card 01094 copy 0 is the faceup villain
    And card 01094 copy 0 is in play
    And card 01094 copy 0 has 14 remaining hit points
    And card 01097b copy 0 is the faceup main scheme
    And card 01097b copy 0 is in play
    And card 01165 copy 0 is in the encounter deck
    And card 01165 copy 0 is out of play
    And card 01166 copy 0 is in seat 1's set-aside nemesis pile
    And card 01166 copy 0 is out of play

  @behavior:rr:modes-of-play.2:published-result
  @covers:behavior:rr:appendix-ii-setup.step.12.c:published-result
  @rr:modes-of-play.2 @rr:appendix-ii-setup.step.12.c
  Scenario: Expert mode substitutes villain stages and adds the Expert set
    # Expert mode follows the scenario setup using its listed expert villain
    # stages and adds the complete Expert encounter set to the encounter deck.
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 802  |
    When the dealt Core scene is inspected
    Then card 01095 copy 0 is the faceup villain
    And card 01096 copy 0 is in the villain deck
    And card 01107 copy 0 has 3 threat counters
    And the encounter deck contains these card counts
      | card  | count |
      | 01191 | 1     |
      | 01192 | 1     |
      | 01193 | 1     |

  @behavior:rr:modular-encounter-set.1:published-result
  @covers:behavior:rr:modular-encounter-set.2:published-result
  @rr:modular-encounter-set.1 @rr:modular-encounter-set.2
  Scenario: A selected modular set replaces the recommendation as a whole set
    # A scenario instructs how many modular sets to include; when a modular set
    # is added, it is added as an entire set.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed | modular sets |
      | rhino    | spider_man | 803  | under_attack |
    When the dealt Core scene is inspected
    Then the encounter deck contains these card counts
      | card  | count |
      | 01109 | 0     |
      | 01151 | 1     |
      | 01152 | 1     |
      | 01153 | 1     |
      | 01154 | 2     |

  @behavior:rr:appendix-ii-setup.step.15:published-result
  @rr:appendix-ii-setup.step.15
  Scenario: A player may replace selected opening-hand cards during mulligan
    # "Each player may discard any number of cards from hand, and then draw up
    # to their starting hand size." The discarded card stays in the discard
    # pile rather than being shuffled back into the deck.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 804  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
      | 01005 | 0    |
      | 01006 | 0    |
      | 01007 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01008     | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 mulligans these cards
      | card  | copy |
      | 01002 | 0    |
    Then seat 1 has 6 cards in hand
    And card 01002 copy 0 is in seat 1's discard pile
    And card 01008 copy 0 is in seat 1's hand

  @behavior:card:01040b:search-your-deck-for-black-panther-upgrade
  @covers:behavior:card:01040b:shuffle-your-deck
  @covers:behavior:rr:ability.6:published-result
  @covers:behavior:rr:appendix-ii-setup.step.16:published-result
  @covers:behavior:rr:search.1:published-result
  @covers:behavior:rr:search.2:published-result
  @covers:behavior:rr:search.3:published-result
  @card:01040b @rr:ability.6 @rr:appendix-ii-setup.step.16
  @rr:search.1 @rr:search.2 @rr:search.3
  Scenario: T'Challa chooses one of his upgrades after mulligans and shuffles
    # Setup step 16 resolves player Setup abilities after mulligans. Foresight
    # searches "your deck," offers every matching Black Panther upgrade, moves
    # the chosen card to hand, and shuffles that entire deck.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 805  |
    And seat 1's hand contains exactly these cards
      | card   | copy |
      | 01041  | 0    |
      | 01042  | 0    |
      | 01043a | 0    |
      | 01043b | 0    |
      | 01043c | 0    |
      | 01043d | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01046     | 0    |
      | 01047     | 0    |
      | 01048     | 0    |
      | 01049     | 0    |
    And card 01091 copy 0 is a support controlled by seat 1
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then card 01046 copy 0 is offered by the pending setup ability
    And card 01047 copy 0 is offered by the pending setup ability
    And card 01048 copy 0 is offered by the pending setup ability
    And card 01049 copy 0 is offered by the pending setup ability
    And card 01091 copy 0 is not offered by the pending setup ability
    And card 01091 copy 0 remains a support controlled by seat 1
    And card 01047 copy 0 is in seat 1's player deck
    When seat 1 chooses card 01046 copy 0 for the pending setup ability
    Then card 01046 copy 0 is in seat 1's hand
    And seat 1's player deck was shuffled by the setup ability
