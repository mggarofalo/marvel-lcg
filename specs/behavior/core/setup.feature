@core
Feature: Canonical Core setup
  A behavioral scene begins from the printed Core deal before any transcript
  arranges a boundary state. The deal itself is therefore executable authority.

  @behavior:rr:appendix-ii-setup.step.1:published-result
  @covers:behavior:rr:appendix-ii-setup.step.2:published-result
  @covers:behavior:rr:appendix-ii-setup.step.3:published-result
  @covers:behavior:rr:appendix-ii-setup.step.4:published-result
  @covers:behavior:rr:appendix-ii-setup.step.5:published-result
  @covers:behavior:rr:appendix-ii-setup.step.8:published-result
  @covers:behavior:rr:appendix-ii-setup.step.9:published-result
  @covers:behavior:rr:appendix-ii-setup.step.10:published-result
  @covers:behavior:rr:appendix-ii-setup.step.14:published-result
  @covers:behavior:rr:modes-of-play.1:published-result
  @covers:behavior:rr:first-player:published-result
  @rr:appendix-ii-setup.step.1 @rr:appendix-ii-setup.step.2
  @rr:appendix-ii-setup.step.3 @rr:appendix-ii-setup.step.4
  @rr:appendix-ii-setup.step.5 @rr:appendix-ii-setup.step.8
  @rr:appendix-ii-setup.step.9 @rr:appendix-ii-setup.step.10
  @rr:appendix-ii-setup.step.14
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
    And seat 1 has the first player token
    And seat 1 has 6 cards in hand
    And seat 1 has 34 cards in their player deck
    And card 01001b copy 0 has 10 remaining hit points
    And card 01094 copy 0 is the faceup villain
    And card 01094 copy 0 has 14 remaining hit points
    And card 01097b copy 0 is the faceup main scheme
    And card 01165 copy 0 is in the encounter deck
    And card 01166 copy 0 is in seat 1's set-aside nemesis pile

  @behavior:rr:modes-of-play.2:published-result
  @rr:modes-of-play.2
  Scenario: Expert mode substitutes villain stages and adds the Expert set
    # Expert mode follows the scenario setup using its listed expert villain
    # stages and adds the complete Expert encounter set to the encounter deck.
    Given a canonical Core scene is dealt
      | campaign     | heroes     | seed |
      | rhino_expert | spider_man | 802  |
    When the dealt Core scene is inspected
    Then card 01095 copy 0 is the faceup villain
    And card 01096 copy 0 is in the villain deck
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
