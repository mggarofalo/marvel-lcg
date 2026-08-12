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
#     Status -> ForcedInterrupt      proven below, decisively
#     ForcedResponse -> Response     proven below, decisively
#     ForcedInterrupt -> Interrupt   not observable in the core set: no board
#                                    makes 01098 and 01003 simultaneous.
#                                    21 events elsewhere carry both; see
#                                    MARVEL-83
#     Constant -> Status             candidates on WhenUnitWouldAttack and
#                                    WhenUnitWouldTakeDamage, not yet authored
#     Interrupt -> Boost             candidates on WhenSchemeBeDefeated and
#                                    WhenUnitBeDefeated, not yet authored
#     Boost -> ForcedResponse        one candidate event (AfterPhaseBegin)
#
# The candidate lists come from grouping `AbilityFactory.X(AbilityType.Y` by
# factory across every pack and keeping the factories that carry two adjacent
# priorities. Sharing an event is necessary and not sufficient -- the two
# abilities also have to bear on the same *subject*, which is what rules out the
# core-set ForcedInterrupt/Interrupt pair.
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

  Scenario: a tough status card cancels an attack and is discarded
    Given "Rhino" is tough

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Rhino" is not tough

  Scenario: the Armored Rhino Suit takes damage that would have gone to Rhino
    # Spider-Man is printed ATK 2, and all 2 land on the Suit rather than on
    # Rhino. The Suit counts them as damage counters on itself; at 5 it is
    # discarded.
    Given "Armored Rhino Suit" is in play

    When I attack "Rhino"
    Then "Rhino" has 0 damage
    And "Armored Rhino Suit" has 2 "damage" counters

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
