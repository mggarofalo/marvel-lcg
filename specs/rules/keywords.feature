# Keyword semantics. MARVEL-23.
#
# Five keywords, not the eighteen the engine implements in
# game/card/face/attribute/. The core set prints exactly these five --
# Toughness, Guard, Retaliate, Surge and Quickstrike -- and the rest (Steady,
# Stalwart, Patrol, Peril, Hinder, Incite, Villainous and the attribute-level
# ones) first appear in packs 04 through 57. Authoring those means bringing up
# scenarios from those packs, which is a different job; see MARVEL-23.
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
