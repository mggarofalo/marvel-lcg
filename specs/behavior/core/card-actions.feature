@core
Feature: Core card actions
  A player initiates a printed action, pays its costs, resolves each explicit
  choice, and observes the card's published effect.

  @behavior:card:01005:deal-8-damage-enemy
  @covers:behavior:rr:attack-player-ability-type.2:published-result
  @covers:behavior:rr:cost.3:published-result
  @covers:behavior:rr:event:published-result
  @covers:behavior:rr:in-play-and-out-of-play.7:published-result
  @covers:behavior:rr:initiating-abilities.step.1:published-result
  @covers:behavior:rr:initiating-abilities.step.3:published-result
  @covers:behavior:rr:initiating-abilities.step.5:published-result
  @covers:behavior:rr:initiating-abilities.step.6:published-result
  @covers:behavior:rr:initiating-abilities.step.7:published-result
  @covers:behavior:rr:play-put-into-play.2:published-result
  @covers:behavior:rr:play-put-into-play:published-result
  @covers:behavior:rr:ownership-and-control.7.3:published-result
  @covers:behavior:rr:player-turn.5:published-result
  @covers:behavior:rr:labeled-ability.1:published-result
  @covers:behavior:rr:labeled-ability.2:published-result
  @covers:behavior:rr:you-your.12:published-result
  @covers:behavior:rr:choose-game-element:published-result
  @covers:behavior:rr:choose-game-element.1:published-result
  @covers:behavior:rr:target.3:published-result
  @covers:behavior:rr:resource.4:published-result
  @covers:behavior:rr:resolve.1:published-result
  @card:01005 @rr:attack-player-ability-type.2 @rr:cost.3 @rr:event
  @rr:in-play-and-out-of-play.7
  @rr:initiating-abilities.step.1 @rr:initiating-abilities.step.3
  @rr:initiating-abilities.step.5 @rr:initiating-abilities.step.6
  @rr:initiating-abilities.step.7 @rr:play-put-into-play
  @rr:play-put-into-play.2 @rr:ownership-and-control.7.3 @rr:player-turn.5
  @rr:labeled-ability.1 @rr:labeled-ability.2 @rr:you-your.12
  @rr:choose-game-element @rr:choose-game-element.1 @rr:target.3
  @rr:resource.4 @rr:resolve.1
  Scenario: Swinging Web Kick pays, chooses an enemy, deals eight, and discards
    # "Hero Action (attack): Deal 8 damage to an enemy." An event is placed
    # faceup while it resolves, its resource cost is paid from hand, and after
    # the selected enemy takes eight damage the event enters its owner's pile.
    # An attack-labeled event is performed by the player's identity, so Klaw's
    # Retaliate damage is dealt to Spider-Man.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 709  |
    And seat 1 shows identity face 01001a
    And card 01119 copy 0 is attached to card 01113 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01005 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01005 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01005 copy 0 is faceup in the resolving area
    And card 01113 copy 0 is offered by the pending action
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's discard pile
    When seat 1 chooses card 01113 copy 0 for the pending action
    Then card 01113 copy 0 has 8 damage
    And card 01001a copy 0 has 1 damage
    And card 01005 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01049:move-1-damage-from-your-hero-enemy-condition-met
  @covers:behavior:card:01049:play-wakanda-forever-event-use-ability
  @covers:behavior:card:01043a:resolve-special-ability-on-each-black-panther
  @covers:behavior:card:01043a:resolving-each-ability-is-step-in-sequence
  @covers:behavior:rr:move.3.1:published-result
  @covers:behavior:rr:move.4:published-result
  @covers:behavior:rr:move.5:published-result
  @covers:behavior:rr:referential-ability.step.2:published-result
  @card:01043a @card:01049 @rr:move.3.1 @rr:move.4 @rr:move.5
  @rr:referential-ability.step.2
  Scenario: Vibranium Suit moves two damage as the final Wakanda Forever step
    # "Move 1 damage from your hero to an enemy (2 damage instead if this is
    # the final step of this sequence)." With Vibranium Suit as the only Black
    # Panther upgrade, its Special is necessarily the final step: two damage is
    # healed from Black Panther and the same amount is dealt to Rhino.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 735  |
    And seat 1 shows identity face 01040a
    And card 01040a copy 0 has 2 damage
    And card 01049 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card   | copy |
      | 01043a | 0    |
      | 01088  | 0    |
    When seat 1 initiates card 01043a copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to order 1 card for the pending action
    When seat 1 orders these cards for the pending action
      | card  | copy |
      | 01049 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01040a copy 0 has 0 damage
    And card 01094 copy 0 has 2 damage
    And 1 Move_Damage event was emitted
    And 1 Attack event was emitted

  @behavior:rr:move.2:published-result
  @covers:behavior:card:01049:move-1-damage-from-your-hero-enemy-condition-not-met
  @rr:move.2 @card:01043a @card:01049
  Scenario: Vibranium Suit cannot move damage when its hero has none
    # Rhino remains a valid enemy target, but the undamaged hero is not a valid
    # source for the move. The Special resolves without creating damage.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 841  |
    And seat 1 shows identity face 01040a
    And card 01049 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card   | copy |
      | 01043a | 0    |
      | 01088  | 0    |
    When seat 1 initiates card 01043a copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to order 1 card for the pending action
    When seat 1 orders these cards for the pending action
      | card  | copy |
      | 01049 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01040a copy 0 has 0 damage
    And card 01094 copy 0 has 0 damage
    And 0 Move_Damage events were emitted

  @behavior:rr:cancel.3:published-result
  @covers:behavior:rr:status-cards.2:published-result
  @covers:behavior:rr:labeled-ability.6:published-result
  @covers:behavior:rr:labeled-ability.6.1:published-result
  @covers:behavior:rr:labeled-ability.6.2:published-result
  @rr:cancel.3 @rr:status-cards.2 @rr:labeled-ability.6
  @rr:labeled-ability.6.1 @rr:labeled-ability.6.2 @card:01005
  Scenario: Stun cancels an event attack after the event is played and paid for
    # "If the effects of an event card are canceled, the card is still
    # considered played, and it is discarded." The status replacement removes
    # stun instead of resolving Swinging Web Kick's attack effect.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 726  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 has a stunned status card
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01005 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01005 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's discard pile
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01001a copy 0 has 0 stunned status cards
    And card 01094 copy 0 has 0 damage
    And card 01005 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01013:if-you-paid-for-card-using-energy-condition-met
  @covers:behavior:card:01013:deal-5-damage-enemy
  @covers:behavior:rr:ability.4:sentence-order
  @covers:behavior:rr:cost.3.1:resource-paid-for-card
  @covers:behavior:rr:energy-resource.1:pays-resource-cost
  @covers:behavior:rr:energy-resource.2:required-by-card-effect
  @covers:behavior:rr:resource.1:discard-card-generates-resource
  @covers:behavior:rr:resource.3:generated-resources-pay-card-cost
  @card:01013 @rr:ability.4 @rr:cost.3.1 @rr:energy-resource.1
  @rr:energy-resource.2 @rr:resource.1 @rr:resource.3
  Scenario: Photonic Blast draws after damage when paid with energy
    # "Deal 5 damage to an enemy. If you paid for this card using an energy
    # resource, draw 1 card."
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | captain_marvel | 710  |
    And seat 1 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01013 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
    When seat 1 initiates card 01013 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And seat 1 has 1 card in hand
    And an Attack event was emitted before a Draw event
    And card 01013 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01013:if-you-paid-for-card-using-energy-condition-not-met
  @covers:behavior:card:01013:deal-5-damage-enemy
  @card:01013
  Scenario: Photonic Blast does not draw when paid without energy
    # The conditional draw occurs only when an energy resource paid for the
    # event; physical and mental resources still pay its cost but not its rider.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 711  |
    And seat 1 shows identity face 01010a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01013 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
    When seat 1 initiates card 01013 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And seat 1 has 0 cards in hand
    And card 01013 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01022:deal-1-damage-each-enemy
  @card:01022
  Scenario: Ground Stomp deals one damage to every enemy
    # "Hero Action: Deal 1 damage to each enemy." The singular action changes
    # both the villain and every engaged minion without a target prompt.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 712  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01022 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01022 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01094 copy 0 has 1 damage
    And card 01101 copy 0 has 1 damage
    And card 01022 copy 0 is faceup on top of seat 1's discard pile

  @behavior:rr:cannot:published-result
  @covers:behavior:rr:cannot.1:published-result
  @covers:behavior:rr:target.3.7:published-result
  @covers:behavior:rr:target.4:published-result
  @covers:behavior:rr:target.4.1:published-result
  @covers:behavior:card:01181:madame-hydra-cannot-take-damage-while-legions
  @rr:cannot @rr:cannot.1 @rr:target.3.7 @rr:target.4 @rr:target.4.1
  @card:01022 @card:01180 @card:01181
  Scenario: An each-enemy effect skips an enemy that cannot take damage
    # Madame Hydra's constant ability says she "cannot take damage" while
    # Legions of Hydra is in play. Ground Stomp can still resolve because Rhino
    # is a valid member of its each-enemy target set, but it cannot damage her.
    Given a canonical Core scene is dealt
      | campaign | heroes   | modular sets     | seed |
      | rhino    | she_hulk | legions_of_hydra | 829  |
    And seat 1 shows identity face 01019a
    And card 01180 copy 0 is a side scheme in play
    And card 01181 copy 0 is a minion engaged with seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01022 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01022 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01094 copy 0 has 1 damage
    And card 01181 copy 0 has 0 damage
    And card 01022 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01054:deal-5-damage-enemy
  @card:01054
  Scenario: Uppercut deals five damage to its chosen enemy
    # "Hero Action (attack): Deal 5 damage to an enemy."
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 713  |
    And seat 1 shows identity face 01019a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01054 | 0    |
      | 01088 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01054 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 5 damage
    And card 01054 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01087:deal-3-damage-enemy
  @covers:behavior:rr:mental-resource.1:pays-resource-cost
  @card:01087 @rr:mental-resource.1
  Scenario: Haymaker deals three damage to its chosen enemy
    # "Hero Action (attack): Deal 3 damage to an enemy."
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 714  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01087 | 0    |
      | 01089 | 0    |
    When seat 1 initiates card 01087 copy 0's action paying with these cards
      | card  | copy |
      | 01089 | 0    |
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 3 damage
    And card 01087 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01053:if-you-paid-for-card-using-physical-condition-met
  @covers:behavior:card:01053:deal-5-damage-minion
  @covers:behavior:card:01053:excess-damage-from-attack-is-dealt-villain
  @covers:behavior:rr:physical-resource.1:pays-resource-cost
  @covers:behavior:rr:physical-resource.2:required-by-card-effect
  @covers:behavior:rr:overkill.2:published-result
  @card:01053 @rr:physical-resource.1 @rr:physical-resource.2 @rr:overkill.2
  Scenario: Relentless Assault gains overkill when physical pays its cost
    # "If you paid for this card using a physical resource, this attack gains
    # overkill." Strength's two physical resources exactly pay the cost, so
    # damage beyond the defeated minion's hit points reaches the villain.
    Given a canonical Core scene is dealt
      | campaign | heroes   | modular sets | seed |
      | rhino    | she_hulk | under_attack | 724  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01153 copy 0 is attached to card 01094 copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01053 | 0    |
      | 01090 | 0    |
    When seat 1 initiates card 01053 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
    Then card 01101 copy 0 is offered by the pending action
    When seat 1 chooses card 01101 copy 0 for the pending action
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01094 copy 0 has 2 damage
    And card 01019a copy 0 has 0 damage
    And card 01053 copy 0 is faceup on top of seat 1's discard pile

  @behavior:rr:overkill.4:published-result
  @rr:overkill.4 @card:01053
  Scenario: Tough prevents all excess damage from an overkill attack
    # Relentless Assault defeats the minion, then its two excess attack damage
    # is dealt to Rhino as one instance. Tough prevents that entire instance.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 845  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01094 copy 0 has a tough status card
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01053 | 0    |
      | 01090 | 0    |
    When seat 1 initiates card 01053 copy 0's action paying with these cards
      | card  | copy |
      | 01090 | 0    |
    Then card 01101 copy 0 is offered by the pending action
    When seat 1 chooses card 01101 copy 0 for the pending action
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01094 copy 0 has 0 damage
    And card 01094 copy 0 has 0 tough status cards

  @behavior:card:01053:if-you-paid-for-card-using-physical-condition-not-met
  @covers:behavior:card:01053:deal-5-damage-minion
  @covers:behavior:rr:cost.4:published-result
  @covers:behavior:rr:cost.4.1:published-result
  @covers:behavior:rr:cost.4.2:published-result
  @covers:behavior:rr:resource.5:excess-resources-lost
  @card:01053 @rr:cost.4 @rr:cost.4.1 @rr:cost.4.2 @rr:resource.5
  Scenario: An overpaid physical resource does not grant overkill
    # The effective Cost 4 authority permits overpayment but says resources
    # beyond the cost are spent, not paid for that cost. Energy pays the
    # two-resource cost; the later physical resources are discarded as
    # overpayment and cannot satisfy Relentless Assault's condition.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 725  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01053 | 0    |
      | 01088 | 0    |
      | 01090 | 0    |
    When seat 1 initiates card 01053 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01090 | 0    |
    Then card 01101 copy 0 is offered by the pending action
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01090 copy 0 is in seat 1's discard pile
    When seat 1 chooses card 01101 copy 0 for the pending action
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01094 copy 0 has 0 damage
    And card 01053 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01031:deal-1-damage-enemy-and-discard-top
  @covers:behavior:card:01031:for-each-printed-energy-resource-discarded-way-multiple
  @covers:behavior:rr:and:published-result
  @covers:behavior:rr:and.1:published-result
  @covers:behavior:rr:and.2:published-result
  @covers:behavior:faq:01031:published-clarification-1
  @card:01031 @rr:and @rr:and.1 @rr:and.2 @faq:01031
  Scenario: Repulsor Blast's combined damage is prevented without canceling its discard
    # Repulsor Blast deals its base and additional damage as one simultaneous
    # five-damage instance. Tough prevents that entire instance; independently,
    # the effect joined by "and" still discards all five cards from the deck.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 834  |
    And seat 1 shows identity face 01029a
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has a tough status card
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01031 | 0    |
      | 01086 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01032     | 0    |
      | 01035     | 0    |
      | 01037     | 0    |
      | 01039     | 0    |
      | 01054     | 0    |
    When seat 1 initiates card 01031 copy 0's action paying with these cards
      | card  | copy |
      | 01086 | 0    |
    Then card 01101 copy 0 is offered by the pending action
    When seat 1 chooses card 01101 copy 0 for the pending action
    Then card 01101 copy 0 has 0 damage
    And card 01101 copy 0 has 0 tough status cards
    And card 01032 copy 0 is in seat 1's discard pile
    And card 01035 copy 0 is in seat 1's discard pile
    And card 01037 copy 0 is in seat 1's discard pile
    And card 01039 copy 0 is in seat 1's discard pile
    And card 01054 copy 0 is in seat 1's discard pile
    And card 01031 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01023:choose-and-discard-up-5-cards-from-minimum
  @covers:behavior:rr:cost.8:published-result
  @covers:behavior:rr:cost.9:published-result
  @card:01023 @rr:cost.8 @rr:cost.9
  Scenario: Legal Practice requires and accepts its minimum of one card
    # "A cost requiring ... 'up to' some number of game elements requires a
    # minimum of one." Because the cost names cards outside play, the payer can
    # use only cards in their own hand. One discarded card removes one threat.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 726  |
    And card 01097b copy 0 has 6 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01023 | 0    |
      | 01024 | 0    |
    When seat 1 initiates card 01023 copy 0's action without payment
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 and discards these cards for the pending action
      | card  | copy |
      | 01024 | 0    |
    Then card 01097b copy 0 has 5 threat counters
    And card 01024 copy 0 is in seat 1's discard pile
    And card 01023 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01023:choose-and-discard-up-5-cards-from-intermediate
  @covers:behavior:rr:for-each:published-result
  @covers:behavior:rr:for-each.1:published-result
  @covers:behavior:rr:for-each.2:published-result
  @covers:behavior:rr:labeled-ability.4:published-result
  @covers:behavior:rr:thwart.2:published-result
  @covers:behavior:rr:you-your.8:published-result
  @rr:for-each @rr:for-each.1 @rr:for-each.2 @rr:labeled-ability.4
  @rr:thwart.2 @rr:you-your.8
  @card:01023
  Scenario: Legal Practice scales to an intermediate three-card cost
    # The chosen quantity is part of the cost; the effect removes exactly one
    # threat for each of the three cards discarded this way.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 727  |
    And card 01097b copy 0 has 6 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01023 | 0    |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
    When seat 1 initiates card 01023 copy 0's action without payment
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 and discards these cards for the pending action
      | card  | copy |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
    Then card 01097b copy 0 has 3 threat counters
    And card 01024 copy 0 is in seat 1's discard pile
    And card 01024 copy 1 is in seat 1's discard pile
    And card 01024 copy 2 is in seat 1's discard pile
    And card 01023 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01023:choose-and-discard-up-5-cards-from-maximum
  @card:01023
  Scenario: Legal Practice accepts no more than its maximum of five cards
    # "Up to 5" permits five cards. Paying that maximum removes five threat,
    # and the event itself is not one of the cards chosen from its owner's hand.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 728  |
    And card 01097b copy 0 has 6 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01023 | 0    |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
      | 01027 | 0    |
      | 01027 | 1    |
    When seat 1 initiates card 01023 copy 0's action without payment
    Then card 01097b copy 0 is offered by the pending action
    When seat 1 chooses card 01097b copy 0 and discards these cards for the pending action
      | card  | copy |
      | 01024 | 0    |
      | 01024 | 1    |
      | 01024 | 2    |
      | 01027 | 0    |
      | 01027 | 1    |
    Then card 01097b copy 0 has 1 threat counter
    And card 01024 copy 0 is in seat 1's discard pile
    And card 01024 copy 1 is in seat 1's discard pile
    And card 01024 copy 2 is in seat 1's discard pile
    And card 01027 copy 0 is in seat 1's discard pile
    And card 01027 copy 1 is in seat 1's discard pile
    And card 01023 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01030:exhaust-war-machine-and-deal-2-damage
  @covers:behavior:rr:cost.11:damage-prevented
  @covers:behavior:rr:prevent.1.4:published-result
  @covers:behavior:rr:damage.3.2:published-result
  @covers:behavior:rr:prevent.1.1:published-result
  @covers:behavior:rr:enemy:villain-or-minion
  @card:01030 @rr:cost.11 @rr:prevent.1.1 @rr:prevent.1.4
  @rr:damage.3.2 @rr:enemy
  Scenario: Preventing War Machine's dealt-damage cost does not prevent its effect
    # "If dealing damage is a cost, that cost is considered paid even if some
    # or all of that damage is prevented." Tough prevents both damage, but the
    # paid cost still exhausts War Machine and its action damages every enemy.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | iron_man | 729  |
    And seat 1 shows identity face 01029a
    And card 01030 copy 0 is an ally controlled by seat 1
    And card 01030 copy 0 has a tough status card
    And card 01101 copy 0 is a minion engaged with seat 1
    When seat 1 initiates card 01030 copy 0's action without payment
    Then card 01030 copy 0 is exhausted
    And card 01030 copy 0 has 0 tough status cards
    And card 01030 copy 0 has 0 damage
    And card 01094 copy 0 has 1 damage
    And card 01101 copy 0 has 1 damage

  @behavior:card:01027:exhaust-focused-rage-and-take-1-damage
  @covers:behavior:rr:cost.12:all-damage-taken
  @covers:behavior:rr:prevent.1.5:published-result
  @card:01027 @rr:cost.12 @rr:prevent.1.5
  Scenario: Focused Rage pays its take-damage cost before drawing
    # "That cost is not considered paid unless all of that damage was taken."
    # She-Hulk takes the printed one damage, Focused Rage exhausts, and only
    # then does the post-arrow draw resolve.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 730  |
    And seat 1 shows identity face 01019a
    And card 01027 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01020 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01024     | 0    |
    When seat 1 initiates card 01027 copy 0's action without payment
    Then card 01027 copy 0 is exhausted
    And card 01019a copy 0 has 1 damage
    And seat 1 has 2 cards in hand

  @behavior:rr:cost.12:damage-prevented
  @covers:behavior:rr:cost.5:simultaneous-costs
  @covers:behavior:rr:prevent.1.5:published-result
  @rr:cost.12 @rr:prevent.1.5 @card:01027
  @rr:cost.5
  Scenario: Tough makes Focused Rage's take-damage cost unpayable
    # A take-damage cost "is not considered paid unless all of that damage was
    # taken." Tough would prevent the one damage, so the action cannot be
    # initiated and neither the status card nor the game state is changed.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 731  |
    And seat 1 shows identity face 01019a
    And card 01027 copy 0 is an upgrade attached to seat 1's identity
    And card 01019a copy 0 has a tough status card
    When seat 1 asks for available card actions
    Then card 01027 copy 0's action is unavailable
    And card 01027 copy 0 is ready
    And card 01019a copy 0 has a tough status card
    And card 01019a copy 0 has 0 damage

  @behavior:rr:ability.3:requires-valid-target
  @covers:behavior:rr:choose-game-element.2:requires-valid-target
  @covers:behavior:rr:cost.6:requires-valid-target
  @covers:behavior:rr:event.3:requires-valid-target
  @covers:behavior:rr:target.2:requires-valid-target
  @covers:behavior:rr:target.2.2:choose-requires-target
  @covers:behavior:rr:initiating-abilities.step.2:published-result
  @covers:behavior:rr:play-restrictions-and-permissions.1:published-result
  @rr:ability.3 @rr:choose-game-element.2 @rr:cost.6 @rr:event.3
  @rr:target.2 @rr:target.2.2 @rr:initiating-abilities.step.2
  @rr:play-restrictions-and-permissions.1 @card:01023
  Scenario: A targeted event is unavailable when no scheme can be affected
    # An ability that "requires one or more targets" can be initiated only
    # when at least one valid target exists. Legal Practice must choose a
    # scheme and remove threat; at zero threat, the only scheme cannot be
    # affected, so the event action is not offered and no cost can be paid.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 732  |
    And card 01097b copy 0 has 0 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01023 | 0    |
      | 01024 | 0    |
    When seat 1 asks for available card actions
    Then card 01023 copy 0's action is unavailable
    And seat 1 has 2 cards in hand
    And card 01097b copy 0 has 0 threat counters

  @behavior:rr:ability.2:in-play-player-card-ability
  @covers:behavior:rr:in-play-and-out-of-play.4:published-result
  @covers:behavior:rr:in-play-and-out-of-play.5:published-result
  @covers:behavior:rr:in-play-and-out-of-play.8:published-result
  @covers:behavior:rr:upgrade.1:published-result
  @rr:ability.2 @rr:in-play-and-out-of-play.4
  @rr:in-play-and-out-of-play.5
  @rr:in-play-and-out-of-play.8 @rr:upgrade.1 @card:01027
  Scenario: An upgrade action is active in play and inactive in hand
    # Abilities on upgrades "may only be used if the card is in play," unless
    # the text expressly refers to an out-of-play state. One Focused Rage is
    # attached in play; its second legal copy remains in its owner's hand.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 733  |
    And seat 1 shows identity face 01019a
    And card 01027 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01027 | 1    |
    When seat 1 asks for available card actions
    Then card 01027 copy 0's action is available
    And card 01027 copy 1's action is unavailable
    And card 01027 copy 0 remains attached to seat 1's identity

  @behavior:rr:ability.13:hero-form-required
  @covers:behavior:rr:in-play-and-out-of-play.1:published-result
  @covers:behavior:rr:identity.4:published-result
  @covers:behavior:rr:player-turn.5.1:published-result
  @covers:behavior:rr:upgrade.4:published-result
  @covers:behavior:rr:you-your.14:published-result
  @rr:ability.13 @rr:in-play-and-out-of-play.1 @rr:identity.4
  @rr:player-turn.5.1 @rr:upgrade.4 @rr:you-your.14 @card:01027
  Scenario: A Hero Action becomes available only in hero form
    # A bold trigger containing "Hero" can be used only in hero form. Focused
    # Rage is in play throughout; changing form is the only changed condition.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 734  |
    And seat 1 shows identity face 01019b
    And card 01027 copy 0 is an upgrade attached to seat 1's identity
    When seat 1 asks for available card actions
    Then card 01027 copy 0's action is unavailable
    When seat 1 changes form by flipping their identity
    Then seat 1 is in hero form
    When seat 1 asks for available card actions
    Then card 01027 copy 0's action is available

  @behavior:rr:target.5:published-result
  @rr:target.5 @card:01092
  Scenario: A future-target discount initiates before there is a card to play
    # Helicarrier chooses the current player but refers to "the next card" only
    # as a future target. An empty hand therefore does not prevent its Action
    # from initiating and creating the pending discount.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 833  |
    And seat 1 shows identity face 01001a
    And card 01092 copy 0 is a support controlled by seat 1
    And seat 1's hand is empty
    When seat 1 asks for available card actions
    Then card 01092 copy 0's action is available
    When seat 1 initiates card 01092 copy 0's action without payment
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 chooses card 01001a copy 0 for the pending action
    Then card 01092 copy 0 is exhausted
    And seat 1 has 0 cards in hand

  @behavior:rr:initiating-abilities.step.4:published-result
  @covers:behavior:card:01092:exhaust-helicarrier-choose-player
  @covers:behavior:card:01092:reduce-resource-cost-next-card-that-player
  @rr:initiating-abilities.step.4 @card:01092
  Scenario: Helicarrier modifies the next card's determined cost
    # After the printed cost is determined, Helicarrier's lasting modifier is
    # applied at initiation step 4. Mockingbird's cost is reduced from 3 to 2.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 842  |
    And card 01092 copy 0 is a support controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01083 | 0    |
    When seat 1 initiates card 01092 copy 0's action without payment
    Then card 01019a copy 0 is offered by the pending action
    When seat 1 chooses card 01019a copy 0 for the pending action
    Then card 01092 copy 0 is exhausted
    And card 01083 copy 0 has modified resource cost 2 for seat 1

  @behavior:rr:target.6:published-result
  @covers:behavior:card:01041:after-shuri-enters-play-search-your-deck
  @covers:behavior:card:01041:shuffle-your-deck
  @rr:target.6 @card:01041
  Scenario: Shuri may search a deck that contains no upgrade
    # A search needs a searchable game area, not a matching card. Shuri's
    # response therefore initiates against this nonempty deck, finds no
    # upgrade, and still shuffles the searched deck.
    Given a canonical Core scene is dealt
      | campaign | heroes        | seed |
      | rhino    | black_panther | 835  |
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01041 | 0    |
      | 01044 | 0    |
    And seat 1's player deck contains only these next cards
      | next card | copy |
      | 01042     | 0    |
      | 01043a    | 0    |
      | 01043b    | 0    |
      | 01043c    | 0    |
      | 01043d    | 0    |
      | 01043d    | 1    |
      | 01044     | 1    |
      | 01044     | 2    |
      | 01045     | 0    |
      | 01075     | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 plays card 01041 copy 0 paying with these cards
      | card  | copy |
      | 01044 | 0    |
    Then card 01041 copy 0 is offered by the pending action
    When seat 1 chooses card 01041 copy 0 for the pending action
    Then card 01041 copy 0 remains an ally controlled by seat 1
    And seat 1 has 6 cards in their player deck
    And seat 1's player deck has card 01044 on top

  @behavior:card:01056:uses-3-attack-counters
  @covers:behavior:card:01056:enters-play-with-3-counters
  @covers:behavior:card:01056:when-those-are-gone-discard-card
  @covers:behavior:card:01056:exhaust-tac-team-and-remove-1-attack
  @covers:behavior:rr:uses-x-type:published-result
  @covers:behavior:rr:uses-x-type.1:published-result
  @covers:behavior:rr:referential-ability.step.1:published-result
  @covers:behavior:rr:cost.1:published-result
  @covers:behavior:rr:ready:published-result
  @covers:behavior:rr:support.1:published-result
  @covers:behavior:rr:support.2:published-result
  @covers:behavior:rr:in-play-and-out-of-play.3:published-result
  @covers:behavior:rr:ownership-and-control.2:published-result
  @card:01056 @rr:uses-x-type @rr:uses-x-type.1 @rr:cost.1
  @rr:ready @rr:support.1 @rr:support.2 @rr:in-play-and-out-of-play.3
  @rr:referential-ability.step.1
  @rr:ownership-and-control.2
  Scenario: Tac Team enters with three uses and discards after the third action
    # "Uses (3 attack counters)" places three counters as Tac Team enters play.
    # Each action exhausts it, spends exactly one counter, and deals two damage;
    # when the third counter is gone the support is discarded.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 715  |
    When card 01056 copy 0 enters play as a support controlled by seat 1
    Then card 01056 copy 0 has 3 attack counters
    And card 01056 copy 0 is ready
    And card 01056 copy 0 remains a support controlled by seat 1
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 2 damage
    And card 01056 copy 0 has 2 attack counters
    And card 01056 copy 0 is exhausted
    And a Remove_Counter event was emitted before a Deal_Damage event
    When the end-of-player-phase ready step resolves
    Then card 01056 copy 0 is ready
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 4 damage
    And card 01056 copy 0 has 1 attack counter
    When the end-of-player-phase ready step resolves
    Then card 01056 copy 0 is ready
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 6 damage
    And card 01056 copy 0 is faceup on top of seat 1's discard pile

  @behavior:rr:support.3:published-result
  @covers:behavior:rr:you-your.18:published-result
  @rr:support.3 @rr:you-your.18 @card:01056 @card:01052
  Scenario: A support defeating a minion is not the controlling hero defeating it
    # Tac Team performs its own action. Its attack-like damage is not an attack
    # by She-Hulk, so Chase Them Down cannot respond to the minion's defeat.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 837  |
    And card 01056 copy 0 is a support controlled by seat 1
    And card 01101 copy 0 is a minion engaged with seat 1
    And card 01101 copy 0 has 1 damage
    And card 01097b copy 0 has 3 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01052 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01056 copy 0's action without payment
    Then card 01101 copy 0 is offered by the pending action
    When seat 1 chooses card 01101 copy 0 for the pending action
    Then card 01101 copy 0 is faceup on top of the encounter discard pile
    And card 01097b copy 0 has 3 threat counters
    And card 01052 copy 0 is in seat 1's hand

  @behavior:card:01018:spend-x-energy-resources-put-x-energy
  @covers:behavior:card:01018:above-damage-cap
  @covers:behavior:rr:max-maximum.6:published-result
  @covers:behavior:rr:non-numerical-variable:published-result
  @covers:behavior:rr:non-numerical-variable.1:published-result
  @card:01018 @rr:max-maximum.6
  @rr:non-numerical-variable @rr:non-numerical-variable.1
  Scenario: Energy Channel remembers X and caps damage above five counters
    # "Spend X [energy] resources → put X energy counters here." Two Energy
    # Absorptions generate six energy and define X as six. The later attack
    # deals two damage per counter "to a maximum of 10," so six counters still
    # deal ten rather than twelve.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | ultron   | captain_marvel | 716  |
    And seat 1 shows identity face 01010a
    And card 01018 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01014 | 0    |
      | 01014 | 1    |
    When seat 1 initiates card 01018 copy 0's first printed action defining X as 6 paying with these cards
      | card  | copy |
      | 01014 | 0    |
      | 01014 | 1    |
    Then card 01018 copy 0 has 6 energy counters
    And card 01014 copy 0 is in seat 1's discard pile
    And card 01014 copy 1 is in seat 1's discard pile
    When seat 1 initiates card 01018 copy 0's second printed action without payment
    Then card 01134 copy 0 is offered by the pending action
    When seat 1 chooses card 01134 copy 0 for the pending action
    Then card 01134 copy 0 has 10 damage
    And card 01018 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01018:below-damage-cap
  @card:01018
  Scenario: Energy Channel deals two damage per counter below its cap
    # Four energy counters are below the printed maximum, so the attack deals
    # eight damage rather than the capped value of ten.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 871  |
    And seat 1 shows identity face 01010a
    And card 01018 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01012 | 0    |
      | 01014 | 0    |
    When seat 1 initiates card 01018 copy 0's first printed action defining X as 4 paying with these cards
      | card  | copy |
      | 01012 | 0    |
      | 01014 | 0    |
    Then card 01018 copy 0 has 4 energy counters
    When seat 1 initiates card 01018 copy 0's second printed action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 8 damage
    And card 01018 copy 0 is faceup on top of seat 1's discard pile

  @behavior:card:01018:at-damage-cap
  @card:01018
  Scenario: Energy Channel deals ten damage at five counters
    # Five energy counters would produce ten damage, exactly the printed
    # maximum, so no part of the product is lost to the cap.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 872  |
    And seat 1 shows identity face 01010a
    And card 01018 copy 0 is an upgrade attached to seat 1's identity
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01014 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01018 copy 0's first printed action defining X as 5 paying with these cards
      | card  | copy |
      | 01014 | 0    |
      | 01088 | 0    |
    Then card 01018 copy 0 has 5 energy counters
    When seat 1 initiates card 01018 copy 0's second printed action without payment
    Then card 01094 copy 0 is offered by the pending action
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 10 damage
    And card 01018 copy 0 is faceup on top of seat 1's discard pile

  @behavior:rr:max-maximum.3:published-result
  @covers:behavior:rr:max-maximum:published-result
  @covers:behavior:card:01057:max-1-per-player
  @covers:behavior:card:01057:your-hero-gets-1-atk
  @rr:max-maximum @rr:max-maximum.3 @card:01057
  Scenario: Max one per player prevents a second controlled copy
    # "Max 1 per player" restricts how many copies of that title a player may
    # control in play at once. One Combat Training is already attached to
    # She-Hulk, so her second owned copy is absent from the legal play offers.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 717  |
    And seat 1 shows identity face 01019a
    And card 01057 copy 0 is attached to card 01019a copy 0
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01057 | 1    |
      | 01088 | 0    |
      | 01089 | 0    |
    When game setup reaches seat 1's mulligan
    Then seat 1 is offered a mulligan
    When seat 1 keeps every opening-hand card at mulligan
    Then seat 1 is the active player
    When seat 1 asks whether card 01057 copy 1 is available to play
    Then card 01057 copy 1 is unavailable to play
    And card 01057 copy 0 remains attached to seat 1's identity
    And card 01019a copy 0 has modified ATK 4

  @behavior:card:01010a:spend-energy-resource-and-heal-1-damage
  @covers:behavior:card:01010a:limit-once-per-round-within-limit
  @covers:behavior:card:01010a:limit-once-per-round-limit-reached
  @covers:behavior:rr:limit:published-result
  @covers:behavior:rr:target.2.3:published-result
  @card:01010a @rr:limit @rr:target.2.3
  Scenario: Rechannel is available once and consumes its round limit
    # "Spend a [energy] resource and heal 1 damage from Captain Marvel → draw
    # 1 card. (Limit once per round.)" Its first initiation pays and resolves;
    # afterward the same ability is absent for the rest of that round.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 718  |
    And seat 1 shows identity face 01010a
    And card 01010a copy 0 has 1 damage
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01014     | 0    |
    When seat 1 asks for available card actions
    Then card 01010a copy 0's action is available
    When seat 1 initiates card 01010a copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then card 01010a copy 0 has 0 damage
    And card 01014 copy 0 is in seat 1's hand
    And card 01088 copy 0 is in seat 1's discard pile
    When seat 1 asks for available card actions
    Then card 01010a copy 0's action is unavailable

  @behavior:card:01071:pay-printed-cost-ally-in-any-player
  @covers:behavior:rr:play-put-into-play.3:published-result
  @covers:behavior:rr:play-put-into-play.5:published-result
  @covers:behavior:rr:play-restrictions-and-permissions.2:published-result
  @covers:behavior:rr:ownership-and-control.3:published-result
  @covers:behavior:rr:ownership-and-control.7.2:published-result
  @card:01071 @rr:play-put-into-play.3 @rr:play-put-into-play.5
  @rr:ownership-and-control.3 @rr:ownership-and-control.7.2
  @rr:play-restrictions-and-permissions.2
  Scenario: Make the Call controls another player's ally until it leaves play
    # "Pay the printed cost of an ally in any player's discard pile → put that
    # ally into play under your control." Putting Black Cat into play is not
    # playing her, so her After-you-play response does not discard Spider-Man's
    # deck. She enters Captain Marvel's ally area, but when defeated returns to
    # her owner Spider-Man's discard pile.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | rhino    | captain_marvel,spider_man | 719  |
    And card 01002 copy 0 starts in seat 2's discard pile
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01071 | 0    |
      | 01088 | 0    |
    And these cards are next on seat 2's player deck
      | next card | copy |
      | 01003     | 0    |
      | 01004     | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01103     | 0    |
    When seat 1 initiates card 01071 copy 0's action without payment
    Then card 01002 copy 0 is offered by the pending action
    When seat 1 chooses card 01002 copy 0 paying with these cards for the pending action
      | card  | copy |
      | 01088 | 0    |
    Then card 01002 copy 0 is in seat 1's play area
    And card 01003 copy 0 is in seat 2's player deck
    And card 01004 copy 0 is in seat 2's player deck
    And card 01071 copy 0 is in seat 1's discard pile
    And card 01088 copy 0 is in seat 1's discard pile
    When the villain attacks seat 1 with card 01002 copy 0 defending
    Then card 01002 copy 0 is faceup on top of seat 2's discard pile

  @behavior:card:01008:exhaust-web-shooter-and-remove-1-web
  @covers:behavior:card:01008:uses-3-web-counters
  @covers:behavior:card:01008:enters-play-with-3-counters
  @covers:behavior:rr:resource-ability.1:published-result
  @covers:behavior:rr:wild-resource.1:published-result
  @card:01008 @rr:resource-ability.1 @rr:wild-resource.1
  Scenario: Web-Shooter supplies a wild resource while paying an event cost
    # "Hero Resource: Exhaust Web-Shooter and remove 1 web counter from it →
    # generate a [wild] resource." It enters with three uses, and its wild
    # resource pays First Aid's generic cost while the resource window is open.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 720  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 has 2 damage
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01086 | 0    |
    When card 01008 copy 0 enters play as an upgrade controlled by seat 1
    Then card 01008 copy 0 has 3 web counters
    And card 01008 copy 0 is ready
    When seat 1 initiates card 01086 copy 0's action paying with these cards
      | card  | copy |
      | 01008 | 0    |
    Then card 01001a copy 0 is offered by the pending action
    And card 01008 copy 0 has 2 web counters
    And card 01008 copy 0 is exhausted
    When seat 1 chooses card 01001a copy 0 for the pending action
    Then card 01001a copy 0 has 0 damage
    And card 01086 copy 0 is faceup on top of seat 1's discard pile

  @behavior:faq:01071:power-of-aspect-pays-printed-ally-cost
  @covers:behavior:card:01072:double-number-resources-card-generates-while-paying
  @covers:behavior:rr:resource-card.1:published-result
  @faq:01071 @card:01071 @card:01072 @rr:resource-card.1
  Scenario: Power of Leadership doubles for Make the Call's chosen ally
    # The official FAQ says Make the Call pays the chosen ally's printed cost.
    # Maria Hill is a Leadership ally costing two, so Power of Leadership's
    # text is active while it generates resources and its one printed wild
    # resource becomes two, exactly paying that cost.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 721  |
    And card 01067 copy 0 starts in seat 1's discard pile
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01071 | 0    |
      | 01072 | 0    |
    And these cards are next on seat 1's player deck
      | next card | copy |
      | 01068     | 0    |
    When seat 1 initiates card 01071 copy 0's action without payment
    Then card 01067 copy 0 is offered by the pending action
    When seat 1 chooses card 01067 copy 0 paying with these cards for the pending action
      | card  | copy |
      | 01072 | 0    |
    Then seat 1 is offered the "Maria Hill" pending opportunity
    When seat 1 accepts the "Maria Hill" pending opportunity
    Then card 01067 copy 0 is in seat 1's play area
    And card 01068 copy 0 is in seat 1's hand
    And card 01071 copy 0 is in seat 1's discard pile
    And card 01072 copy 0 is in seat 1's discard pile

  @behavior:rr:then:published-result
  @covers:behavior:rr:then.1:published-result
  @covers:behavior:card:01012:remove-2-threat-from-scheme
  @covers:behavior:card:01012:then-if-you-have-aerial-trait-remove-condition-met
  @rr:then @rr:then.1 @card:01012 @card:01017
  Scenario: A fully resolved pre-then thwart attempts the Aerial follow-up
    # Crisis Interdiction removes its full two threat from the first scheme.
    # The text after "Then" must therefore attempt to resolve, and Aerial makes
    # its two-threat removal from the different second scheme applicable.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 830  |
    And seat 1 shows identity face 01010a
    And card 01017 copy 0 is an upgrade attached to seat 1's identity
    And card 01107 copy 0 is a side scheme in play
    And card 01097b copy 0 has 2 threat counters
    And card 01107 copy 0 has 2 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01012 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01012 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose 2 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card   | copy |
      | 01097b | 0    |
      | 01107  | 0    |
    Then card 01097b copy 0 has 0 threat counters
    And card 01107 copy 0 has 0 threat counters

  @behavior:card:01012:then-if-you-have-aerial-trait-remove-condition-not-met
  @card:01012
  Scenario: Crisis Interdiction has no second thwart without Aerial
    # The first instruction removes its full two threat. Captain Marvel has no
    # Aerial trait, so the conditional removal from a different scheme does not
    # occur and the side scheme retains both threat.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 861  |
    And seat 1 shows identity face 01010a
    And card 01107 copy 0 is a side scheme in play
    And card 01097b copy 0 has 2 threat counters
    And card 01107 copy 0 has 2 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01012 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01012 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose 1 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card   | copy |
      | 01097b | 0    |
    Then card 01097b copy 0 has 0 threat counters
    And card 01107 copy 0 has 2 threat counters

  @behavior:rr:then.2:published-result
  @rr:then.2 @card:01012 @card:01017
  Scenario: A partially resolved pre-then thwart skips the Aerial follow-up
    # Removing only the one available threat does not fully resolve the
    # pre-"then" instruction to remove two. The different side scheme remains
    # unchanged because the Aerial follow-up is not attempted.
    Given a canonical Core scene is dealt
      | campaign | heroes         | seed |
      | rhino    | captain_marvel | 831  |
    And seat 1 shows identity face 01010a
    And card 01017 copy 0 is an upgrade attached to seat 1's identity
    And card 01107 copy 0 is a side scheme in play
    And card 01097b copy 0 has 1 threat counter
    And card 01107 copy 0 has 2 threat counters
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01012 | 0    |
      | 01088 | 0    |
    When seat 1 initiates card 01012 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
    Then seat 1 is asked to choose 2 cards for the pending action
    When seat 1 chooses these cards for the pending action
      | card   | copy |
      | 01097b | 0    |
      | 01107  | 0    |
    Then card 01097b copy 0 has 0 threat counters
    And card 01107 copy 0 has 2 threat counters
