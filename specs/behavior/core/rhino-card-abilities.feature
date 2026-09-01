@core
Feature: Core Rhino card abilities
  Rhino scenario cards resolve from legal Core scenes according to their
  printed text and the shared reveal, damage, status, and action rules.

  @behavior:card:01095:search-encounter-deck-and-discard-pile-for
  @covers:behavior:card:01095:shuffle-encounter-deck
  @card:01095
  Scenario: Rhino II reveals Breakin and Takin and shuffles the encounter deck
    # Defeating Rhino I reveals Rhino II. His printed When Revealed search
    # reveals Breakin' & Takin', then the searched encounter deck is shuffled.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 935  |
    And seat 1 shows identity face 01001a
    And card 01094 copy 0 has 13 damage
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01095 copy 0 is the faceup villain
    And card 01107 copy 0 is in the villain's play area

  @behavior:card:01097b:if-stage-is-completed-players-lose-game-condition-not-met
  @card:01097b
  Scenario: The Rhino main scheme below its target does not end the game
    # The stage is not completed while its threat is below seven, so its
    # printed loss instruction does not apply.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 936  |
    And card 01097b copy 0 has 5 threat counters
    When 1 threat is placed on card 01097b copy 0
    Then card 01097b copy 0 has 6 threat counters
    And the game is unfinished

  @behavior:card:01098:attach-rhino
  @covers:behavior:card:01098:when-any-amount-damage-would-be-dealt
  @covers:behavior:card:01098:then-if-there-is-at-least-5-condition-met
  @covers:behavior:ruling:074448583686e795:armored-rhino-suit-interrupts-before-tough
  @card:01098 @card:01005 @ruling:074448583686e795
  Scenario: Armored Rhino Suit absorbs a large hit and is discarded
    # The revealed Suit attaches to Rhino. Swinging Web Kick's eight damage is
    # placed on the Suit instead; reaching at least five discards the Suit.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 937  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01005 | 0    |
      | 01088 | 0    |
      | 01003 | 0    |
    And card 01094 copy 0 has a tough status card
    When card 01098 copy 0 is revealed to seat 1
    Then card 01098 copy 0 is attached to card 01094 copy 0
    When seat 1 initiates card 01005 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01003 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 0 damage
    And card 01094 copy 0 has 1 tough status card
    And card 01098 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01098:then-if-there-is-at-least-5-condition-not-met
  @covers:behavior:card:01098:when-any-amount-damage-would-be-dealt
  @card:01098
  Scenario: Armored Rhino Suit remains below five stored damage
    # Spider-Man's two attack damage is placed on the attached Suit instead of
    # Rhino. Two is below five, so the Suit remains attached.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 938  |
    And seat 1 shows identity face 01001a
    When card 01098 copy 0 is revealed to seat 1
    Then card 01098 copy 0 is attached to card 01094 copy 0
    When seat 1 uses their basic attack against card 01094 copy 0
    Then card 01094 copy 0 has 0 damage
    And card 01098 copy 0 has 2 damage
    And card 01098 copy 0 is attached to card 01094 copy 0

  @behavior:card:01100:attach-rhino
  @covers:behavior:card:01100:spend-physical-physical-physical-resources-discard-card
  @card:01100
  Scenario: Enhanced Ivory Horn attaches and is discarded for three physical resources
    # The revealed Horn attaches to Rhino. Its Hero Action spends exactly three
    # physical resources and discards the attachment.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 939  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01090 | 0    |
      | 01003 | 0    |
    When card 01100 copy 0 is revealed to seat 1
    Then card 01100 copy 0 is attached to card 01094 copy 0
    When seat 1 initiates card 01100 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
      | 01003 | 0    |
    Then card 01100 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01103:deal-1-damage-each-hero
  @card:01103
  Scenario: Shocker damages each hero but not an alter ego
    # Shocker deals one damage to each identity in hero form. Spider-Man is a
    # hero; Carol Danvers is an alter ego and is not a hero.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 940  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010b
    When card 01103 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has 1 damage
    And card 01010b copy 0 has 0 damage
    And card 01103 copy 0 is engaged with seat 1

  @behavior:card:01105:give-rhino-tough-status-card
  @covers:behavior:card:01105:if-rhino-already-has-tough-status-card-condition-not-met
  @card:01105
  Scenario: Im Tough gives an unprotected Rhino a tough status card
    # Rhino begins without Tough, so the revealed treachery gives him one and
    # does not gain Surge.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 941  |
    When card 01105 copy 0 is revealed to seat 1
    Then card 01094 copy 0 has 1 tough status card
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01105:if-rhino-already-has-tough-status-card-condition-met
  @card:01105
  Scenario: Im Tough gains surge when Rhino is already tough
    # Rhino cannot receive a second Tough card, so the conditional Surge deals
    # the next encounter card facedown to the revealing player.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 942  |
    And card 01094 copy 0 has a tough status card
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01105 copy 0 is revealed to seat 1
    Then card 01094 copy 0 has 1 tough status card
    And card 01101 copy 0 is facedown in seat 1's encounter queue

  @behavior:card:01106:card-gains-surge
  @card:01106
  Scenario: Stampede gains surge for a player in alter ego form
    # The Alter-Ego branch does not initiate an attack; it gains Surge and
    # deals the next encounter card facedown.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 943  |
    And seat 1 shows identity face 01001b
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01106 copy 0 is revealed to seat 1
    Then card 01001b copy 0 has 0 damage
    And card 01101 copy 0 is facedown in seat 1's encounter queue

  @behavior:card:01106:if-character-is-damaged-by-attack-that-condition-not-met
  @card:01106
  Scenario: Stampede does not stun a character whose Tough prevents the attack damage
    # Tough prevents all damage from Stampede's attack, so the character was
    # not damaged and the delayed stun condition is not met.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 944  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 has a tough status card
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When card 01106 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 has 0 damage
    And card 01001a copy 0 has 0 tough status cards
    And card 01001a copy 0 has 0 stunned status cards

  @behavior:card:01110:when-revealed-take-two-damage
  @card:01110
  Scenario: Hydra Bomber lets the revealing player take two damage
    # The revealing player selects the first printed option and their identity
    # takes two damage.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 945  |
    When card 01110 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    When seat 1 chooses option 1 for the pending encounter-card decision
    Then card 01001b copy 0 has 2 damage

  @behavior:card:01110:when-revealed-place-one-threat
  @card:01110
  Scenario: Hydra Bomber lets the revealing player place one threat
    # The revealing player selects the second printed option and one threat is
    # placed on the main scheme.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 946  |
    And card 01097b copy 0 has 0 threat counters
    When card 01110 copy 0 is revealed to seat 1
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01097b copy 0 has 1 threat counter

  @behavior:card:01112:if-you-are-already-confused-card-gains-condition-met
  @card:01112
  Scenario: False Alarm gains surge when the revealing identity is already confused
    # A character cannot receive a second Confused card. The already-confused
    # branch therefore gains Surge and deals the next encounter card facedown.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 947  |
    And card 01001b copy 0 has a confused status card
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01112 copy 0 is revealed to seat 1
    Then card 01001b copy 0 has 1 confused status card
    And card 01101 copy 0 is facedown in seat 1's encounter queue

  @behavior:card:01111:if-bomb-scare-is-in-play-assign-condition-met
  @covers:behavior:card:01111:if-bomb-scare-is-not-in-play-condition-not-met
  @card:01111
  Scenario: Explosion assigns Bomb Scares threat as damage among friendly characters
    # Bomb Scare has three threat, so Explosion assigns exactly three damage.
    # Two points go to Spider-Man and one to Black Cat before any is resolved.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 948  |
    And seat 1 shows identity face 01001a
    And card 01002 copy 0 is an ally controlled by seat 1
    And card 01109 copy 0 is a side scheme in play
    And card 01109 copy 0 has 3 threat counters
    When card 01111 copy 0 is revealed to seat 1
    Then seat 1 is asked to choose 3 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card   | copy |
      | 01001a | 0    |
      | 01001a | 0    |
      | 01002  | 0    |
    Then card 01001a copy 0 has 2 damage
    And card 01002 copy 0 has 1 damage
    And seat 1 has 0 facedown encounter cards

  @behavior:card:01111:if-bomb-scare-is-not-in-play-condition-met
  @covers:behavior:card:01111:if-bomb-scare-is-in-play-assign-condition-not-met
  @card:01111
  Scenario: Explosion gains surge when Bomb Scare is not in play
    # With no Bomb Scare in play there is no damage assignment. Explosion
    # instead gains Surge and deals the next encounter card facedown.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 949  |
    And seat 1 shows identity face 01001a
    And these cards are next on the encounter deck
      | next card | copy |
      | 01101     | 0    |
    When card 01111 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has 0 damage
    And card 01101 copy 0 is facedown in seat 1's encounter queue
