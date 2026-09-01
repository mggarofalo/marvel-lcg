@core
Feature: Core attachment lifecycle
  Cards that print "attach to" enter play on the named game element and remain
  there until an effect or their host makes them leave play.

  @behavior:rr:attach-to:published-result
  @covers:behavior:rr:reveal.1:attach-to-text
  @rr:attach-to @rr:reveal.1 @card:01099
  Scenario: A revealed attachment enters play attached to its named host
    # "If a card uses the phrase 'attach to', it must be attached to ... the
    # specified game element as it enters play." Charge names Rhino.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 806  |
    And seat 1 shows identity face 01001a
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01099     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01099 copy 0 is attached to card 01094 copy 0
    And card 01099 copy 0 is in the villain's play area

  @behavior:rr:attach-to.1:published-result
  @covers:behavior:rr:leaves-play:published-result
  @covers:behavior:rr:leaves-play.2:published-result
  @covers:behavior:rr:leaves-play.2.1:published-result
  @rr:attach-to.1 @rr:leaves-play @rr:leaves-play.2
  @rr:leaves-play.2.1 @card:01074
  Scenario: An attachment is discarded when its attached ally leaves play
    # An attached card remains in play until its host leaves play, "in which
    # case the attached card is discarded." Inspired is attached to Spider-Woman.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 807  |
    And seat 1 shows identity face 01010a
    And card 01011 copy 0 is an ally controlled by seat 1
    And card 01074 copy 0 is attached to card 01011 copy 0
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 with card 01011 copy 0 defending
    Then card 01011 copy 0 is in seat 1's discard pile
    And card 01074 copy 0 is in seat 1's discard pile

  @behavior:rr:attach-to.3:published-result
  @rr:attach-to.3 @card:01163
  Scenario: Attach-to legality is not checked again after attachment
    # The "attach to" phrase is checked when the card would attach, "but it is
    # not checked again after it is attached." Genetically Enhanced therefore
    # remains on Hydra Mercenary when higher-printed-hit-point Titania enters.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 808  |
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01163 copy 0 is attached to card 01101 copy 0
    When card 01162 copy 0 enters play as a minion engaged with seat 1
    Then card 01163 copy 0 is attached to card 01101 copy 0
    And card 01162 copy 0 is engaged with seat 1

  @behavior:rr:max-maximum.4:published-result
  @covers:behavior:card:01074:max-1-per-ally
  @rr:max-maximum.4 @card:01074
  Scenario: Max one per ally prevents a second attachment on the same ally
    # "Max 1 per [game element]" restricts copies attached to each named game
    # element. Spider-Woman is the only ally in play and already has Inspired,
    # so the second owned copy has no legal host and is not offered for play.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 809  |
    And card 01011 copy 0 is an ally controlled by seat 1
    And card 01074 copy 0 is attached to card 01011 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01074 | 1    |
      | 01088 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 asks whether card 01074 copy 1 is available to play
    Then card 01074 copy 1 is unavailable to play
    And card 01074 copy 0 is attached to card 01011 copy 0
