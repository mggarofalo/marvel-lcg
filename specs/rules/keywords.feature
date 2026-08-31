# Keyword and icon semantics. MARVEL-23, MARVEL-84.
#
# The core-set shard first covered the five keywords it prints -- Toughness,
# Guard, Retaliate, Surge and Quickstrike. MARVEL-84 adds the seven keyword
# behaviors that first appear in later products: Steady, Stalwart, Patrol,
# Peril, Hinder, Incite and Villainous.
#
# Six icon-like attributes were filed with those keywords. Assault, Amplify,
# Vulnerable and a second acceleration icon are measured below. Crisis already
# has positive and negative controls in damage-and-threat.feature, and hazard
# has its pair in 01107-breakin-and-takin.feature. Those scenarios are reused,
# not copied: the tags on those files are the coverage join. Every scenario is
# still a draft; parsing these transcripts does not claim the C# engine runs
# them. See specs/README.md.
#
# ---------------------------------------------------------------------------
# `the encounter deck is "A", "B", "C"` puts A on top.
#
# A deck literal is written top-first, so the first card named is the next one
# dealt. It matters here more than anywhere else because a villain activation
# takes two cards off the top: the boost card first, then the encounter card
# that is dealt and revealed. So in a three-card list the first is the boost
# card, the second is the one revealed, and the third is what a surge reaches.
#
# It read the other way round until MARVEL-82 and cost an hour of MARVEL-23 to
# work out from behaviour, so it is worth stating wherever scenarios depend on
# it -- which every one below does.
#
# ---------------------------------------------------------------------------
# Why a rules file carries `@card:` tags. MARVEL-120.
#
# It did not until now, and that was an under-count rather than a policy.
# `docs/spec-campaign.md` argues the campaign's denominator is 3,996 and not
# 3,781 *from this file*: Hydra Mercenary and Sandman have no script at all,
# their whole behaviour is printed keywords the engine applies from
# `game/card/face/attribute/`, and the scenarios below pin it. So the campaign
# counted them in the denominator on the strength of these scenarios while
# `tools.spec.coverage` -- which joins on `@card:` -- credited them to nobody.
# The denominator moved and the numerator did not, which is the MARVEL-16 shape
# and the direction a coverage number must never drift on its own. The tag is a
# join key, not a claim: every scenario here already existed, already passed,
# and already said what it says.
#
# `specs/rules/crisis-bypass.feature` has tagged five card ids since MARVEL-90,
# so the practice is settled; this file was simply written before it.
#
# **The rule applied here**: tag the card whose *printed text the scenario is
# written to measure*, positively or as the deliberate control for that same
# keyword. Not every card whose printed number enters the arithmetic. So Rhino
# is untagged throughout even though its ATK 2 is in three of these numbers,
# Hydra Mercenary's boost icon is untagged in the retaliate scenario, and
# "with no guard in play the villain is attackable" is untagged because its
# subject is the basic attack rather than any card. Tagging on contact would
# credit Pepper Potts as deck filler, which is exactly the hollow coverage
# `docs/spec-campaign.md` warns about.

