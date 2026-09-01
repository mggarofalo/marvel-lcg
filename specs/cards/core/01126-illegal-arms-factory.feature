# Printed: "When Revealed: Place an additional 1 [per_hero] threat here."
# Printed statistics: 3 starting threat, boost 2, hazard icon.
#
# The same script line Defense Network runs, against a different printed
# starting threat and a different icon -- which is exactly why it is a separate
# card of work. The threat arithmetic has to be re-measured because 3 is not 2,
# and the hazard icon is behaviour that Defense Network's crisis icon says
# nothing about.
#
#   one hero    3 + 1  =  4      a flat 4 and "4 per hero" agree here
#   two heroes  3 + 2  =  5      a flat 4 says 4, "4 per hero" says 8
#
# The icon is measured here, against the control that matters -- another side
# scheme, in play, revealed the same way, without the icon. The canonical
# hazard pair and its Rules Reference tags are on Breakin' & Takin' (01107),
# which `specs/rules/keywords.feature` reuses for the original investigation rather than copying.

Feature: Illegal Arms Factory

  Background:
    Given the scenario is "klaw"

  @card:01126
  Scenario: it arrives on its printed threat plus one for the hero
    Given the hero is "captain_marvel"
    And I am in hero form
    And "Illegal Arms Factory" is revealed

    Then "Illegal Arms Factory" is in the "SideSchemesArea"
    And "Illegal Arms Factory" has 4 threat
    And "Illegal Arms Factory" has 1 "hazard"

  @card:01126
  Scenario: only the additional threat is per hero, not the printed 3
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form
    And "Illegal Arms Factory" is revealed

    Then "Illegal Arms Factory" has 5 threat
    And "Illegal Arms Factory" has 1 "hazard"

  @card:01126
  Scenario: the hazard icon deals a second encounter card in the villain phase
    # Alter-ego form, so the villain schemes rather than attacks and the round
    # walks to its end on a single decision. The encounter deck is written
    # top-first: Sonic Boom is the boost card for the scheme activation --
    # a [star] boost, so it carries no icons and moves no number this scenario
    # reads -- and the Armored Guards behind it are what gets dealt.
    #
    # Two minions arrive engaged instead of one, and the third is still in the
    # deck, so the count is read off the board rather than inferred.
    Given the hero is "captain_marvel"
    And I am in alter-ego form
    And "Illegal Arms Factory" is revealed
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Sonic Boom", "Armored Guard", "Armored Guard", "Armored Guard", "Armored Guard"

    When I pass
    Then "Armored Guard #1" is in the "EngagedEnemiesArea"
    And "Armored Guard #2" is in the "EngagedEnemiesArea"
    And "Armored Guard #3" is in the "EncounterDeck"
    And it is round 2

  @card:01126
  Scenario: a side scheme without the icon deals only one
    # The control, on the same board with Defense Network standing in for the
    # hazard scheme: also a klaw side scheme, also revealed by a `Given`, also
    # in play for the whole villain phase, and printed without the icon. One
    # minion arrives and the second stays in the deck.
    #
    # Without this the scenario above is satisfied by an engine that deals two
    # encounter cards to everybody.
    Given the hero is "captain_marvel"
    And I am in alter-ego form
    And "Defense Network" is revealed
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Sonic Boom", "Armored Guard", "Armored Guard", "Armored Guard", "Armored Guard"

    When I pass
    Then "Armored Guard #1" is in the "EngagedEnemiesArea"
    And "Armored Guard #2" is in the "EncounterDeck"
    And it is round 2
