# Printed: "When Revealed: Discard an upgrade or support you control. If no
# cards were discarded this way, this card gains surge."
#
# Two things to pin and they need different setups.
#
# The *discard* is observable from a bare reveal: `"Caught Off Guard" is
# revealed` in the Given runs the reveal pipeline and the first decision the
# policy answers is the card's own "Discard" option.
#
# The *surge* is not. Revealed that way the surged card is dealt but stops in
# the dealt-encounter-cards deck, because nothing outside the encounter step is
# there to reveal it. So the three surge scenarios play a real villain phase
# instead: the encounter deck is stacked top-first, the first card boosts the
# villain's activation, the second is the one dealt and revealed, and the third
# is what surge reaches. `specs/rules/phase-structure.feature` is the model, and
# the same two obligations apply -- an alter-ego so no defence interrupts the
# walk, and a stocked player deck so the hero is not eliminated drawing up.
#
# The filler is chosen to be inert. Aunt May and Heroic Intuition do nothing
# while they sit in play, Jessica Jones is a passive ally with no enter-play
# response, and every encounter card is the same Hydra Mercenary so the shape of
# the villain phase is not a coincidence of what got dealt.

Feature: Caught Off Guard

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"

  @card:01188
  Scenario: with two eligible cards the player is asked which one goes
    # The mid-resolution choice. The engine offers one option with two legal
    # targets rather than one option per card, so the transcript names the
    # target. The assertion that Heroic Intuition survives is the one that would
    # pass regardless under a resolver that discarded the first match.
    Given I am in hero form
    And "Aunt May" is in play
    And "Heroic Intuition" is in play
    And "Caught Off Guard" is revealed

    Then I am prompted to choose one
      | Discard |

    When I choose "Discard" targeting "Aunt May"
    Then "Aunt May" is in the "DiscardPile"
    And "Heroic Intuition" is in play
    And I am not prompted again

  @card:01188
  Scenario: a single eligible card is discarded and the card does not surge
    # Both halves of the printed text in one villain phase. Heroic Intuition is
    # the only legal target, so the engine takes it without asking; a card *was*
    # discarded, so surge does not fire and the third encounter card is still
    # sitting in the deck at the end of the round.
    Given I am in alter-ego form
    And "Heroic Intuition" is in play
    And my deck is "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May"
    And the encounter deck is "Hydra Mercenary", "Caught Off Guard", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then "Heroic Intuition" is in the "DiscardPile"
    And "Caught Off Guard" is in the "EncounterDiscardPile"
    And "Hydra Mercenary #2" is in the "EncounterDeck"
    And "Hydra Mercenary #3" is in the "EncounterDeck"
    And it is round 2

  @card:01188
  Scenario: with nothing to discard the card surges
    # The same villain phase with an empty board on the player's side. Hydra
    # Mercenary #1 boosted the villain's activation and is in the encounter
    # discard pile; #2 was reached by surge and is in play engaged with the
    # hero; #3 proves surge revealed exactly one more card and stopped.
    Given I am in alter-ego form
    And my deck is "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May"
    And the encounter deck is "Hydra Mercenary", "Caught Off Guard", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then "Caught Off Guard" is in the "EncounterDiscardPile"
    And "Hydra Mercenary #1" is in the "EncounterDiscardPile"
    And "Hydra Mercenary #2" is in play
    And "Hydra Mercenary #3" is in the "EncounterDeck"
    And it is round 2

  @card:01188
  Scenario: an ally is neither an upgrade nor a support, so the card still surges
    # The card names two card types and an ally is not one of them. Jessica
    # Jones is untouched and surge fires anyway -- the pair of assertions is the
    # claim, because either one alone is satisfied by an engine that got the
    # card-type filter wrong in the other direction.
    Given I am in alter-ego form
    And "Jessica Jones" is in play
    And my deck is "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May", "Aunt May"
    And the encounter deck is "Hydra Mercenary", "Caught Off Guard", "Hydra Mercenary", "Hydra Mercenary"

    When I pass
    Then "Jessica Jones" is in play
    And "Caught Off Guard" is in the "EncounterDiscardPile"
    And "Hydra Mercenary #2" is in play
    And it is round 2
