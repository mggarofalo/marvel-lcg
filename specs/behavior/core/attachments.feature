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
