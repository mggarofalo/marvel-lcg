# Printed (01144a): "When Revealed: Each player puts the top card of their deck
# into play facedown, engaged with them as a [[Drone]] minion."
# "[star] Boost: Choose to either spend a [energy] resource or put the top card
# of the deck into play facedown, engaged with you as a [[Drone]] minion."
#
# 01144b and 01144c print the identical text with [mental] and [physical] in
# place of [energy]. The three faces share one ability card, 01144, which is
# what the engine scripts; the resource icon is the only thing that differs, and
# it is read from the face -- so it is worth a scenario per face rather than one
# scenario and a claim that the other two are the same.
#
# ---------------------------------------------------------------------------
# Every scenario here puts "Ultron Drones" in play, and that is not padding.
#
# A DRONE minion has no printed statistics of its own: the Ultron Drones
# permanent is what gives every Drone Minion in play 1 hit point, 1 attack and 1
# scheme. Without it a drone enters play with 0 hit points and is defeated
# immediately, so the card that was put into play facedown lands in the player's
# discard pile and the board looks as though this treachery did nothing. That is
# the real rule applied to an artificial board -- Ultron Drones is put into play
# during the Ultron scenario's setup and every card that makes a drone comes
# from the same encounter set -- but a puzzle scene starts with neither, so each
# scenario has to ask for it. Both spellings were measured: naming the ultron
# scenario does not help, because a puzzle scene strips the villain's setup too.
#
# The drone is addressed by the name of the card that became it. The engine
# swaps the player card's face for the Drone Minion face in place, so the card
# object keeps its printed identity ("Aunt May") while presenting as a minion,
# and that is exactly what a scenario wants to say: the card that was on top of
# the deck is the thing now engaged with me.
#
# Iron Man is the hero in the boost scenarios because his identity carries no
# interrupt and no response -- Spider-Man's Spider-Sense fires on the villain's
# attack and would put a decision in the middle of every one of them. His
# hero-form hand size is 1, which is why one card is drawn at the end of the
# turn and why the spend branch empties the hand.

