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

  @behavior:rr:treachery.1:published-result
  @covers:behavior:rr:reveal.6:published-result
  @covers:behavior:rr:reveal.step.4:published-result
  @covers:behavior:rr:treachery.2:published-result
  @covers:behavior:card:01104:rhino-heals-4-damage
  @covers:behavior:card:01104:if-no-damage-was-healed-way-card-condition-not-met
  @rr:treachery.1 @rr:reveal.6 @rr:reveal.step.4 @rr:treachery.2
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
