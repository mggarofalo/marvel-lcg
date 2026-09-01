# Breakin' & Takin'. Printed:
#
#   "When Revealed: Place an additional 1 [per_hero] threat here.
#    (Hazard Icon: Deal +1 encounter card during the villain phase.)"
#
# Printed 2 starting threat (fixed), boost 2, one hazard icon.
#
# ---------------------------------------------------------------------------
# The threat total is two numbers with different scaling rules, which is the
# whole point of measuring it at two player counts.
#
# The printed 2 is *fixed* and must not move; the 1 the card places carries the
# per-hero star and must. So the total is 3 at one hero and 4 at two, and those
# two readings are what separate the printed card from every wrong way of
# implementing it: an engine that scaled nothing reads 3 twice, one that scaled
# everything reads 3 and 6, one that ignored the When Revealed reads 2 twice.
#
# ---------------------------------------------------------------------------
# The hazard icon is a different behaviour and needs a control.
#
# It is a printed icon rather than anything in the card's script -- the engine
# implements it generically -- so the pair below is the same round played with
# and without the side scheme in play, and the only difference between them is
# how far down the encounter deck the villain phase reaches.
#
# The deck is written top-first (the original investigation) and the activation takes the first
# card as its boost, so `"Hydra Mercenary", "Shocker", "Vulture", ...` gives
# Shocker as the encounter card every round deals and Vulture as the one only a
# hazard round reaches. Both are minions and both enter play when revealed, so
# "did the second card come out" is a plain assertion about the board.
#
# Iron Man in alter-ego form: an alter-ego is schemed against rather than
# attacked, so nothing interrupts the walk with a defence, and Shocker's printed
# "deal 1 damage to each hero" finds no hero.

Feature: Breakin' & Takin'

  Background:
    Given the scenario is "rhino"

  @card:01107
  Scenario: at one hero it lands with its printed 2 plus the 1 it places
    Given the hero is "spider_man"
    And I am in hero form
    And "Breakin' & Takin'" is revealed

    Then "Breakin' & Takin'" is in the "SideSchemesArea"
    And "Breakin' & Takin'" has 3 threat
    And the main scheme has 0 threat
    And I am not prompted again

  @card:01107
  Scenario: at two heroes the printed 2 stays put and the placed 1 doubles
    # 4, not 3 and not 6. The starting threat is printed fixed and the placed
    # threat is printed per hero, and this is the only board in the file where
    # the two rules disagree.
    Given the heroes are "spider_man", "captain_marvel"
    And I am in hero form
    And "Breakin' & Takin'" is revealed

    Then "Breakin' & Takin'" has 4 threat
    And the main scheme has 0 threat

  @card:01107
  @rr:hazard-icon.1
  Scenario: its hazard icon deals a second encounter card in the villain phase
    Given the hero is "iron_man"
    And I am in alter-ego form
    And "Breakin' & Takin'" is in play
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Shocker", "Vulture", "Hydra Mercenary"

    When I pass
    Then "Shocker" is in play
    And "Vulture" is in play
    And it is round 2

  @card:01107
  @rr:villain-phase.step.3
  Scenario: the same round without the hazard scheme deals one
    # The control. Identical deck, identical form, no side scheme in play:
    # Shocker still comes out and Vulture is still sitting in the encounter
    # deck, so the extra card above is the icon and not the deck.
    Given the hero is "iron_man"
    And I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Hydra Mercenary", "Shocker", "Vulture", "Hydra Mercenary"

    When I pass
    Then "Shocker" is in play
    And "Vulture" is not in play
    And "Vulture" is in the "EncounterDeck"
    And it is round 2
