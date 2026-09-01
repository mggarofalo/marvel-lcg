# Underground Distribution, stage 1A. Printed: "Setup: Search the encounter
# deck for the Defense Network side scheme and reveal it. Shuffle the encounter
# deck. Advance to stage 1B."
#
# ---------------------------------------------------------------------------
# This header used to open with "READ THIS BEFORE TRUSTING THIS FILE TO COVER
# THE CARD" and say the Defense Network search was not reachable from the spec
# harness. **It is reachable now**, and the old reasoning is kept because it was
# correct about the engine and wrong only about what could be done from outside
# it.
#
# `cards/pack/core/klaw/01116a.py` hangs its one ability off
# `AbilityFactory.WhenCardSetup`, and `Message.WhenCardSetup` is sent from
# exactly one place for a main scheme: `World` step 12, `scheme.Setup(False)`
# immediately before `scheme.Advance(...)`. That is inside `GameSetup()`. The
# harness applies every `Given` step *after* `GameSetup()` returns, so the search
# ran against an empty encounter deck and found nothing, and revealing the card
# again did not help -- the card object is presenting its 1B face by then, so a
# reveal runs 1B's When Revealed rather than 1A's setup.
#
# What changed is that a scene can now be handed a stocked encounter deck before
# `GameSetup()` runs (the original investigation):
#
#     Given the encounter deck at setup is "Defense Network", ...
#
# That is not a `Given` applied earlier; it is part of the scene the engine sets
# up from, which is why it reads as configuration. This card was one of three in
# the core set carrying the gap as prose, and one of 49 measured across the game.
#
# So the second scenario below **is** a test of `01116a.py`, and mutating that
# script fails it. The first scenario is not and never claimed to be: it pins the
# third printed sentence, "Advance to stage 1B", as observed at the start of a
# game.
#
# Two things about the board that are worth knowing before editing the second
# scenario. The search runs *before* the advance, so Defense Network is out of
# the deck by the time 1B's own When Revealed -- "discard cards from the
# encounter deck until a minion is discarded, put that minion into play" -- eats
# into what is left. And "Shuffle the encounter deck" is still not assertable:
# the vocabulary has no step for deck order, and order does not survive a shuffle
# anyway (the original investigation).

Feature: Underground Distribution (1A)

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01116a
  Scenario: the game begins with stage 1A already advanced to 1B
    # One card, two faces, and the game starts on the second of them. The
    # printed-stage and threshold assertions are what makes this a claim about
    # *which* face rather than about a card being somewhere -- 1A has no
    # completion threshold at all, so 6 is only readable off 1B.
    Given I am in hero form

    Then "01116a" is in the "MainSchemesArea"
    And "01116a" has 1 "printed_stage"
    And "the main scheme" has 6 "target_threat"
    And "the main scheme" has 1 "escalation_threat"
    And the main scheme has 0 threat

  @card:01116a
  Scenario: the setup line finds Defense Network in the encounter deck and reveals it
    # Found and *revealed*, not merely found: it is in play as a side scheme
    # carrying its own printed threat rather than sitting in the encounter deck.
    # Defense Network prints 2 fixed starting threat and places an additional 1
    # per hero when revealed, so 3 at one hero -- which is also the assertion
    # that says the reveal pipeline ran rather than the card being moved.
    #
    # Illegal Arms Factory is the control on the name half of the search. It is
    # the other Klaw side scheme, it is in the same deck, and an engine that
    # searched for "a side scheme" rather than for this one could answer either.
    # It is asserted `not in play` rather than by zone because 1B's own reveal
    # discards from the encounter deck immediately afterwards, so which of the
    # two zones it ends in is the shuffle's business; that it was not revealed is
    # the card's.
    #
    # The two Armored Guards are there so 1B's discard-until-a-minion terminates
    # on a minion rather than on an empty deck.
    Given the encounter deck at setup is "Defense Network", "Illegal Arms Factory", "Armored Guard", "Armored Guard"
    And I am in hero form

    Then "Defense Network" is in the "SideSchemesArea"
    And "Defense Network" has 3 threat
    And "Illegal Arms Factory" is not in play
    And "01116a" is in the "MainSchemesArea"
    And I am not prompted again
