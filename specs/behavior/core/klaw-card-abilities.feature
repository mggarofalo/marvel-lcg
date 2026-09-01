@core
Feature: Core Klaw card abilities
  Klaw scenario cards resolve from legal Core scenes according to their
  printed text and the shared reveal, attack, status, and attachment rules.

  @behavior:card:01114:search-encounter-deck-and-discard-pile-for
  @covers:behavior:card:01114:shuffle-encounter-deck
  @card:01114
  Scenario: Klaw II reveals The Immortal Klaw and shuffles the encounter deck
    # Defeating Klaw I reveals Klaw II. His When Revealed search reveals The
    # "Immortal" Klaw, then shuffles the searched encounter deck.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 951  |
    And seat 1 shows identity face 01001a
    And card 01113 copy 0 has 10 damage
    When seat 1 uses their basic attack against card 01113 copy 0
    Then card 01114 copy 0 is the faceup villain
    And card 01127 copy 0 is in the villain's play area

  @behavior:card:01114:when-klaw-attacks-give-him-1-additional
  @card:01114
  Scenario: Klaw II receives an additional boost card when he attacks
    # Klaw II's Forced Interrupt gives his attack one additional boost card,
    # so the activation resolves two boost cards rather than one.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 952  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
      | 01187     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then 2 cards were turned faceup as boost cards

  @behavior:card:01115:toughness
  @covers:behavior:card:01115:character-enters-play-with-tough-status-card
  @card:01115
  Scenario: Klaw III enters play with a tough status card
    # Defeating Klaw II reveals Klaw III. Toughness gives the newly entered
    # villain one Tough status card.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 953  |
    And seat 1 shows identity face 01001a
    And card 01114 copy 0 has 17 damage
    When seat 1 uses their basic attack against card 01114 copy 0
    Then card 01115 copy 0 is the faceup villain
    And card 01115 copy 0 has 1 tough status card

  @behavior:card:01115:when-klaw-attacks-give-him-1-additional
  @card:01115
  Scenario: Klaw III receives an additional boost card when he attacks
    # After Klaw III enters, his Forced Interrupt gives the next attack one
    # additional boost card.
    Given a canonical Core scene is dealt
      | campaign    | heroes     | seed |
      | klaw_expert | spider_man | 954  |
    And seat 1 shows identity face 01001a
    And card 01114 copy 0 has 17 damage
    And seat 1's hand is empty
    When seat 1 uses their basic attack against card 01114 copy 0
    Then card 01115 copy 0 is the faceup villain
    When the villain attacks seat 1 with every optional choice declined
    Then 2 cards were turned faceup as boost cards

  @behavior:card:01118:attach-klaw
  @covers:behavior:card:01118:spend-energy-mental-physical-resources-discard-card
  @card:01118
  Scenario: Sonic Converter attaches to Klaw and is discarded for three resources
    # The revealed Converter attaches to Klaw. Its Hero Action spends one
    # energy, mental, and physical resource and discards it.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 955  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01118 copy 0 is revealed to seat 1
    Then card 01118 copy 0 is attached to card 01113 copy 0
    When seat 1 initiates card 01118 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01118 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01119:attach-klaw
  @covers:behavior:card:01119:spend-energy-mental-physical-resources-discard-card
  @card:01119
  Scenario: Solid Sound Body attaches to Klaw and is discarded for three resources
    # The revealed Body attaches to Klaw. Its Hero Action spends one energy,
    # mental, and physical resource and discards it.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 956  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01119 copy 0 is revealed to seat 1
    Then card 01119 copy 0 is attached to card 01113 copy 0
    When seat 1 initiates card 01119 copy 0's action paying with these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01119 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01125:place-additional-1-per-hero-threat-here
  @card:01125
  Scenario: Defense Network adds one threat per player when revealed
    # At two players, Defense Network enters with two starting threat and its
    # When Revealed ability places two additional threat.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 957  |
    When card 01125 copy 0 is revealed to seat 1
    Then card 01125 copy 0 has 4 threat counters

  @behavior:card:01126:place-additional-1-per-hero-threat-here
  @card:01126
  Scenario: Illegal Arms Factory adds one threat per player when revealed
    # At two players, Illegal Arms Factory enters with three starting threat
    # and its When Revealed ability places two additional threat.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 958  |
    When card 01126 copy 0 is revealed to seat 1
    Then card 01126 copy 0 has 5 threat counters

  @behavior:card:01127:klaw-gets-10-hit-points
  @covers:behavior:card:01127:when-scheme-is-defeated-klaw-loses-those
  @card:01127
  Scenario: The Immortal Klaw grants ten hit points only while in play
    # Klaw I has twelve hit points. The side scheme raises that to twenty-two;
    # removing its final threat defeats it and Klaw returns to twelve.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 959  |
    And card 01127 copy 0 is a side scheme in play
    And card 01127 copy 0 has 1 threat counter
    When the printed characteristics of card 01113 copy 0 are requested
    Then card 01113 copy 0 has 22 remaining hit points
    When 1 threat is removed from card 01127 copy 0
    Then card 01127 copy 0 is faceup on top of the encounter discard pile
    And card 01113 copy 0 has 12 remaining hit points

  @behavior:faq:01127:published-clarification-1
  @faq:01127 @card:01127
  Scenario: The Immortal Klaw grants ten hit points to the next villain stage
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 982  |
    And seat 1 shows identity face 01001a
    And card 01127 copy 0 is a side scheme in play
    And card 01113 copy 0 has 21 damage
    When seat 1 uses their basic attack against card 01113 copy 0
    Then card 01114 copy 0 is the faceup villain
    And card 01114 copy 0 has 28 remaining hit points

  @behavior:card:01122:discard-1-card-at-random-from-your
  @card:01122
  Scenario: Klaw's Vengeance discards a random card in alter-ego form
    # The alter-ego branch discards one random card. A one-card hand makes the
    # legal random choice deterministic without prescribing the RNG stream.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 960  |
    And seat 1 shows identity face 01001b
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
    When card 01122 copy 0 is revealed to seat 1
    Then card 01002 copy 0 is in seat 1's discard pile
    And card 01122 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01122:klaw-attacks-you
  @covers:behavior:card:01122:if-attack-deals-damage-place-1-threat-condition-met
  @card:01122
  Scenario: A damaging Vengeance attack places threat on the main scheme
    # In hero form Klaw attacks. Damage from that attack satisfies the printed
    # condition and places one threat on Underground Distribution.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 961  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And card 01116b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
    When card 01122 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 has 2 damage
    And card 01116b copy 0 has 1 threat counters

  @behavior:card:01122:if-attack-deals-damage-place-1-threat-condition-not-met
  @card:01122
  Scenario: A fully defended Vengeance attack places no threat
    # Spider-Man's DEF reduces the calculated attack damage to zero. With no
    # damage dealt, the conditional threat placement does not occur.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 962  |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And card 01116b copy 0 has 0 threat counters
    And these cards are next on the encounter deck
      | next card | copy |
      | 01186     | 0    |
    When card 01122 copy 0 is revealed to seat 1
    Then seat 1 may pass the pending window
    When seat 1 declines the pending opportunity
    Then card 01001a copy 0 is offered by the pending action
    When seat 1 chooses card 01001a copy 0 for the pending action
    Then card 01001a copy 0 has 0 damage
    And card 01116b copy 0 has 0 threat counters

  @behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-1
  @card:01123
  Scenario: Sonic Boom can be paid with all three resource types
    # The first choice spends one energy, one mental, and one physical
    # resource. The identity remains ready after the payment resolves.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 963  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01123 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is offered by the pending decision
    When seat 1 chooses option 1 paying with these cards for the pending encounter-card decision
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    Then card 01088 copy 0 is in seat 1's discard pile
    And card 01089 copy 0 is in seat 1's discard pile
    And card 01090 copy 0 is in seat 1's discard pile
    And card 01001a copy 0 is ready

  @behavior:card:01123:either-spend-energy-mental-physical-resources-or-choice-2
  @card:01123
  Scenario: Sonic Boom can exhaust every controlled character
    # The second choice exhausts the identity and every controlled ally.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 964  |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01123 copy 0 is revealed to seat 1
    Then option 2 is offered by the pending decision
    When seat 1 chooses option 2 for the pending encounter-card decision
    Then card 01001a copy 0 is exhausted
    And card 01083 copy 0 is exhausted

  @behavior:faq:01123:published-clarification-1
  @faq:01123 @card:01123
  Scenario: Sonic Boom cannot choose to exhaust only exhausted characters
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 983  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01083 copy 0 is exhausted
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01088 | 0    |
      | 01089 | 0    |
      | 01090 | 0    |
    When card 01123 copy 0 is revealed to seat 1
    Then option 1 is offered by the pending decision
    And option 2 is not offered by the pending decision

  @behavior:card:01124:klaw-heals-4-damage
  @covers:behavior:card:01124:if-no-damage-was-healed-way-card-condition-not-met
  @card:01124
  Scenario: Sound Manipulation heals four damage in alter-ego form
    # Klaw has four damage to heal, so the full heal resolves and the surge
    # condition is false.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 965  |
    And seat 1 shows identity face 01001b
    And card 01113 copy 0 has 4 damage
    When card 01124 copy 0 is revealed to seat 1
    Then card 01113 copy 0 has 0 damage
    And card 01124 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01124:if-no-damage-was-healed-way-card-condition-met
  @card:01124
  Scenario: Sound Manipulation surges when Klaw has no damage
    # With no damage on Klaw, the heal changes nothing and the treachery gains
    # surge, dealing the next encounter card facedown.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 966  |
    And seat 1 shows identity face 01001b
    And card 01113 copy 0 has 0 damage
    And these cards are next on the encounter deck
      | next card | copy |
      | 01125     | 0    |
    When card 01124 copy 0 is revealed to seat 1
    Then card 01125 copy 0 is facedown in seat 1's encounter queue
    And card 01124 copy 0 is faceup on top of the encounter discard pile

  @behavior:card:01124:take-2-damage
  @covers:behavior:card:01124:klaw-heals-2-damage
  @card:01124
  Scenario: Sound Manipulation damages the hero and heals Klaw
    # The hero branch deals two damage to the identity, then heals two damage
    # from Klaw.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 967  |
    And seat 1 shows identity face 01001a
    And card 01113 copy 0 has 3 damage
    When card 01124 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has 2 damage
    And card 01113 copy 0 has 1 damage

  @behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-met
  @card:01123
  Scenario: Sonic Boom exhausts a hero damaged by the activation
    # Sonic Converter gives Klaw two ATK. Sonic Boom contributes no boost
    # icons, but its boost ability exhausts the hero after that damage lands.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 968  |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And card 01118 copy 0 is attached to card 01113 copy 0
    And these cards are next on the encounter deck
      | next card | copy |
      | 01123     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 2 damage
    And card 01001a copy 0 is exhausted

  @behavior:card:01123:if-activation-deals-damage-you-exhaust-your-condition-not-met
  @card:01123
  Scenario: Sonic Boom leaves an undamaged hero ready
    # Klaw I has zero ATK and Sonic Boom has no boost icons. The activation
    # deals no damage, so the boost condition does not exhaust the hero.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 969  |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01123     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 0 damage
    And card 01001a copy 0 is ready

  @behavior:card:01128:discard-cards-from-encounter-deck-until-masters
  @covers:behavior:card:01128:put-that-minion-into-play-engaged-with
  @card:01128
  Scenario: The Masters of Evil discards until its minion engages the first player
    # The side scheme discards Defense Network, stops at the Masters of Evil
    # minion, and puts Radioactive Man into play with the first player.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 970  |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01125     | 0    |
      | 01129     | 0    |
    When card 01128 copy 0 is revealed to seat 2
    Then card 01129 copy 0 is engaged with seat 1
    And card 01128 copy 0 is in play

  @behavior:card:01129:after-radioactive-man-attacks-you-discard-1
  @card:01129
  Scenario: Radioactive Man discards a random card after attacking
    # Radioactive Man attacks during his engaged player's activation step. A
    # one-card hand makes his forced random discard deterministic.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 971  |
    And seat 1 shows identity face 01001a
    And card 01129 copy 0 is a minion engaged with seat 1
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
    When card 01129 copy 0 attacks seat 1 with every optional choice declined
    Then card 01002 copy 0 is in seat 1's discard pile
    And card 01001a copy 0 has 1 damage

  @behavior:card:01129:discard-1-card-at-random-from-your
  @card:01129
  Scenario: Radioactive Man discards a random hand card as a boost
    # The boost ability resolves independently of the minion text. A one-card
    # hand pins the random discard without depending on a shuffled position.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 972  |
    And seat 1 shows identity face 01001a
    And seat 1's hand contains exactly these cards
      | card  | copy |
      | 01002 | 0    |
    And these cards are next on the encounter deck
      | next card | copy |
      | 01129     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01002 copy 0 is in seat 1's discard pile

  @behavior:card:01130:when-whirlwind-attacks-you-also-resolve-his
  @covers:behavior:ruling:82d66e85735baf99:whirlwind-simultaneous-attacks
  @card:01130 @ruling:82d66e85735baf99
  Scenario: Whirlwind attacks every hero
    # Whirlwind attacks his engaged hero. His forced interrupt also resolves
    # that attack against the other hero.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 973  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    And card 01001a copy 0 is exhausted
    And card 01010a copy 0 is exhausted
    And seat 1's hand is empty
    And seat 2's hand is empty
    And card 01130 copy 0 is a minion engaged with seat 1
    When card 01130 copy 0 attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 2 damage
    And card 01010a copy 0 has 2 damage

  @behavior:card:01130:deal-1-damage-each-hero
  @card:01130
  Scenario: Whirlwind deals one boost damage to each hero
    # The boost ability deals one damage to both heroes, including the hero who
    # is not the target of Klaw's zero-ATK attack.
    Given a canonical Core scene is dealt
      | campaign | heroes                    | seed |
      | klaw     | spider_man,captain_marvel | 974  |
    And seat 1 shows identity face 01001a
    And seat 2 shows identity face 01010a
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01130     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 1 damage
    And card 01010a copy 0 has 1 damage

  @behavior:card:01131:after-tiger-shark-attacks-give-him-tough
  @card:01131
  Scenario: Tiger Shark gains tough after attacking
    # Tiger Shark's forced response gives him Tough after his attack resolves.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 975  |
    And seat 1 shows identity face 01001a
    And card 01131 copy 0 is a minion engaged with seat 1
    And seat 1's hand is empty
    When card 01131 copy 0 attacks seat 1 with every optional choice declined
    Then card 01131 copy 0 has 1 tough status card
    And card 01001a copy 0 has 3 damage

  @behavior:card:01131:give-villain-tough-status-card
  @card:01131
  Scenario: Tiger Shark gives Klaw tough as a boost
    # The boost half of Tiger Shark gives the villain one Tough status card.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 976  |
    And seat 1 shows identity face 01001a
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01131     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01113 copy 0 has 1 tough status card

  @behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-met
  @card:01132
  Scenario: Melter forces an available ally to defend
    # Mockingbird is able to defend, so Melter's constant ability requires her
    # to become the defender. His three attack damage defeats her instead of
    # damaging Spider-Man.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 977  |
    And seat 1 shows identity face 01001a
    And card 01083 copy 0 is an ally controlled by seat 1
    And card 01132 copy 0 is a minion engaged with seat 1
    And seat 1's hand is empty
    When card 01132 copy 0 attacks seat 1 with card 01083 copy 0 defending
    Then card 01083 copy 0 is in seat 1's discard pile
    And card 01001a copy 0 has 0 damage

  @behavior:card:01132:star-engaged-player-must-defend-against-melter-condition-not-met
  @card:01132
  Scenario: Melter attacks the hero when no ally can defend
    # With no controlled ally, the “if able” requirement is false and Melter's
    # undefended attack damages the engaged hero.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 978  |
    And seat 1 shows identity face 01001a
    And card 01132 copy 0 is a minion engaged with seat 1
    And seat 1's hand is empty
    When card 01132 copy 0 attacks seat 1 with every optional choice declined
    Then card 01001a copy 0 has 3 damage

  @behavior:card:01132:exhaust-each-ally-you-control
  @card:01132
  Scenario: Melter exhausts each controlled ally as a boost
    # Both allies are ready when the boost ability resolves, and each is
    # exhausted without becoming the defender.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 979  |
    And seat 1 shows identity face 01001a
    And card 01002 copy 0 is an ally controlled by seat 1
    And card 01083 copy 0 is an ally controlled by seat 1
    And seat 1's hand is empty
    And these cards are next on the encounter deck
      | next card | copy |
      | 01132     | 0    |
    When the villain attacks seat 1 with every optional choice declined
    Then card 01002 copy 0 is exhausted
    And card 01083 copy 0 is exhausted

  @behavior:card:01133:each-masters-evil-minion-attacks-hero-it
  @covers:behavior:card:01133:if-no-attacks-were-made-way-search-condition-not-met
  @card:01133
  Scenario: Masters of Mayhem attacks without searching
    # Radioactive Man is a Masters of Evil minion engaged with the hero, so he
    # attacks for one damage. Because an attack was made, Whirlwind remains in
    # the encounter deck instead of being found by the fallback search.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 980  |
    And seat 1 shows identity face 01001a
    And card 01001a copy 0 is exhausted
    And seat 1's hand is empty
    And card 01129 copy 0 is a minion engaged with seat 1
    When card 01133 copy 0 is revealed to seat 1
    Then card 01001a copy 0 has 1 damage
    And card 01130 copy 0 is in the encounter deck

  @behavior:card:01133:if-no-attacks-were-made-way-search-condition-met
  @card:01133
  Scenario: Masters of Mayhem finds a minion when none attack
    # With no Masters of Evil minion in play, no attack is made. The treachery
    # searches the isolated encounter deck and discard pile for Radioactive
    # Man, puts him into play engaged with the revealing player, and shuffles.
    Given a canonical Core scene is dealt
      | campaign | heroes     | seed |
      | klaw     | spider_man | 981  |
    And the encounter deck contains only these next cards with all other deck cards dealt facedown to seat 1
      | next card | copy |
      | 01125     | 0    |
      | 01129     | 0    |
    When card 01133 copy 0 is revealed to seat 1
    Then card 01129 copy 0 is offered by the pending action
    When seat 1 chooses card 01129 copy 0 for the pending action
    Then card 01129 copy 0 is engaged with seat 1