Feature: Keywords

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  # --------------------------------------------------------------------------
  # Toughness
  #
  # "This character enters play with a tough status card." The tough status
  # cancels the next damage entirely, however large, and is discarded doing it.

  @card:01102
  @rr:tough
  Scenario: a tough minion takes no damage from the first attack
    # Sandman is printed 4 hit points and enters play tough. Spider-Man's
    # printed ATK 2 is cancelled in full, not reduced.
    Given the encounter deck is "Sandman", "Sandman"
    And "Sandman #1" is in play

    When I attack "Sandman #1"
    Then "Sandman #1" has 0 damage
    And "Sandman #1" is not tough

  @rr:tough
  @rr:toughness
  Scenario: toughness cancels the damage rather than reducing it
    # The point of "however large". A tough card takes nothing from an attack
    # that would otherwise have defeated it outright.
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #1" is tough

    When I attack "Hydra Mercenary #1"
    Then "Hydra Mercenary #1" has 0 damage
    And "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #1" is not tough

  # --------------------------------------------------------------------------
  # Guard
  #
  # "While this minion is engaged with you, you cannot attack the villain."
  #
  # The engine enforces this by filtering the Attack option's legal targets
  # rather than by removing the option, so the restriction shows up in neither
  # the option set nor any card's state. `Then I cannot attack "<card>"` is the
  # step that can see it (MARVEL-84).

  @card:01101
  @rr:guard.1
  Scenario: a guard minion puts the villain out of reach
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play

    Then I cannot attack "Rhino"

  @card:01101
  @rr:guard.1
  Scenario: the guard itself is still attackable
    # The restriction is about the villain, not about attacking at all. Without
    # this the scenario above would also pass against an engine that had
    # forgotten how to attack.
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play

    When I attack "Hydra Mercenary #1"
    Then "Hydra Mercenary #1" has 2 damage

  @rr:attack-player-ability-type.1
  Scenario: with no guard in play the villain is attackable
    # The control for the restriction. `I cannot attack` must be capable of
    # failing, or the scenario above establishes nothing -- so here is the same
    # board without the minion, where the villain takes the hero's printed 2.
    When I attack "Rhino"
    Then "Rhino" has 2 damage

  @card:01101
  @rr:guard.1
  Scenario: the villain becomes attackable once the guard is defeated
    # Hellcat is printed ATK 1 and the minion has 1 hit point left, so the ally
    # clears the guard and the hero -- still ready, having done nothing yet --
    # attacks the villain for his printed 2.
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hydra Mercenary #1" is in play
    And "Hydra Mercenary #1" has 2 damage
    And "Hellcat" is in play

    Then I cannot attack "Rhino"

    When I choose "attack" on "Hellcat" targeting "Hydra Mercenary #1"
    Then "Hydra Mercenary #1" is not in play

    When I attack "Rhino"
    Then "Rhino" has 2 damage

  # --------------------------------------------------------------------------
  # Retaliate
  #
  # "After this character is attacked, deal N damage to the attacking
  # character." Black Panther is the only core-set identity that prints it.

  @card:01040a
  @rr:retaliate-x.1
  Scenario: retaliate answers the villain that attacked
    # Black Panther is printed Retaliate 1 and 11 hit points; Rhino's printed
    # ATK 2 is boosted to 3 by Hydra Mercenary's boost icon. The hero declines
    # to defend, takes all 3, and Rhino takes 1 back.
    Given the hero is "black_panther"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then I have 3 damage
    And "Rhino" has 1 damage

  @card:01040a
  @rr:retaliate-x.1
  Scenario: retaliate does not fire when the hero is the one attacking
    # "After this character is attacked" -- attacking is not being attacked, so
    # nothing comes back at Black Panther for swinging first.
    Given the hero is "black_panther"

    When I attack "Rhino"
    Then "Rhino" has 2 damage
    And I have 0 damage

  # --------------------------------------------------------------------------
  # Surge
  #
  # "After this card is revealed, reveal 1 additional encounter card."
  #
  # Weapons Runner is the revealed card in both scenarios and the only
  # difference is whether it surges, so the extra minion in play is the surge
  # and nothing else. Per the ordering note above: the first card listed is the
  # boost card, the second is revealed, and the third is what surge reaches.

  @card:01121
  @rr:surge.1
  Scenario: surge reveals one more encounter card
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Weapons Runner", "Hydra Mercenary"

    When I pass
    When I pass
    Then "Weapons Runner" is in play
    And "Hydra Mercenary #2" is in play
    And "Hydra Mercenary #1" is not in play

  @card:01101
  @rr:villain-phase.step.4
  Scenario: a card without surge reveals nothing more
    # The control. Three identical minions: the first boosts, the second is
    # revealed and enters play, and the third is never reached.
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then "Hydra Mercenary #2" is in play
    And "Hydra Mercenary #1" is not in play
    And "Hydra Mercenary #3" is not in play

  # --------------------------------------------------------------------------
  # Quickstrike
  #
  # "After this minion engages your hero, it attacks." The attack is an extra
  # one, taken the moment the minion arrives rather than waiting for the next
  # villain phase, so it shows up as a second defence prompt in the same round.

  @card:01167
  @rr:quickstrike.1
  Scenario: a quickstrike minion attacks the moment it engages
    # Rhino's printed ATK 2 boosted to 3, then Vulture's printed ATK 3 -- 6 in
    # one round. A minion's own attack is not boosted, which is why the second
    # number is the printed one.
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Vulture", "Hydra Mercenary"

    When I pass
    When I pass
    When I pass
    Then "Vulture" is in play
    And I have 6 damage

  @card:01101
  @rr:villain-phase.step.2
  Scenario: a minion without quickstrike waits for the next villain phase
    # The control, and the reason the number above is worth writing down: the
    # same round against a plain minion is one defence and 3 damage.
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then I have 3 damage

  # --------------------------------------------------------------------------
  # Steady
  #
  # A steady character can hold one extra status card of each type and is not
  # considered stunned or confused until it has two of that type.

  @card:27161
  @rr:steady.1
  Scenario: the first stunned status card does not stun a steady character but the second does
    # Kraven the Hunter prints Steady. The setup status is the first card;
    # Speed Cyclone applies the second, so the transcript observes both limits
    # without depending on how duplicate events in one hand are named.
    Given the hero is "quicksilver"
    And "Kraven the Hunter" is in play
    And "Kraven the Hunter" is stunned
    And my hand is "Speed Cyclone", "Always Be Running"
    Then "Kraven the Hunter" is not stunned

    When I choose "Play" on "Speed Cyclone" paying 1 resources targeting "Kraven the Hunter"
    Then "Kraven the Hunter" is stunned

  @card:27161
  @rr:steady.1
  Scenario: the first confused status card does not confuse a steady character but the second does
    # Concussive Blow is used because its damage clause keeps a status-resistant
    # enemy legal. The ordinary-enemy control is already the first scenario in
    # 05031-41014-concussive-blow.feature, where one copy confuses Rhino.
    Given "Kraven the Hunter" is in play
    And "Kraven the Hunter" is confused
    And my hand is "05031", "Backflip", "Backflip", "Backflip"
    Then "Kraven the Hunter" is not confused

    When I play "05031" targeting "Kraven the Hunter"
    Then "Kraven the Hunter" is confused
    And "Kraven the Hunter" has 3 damage

  @card:01101
  @rr:stun-stunned
  Scenario: one stunned status card stuns an ordinary character and leaves it in play
    # The shared control for Steady, Stalwart and Vulnerable: the same first
    # application is enough on Hydra Mercenary, and no discard follows it.
    Given the hero is "quicksilver"
    And "Hydra Mercenary" is in play
    And my hand is "Speed Cyclone", "Always Be Running"

    When I choose "Play" on "Speed Cyclone" paying 1 resources targeting "Hydra Mercenary"
    Then "Hydra Mercenary" is stunned
    And "Hydra Mercenary" is in play

  # --------------------------------------------------------------------------
  # Stalwart
  #
  # Stalwart is stronger than steady: the character cannot hold either status
  # card at all.

  @card:16133
  @rr:stalwart.1
  Scenario: a stalwart character cannot be stunned
    # A pure stun effect cannot legally choose a character that cannot take the
    # status. Hydra Mercenary is present as the control target on the same board.
    Given the hero is "quicksilver"
    And "Kree Lieutenant" is in play
    And "Hydra Mercenary" is in play
    And my hand is "Speed Cyclone", "Always Be Running"

    Then the legal targets for "Play" on "Speed Cyclone" are
      | Rhino           |
      | Hydra Mercenary |

    When I choose "Play" on "Speed Cyclone" paying 1 resources targeting "Hydra Mercenary"
    Then "Kree Lieutenant" is not stunned
    And "Hydra Mercenary" is stunned
    And "Kree Lieutenant" is in play

  @card:16133
  @rr:stalwart.1
  Scenario: a stalwart character cannot be confused
    # Concussive Blow remains playable because its physical-resource clause
    # deals damage even though the status cannot be placed.
    Given "Kree Lieutenant" is in play
    And my hand is "05031", "Backflip", "Backflip", "Backflip"

    When I play "05031" targeting "Kree Lieutenant"
    Then "Kree Lieutenant" is not confused
    And "Kree Lieutenant" has 3 damage

  # --------------------------------------------------------------------------
  # Patrol
  #
  # Patrol removes the main scheme from the engaged player's thwart targets;
  # it does not prevent that player from thwarting a side scheme.

  @card:16119
  @rr:patrol.1
  Scenario: a patrol minion prevents its engaged player from thwarting the main scheme
    Given "Badoon Lieutenant" is in play
    And "Bomb Scare" is in play

    Then I cannot thwart "The Break-In!"

    When I thwart "Bomb Scare"
    Then "Bomb Scare" has 1 threat

  @card:01101
  @rr:thwart.1
  Scenario: a minion without patrol does not restrict the main scheme
    Given "Hydra Mercenary" is in play
    And the main scheme has 5 threat

    When I thwart "The Break-In!"
    Then the main scheme has 4 threat

  # --------------------------------------------------------------------------
  # Peril
  #
  # Peril is a multiplayer affordance restriction. The retired runner had no
  # seat-qualified prompt assertion, so these two draft steps state what a
  # future binding must expose: whether player 2 may trigger their own card
  # while player 1 resolves the encounter card. Enhanced Spider-Sense is
  # printed as a Hero Interrupt when a treachery is revealed from the encounter
  # deck; it does not say "you reveal", so player 2 can play it on the control
  # and is barred only by peril on Blind Side.

  @card:16145
  @rr:peril
  Scenario: another player cannot trigger an ability while I resolve a peril card
    Given the heroes are "black_widow", "spider_man"
    And player 2 is in hero form
    And player 2's hand is "Enhanced Spider-Sense"

    When "Blind Side" is revealed to me
    Then player 2 cannot play "Enhanced Spider-Sense"

  @card:01186
  @rr:interrupt
  Scenario: another player can trigger the same ability while I resolve a card without peril
    Given the heroes are "black_widow", "spider_man"
    And player 2 is in hero form
    And player 2's hand is "Enhanced Spider-Sense"

    When "Advance" is revealed to me
    Then player 2 can play "Enhanced Spider-Sense"

  # --------------------------------------------------------------------------
  # Hinder X
  #
  # Hinder threat is added to the threat the card normally enters play with.

  @card:16066
  @rr:hinder-x.1
  @rr:hinder-x.2
  Scenario: hinder adds its threat to a side scheme's starting threat
    # Blockade prints 2 starting threat and Hinder 2 per player: four at one
    # player, on the card rather than on the main scheme.
    Given "Blockade" is revealed

    Then "Blockade" has 4 threat
    And the main scheme has 0 threat

  @card:16054
  @rr:side-scheme
  Scenario: a side scheme without hinder enters with only its starting threat
    # Vendetta also prints 2 starting threat, but no Hinder attribute.
    Given "Vendetta" is revealed

    Then "Vendetta" has 2 threat
    And the main scheme has 0 threat

  # --------------------------------------------------------------------------
  # Incite X

  @card:04056
  @rr:incite-x.1
  Scenario: revealing a card with incite places its value on the main scheme
    # Hydra Regular prints Incite 1. Its minion hit points are irrelevant to
    # the observable: the threat belongs on The Break-In!.
    Given "Hydra Regular" is revealed

    Then the main scheme has 1 threat
    And "Hydra Regular" is in play

  @card:01101
  @rr:reveal
  Scenario: revealing a minion without incite places no threat on the main scheme
    Given "Hydra Mercenary" is revealed

    Then the main scheme has 0 threat
    And "Hydra Mercenary" is in play

  # --------------------------------------------------------------------------
  # Villainous
  #
  # Both minions below print ATK 1. With Rhino stunned so the villain consumes
  # no boost card, any extra damage from Deathunt 9000 is its own boost.

  @card:11041
  @rr:villainous.1
  Scenario: a villainous minion receives a boost card when it attacks
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Rhino" is stunned
    And "Deathunt 9000" is in play

    When I pass
    When I pass
    Then I have 2 damage

  @card:01101
  @rr:attack-enemy-activation
  Scenario: a minion without villainous attacks without a boost card
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Rhino" is stunned
    And "Hydra Mercenary #1" is in play

    When I pass
    When I pass
    Then I have 1 damage

  # --------------------------------------------------------------------------
  # Assault
  #
  # Assault is stored as a card attribute because it is printed as an icon-like
  # keyword. It changes which basic power supplies a thwart's value.

  @card:43018
  @rr:assault.1
  Scenario: a basic thwart against an assault scheme uses ATK instead of THW
    # Spider-Man prints ATK 2 and THW 1, so the two powers are distinguishable.
    Given "Keep Them Busy" is in play
    And "Keep Them Busy" has 3 threat

    When I thwart "Keep Them Busy"
    Then "Keep Them Busy" has 1 threat

  @card:01109
  @rr:thwart.1
  Scenario: a basic thwart against a scheme without assault uses THW
    Given "Bomb Scare" is in play
    And "Bomb Scare" has 3 threat

    When I thwart "Bomb Scare"
    Then "Bomb Scare" has 2 threat

  # --------------------------------------------------------------------------
  # Amplify icon

  @card:16054
  @rr:amplify-icon.1
  Scenario: an amplify icon adds one icon to a boost card
    # Rhino prints ATK 2 and Hydra Mercenary prints one boost icon. Vendetta's
    # amplify makes that boost worth 2, for 4 damage total.
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Vendetta" is in play

    When I pass
    When I pass
    Then I have 4 damage

  @card:01101
  @rr:boost-boost-icon
  Scenario: the same boost card keeps its printed value with no amplify icon in play
    Given the hero is "iron_man"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then I have 3 damage

  # --------------------------------------------------------------------------
  # Vulnerable

  @card:50083
  @rr:vulnerable.1
  Scenario: a vulnerable character is discarded when it becomes stunned
    Given the hero is "quicksilver"
    And "A.I.M. Scientist" is in play
    And my hand is "Speed Cyclone", "Always Be Running"

    When I choose "Play" on "Speed Cyclone" paying 1 resources targeting "A.I.M. Scientist"
    Then "A.I.M. Scientist" is not in play
    And "A.I.M. Scientist" is in the "EncounterDiscardPile"

  # --------------------------------------------------------------------------
  # Acceleration icon
  #
  # damage-and-threat.feature already proves The Break-In!'s printed icon adds
  # one threat. This pair adds and removes a second icon so the count itself is
  # observable on otherwise identical boards.

  @card:01109
  @rr:acceleration-icon.1
  Scenario: each additional acceleration icon adds one more threat in the villain phase
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"
    And "Bomb Scare" is in play

    When I pass
    When I pass
    Then the main scheme has 2 threat

  @rr:acceleration-icon
  Scenario: with no additional icon only the main scheme's printed acceleration applies
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I pass
    Then the main scheme has 1 threat
