# Printed: "Surge. (After this card is revealed, reveal 1 additional encounter
# card.)"
# "[star] Boost: Put Weapons Runner into play engaged with you."
# Printed statistics: 2 hit points, ATK 1, SCH 1.
#
# The card has two ways into play and the script implements one of them:
# `AbilityFactory.WhenCardBecomeBoost("This", PutThisIntoPlay)`. The other --
# being revealed as an encounter card, surging as it goes -- is the Surge
# keyword and is already pinned as a rule, on this same card, in
# `specs/rules/keywords.feature`. Repeating it here would be one claim written
# twice, so what this file covers is the boost path.
#
# ---------------------------------------------------------------------------
# A boost card is never revealed, so nothing a `Given` can do puts one into
# play. The transcript walks a real villain phase instead.
#
# Decks are written top-first and the engine's villain phase takes cards in
# this order: escalation threat is placed, then the villain activates and draws
# its boost cards, then an encounter card is dealt and revealed. Klaw stage I
# gives himself 1 additional boost card whenever he attacks, so an attacking
# activation takes two:
#
#   Weapons Runner        boost card 1 -- the [star] ability fires here
#   Sonic Boom            boost card 2 -- also a [star] boost, also no icons
#   Illegal Arms Factory  the encounter card dealt afterwards
#   Armored Guard         filler, so the deck never empties and reshuffles
#
# Both boost cards carry [star] rather than a number, so Klaw's printed ATK 0
# stays 0 and the hero takes nothing from the villain. That is what makes the
# 1 damage in the transcript legible: it is Weapons Runner's own printed ATK 1,
# landing because the minion arrived *engaged* and every engaged enemy
# activates in the villain phase it is engaged for.

Feature: Weapons Runner

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01121
  Scenario: as a boost card it puts itself into play engaged with you
    Given I am in hero form
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"
    And the encounter deck is "Weapons Runner", "Sonic Boom", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then "Weapons Runner" is in the "EngagedEnemiesArea"
    And "Weapons Runner" has 2 health
    And "Weapons Runner" has 1 "attack"
    And "Weapons Runner" has 1 "scheme"
    And I have 0 damage

    # The second defence prompt of one villain phase. The minion is engaged, so
    # it activates behind the villain -- and a boost card that had gone to the
    # discard pile the way a boost card normally does would never have asked.
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 1 damage
    And "Sonic Boom" is in the "EncounterDiscardPile"
    And "Illegal Arms Factory" is in the "SideSchemesArea"
    And it is round 2
