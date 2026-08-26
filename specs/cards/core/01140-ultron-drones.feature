# Printed: "Each facedown [[Drone]] minion engaged with a player has a base SCH
# of 1, a base ATK of 1, and a base hit points of 1.
# Forced Response: After a facedown [[Drone]] minion is defeated, place that card
# in it's owners discard pile."
#
# This is one of three cards in the game that grant a *base* statistic line, and
# the only one of the three that any scenario reaches: 26031 is the same card
# reprinted into the Vision nemesis set, and 50032 (Controlled Innocents) is in
# no scenario at all. That makes this file the only executable description of a
# base grant, so it is written to pin the whole line rather than one number.
#
# A base grant is not a bonus. It *replaces* the printed statistic, and a
# facedown DRONE has no printed statistics at all -- the card underneath is a
# player card, so its printed hit points are whatever a Backflip has, which is
# nothing. Without this environment a drone enters play with 0 hit points and is
# defeated in the same breath, which is the control below and is what makes the
# other scenarios claims about this card rather than about drones in general.
#
# Three layers meet on one drone and they are deliberately kept apart here:
#
#   base       this card (01140), and only for a *facedown* drone
#   keyword    Upgraded Drones (01142) and Ultron (III) (01136), which add to
#              whatever base is underneath
#   printed    Advanced Ultron Drone (01143), a DRONE minion that prints its own
#              4 hit points and is not facedown -- so this card does not touch it
#
# The last scenario is the MARVEL-111 regression. Each grant used to *set* the
# base on application and restore the *printed* value on removal, so two sources
# granting a base to one character did not stack: removing either one wiped the
# other's live grant and the drone reverted to a printed 0 and died. The base is
# an ordered stack per statistic now, and what proves it is a board with two live
# sources where one goes away -- which is why the second source is 26031, the
# Vision nemesis printing of this same card, and not a second copy of 01140.
#
# Android Efficiency (01144a) is what makes the drone in most of these
# scenarios. It is the cheapest card in the set that makes exactly one drone off
# the top of the named player's deck with no other board effect, and its own
# behaviour is specced in 01144-android-efficiency.feature.

Feature: Ultron Drones

  Background:
    Given the scenario is "ultron"
    And the hero is "spider_man"
    And I am in hero form

  @card:01140
  Scenario: a facedown drone is given a base scheme, attack and hit point line
    # All three statistics, because the grant is one line of printed text and an
    # engine that dropped one of the three would still pass a scenario that only
    # checked hit points. The `0` case is not hypothetical either: until
    # MARVEL-108 a base grant was guarded on truthiness, so "a base ATK of 0"
    # and "no base ATK at all" were the same thing to the engine.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Ultron Drones" is in play
    And "01144a" is revealed

    Then "Drone Minion" has 1 health
    And "Drone Minion" has 1 "attack"
    And "Drone Minion" has 1 "scheme"
    # The drone is a card that left my deck and is standing in the engaged
    # enemies area, named by the printed identity it keeps underneath the drone
    # face -- so the three numbers above are about a minion in play.
    And "Aunt May" is in the "EngagedEnemiesArea"
    And I am not prompted again

  @card:01140
  Scenario: without this card a facedown drone has no hit points and is defeated at once
    # The control, and the reason every other Ultron scenario in the suite puts
    # this card into play. The same reveal on the same deck, with the environment
    # left out: nothing grants the drone a base hit point, so it enters play at 0
    # and is defeated immediately. The card that was on top of my deck is in my
    # discard pile rather than engaged with me.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "01144a" is revealed

    Then "Aunt May" is in the "DiscardPile"
    And "Aunt May" is not in play
    And I have 3 cards in my deck
    And I am not prompted again

  @card:01140
  Scenario: a drone minion that is not facedown keeps its own printed statistics
    # "Each *facedown* [[Drone]] minion" is a restriction, and Advanced Ultron
    # Drone is the card it excludes: a DRONE minion, engaged with me, in play
    # alongside this environment, and printed with 4 hit points, 1 ATK and 1 SCH
    # of its own. A base grant replaces a printed line, so an engine that applied
    # this one to every DRONE would take it to 1 hit point and this scenario
    # would read 1 instead of 4.
    #
    # The facedown drone is on the board at the same time, under the same
    # environment, to show the grant is live and is simply not reaching the other
    # minion.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Ultron Drones" is in play
    And "Advanced Ultron Drone" is in play
    And "01144a" is revealed

    Then "Advanced Ultron Drone" has 4 health
    And "Advanced Ultron Drone" has 1 "attack"
    And "Advanced Ultron Drone" has 1 "scheme"
    And "Drone Minion" has 1 health
    And I am not prompted again

  @card:01140
  Scenario: a defeated facedown drone goes to its owner's discard pile
    # The Forced Response. The card is a player card standing up as a minion, so
    # "it's owners discard pile" is my discard pile and not the encounter discard
    # pile that a defeated encounter minion goes to. Spider-Man's printed ATK 2
    # is more than the 1 hit point the environment granted.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "Ultron Drones" is in play
    And "01144a" is revealed

    When I attack "Drone Minion"
    Then "Aunt May" is in the "DiscardPile"
    And "Aunt May" is not in play
    And I have 1 cards in my discard pile
    And I am not prompted again

  @card:01140
  Scenario: two live base grants stack, so removing one leaves the other standing
    # MARVEL-111. 26031 is this same card printed into the Vision nemesis set --
    # not a second script, a second id linked to this one -- so a board holding
    # both has two independent sources granting the same drone a base 1 hit
    # point, 1 ATK and 1 SCH.
    #
    # Each source used to *set* the base when it applied and restore the card's
    # *printed* value when it was removed, so the first source to leave wiped a
    # grant that was still live and the drone dropped to a printed 0 and was
    # defeated. Discarding one of the two is therefore the whole scenario: the
    # drone has to still be standing, with the same statistics, granted by the
    # source that is still in play.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "01140" is in play
    And "26031" is in play
    And "01144a" is revealed
    And "26031" is discarded

    Then "Aunt May" is in the "EngagedEnemiesArea"
    And "Drone Minion" has 1 health
    And "Drone Minion" has 1 "attack"
    And "Drone Minion" has 1 "scheme"
    And I am not prompted again

  @card:01140
  Scenario: removing the only source of the grant does defeat the drone
    # The control for the scenario above, and the thing that makes it an
    # assertion about stacking rather than about discarding an environment. Same
    # board with one source instead of two, discarded the same way: now there is
    # no live grant left, the drone's base hit points fall back to the printed
    # value a Backflip has -- none -- and it is defeated on the spot.
    Given my deck is "Aunt May", "Backflip", "Backflip", "Backflip"
    And "01140" is in play
    And "01144a" is revealed
    And "01140" is discarded

    Then "Aunt May" is in the "DiscardPile"
    And "Aunt May" is not in play
    And I am not prompted again
