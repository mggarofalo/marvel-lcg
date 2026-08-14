# Printed (Ultron, stage III): "Each [[Drone]] minion gets +1 ATK and +1 hit
# point. Ultron cannot take damage while a [[Drone]] minion is in play.
# When Revealed: Search the encounter deck and discard pile for the Ultron's
# Imperative side scheme and reveal it. Then shuffle the encounter deck."
# Printed ATK 4, SCH 2, 27 [star] hit points.
#
# Stage III, which only the expert scenario contains: `ultron_expert` is built
# from Ultron (II) and Ultron (III) where the standard scenario is built from (I)
# and (II). So every scenario here starts on stage II and defeats it, and the 27
# hit points below are the assertion that says which stage is in play -- the
# other two are printed 17 and 22.
#
# Three printed sentences, five claims, and they come apart cleanly:
#
#   the search      two source zones, and an engine that reads only the first
#                   passes a scenario that only stocks the encounter deck
#   the grant       "each [[Drone]] minion", which is where this card differs
#                   from Upgraded Drones (01142): that one says "each *facedown*
#                   [[Drone]] minion" and leaves Advanced Ultron Drone alone,
#                   and this one does not
#   the immunity    and its control, because "0 damage" is also what an engine
#                   that had forgotten how to damage a villain produces
#
# Two ways of defeating stage II appear below and the choice is forced rather
# than stylistic. Where the board has no Advanced Ultron Drone the hero attacks,
# which is the ordinary path. Where it does, that minion's printed Guard makes
# the villain an illegal target for an attack *and* for Haymaker, so the stage is
# defeated by setting its damage to its printed hit points instead.
#
# Haymaker is how damage is put on stage III at all. A hero exhausts when it
# attacks, so the attack that defeated stage II cannot be repeated in the same
# turn; Haymaker is a basic event that deals 3 to an enemy and costs 2, which one
# Energy pays.

Feature: Ultron III

  Background:
    Given the scenario is "ultron_expert"
    And the hero is "iron_man"
    And I am in hero form
    And "Ultron Drones" is in play

  @card:01136
  Scenario: revealing this stage finds Ultron's Imperative in the encounter deck and reveals it
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And "Ultron" has 21 damage
    And the encounter deck is "Hydra Mercenary", "Ultron's Imperative", "Hydra Mercenary"

    When I attack "Ultron"

    # The stage that is now in play, by its printed hit points.
    Then "Ultron" has 27 health
    And "Ultron" has 0 damage
    # Found and *revealed*, not merely found: it is in play as a side scheme with
    # its own printed starting threat rather than sitting in the encounter deck.
    And "Ultron's Imperative" is in the "SideSchemesArea"
    And "Ultron's Imperative" has 2 threat

  @card:01136
  Scenario: it finds Ultron's Imperative in the encounter discard pile as well
    # "Search the encounter deck *and discard pile*". The encounter deck here
    # holds only Hydra Mercenaries, so an engine that searched one zone would
    # find nothing and this scheme would stay where it was.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And "Ultron" has 21 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And the encounter discard pile is "Ultron's Imperative", "Crowd Control"

    When I attack "Ultron"

    Then "Ultron" has 27 health
    And "Ultron's Imperative" is in the "SideSchemesArea"
    # The other card in the discard pile is not swept up with it: the search
    # names one scheme.
    And "Crowd Control" is in the "EncounterDiscardPile"

  @card:01136
  Scenario: every drone minion gets +1 ATK and +1 hit point, printed statistics or not
    # Both kinds of DRONE minion are on the board when this stage arrives.
    #
    # The facedown drone has no printed statistics at all -- Ultron Drones grants
    # it a base 1/1/1 -- so this card's +1s take it to 2/2. Advanced Ultron Drone
    # prints its own 4 hit points and 1 ATK and is not facedown, so it goes to
    # 5/2. That second one is the whole difference between this card and Upgraded
    # Drones, whose otherwise identical +1s reach only facedown drones.
    #
    # Stage II is defeated by damage rather than by an attack because Advanced
    # Ultron Drone's printed Guard makes the villain an illegal target while it
    # is engaged with me.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "Advanced Ultron Drone" is in play
    And "01144a" is revealed
    And "Ultron" has 22 damage

    Then "Ultron" has 27 health
    And "Drone Minion" has 2 health
    And "Drone Minion" has 2 "attack"
    # Untouched, because this card says nothing about scheming.
    And "Drone Minion" has 1 "scheme"
    And "Advanced Ultron Drone" has 5 health
    And "Advanced Ultron Drone" has 2 "attack"
    And "Advanced Ultron Drone" has 1 "scheme"

  @card:01136
  Scenario: he cannot be damaged while a drone minion is in play
    # Haymaker is played at him with a facedown drone engaged with me, and it
    # does nothing. The event is spent doing it -- the restriction is on the
    # damage, not on the card being playable.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And my hand is "Haymaker", "Energy"
    And "Ultron" has 21 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
    And "01144a" is revealed

    When I attack "Ultron"
    When I play "Haymaker" targeting "Ultron"

    Then "Ultron" has 27 health
    And "Ultron" has 0 damage
    And "Haymaker" is in the "DiscardPile"
    And "Drone Minion" has 2 health

  @card:01136
  Scenario: with no drone in play the same Haymaker lands
    # The control. Without it the scenario above is equally satisfied by an
    # engine that had forgotten how to damage a villain, or one that would not
    # play the card at all.
    Given my deck is "Aunt May", "Energy", "Genius", "Pepper Potts", "Backflip", "Backflip"
    And my hand is "Haymaker", "Energy"
    And "Ultron" has 21 damage
    And the encounter deck is "Hydra Mercenary", "Hydra Mercenary"

    When I attack "Ultron"
    When I play "Haymaker" targeting "Ultron"

    Then "Ultron" has 3 damage
    And "Ultron" has 24 health
    And "Haymaker" is in the "DiscardPile"
