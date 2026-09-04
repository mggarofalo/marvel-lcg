@core
Feature: Revealing Core encounter card types
  The reveal procedure places each encounter card by type before resolving its
  When Revealed text and any responses to the completed reveal.

  @behavior:rr:reveal.5:published-result
  @covers:behavior:rr:side-scheme.1:published-result
  @covers:behavior:card:01109:place-additional-1-per-hero-threat-here
  @covers:behavior:faq:01019a:side-scheme-enters-with-threat
  @rr:reveal.5 @rr:side-scheme.1 @card:01109 @faq:01019a
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
  @covers:behavior:rr:first-player.3:published-result
  @card:01149 @rr:each-player @rr:each-player.1 @rr:in-player-order.1
  @rr:first-player.3
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
  @covers:behavior:rr:resolve.4:published-result
  @rr:treachery.1 @rr:reveal.6 @rr:reveal.step.4 @rr:treachery.2 @rr:heal.2
  @card:01104 @rr:resolve.4
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
  @covers:behavior:rr:alteration-effect.1:published-result
  @card:01106 @rr:activation.4 @rr:activation.7 @rr:treachery.2.1
  @rr:alteration-effect.1
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

  @behavior:faq:01066:published-clarification-1
  @faq:01066 @card:01066 @card:01121
  Scenario: A defeated Weapons Runner still surges after Hawkeye responds
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | klaw     | captain_marvel | 348  |
    And card 01066 copy 0 is an ally controlled by seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01121     | 1    |
    When card 01121 copy 0 is revealed to seat 1
    Then seat 1 is offered the "Hawkeye" pending opportunity
    When seat 1 accepts card 01066 copy 0's pending opportunity
    Then card 01121 copy 0 is faceup on top of the encounter discard pile
    And seat 1 has 1 facedown encounter card

  @behavior:rr:you-your.5:published-result
  @covers:behavior:card:01112:you-are-confused
  @covers:behavior:card:01112:if-you-are-already-confused-card-gains-condition-not-met
  @rr:you-your.5 @card:01112
  Scenario: A treachery that confuses you places the status on your identity
    # False Alarm's "you are confused" addresses the resolving player. The
    # confused status is therefore placed on that player's active identity.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 840  |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    When card 01112 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has 1 confused status card
    And card 01112 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:choose-option.1:published-result
  @covers:behavior:faq:01155:published-clarification-1
  @rr:choose-option.1 @card:01155 @faq:01155
  Scenario: Affairs of State omits its upgrade option when no target exists
    # T'Challa is already in alter-ego form, so the optional flip is skipped.
    # With no Black Panther upgrade in play, only the option that exhausts the
    # identity has every target it requires.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 844  |
    And seat 1 shows identity face 01040b
    And seat 1's hand is empty
    When card 01155 copy 0 is revealed to seat 1
    Then option 1 is not offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then option 1 is offered by the pending decision
    And option 2 is not offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01040b copy 0 is exhausted
    And card 01155 copy 0 is removed from the game
