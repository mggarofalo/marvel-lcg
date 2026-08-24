# Timing priority: which of two things that want to happen at the same moment
# happens first. MARVEL-23.
#
# The order is `TimingPriority` in game/ability/ability_type.py:
#
#     Rule  Statistics  Constant  Status  ForcedInterrupt  Interrupt
#     Boost  ForcedResponse  Response  Normal  Consequential  End
#
# ---------------------------------------------------------------------------
# What a transcript can prove about ordering, and what it cannot.
#
# Two abilities of *different* priority prove their order only when both bear on
# the same moment and the order changes what the board looks like afterwards.
# That is a much narrower set than "every adjacent pair", because most abilities
# that could race are printed on cards that never meet: the Armored Rhino Suit
# redirects damage dealt to Rhino and Backflip prevents damage dealt to you, so
# no board makes them simultaneous however it is built.
#
# Where the order *is* observable, this file proves it. Where it is not, saying
# so is better than a scenario that passes without establishing anything.
#
# Five of the twelve levels are reached by no card script in any pack:
#
#     Rule           game/rule/gameplay.py, has_assault, card_status
#     Statistics     game/rule/statistics.py, game/rule/achievement.py
#     Normal         game/ability/factory/on_while.py  (AbilityType.Temp1)
#     Consequential  game/card/face/card_type/ally.py  (ally attack damage)
#     End            game/effect/effect.py, game/operate/faces.py  (Temp2)
#
# They are engine-internal, so no printed card text can name them and no
# scenario can put two of them in a race deliberately. Their *effects* are still
# specifiable -- consequential damage to an attacking ally is a rule a scenario
# can pin -- but that pins the effect, not its position in this list. See
# MARVEL-23 for the finding.
#
# The remaining pairs are covered as follows:
#
#     Constant -> Status             proven below, decisively
#     Status -> ForcedInterrupt      proven below, decisively
#     ForcedResponse -> Response     proven below, decisively
#     ForcedInterrupt -> Interrupt   not observable in the core set: no board
#                                    makes 01098 and 01003 simultaneous.
#                                    21 events elsewhere carry both; see
#                                    MARVEL-83
#     Interrupt -> Boost             proven below, decisively (unblocked by
#                                    MARVEL-91)
#     Boost -> ForcedResponse        not observable: the single candidate is a
#                                    mistyped card. See the end of this file.
#
# The candidate lists come from an index of (event, priority, card) rows built
# by resolving every `AbilityFactory.*` call site in all 64 packs through the
# factory definitions -- 4,172 of 5,788 sites resolved into 4,332 rows over 141
# distinct `Message` classes. The remaining 1,616 sites belong to 51 factories
# that were audited separately and introduce no new overlap for any open pair.
#
# `Message` class is the right axis because `EventManager.BroadcastInternal`
# runs `for priority in list(TimingPriority)` *within a single broadcast*
# (game/event/manager.py:970). Two abilities on different `Message` classes can
# never race, whatever their priorities.
#
# Sharing an event is necessary and not sufficient -- the two abilities also
# have to bear on the same *subject*, which is what rules out the core-set
# ForcedInterrupt/Interrupt pair.
#
# ---------------------------------------------------------------------------
# A note on how the ordering shows up at all.
#
# The engine records the priority of the ability it is asking about, and the
# transcript's beat order is the assertion: if the engine asked in a different
# order, the first `When` would fail as "the engine offered X". That is why the
# interrupt-window scenarios below are written out beat by beat rather than
# collapsed into a setup and one assertion -- a batched form would pass against
# an engine that resolved the same windows in the wrong order.