Feature: Android Efficiency

  Background:
    Given the scenario is "rhino"
    And "Ultron Drones" is in play

  @card:01144 @card:01144a
  Scenario: the reveal takes the top card of the deck and nothing under it
    # "the top card of their deck" is the claim, so the deck is written with a
    # distinguishable card on top and the second card is asserted still in the
    # deck. A scenario that only counted the deck down by one would pass on an
    # engine that reached anywhere into it.
    Given the hero is "spider_man"
    And I am in hero form
    And my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "01144a" is revealed

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Backflip #1" is in the "PlayerDeck"
    And I have 3 cards in my deck
    And "01144a" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01144 @card:01144a
  Scenario: the reveal resolves for every player, off each player's own deck
    # The "each" in "each player puts the top card of their deck into play". The
    # single-player scenario above is equally consistent with an engine that
    # resolved this once, for whoever revealed it -- which is exactly the class
    # of bug MARVEL-16 says self-play will not find, because a bot game that
    # reaches this card at all still only ever sees one board.
    #
    # The two decks are stocked with different cards on top on purpose. Two
    # drones both made from the same card would pass against an engine that took
    # two cards off *one* deck, and the deck counts alone would not separate them
    # either. Naming the two printed identities does.
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And my deck is "Aunt May", "Backflip", "Backflip"
    And player 2's deck is "Pepper Potts", "Energy", "Energy"
    And "01144a" is revealed

    # Each drone under the printed identity of the card that became it: mine off
    # my deck, the second player's off theirs.
    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Pepper Potts" is in the "EngagedEnemiesArea"
    # ...and both under the name the game displays for a facedown drone, which
    # needs an ordinal because there are now two of them. `#N` is creation order,
    # so #1 is the card written first -- mine (MARVEL-102).
    And "Drone Minion #1" is in the "EngagedEnemiesArea"
    And "Drone Minion #2" is in the "EngagedEnemiesArea"
    # One card off each deck, not two off one. This is the assertion the
    # vocabulary could not make before MARVEL-101: the second player's deck had
    # no step that could stock it and none that could count it.
    And player 1 has 2 cards in their deck
    And player 2 has 2 cards in their deck
    And "01144a" is in the "EncounterDiscardPile"
    And I am not prompted again

  @card:01144 @card:01144a
  Scenario: as a boost card it offers the printed [energy] cost, and paying it makes no drone
    # Android Efficiency is written first in the encounter deck, so it is the
    # card dealt face down to boost Rhino's activation. Iron Man draws one card
    # at the end of his turn -- an Energy, which is what pays the cost -- and
    # the deck below it is untouched.
    #
    # 2 damage is the negative that matters. Rhino stage 1 is printed ATK 2 and
    # a star boost adds no icons, so an undefended attack is 2; had a drone been
    # made it would have activated in the same villain phase and added its own
    # 1, which is exactly what the next scenario measures.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Energy", "Aunt May", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "01144a", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Spend [[energy]]                                                                    |
      | Put the top card of the deck into play facedown, engaged with you as a DRONE minion |

    When I choose "Spend [[energy]]"
    Then I have 0 cards in hand
    And I have 7 cards in my deck
    And "Aunt May" is in the "PlayerDeck"
    And I have 2 damage
    And it is round 2

  @card:01144 @card:01144b
  Scenario: the [mental] face offers the same choice, and the drone branch costs no card
    # The other branch of the boost, on the face that prints [mental]. The hand
    # is untouched and the deck is one shorter: the two halves of "either spend
    # a resource or put the top card into play" have to be told apart by both,
    # because either one alone is satisfied by an engine that resolved neither.
    #
    # 3 damage, against the 2 above: Rhino's undefended 2 plus 1 from the drone,
    # which was engaged with the hero in time to activate in the same villain
    # phase. That second Defense prompt is the drone's attack.
    #
    # The deck is led with a Genius rather than an Energy so that the card drawn
    # at the end of the turn can actually pay [mental]. With an Energy there the
    # spend branch is unaffordable and is not offered, which is the scenario
    # below rather than this one.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Genius", "Aunt May", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "01144b", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Spend [[mental]]                                                                    |
      | Put the top card of the deck into play facedown, engaged with you as a DRONE minion |

    When I choose "Put the top card of the deck into play facedown, engaged with you as a DRONE minion"
    Then "Aunt May" is in the "EngagedEnemiesArea"
    And I have 1 card in hand
    And I have 6 cards in my deck
    And I am prompted to choose one
      | Defense |

    When I pass
    Then I have 3 damage
    And it is round 2

  @card:01144 @card:01144c
  Scenario: the [physical] face charges a physical resource for the same boost
    # The third face, and the reason the faces are not one scenario. The deck is
    # stocked with Strength rather than Energy, so the card drawn at the end of
    # the turn is the one this face can actually spend -- an engine that read
    # the cost from the shared ability card instead of from the face would
    # offer [energy] here and fail on the option table.
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Strength", "Aunt May", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "01144c", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I am prompted to choose one
      | Spend [[physical]]                                                                  |
      | Put the top card of the deck into play facedown, engaged with you as a DRONE minion |

    When I choose "Spend [[physical]]"
    Then I have 0 cards in hand
    And "Aunt May" is in the "PlayerDeck"
    And I have 2 damage
    And it is round 2

  @card:01144 @card:01144a
  Scenario: a cost this player cannot pay is not offered, and the other branch just happens
    # The [energy] face against a hand that holds a physical icon and nothing
    # else. "Choose to either spend a [energy] resource or put the top card of
    # the deck into play" has one branch this player can fulfill, so there is no
    # choice to make: the option that cannot be paid for is withheld the way a
    # targetless option is, and the drone is made without a prompt (MARVEL-109).
    #
    # There is no `Then I am prompted to choose one` here on purpose. The step
    # after the Defense is the *drone's* attack, which is only reachable if the
    # boost resolved with no question in between -- an engine that still asked
    # would stop the transcript at a decision it does not answer, which is the
    # harness's `FAIL-spec-wrong`.
    #
    # Before the fix both rows were offered here, and picking the [energy] one
    # was then refused with "Spend_[[energy]] is offered but cannot be paid for".
    Given the hero is "iron_man"
    And I am in hero form
    And my deck is "Strength", "Aunt May", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "01144a", "Hydra Mercenary", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then "Aunt May" is in the "EngagedEnemiesArea"
    # The Strength is still in hand: nothing was spent, because nothing could be.
    And I have 1 card in hand
    And I have 6 cards in my deck
    And I am prompted to choose one
      | Defense |

    When I pass
    Then I have 3 damage
    And it is round 2
