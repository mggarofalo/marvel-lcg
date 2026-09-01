@core
Feature: Core attachment lifecycle
  Cards that print "attach to" enter play on the named game element and remain
  there until an effect or their host makes them leave play.

  @behavior:card:01009:attach-enemy
  @covers:behavior:card:01009:max-1-per-enemy
  @covers:behavior:card:01009:when-attached-enemy-would-attack-discard-webbed
  @covers:behavior:card:01009:then-stun-that-enemy
  @covers:behavior:faq:01009:prevents-two-attacks
  @covers:behavior:faq:01009:replaced-attack-does-not-trigger-spider-sense
  @card:01009 @faq:01009
  Scenario: Webbed Up replaces one enemy attack and stuns its attacker
    # Webbed Up attaches to Rhino, and its maximum prevents the second copy
    # from attaching to that same enemy. When Rhino would attack, the forced
    # interrupt discards Webbed Up and gives Rhino stun; that new stun then
    # replaces the attack and is itself discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 881  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01009 | 0    |
      | 01009 | 1    |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 takes their voluntary form change
    Then seat 1 changed from alter-ego to hero form
    When seat 1 plays card 01009 copy 0 paying with these cards
      | card  | copy |
      | 01002 | 0    |
      | 01003 | 0    |
      | 01088 | 0    |
    Then card 01009 copy 0 is attached to card 01094 copy 0
    When seat 1 asks whether card 01009 copy 1 is available to play
    Then card 01009 copy 1 is unavailable to play
    When the villain attacks seat 1 with every optional choice declined
    Then card 01009 copy 0 is faceup on top of seat 1's discard pile
    And card 01001a copy 0 has 0 damage
    And card 01094 copy 0 has 0 stunned status cards
    And 3 Give_Status events were emitted
    And 0 Draw_Cards events were emitted
    And the attack has ended

  @behavior:rr:attach-to:published-result
  @covers:behavior:rr:reveal.1:attach-to-text
  @covers:behavior:card:01099:attach-rhino
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

  @behavior:card:01163:attach-minion-with-highest-printed-hit-points
  @covers:behavior:card:01163:if-there-are-no-minions-in-play-condition-not-met
  @covers:behavior:card:01163:attached-minion-gets-3-hit-points
  @card:01163
  Scenario: Genetically Enhanced attaches to the highest-hit-point minion
    # "Attach to the minion with the highest printed hit points." Titania's
    # printed six exceeds Hydra Mercenary's three, and the attachment then
    # raises Titania's remaining hit points from six to nine.
    Given a canonical Core scene is dealt
      | campaign | heroes   | modular sets       | seed |
      | rhino    | she_hulk | the_doomsday_chair | 849  |
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01162 copy 0 is a minion engaged with seat 1
    When card 01163 copy 0 is revealed to seat 1
    Then card 01163 copy 0 is attached to card 01162 copy 0
    And card 01162 copy 0 has 9 remaining hit points

  @behavior:card:01163:if-there-are-no-minions-in-play-condition-met
  @card:01163
  Scenario: Genetically Enhanced surges when there is no minion to attach to
    # "If there are no minions in play, this card gains surge." With no legal
    # host, Genetically Enhanced is discarded and its additional card reveals.
    Given a canonical Core scene is dealt
      | campaign | heroes     | modular sets   | seed |
      | rhino    | she_hulk | the_doomsday_chair | 850  |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01163     | 0    |
      | 01184     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01163 copy 0 is faceup on top of the encounter discard pile
    And card 01184 copy 0 is engaged with seat 1

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