Feature: Timing priority

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form

  # --------------------------------------------------------------------------
  # Constant (2) before Status (3)
  #
  # A tough status card and a "cannot take damage" constant both want the same
  # `WhenUnitWouldTakeDamage` message, on the same character:
  #
  #   Dragnet (39039)   Constant, "The villain cannot take damage." Registered
  #                     as AbilityFactory.UnitCannotTakeDamageWhile with no
  #                     `while` clause at all, so it is unconditional while the
  #                     side scheme is in play. It calls PreventDamage("All").
  #   the tough card    Status, cancels the damage and is discarded doing it.
  #
  # Whichever fires first consumes the damage. What makes this observable is
  # that the tough ability is *gated* on the damage still being there:
  # game/ability/factory/damage.py registers it under
  #
  #     not message.IsBePrevent() and message.will_take_damage >= 1
  #
  # and `ProcessForcedEffect` re-runs the filter at each priority level. So a
  # Constant that has already prevented the damage makes the tough ability drop
  # out rather than resolve, and the status card is never spent.
  #
  # Both orders leave Rhino on 0 damage. The whole reading is in the status
  # card, which is why the second scenario asserts it.
  #
  # This section needs two controls and only writes one. The other -- a tough
  # card alone being spent -- is the first scenario of the next section, "a tough
  # status card cancels an attack and is discarded", which is the identical
  # transcript. Writing it twice would put one transcript in the trusted suite
  # under two names.

  @rr:ability.step.1
  Scenario: a constant alone prevents the damage
    Given "Dragnet" is in play

    When I attack "Rhino"
    Then "Rhino" has 0 damage

  @rr:ability.step.1
  @rr:ability.step.2.a
  Scenario: the constant resolves first and leaves the tough card unspent
    # The decisive one. If Status had gone first, the tough card would have
    # cancelled the damage and been discarded, and Rhino would end not tough.
    # He ends still tough, so the Constant consumed the damage before the status
    # card was offered it.
    #
    # Verified decisive by mutation: remapping AbilityType.Status to a priority
    # ahead of Constant flips this assertion to "not tough" while both scenarios
    # above continue to pass.
    Given "Dragnet" is in play
    And "Rhino" is tough

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Rhino" is tough

  # --------------------------------------------------------------------------
  # Status (3) before ForcedInterrupt (4)
  #
  # Rhino wearing the Armored Rhino Suit and holding a tough status card is the
  # one board in the core set where two different priorities want the same
  # damage event:
  #
  #   the tough status card   Status, cancels all the damage and is discarded
  #   Armored Rhino Suit      Forced Interrupt, "When any amount of damage would
  #                           be dealt to Rhino, place it here instead"
  #
  # Whichever fires first consumes the damage, and the loser sees nothing. The
  # three scenarios are one experiment: each ability alone establishes what it
  # does, and the third shows which one got the damage when both wanted it.

  @rr:ability.step.2.a
  @rr:tough
  Scenario: a tough status card cancels an attack and is discarded
    Given "Rhino" is tough

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Rhino" is not tough

  @rr:replacement-effect
  Scenario: the Armored Rhino Suit takes damage that would have gone to Rhino
    # Spider-Man is printed ATK 2, and all 2 land on the Suit rather than on
    # Rhino. The Suit counts them as damage counters on itself; at 5 it is
    # discarded.
    Given "Armored Rhino Suit" is in play

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Armored Rhino Suit" has 2 "damage" counters

  @rr:ability.step.2.a
  @rr:tough
  Scenario: toughness resolves first and leaves the Suit nothing to absorb
    # The decisive one. If the Forced Interrupt had gone first, the Suit would
    # hold 2 damage counters and Rhino would still be tough. It holds none and
    # the tough status is gone, so Status resolved first and cancelled the
    # damage the Suit was waiting for.
    Given "Armored Rhino Suit" is in play
    And "Rhino" is tough

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Rhino" is not tough
    And "Armored Rhino Suit" has 0 "damage" counters

  # --------------------------------------------------------------------------
  # Interrupt windows within a single villain attack
  #
  # One attack opens three windows in a fixed order:
  #
  #   WhenUnitWouldAttack      the villain initiates    -- Spider-Sense
  #   WhenUnitBeingAttack      the defence step         -- Defense
  #   WhenUnitWouldTakeDamage  damage is about to land  -- Backflip
  #
  # A scenario cannot assert "these came in this order" in one step. It asserts
  # it by *being* that order: each `When` names the window it answers, and a
  # reordered engine would offer something else at the first one and fail there.
  #
  # These stock both decks for the reason written at the top of
  # phase-structure.feature -- a round draws from both, and a scene that starts
  # empty ends the game rather than reaching the villain phase.

  @rr:interrupt
  @rr:response
  @rr:ability.step.3
  Scenario: a villain attack opens three windows in order
    Given my hand is "Backflip"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "End Phase"

    Then I am prompted to choose one
      | Spider-Sense |

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Play |

    When I pass
    Then I have 3 damage

  @rr:interrupt
  @rr:ability.step.2.c
  Scenario: an interrupt at the damage window prevents the whole attack
    # Backflip is printed "Interrupt (defense): When you would take any amount
    # of damage from an attack, prevent all of that damage." Rhino's boosted
    # attack is 3 and none of it lands.
    Given my hand is "Backflip"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    When I play "Backflip"
    Then I have 0 damage

  @rr:interrupt
  @rr:ability.step.2.c
  Scenario: an interrupt at the initiation window resolves before the attack lands
    # Spider-Sense is printed "Interrupt: When the villain initiates an attack
    # against you, draw 1 card." The attack still lands for its full 3 -- the
    # window it answers is not the damage window -- and the hand is one card
    # larger than the same transcript that declines it.
    #
    # 6 rather than 1: the end of the turn draws Spider-Man up to his printed
    # hand size of 5, and Spider-Sense adds the sixth.
    Given my hand is "Backflip"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "End Phase"
    When I choose "Spider-Sense"
    When I pass
    When I pass
    Then I have 3 damage
    And I have 6 cards in hand

  @rr:forced
  @rr:interrupt
  Scenario: declining every window leaves the hand at hand size
    # The control for the scenario above. Same board, same beats, one answer
    # different, one card fewer.
    Given my hand is "Backflip"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    When I choose "End Phase"
    When I pass
    When I pass
    When I pass
    Then I have 3 damage
    And I have 5 cards in hand

  # --------------------------------------------------------------------------
  # ForcedResponse (7) before Response (8)
  #
  # A minion entering play triggers both, on the same minion:
  #
  #   Taskmaster's Training Camp   Forced Response, "After a minion enters play,
  #                                give it a tough status card"
  #   Hawkeye                      Response, "After a minion enters play, remove
  #                                1 arrow counter from Hawkeye -> deal 2 damage
  #                                to that minion"
  #
  # The order decides whether the damage lands, because a tough status card
  # cancels the next damage entirely. Forced first: the minion is tough before
  # Hawkeye is even offered, and his 2 damage is eaten by the status. Response
  # first: 2 damage lands and the minion is tough afterwards.
  #
  # The two scenarios differ by one Given.

  @rr:response
  @rr:ability.step.4.b
  Scenario: an optional response deals its damage when nothing intervenes
    # Hawkeye is printed to enter play with 4 arrow counters and spends one to
    # do this. Hydra Mercenary is printed 3 hit points, so 2 damage leaves it
    # standing and the reading is unambiguous.
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Hawkeye" is in play
    And "Hydra Mercenary #1" is in play

    When I choose "Response" on "Hawkeye"
    Then "Hydra Mercenary #1" has 2 damage
    And "Hawkeye" has 3 "arrow" counters

  @rr:ability.step.4.a
  @rr:ability.step.4.b
  @rr:forced
  Scenario: a forced response has already resolved when the optional one is offered
    # The decisive one, and it is decisive twice over.
    #
    # The `Then` before the `When` is evaluated at the decision where Hawkeye is
    # offered, and the minion is *already tough* there -- so the Forced Response
    # resolved before the engine asked about the Response at all. Then the
    # damage confirms it: 0 rather than 2, because toughness cancelled it.
    #
    # Reverse the order and both assertions flip: the minion would not yet be
    # tough when Hawkeye is offered, and would end on 2 damage and tough.
    Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "04108" is in play
    And "Hawkeye" is in play
    And "Hydra Mercenary #1" is in play

    Then "Hydra Mercenary #1" is tough

    When I choose "Response" on "Hawkeye"
    Then "Hydra Mercenary #1" has 0 damage
    And "Hydra Mercenary #1" is not tough
    And "Hawkeye" has 3 "arrow" counters

  # --------------------------------------------------------------------------
  # Interrupt (5) before Boost (6)
  #
  # The only board in the corpus where these two bear on the same subject. Both
  # fire on the same `WhenUnitBeDefeated` message for the same minion, and both
  # write the same counter -- threat on the main scheme:
  #
  #   Gatekeeper (32044)  Interrupt, attached to a minion. "When attached minion
  #                       is defeated, remove 4 threat from the main scheme."
  #   Jolt (50133)        Boost, via AbilityType.WhenDefeated. "When Defeated:
  #                       Place 3 threat on the main scheme."
  #
  # Starting from 2 threat: Interrupt first removes 4, clamped to 0, and Jolt
  # then places 3, ending at 3. Boost first places 3 to make 5, and the removal
  # takes it to 1. Verified decisive by mutation -- remapping AbilityType.Interrupt
  # to a priority after Boost gives exactly 1, while both controls keep passing.
  #
  # This section was recorded as BLOCKED until MARVEL-91. Playing Gatekeeper used
  # to stop the game with a two-option prompt whose options had no names, because
  # `TimingPriority.Constant` reached the simultaneous-forced-ability ordering
  # prompt -- and a constant applies continuously, so there is no moment to
  # order. With that fixed the board is reachable and the pair is proven.
  #
  # Jolt enters at 5 hit points and Gatekeeper grants +2, so 5 damage leaves it
  # one Spider-Man attack from defeat.

  @rr:when-defeated-abilities
  Scenario: a when-defeated ability places its threat with nothing to race
    # The control. Jolt alone, defeated, takes the main scheme from 2 to 5.
    Given the main scheme has 2 threat
    And "Jolt" is in play
    And "Jolt" has 3 damage

    When I attack "Jolt"
    Then the main scheme has 5 threat

  @rr:ability.step.2
  @rr:when-defeated-abilities
  Scenario: the interrupt removes its threat before the when-defeated adds any
    # The decisive one. 3, not 1: the removal found 2 threat and clamped to 0,
    # and Jolt's 3 went onto an empty scheme. Had Boost gone first the scheme
    # would have reached 5 and the removal would have left 1.
    Given the main scheme has 2 threat
    And "Jolt" is in play
    And "Gatekeeper" is in play
    And "Jolt" has 5 damage

    When I attack "Jolt"
    When I choose "Interrupt" on "Gatekeeper"
    Then "Jolt" is not in play
    And the main scheme has 3 threat

  @rr:interrupt
  @rr:when-defeated-abilities
  Scenario: declining the interrupt leaves only the when-defeated
    # The second control, and the one that shows the Interrupt is optional. Same
    # board, same beats, the offer declined -- and the result is the first
    # scenario's 5.
    Given the main scheme has 2 threat
    And "Jolt" is in play
    And "Gatekeeper" is in play
    And "Jolt" has 5 damage

    When I attack "Jolt"
    When I pass
    Then the main scheme has 5 threat

  # --------------------------------------------------------------------------
  # Boost (6) before ForcedResponse (7) -- NOT OBSERVABLE
  #
  # Exactly one event in all 64 packs carried both priorities, and it was a
  # mistyped card rather than a race.
  #
  #   AfterPhaseBegin   Boost:          18025 Sibling Rivalry
  #                     ForcedResponse: 16026, 38001a, 43012
  #
  # Sibling Rivalry prints "Forced Response: After the villain phase begins, deal
  # 1 facedown encounter card to Gamora" and was scripted
  # `AbilityType.WhenDefeated`, which maps to Boost. Nothing about the card is a
  # When Defeated, a When Revealed or a boost, so a scenario built on it would
  # have pinned the typo as the rule.
  #
  # MARVEL-89 has since retyped it to `ForcedResponse`, so no event carries both
  # priorities at all now and the pair is not merely unobserved but structurally
  # unreachable. `test_card_dataset.py` guards the class of mistake: a
  # Boost-priority ability type may only be registered on a defeat event.
  #
  # The three ForcedResponse partners fail independently of that. Rogue (38001a)
  # and Puncture Wound (43012) fire when the *player* phase begins, so they can
  # never be simultaneous with it. Blazing Inferno (16026) does share the event,
  # but deals indirect damage to each identity while 18025 deals Gamora an
  # encounter card -- different subjects, neither consuming what the other wants.
  #
  # The structural reason there is nothing else: Boost priority is reached by
  # only five events in the whole corpus (AfterPhaseBegin, WhenCardBecomeBoost,
  # WhenCardRevealed, WhenSchemeBeDefeated, WhenUnitBeDefeated), and the engine
  # consistently splits "When X" messages from "After X" messages. Boost-priority
  # abilities live on the "When" windows and Forced Responses on the "After"
  # ones, so the two populations barely touch. This pair becomes provable only if
  # 18025 is retyped *and* a genuine Boost ability is added to a message that
  # also carries a Forced Response.
