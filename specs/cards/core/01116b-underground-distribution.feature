# Underground Distribution, stage 1B. Printed: "When Revealed: Discard cards
# from the encounter deck until a minion is discarded. Put that minion into
# play engaged with the first player."
# Printed statistics: starting threat 0, 6 threat per hero to complete,
# escalation 1 per hero.
#
# One ability with one branch in it -- the search either reaches a minion or
# runs the deck out -- and a pair of starred numbers that a one-hero board
# cannot read correctly, which is the third scenario.
#
# ---------------------------------------------------------------------------
# Why these transcripts re-reveal a card that is already in play.
#
# 1B is the stage a klaw game begins on, and its When Revealed fires during
# game setup. A puzzle scene has no encounter deck at setup -- that is the
# point of a puzzle scene -- so the setup firing finds nothing and there is
# nothing to observe. `Given "01116b" is revealed` runs the same reveal
# pipeline again, this time against a deck the scenario stocked, which is the
# only way to put this ability in front of an assertion.
#
# Decks are written top-first, so the first card named is the first one
# discarded.

Feature: Underground Distribution (1B)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01116b
  Scenario: cards come off the deck until a minion does, and that minion enters play
    # "Until" is the claim: the two cards above the minion are discarded, the
    # minion itself is the one that stops the loop and the one that enters
    # play, and the card *below* it never moves. An engine that discarded the
    # whole deck, or that stopped one card early, disagrees with one of those
    # four zones.
    #
    # The minion arrives engaged rather than in the villain's area, which is
    # the second half of the printed sentence.
    Given I am in hero form
    And the encounter deck is "Sonic Boom", "Klaw's Vengeance", "Armored Guard", "Armored Guard"
    And "01116b" is revealed

    Then "Sonic Boom" is in the "EncounterDiscardPile"
    And "Klaw's Vengeance" is in the "EncounterDiscardPile"
    And "Armored Guard #1" is in the "EngagedEnemiesArea"
    And "Armored Guard #2" is in the "EncounterDeck"
    And the main scheme has 0 threat
    And "the main scheme" has 6 "target_threat"
    And "the main scheme" has 1 "escalation_threat"

  @card:01116b
  Scenario: a deck with no minion in it puts nothing into play
    # The other side of "if a minion was discarded". The deck holds a treachery
    # and a side scheme and runs out without producing one, so there is nothing
    # to put into play and nothing arrives.
    #
    # The copy of Armored Guard sits in the *encounter discard pile*, where the
    # printed text never looks -- it says "discard cards from the encounter
    # deck". It is there to give the scenario something that would visibly move
    # if the engine widened the search, so "nothing entered play" is a claim
    # about the search and not merely about an empty board.
    Given I am in hero form
    And the encounter discard pile is "Armored Guard"
    And the encounter deck is "Sonic Boom", "Klaw's Vengeance"
    And "01116b" is revealed

    Then "Armored Guard" is not in play
    And the main scheme has 0 threat
    And "the main scheme" has 1 "printed_stage"

  @card:01116b
  Scenario: the threat the stage completes at is per hero, and so is the escalation
    # `6*` and `1*`. Both double at a second hero, and a one-hero board cannot
    # tell either of them from a flat number -- 6 and 1 are what the card
    # prints, and 12 and 2 are what it means.
    Given the heroes are "captain_marvel", "iron_man"
    And I am in hero form

    Then "the main scheme" has 12 "target_threat"
    And "the main scheme" has 2 "escalation_threat"
    And the main scheme has 0 threat
