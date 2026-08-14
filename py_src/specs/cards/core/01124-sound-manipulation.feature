# Printed: "When Revealed (Alter-Ego): Klaw heals 4 damage. If no damage was
# healed this way, this card gains surge."
# "When Revealed (Hero): Take 2 damage. Klaw heals 2 damage."
#
# Three decision paths, not two. The form the card is revealed to picks one of
# the two When Revealed clauses, and the alter-ego clause then branches again on
# whether there was anything to heal -- which is the only path that produces a
# second encounter card.
#
# ---------------------------------------------------------------------------
# The surge branch cannot be seen from a `Given`-time reveal.
#
# A card that gains surge mid-reveal hands the extra reveal to the encounter
# machinery, and on a puzzle board with no villain phase the surged card stops
# in `DealtEncounterCardsDeck` where nothing can name it. So the two alter-ego
# scenarios walk a real round instead: the hero ends their turn in alter-ego
# form, the villain phase runs, and Sound Manipulation arrives as the dealt
# encounter card.
#
# Decks are written top-first and the villain phase takes two different cards
# off this one, in this order:
#
#   Armored Guard         the boost card for Klaw's scheme activation
#   Sound Manipulation    the encounter card dealt to the player and revealed
#   Illegal Arms Factory  what a surge reaches, and nothing else does
#   Armored Guard         filler, so the deck never empties and reshuffles
#
# Illegal Arms Factory is the surge target because it is a side scheme: it
# lands in `SideSchemesArea` if it is revealed and stays in `EncounterDeck` if
# it is not, so one zone assertion separates the two branches. A minion would
# have worked too, but a minion put into play during the villain phase then
# activates in the same phase and moves the threat number the scenarios below
# also read.

Feature: Sound Manipulation

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01124
  Scenario: revealed to a hero it costs the hero 2 and gives Klaw 2 back
    # Both halves of the hero clause, on one board. Klaw is put at 5 damage so
    # that the heal has room to land and is still short of full -- at 0 damage
    # "healed 2" and "healed nothing" are the same board, and this scenario
    # would not be able to tell them apart.
    Given I am in hero form
    And "Klaw" has 5 damage
    And "Sound Manipulation" is revealed

    Then I have 2 damage
    And "Klaw" has 3 damage
    And I am not prompted again

  @card:01124
  Scenario: revealed to an alter-ego it heals Klaw 4 and does not surge
    # The alter-ego clause heals twice what the hero clause does, and costs the
    # player nothing -- 6 damage down to 2, and the alter-ego untouched.
    #
    # "If no damage was healed this way" is false here, so no surge: Illegal
    # Arms Factory is still in the encounter deck at the end of the round. That
    # is the assertion the next scenario inverts.
    Given I am in alter-ego form
    And "Klaw" has 6 damage
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Armored Guard", "Sound Manipulation", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then "Klaw" has 2 damage
    And I have 0 damage
    And "Sound Manipulation" is in the "EncounterDiscardPile"
    And "Illegal Arms Factory" is in the "EncounterDeck"
    And it is round 2

  @card:01124
  Scenario: an undamaged Klaw heals nothing and the card surges instead
    # The conditional. Klaw is at full health, so there is no damage to heal
    # and the card gains surge -- and the surge is the only thing that reveals
    # Illegal Arms Factory, which is why its arrival in play is this scenario's
    # whole claim.
    #
    # An engine that surged unconditionally passes the scenario above only if
    # it also fails this pair's control; an engine that never surged passes the
    # control and fails here. Neither reading survives both.
    Given I am in alter-ego form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Armored Guard", "Sound Manipulation", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then "Klaw" has 0 damage
    And "Sound Manipulation" is in the "EncounterDiscardPile"
    And "Illegal Arms Factory" is in the "SideSchemesArea"
    And it is round 2
