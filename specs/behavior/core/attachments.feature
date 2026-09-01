@core
Feature: Core attachment lifecycle
  Cards that print "attach to" enter play on the named game element and remain
  there until an effect or their host makes them leave play.

  @behavior:rr:attach-to:published-result
  @rr:attach-to @card:01099
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
  @rr:attach-to.1 @card:01074
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
