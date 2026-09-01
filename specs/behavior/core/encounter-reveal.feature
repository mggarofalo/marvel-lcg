@core
Feature: Revealing Core encounter card types
  The reveal procedure places each encounter card by type before resolving its
  When Revealed text and any responses to the completed reveal.

  @behavior:rr:reveal.5:published-result
  @covers:behavior:rr:side-scheme.1:published-result
  @covers:behavior:card:01109:place-additional-1-per-hero-threat-here
  @rr:reveal.5 @rr:side-scheme.1 @card:01109
  Scenario: A revealed side scheme enters the villain play area with its threat
    # "Side scheme: It enters play in the villain's play area." Its starting
    # threat is placed on entry before its When Revealed text adds 1 per player.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 345  |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01109     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01109 copy 0 is in the villain's play area
    And card 01109 copy 0 has 3 threat counters

  @behavior:card:01149:each-player-discards-top-3-cards-their-one-player
  @card:01149
  Scenario: Invasive AI discards three owned cards in a one-player game
    # "When Revealed: Each player discards the top 3 cards of their deck."
    # Spider-Man's three cards move, one at a time, to Spider-Man's discard
    # pile; no foreign signature card is introduced and the player remains in
    # the game.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | ultron   | spider_man | 350  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
      | 01004     | 0    |
    When card 01149 copy 0 is revealed to seat 1
    Then seat 1's discard pile has these cards from top to bottom
      | card  | copy |
      | 01004 | 0    |
      | 01003 | 0    |
      | 01002 | 0    |
    And seat 1 is not eliminated

  @behavior:card:01149:each-player-discards-top-3-cards-their-multiple-players
  @covers:behavior:rr:each-player:published-result
  @covers:behavior:rr:each-player.1:published-result
  @covers:behavior:rr:in-player-order.1:published-result
  @card:01149 @rr:each-player @rr:each-player.1 @rr:in-player-order.1
  Scenario: Invasive AI lets the first player order its multiplayer effect
    # "When each player is instructed to resolve an effect, each player
    # resolves that effect one at a time." Because Invasive AI specifies no
    # order, the first player chooses seat 2 before seat 1; all three of seat
    # 2's cards move before any of seat 1's cards.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | ultron   | spider_man,captain_marvel | 351  |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01002     | 0    |
      | 01003     | 0    |
      | 01004     | 0    |
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01011     | 0    |
      | 01012     | 0    |
      | 01013     | 0    |
    When card 01149 copy 0 is revealed to seat 1
    Then seat 1 is asked to order 2 players for the pending encounter-card decision
    When seat 1 orders these players for the pending encounter-card decision
      | seat |
      | 2    |
      | 1    |
    Then the Discard events moved these cards in order
      | card  | copy |
      | 01011 | 0    |
      | 01012 | 0    |
      | 01013 | 0    |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01004 | 0    |
    And seat 1's discard pile has these cards from top to bottom
      | card  | copy |
      | 01004 | 0    |
      | 01003 | 0    |
      | 01002 | 0    |
    And seat 2's discard pile has these cards from top to bottom
      | card  | copy |
      | 01013 | 0    |
      | 01012 | 0    |
      | 01011 | 0    |

  @behavior:rr:treachery.1:published-result
  @covers:behavior:rr:reveal.6:published-result
  @covers:behavior:rr:reveal.step.4:published-result
  @covers:behavior:rr:treachery.2:published-result
  @covers:behavior:rr:heal.2:published-result
  @covers:behavior:card:01104:rhino-heals-4-damage
  @covers:behavior:card:01104:if-no-damage-was-healed-way-card-condition-not-met
  @rr:treachery.1 @rr:reveal.6 @rr:reveal.step.4 @rr:treachery.2 @rr:heal.2
  @card:01104
  Scenario: A revealed treachery resolves and is discarded
    # A revealed treachery's effects resolve, and "after resolving the effects
    # of a treachery card ... place the card in the encounter discard pile."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 346  |
    And card 01094 copy 0 has 4 damage
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01104     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01094 copy 0 has 0 damage
    And card 01104 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01104:if-no-damage-was-healed-way-card-condition-met
  @card:01104
  Scenario: Hard to Keep Down surges when Rhino has no damage to heal
    # "If no damage was healed this way, this card gains surge." With an
    # undamaged Rhino, Hard to Keep Down resolves and discards before its surge
    # reveals the next encounter card.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 348  |
    And card 01094 copy 0 has 0 damage
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01104     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01094 copy 0 has 0 damage
    And card 01101 copy 0 is engaged with seat 1
    And card 01104 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01106:rhino-attacks-you
  @covers:behavior:card:01106:if-character-is-damaged-by-attack-that-condition-met
  @covers:behavior:rr:activation.4:published-result
  @covers:behavior:rr:activation.7:published-result
  @covers:behavior:rr:treachery.2.1:published-result
  @card:01106 @rr:activation.4 @rr:activation.7 @rr:treachery.2.1
  Scenario: Stampede remains resolving until its villain attack finishes
    # "If a treachery causes one or more enemies to activate as its last effect,
    # that treachery card is considered resolved and is discarded after all of
    # those activations have resolved." After the phase's first attack,
    # Stampede initiates another attack for two more damage, stuns the damaged
    # hero, and only afterward enters the discard pile.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 349  |
    And seat 1 shows identity face 01001a
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01106     | 0    |
      | 01104     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01001a copy 0 has 6 damage
    And card 01001a copy 0 has 1 stunned status card
    And card 01106 copy 0 is faceup on top of the encounter discard pile
    And card 01106 copy 0 was discarded after a Deal_Damage event
    And 2 Deal_Damage events were emitted

  @behavior:rr:reveal.8:published-result
  @covers:behavior:card:01066:hawkeye-enters-play-with-4-arrow-counters
  @covers:behavior:card:01066:after-minion-enters-play-remove-1-arrow
  @rr:reveal.8 @card:01066
  Scenario: A response to a revealed minion resolves after the minion enters play
    # Responses to any reveal step wait "until after all steps of the reveal
    # process have been completed." Hawkeye can therefore damage the minion
    # that has just entered play and spend one of his four arrow counters.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 347  |
    And card 01066 copy 0 is an ally controlled by seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves accepting "Hawkeye"
    Then card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 0 has 2 damage
    And card 01066 copy 0 has 3 arrow counters
