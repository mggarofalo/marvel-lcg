# Printed: "Attach to Klaw."
# "Klaw gains retaliate 1. (After this character is attacked, deal 1 damage to
#  the attacking character.)"
# "Hero Action: Spend [energy] [mental] [physical] resources -> discard this
#  card."
# Printed statistics: boost 3.
#
# Three declarative factory calls and no handler, which is the tier the card is
# filed under -- and two decision paths all the same, because the card is a
# thing that can be removed. Attached, the villain answers back; discarded, he
# does not, and that second transcript is the only thing that says the keyword
# travelled with the attachment rather than being printed on Klaw.
#
# The hero action costs resources and nothing else -- no exhaust, no action
# window spent -- so the hero who pays it is still ready to attack in the same
# transcript, which is what makes the pair measurable in two beats.

Feature: Solid-Sound Body

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01119
  Scenario: attached to Klaw it answers an attack with 1 damage
    # "Attach to Klaw" is resolved by the card as it enters play, and the
    # keyword lands on the host: Klaw stage I prints no retaliate at all, so
    # the 1 is entirely this card's.
    #
    # Captain Marvel's printed ATK 2 goes in, 1 comes back. She takes it while
    # attacking, which is the timing the printed reminder text describes --
    # "after this character is attacked".
    Given I am in hero form
    And "Solid-Sound Body" is in play

    Then "Solid-Sound Body" is in the "UpgradesArea"
    And "Klaw" has 1 "retaliate"

    When I attack "Klaw"
    Then "Klaw" has 2 damage
    And I have 1 damage

  @card:01119
  Scenario: the hero action pays three resources and takes the retaliate with it
    # The hand is one card of each printed resource -- Crisis Interdiction is
    # [energy], Alpha Flight Station is [mental], Photonic Blast is [physical]
    # -- so the cost is payable exactly once and paying it empties the hand.
    #
    # The attack afterwards is this scenario's real assertion and the control
    # for the one above: the same swing against the same villain, and nothing
    # comes back.
    Given I am in hero form
    And "Solid-Sound Body" is in play
    And my hand is "Crisis Interdiction", "Alpha Flight Station", "Photonic Blast"

    When I choose "Hero Action" on "Solid-Sound Body"
    Then "Solid-Sound Body" is in the "EncounterDiscardPile"
    And "Klaw" has 0 "retaliate"
    And I have 0 cards in hand
    And I have 3 cards in my discard pile

    When I attack "Klaw"
    Then "Klaw" has 2 damage
    And I have 0 damage
