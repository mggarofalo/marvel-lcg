@core
Feature: Core card actions
  A player initiates a printed action, pays its costs, resolves each explicit
  choice, and observes the card's published effect.

  @behavior:card:01005:deal-8-damage-enemy
  @covers:behavior:rr:attack-player-ability-type.2:published-result
  @covers:behavior:rr:cost.3:published-result
  @covers:behavior:rr:event:published-result
  @covers:behavior:rr:initiating-abilities.step.1:published-result
  @covers:behavior:rr:initiating-abilities.step.3:published-result
  @covers:behavior:rr:initiating-abilities.step.5:published-result
  @covers:behavior:rr:initiating-abilities.step.6:published-result
  @covers:behavior:rr:initiating-abilities.step.7:published-result
  @covers:behavior:rr:play-put-into-play.2:published-result
  @covers:behavior:rr:player-turn.5:published-result
  @card:01005 @rr:attack-player-ability-type.2 @rr:cost.3 @rr:event
  @rr:initiating-abilities.step.1 @rr:initiating-abilities.step.3
  @rr:initiating-abilities.step.5 @rr:initiating-abilities.step.6
  @rr:initiating-abilities.step.7 @rr:play-put-into-play.2 @rr:player-turn.5
  Scenario: Swinging Web Kick pays, chooses an enemy, deals eight, and discards
    # "Hero Action (attack): Deal 8 damage to an enemy." An event is placed
    # faceup while it resolves, its resource cost is paid from hand, and after
    # the selected enemy takes eight damage the event enters its owner's pile.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | rhino    | spider_man | 709  |
    And seat 1 shows identity face 01001a
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
    And card 01094 copy 0 is offered by the pending action
    And card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's discard pile
    When seat 1 chooses card 01094 copy 0 for the pending action
    Then card 01094 copy 0 has 8 damage
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
  @card:01053 @rr:physical-resource.1 @rr:physical-resource.2
  Scenario: Relentless Assault gains overkill when physical pays its cost
    # "If you paid for this card using a physical resource, this attack gains
    # overkill." Strength's two physical resources exactly pay the cost, so
    # damage beyond the defeated minion's hit points reaches the villain.
    Given a canonical Core scene is dealt
      | campaign | heroes   | seed |
      | rhino    | she_hulk | 724  |
    And seat 1 shows identity face 01019a
    And card 01101 copy 0 is a minion engaged with seat 1
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
    And card 01053 copy 0 is faceup on top of seat 1's discard pile

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
  @covers:behavior:rr:enemy:villain-or-minion
  @card:01030 @rr:cost.11 @rr:prevent.1.4 @rr:enemy
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
  @rr:ability.3 @rr:choose-game-element.2 @rr:cost.6 @rr:event.3
  @rr:target.2 @rr:target.2.2 @card:01023
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
  @covers:behavior:rr:in-play-and-out-of-play.5:published-result
  @covers:behavior:rr:in-play-and-out-of-play.8:published-result
  @covers:behavior:rr:upgrade.1:published-result
  @rr:ability.2 @rr:in-play-and-out-of-play.5
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
  @rr:ability.13 @rr:in-play-and-out-of-play.1 @rr:identity.4 @card:01027
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

  @behavior:card:01056:uses-3-attack-counters
  @covers:behavior:card:01056:enters-play-with-3-counters
  @covers:behavior:card:01056:when-those-are-gone-discard-card
  @covers:behavior:card:01056:exhaust-tac-team-and-remove-1-attack
  @covers:behavior:rr:uses-x-type:published-result
  @covers:behavior:rr:uses-x-type.1:published-result
  @covers:behavior:rr:cost.1:published-result
  @covers:behavior:rr:ready:published-result
  @covers:behavior:rr:support.1:published-result
  @covers:behavior:rr:support.2:published-result
  @covers:behavior:rr:in-play-and-out-of-play.3:published-result
  @card:01056 @rr:uses-x-type @rr:uses-x-type.1 @rr:cost.1
  @rr:ready @rr:support.1 @rr:support.2 @rr:in-play-and-out-of-play.3
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
