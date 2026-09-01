@core
Feature: Core villain phase
  The villain phase places scheme threat, resolves the villain activation, and
  then deals and reveals encounter cards in the published order.

  @behavior:rr:villain-phase:published-result
  @covers:behavior:rr:villain-phase.step.1:published-result
  @covers:behavior:rr:villain-phase.step.2.a:published-result
  @covers:behavior:rr:villain-phase.step.3:published-result
  @covers:behavior:rr:villain-phase.step.4:published-result
  @covers:behavior:rr:activation.1:published-result
  @covers:behavior:rr:activation.3:published-result
  @covers:behavior:rr:boost-boost-icon:published-result
  @covers:behavior:rr:boost-boost-icon.5:published-result
  @covers:behavior:rr:deal-deal-an-encounter-card:villain-phase-step-three
  @covers:behavior:rr:reveal:published-result
  @covers:behavior:rr:engage:published-result
  @covers:behavior:rr:scheme-enemy-activation:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.1:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.a:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.c:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.2.d:published-result
  @covers:behavior:rr:scheme-enemy-activation.step.3:published-result
  @covers:behavior:rr:main-scheme-main-scheme-deck.1:published-result
  @covers:behavior:rr:reveal.3:published-result
  @rr:villain-phase @rr:villain-phase.step.1 @rr:villain-phase.step.2.a
  @rr:villain-phase.step.3 @rr:villain-phase.step.4 @rr:activation.1
  @rr:activation.3 @rr:boost-boost-icon @rr:boost-boost-icon.5
  @rr:deal-deal-an-encounter-card @rr:reveal @rr:engage
  @rr:scheme-enemy-activation @rr:scheme-enemy-activation.step.1
  @rr:scheme-enemy-activation.step.2 @rr:scheme-enemy-activation.step.2.a
  @rr:scheme-enemy-activation.step.2.c @rr:scheme-enemy-activation.step.2.d
  @rr:scheme-enemy-activation.step.3
  @rr:main-scheme-main-scheme-deck.1 @rr:reveal.3
  Scenario: An alter-ego receives scheme threat before its encounter card is revealed
    # Step 1 places the main scheme's acceleration threat. The villain then
    # schemes with SCH plus boost icons; only afterward is one encounter card
    # dealt and revealed.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 318  |
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 4 threat counters
    And card 01103 copy 0 is faceup on top of the encounter discard pile
    And card 01101 copy 0 is engaged with seat 1
    And seat 1 has 0 facedown encounter cards
    And a Boost event was emitted before a Reveal event

  @behavior:rr:villain-phase.step.5:published-result
  @covers:behavior:rr:villain-phase.step.2:published-result
  @covers:behavior:rr:villain-phase.step.3:published-result
  @covers:behavior:rr:villain-phase.step.4:published-result
  @covers:behavior:rr:activation.1:published-result
  @covers:behavior:rr:deal-deal-an-encounter-card:villain-phase-step-three
  @covers:behavior:rr:reveal:published-result
  @covers:behavior:rr:in-player-order:published-result
  @covers:behavior:rr:in-player-order.2:published-result
  @rr:villain-phase.step.2 @rr:villain-phase.step.3
  @rr:villain-phase.step.4 @rr:villain-phase.step.5 @rr:activation.1
  @rr:deal-deal-an-encounter-card @rr:reveal @rr:in-player-order
  @rr:in-player-order.2
  Scenario: Two players resolve in clockwise order and pass the first player token
    # Each player resolves the villain activation in player order, receives one
    # encounter card, then reveals in that order before the token passes.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 319  |
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01105     | 0    |
      | 01101     | 0    |
      | 01101     | 1    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 4 threat counters
    And card 01101 copy 0 is engaged with seat 1
    And card 01101 copy 1 is engaged with seat 2
    And seat 1 has 0 facedown encounter cards
    And seat 2 has 0 facedown encounter cards
    And seat 2 has the first player token

  @behavior:rr:attack-enemy-activation:published-result
  @covers:behavior:rr:activation:published-result
  @covers:behavior:rr:attack-enemy-activation.1:published-result
  @covers:behavior:rr:attack-enemy-activation.1.1:published-result
  @covers:behavior:rr:attack-enemy-activation.4:published-result
  @covers:behavior:rr:attack-enemy-activation.step.1:published-result
  @covers:behavior:rr:attack-enemy-activation.step.3.a:published-result
  @covers:behavior:rr:attack-enemy-activation.step.3.c:published-result
  @covers:behavior:rr:attack-enemy-activation.step.3.d:published-result
  @covers:behavior:rr:attack-enemy-activation.step.4:published-result
  @covers:behavior:rr:attack-enemy-activation.step.5:published-result
  @covers:behavior:rr:attack-enemy-activation.step.6:published-result
  @covers:behavior:rr:ability.8:published-result
  @covers:behavior:rr:ability.11:published-result
  @covers:behavior:rr:damage.1:published-result
  @covers:behavior:rr:damage.3:published-result
  @covers:behavior:rr:damage.step.5:published-result
  @covers:behavior:rr:defend-defense.6:published-result
  @rr:attack-enemy-activation @rr:activation
  @rr:attack-enemy-activation.1 @rr:attack-enemy-activation.1.1
  @rr:attack-enemy-activation.4 @rr:attack-enemy-activation.step.1
  @rr:attack-enemy-activation.step.3.a @rr:attack-enemy-activation.step.3.c
  @rr:attack-enemy-activation.step.3.d @rr:attack-enemy-activation.step.4
  @rr:attack-enemy-activation.step.5 @rr:attack-enemy-activation.step.6
  @rr:damage.1 @rr:damage.3 @rr:damage.step.5
  @rr:ability.8 @rr:ability.11 @rr:defend-defense.6
  Scenario: A villain attacks an undefended hero with its ATK plus boost icons
    # A villain attack targets both the player and their hero. The facedown
    # boost is flipped, its icons modify ATK, and the calculated damage is then
    # placed on the undefended target.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 320  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01001a copy 0 has 4 damage
    And seat 1 has 6 cards in hand
    And card 01097b copy 0 has 1 threat counter
    And card 01103 copy 0 is faceup on top of the encounter discard pile
    And card 01101 copy 0 is engaged with seat 1
    And a Boost event was emitted before a Deal_Damage event
    And the last attack was undefended

  @behavior:rr:boost-boost-icon.1:published-result
  @covers:behavior:rr:star-icon.6:published-result
  @covers:behavior:card:01178:if-villain-is-making-undefended-attack-place-condition-met
  @rr:boost-boost-icon.1 @rr:star-icon.6 @card:01178
  Scenario: A boost star resolves its ability without increasing attack damage
    # "A star icon is not itself considered a boost icon, and does not
    # contribute to the villain's ATK or SCH value." Kree Manipulator's boost
    # text places one threat during an undefended attack; Rhino's printed ATK 2
    # remains the entire damage amount.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 728  |
    And seat 1 shows identity face 01010a
    And card 01010a copy 0 is exhausted
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01178     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01010a copy 0 has 2 damage
    And card 01097b copy 0 has 1 threat counter
    And card 01178 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:defend-defense.2:published-result
  @covers:behavior:rr:attack-enemy-activation.2:published-result
  @covers:behavior:rr:attack-enemy-activation.2.1:published-result
  @rr:defend-defense.2 @rr:attack-enemy-activation.2
  @rr:attack-enemy-activation.2.1
  Scenario: A hero exhausts to defend and reduces the attack by DEF
    # A hero can use its basic defense. If declared, the attack's damage is
    # dealt to that hero after reduction by its DEF value.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 321  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with card 01001a copy 0 defending the first attack
    Then card 01001a copy 0 is exhausted
    And card 01001a copy 0 has 1 damage
    And card 01101 copy 0 is engaged with seat 1

  @behavior:rr:attack-enemy-activation.2.2:published-result
  @covers:behavior:rr:attack-enemy-activation.step.2:published-result
  @rr:attack-enemy-activation.2.2 @rr:attack-enemy-activation.step.2
  Scenario: A tough hero defends before tough prevents the reduced damage
    # "If a hero with a tough status makes a basic defense, the damage is first
    # reduced by that hero's DEF value." Declaring the defense exhausts the
    # hero; tough then prevents the remaining damage instance.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 719  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 has a tough status card
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with card 01001a copy 0 defending the first attack
    Then card 01001a copy 0 is exhausted
    And card 01001a copy 0 has 0 damage
    And card 01001a copy 0 has 0 tough status cards

  @behavior:rr:attack-enemy-activation.1.2:published-result
  @covers:behavior:rr:attack-enemy-activation.1.3:published-result
  @covers:behavior:rr:defend-defense.5:published-result
  @rr:attack-enemy-activation.1.2 @rr:attack-enemy-activation.1.3
  @rr:defend-defense.5
  Scenario: Another player's hero becomes the attack target by defending
    # "If a player other than the attacked player defends the attack with a
    # character they control, that player becomes the new target of that
    # attack." The defending character likewise becomes its character target.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | spider_man,captain_marvel | 720  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 with card 01010a copy 0 defending
    Then card 01010a copy 0 is exhausted
    And card 01010a copy 0 has 3 damage
    And card 01001a copy 0 has 0 damage

  @behavior:card:01001a:when-villain-initiates-attack-against-you-draw
  @covers:behavior:rr:attack-enemy-activation.1.4:published-result
  @covers:behavior:rr:defend-defense.5.1:published-result
  @card:01001a @rr:attack-enemy-activation.1.4 @rr:defend-defense.5.1
  Scenario: Spider-Sense follows the attacked player when an ally defends
    # Spider-Sense says, "When the villain initiates an attack against you,
    # draw 1 card." Abilities that trigger when an enemy attacks "you" inspect
    # the attacked player regardless of which controlled character is attacked.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 721  |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 accepting "Spider-Sense" with card 01083 copy 0 defending
    Then seat 1 has 7 cards in hand
    And card 01083 copy 0 is faceup on top of seat 1's discard pile
    And card 01001a copy 0 has 0 damage

  @behavior:card:01099:when-rhino-attacks-attack-gains-overkill
  @covers:behavior:card:01099:excess-damage-ally-from-attack-is-dealt
  @covers:behavior:rr:attack-enemy-activation.5:published-result
  @covers:behavior:rr:ability.7:published-result
  @covers:behavior:rr:ability.12:published-result
  @covers:behavior:rr:ability.step.2.b:published-result
  @covers:behavior:rr:overkill:published-result
  @covers:behavior:rr:overkill.1:published-result
  @covers:behavior:rr:excess-damage:published-result
  @card:01099 @rr:attack-enemy-activation.5 @rr:ability.7 @rr:ability.12
  @rr:ability.step.2.b @rr:overkill @rr:overkill.1 @rr:excess-damage
  Scenario: Charge gives Rhino overkill before a defending ally takes damage
    # Charge's forced interrupt says, "When Rhino attacks, the attack gains
    # overkill." Overkill deals damage beyond the defeated ally's hit points to
    # that ally's controller, and Charge is discarded when the attack ends.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 722  |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01099 copy 0 is attached to card 01094 copy 0
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 with card 01083 copy 0 defending
    Then card 01083 copy 0 is faceup on top of seat 1's discard pile
    And card 01001a copy 0 has 4 damage
    And card 01099 copy 0 is faceup on top of the encounter discard pile

  @behavior:rr:defend-defense.3:published-result
  @covers:behavior:rr:defend-defense.3.1:published-result
  @covers:behavior:rr:attack-enemy-activation.3:published-result
  @covers:behavior:rr:attack-enemy-activation.3.1:published-result
  @covers:behavior:rr:ally.4:published-result
  @rr:defend-defense.3 @rr:defend-defense.3.1
  @rr:attack-enemy-activation.3 @rr:attack-enemy-activation.3.1 @rr:ally.4
  Scenario: An ally exhausts to defend and receives the entire attack
    # "If an ally was declared the defender of the attack, all damage from the
    # attack is dealt to the ally."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 322  |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with card 01083 copy 0 defending the first attack
    Then card 01083 copy 0 is faceup on top of seat 1's discard pile
    And card 01001a copy 0 has 0 damage
    And card 01101 copy 0 is engaged with seat 1

  @behavior:rr:activation.2:minion-attacks-hero
  @covers:behavior:rr:minion.1:published-result
  @rr:activation.2 @rr:minion.1
  Scenario: An engaged minion attacks its hero after the villain attacks
    # During step two, each minion engaged with a player activates after the
    # villain. Against a hero, that activation is an attack and uses no boost
    # card unless another rule says otherwise.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 323  |
    And seat 1 shows identity face 01001a
    And card 01103 copy 0 is a minion engaged with seat 1
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01001a copy 0 has 4 damage
    And card 01103 copy 0 is engaged with seat 1
    And 2 Deal_Damage events were emitted

  @behavior:rr:activation.2:minion-schemes-against-alter-ego
  @covers:behavior:rr:minion.1:published-result
  @rr:activation.2 @rr:minion.1
  Scenario: An engaged minion schemes after the villain schemes
    # During step two, each minion engaged with a player activates after the
    # villain. Against an alter-ego, both enemies add their SCH and only the
    # villain receives a boost card.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 324  |
    And card 01103 copy 0 is a minion engaged with seat 1
    And card 01097b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01104     | 0    |
      | 01101     | 0    |
    When villain phase 1 resolves with every optional choice declined
    Then card 01097b copy 0 has 3 threat counters
    And card 01103 copy 0 is engaged with seat 1

  @behavior:card:01003:when-you-would-take-any-amount-damage
  @covers:behavior:rr:damage.step.3:published-result
  @covers:behavior:rr:prevent.1:published-result
  @covers:behavior:rr:prevent.1.2:published-result
  @covers:behavior:rr:defend-defense.4:published-result
  @covers:behavior:rr:defend-defense.4.1:published-result
  @covers:behavior:rr:defend-defense.4.3:published-result
  @covers:behavior:rr:defend-defense.4.4:published-result
  @covers:behavior:rr:interrupt.3.1:published-result
  @covers:behavior:rr:labeled-ability.3:published-result
  @covers:behavior:rr:labeled-ability.3.1:published-result
  @covers:behavior:rr:target.3.6:published-result
  @card:01003 @rr:damage.step.3 @rr:prevent.1 @rr:prevent.1.2
  @rr:defend-defense.4 @rr:defend-defense.4.1 @rr:defend-defense.4.3
  @rr:defend-defense.4.4 @rr:interrupt.3.1 @rr:labeled-ability.3
  @rr:labeled-ability.3.1 @rr:target.3.6
  Scenario: Backflip prevents all imminent attack damage without exhausting
    # "When you would take any amount of damage from an attack, prevent all of
    # that damage." A defense-labeled ability does not exhaust the hero unless
    # its text says it does. The identity becomes the defender as this
    # defense-labeled ability begins resolving, and prevented damage still
    # affects its target even though the target takes none.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 724  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01003 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When the villain attacks seat 1 accepting "Backflip"
    Then card 01001a copy 0 has 0 damage
    And card 01001a copy 0 is ready
    And card 01001a copy 0 defended the last attack without a basic defense
    And card 01003 copy 0 is faceup on top of seat 1's discard pile
    And 0 Damage events were emitted

  @behavior:card:01004:when-treachery-card-is-revealed-from-encounter
  @covers:behavior:rr:cancel.1:published-result
  @covers:behavior:rr:cancel.2:published-result
  @covers:behavior:rr:cancel.4:published-result
  @covers:behavior:rr:in-play-and-out-of-play.7:published-result
  @card:01004 @rr:cancel.1 @rr:cancel.2 @rr:cancel.4
  @rr:in-play-and-out-of-play.7
  Scenario: Enhanced Spider-Sense cancels a treachery's When Revealed effect
    # "Cancel abilities interrupt the initiation of effects and prevent them
    # from resolving." The treachery is nevertheless revealed and discarded,
    # and Enhanced Spider-Sense is still played and paid for.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 725  |
    And seat 1 shows identity face 01001a
    And card 01097b copy 0 has 0 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01004 | 0    |
      | 01088 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
      | 01186     | 0    |
    When villain phase 1 resolves accepting "Enhanced Spider-Sense" paid with card 01088 copy 0
    Then card 01097b copy 0 has 1 threat counter
    And card 01186 copy 0 is faceup on top of the encounter discard pile
    And card 01004 copy 0 is faceup on top of seat 1's discard pile
    And card 01088 copy 0 is in seat 1's discard pile
