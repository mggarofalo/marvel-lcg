# Printed: "When Revealed (Alter-Ego): Discard 1 card at random from your hand."
# "When Revealed (Hero): Klaw attacks you. If this attack deals damage, place 1
#  threat on the main scheme."
#
# Three decision paths: the form the card is revealed to, and then -- on the
# hero side -- whether the attack it triggers actually lands anything. The
# second clause is a conditional and the two scenarios below sit either side of
# it on boards that differ in one thing only, the boost cards.
#
# ---------------------------------------------------------------------------
# An attack this treachery starts is a Klaw activation like any other.
#
# Klaw stage I prints ATK 0 and "Forced Interrupt: When Klaw attacks, give him
# 1 additional boost card for this activation", so the attack takes *two* cards
# off the top of the encounter deck and its damage is 0 plus whatever boost
# icons those two carry. That is what makes both branches reachable from the
# same printed card:
#
#   "Armored Guard", "Armored Guard"   ->  0 + 1 + 1  =  2 damage
#   "Sonic Boom", "Sonic Boom"         ->  0 + 0 + 0  =  0 damage
#
# Sonic Boom is a [star] boost, so it carries no numeric icon at all. Its own
# boost ability -- "if this activation deals damage to you, exhaust your hero"
# -- is what the third scenario's `I am not exhausted` incidentally confirms
# did not fire, because the activation dealt nothing.
#
# `Given "<card>" is revealed` runs the whole reveal pipeline, which is why
# both hero scenarios open with a defence prompt and no preceding `When`.

Feature: Klaw's Vengeance

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"

  @card:01122
  Scenario: revealed to an alter-ego it takes a card out of hand
    # The alter-ego clause. Three named cards in, two left and one in the
    # discard pile -- "at random" is not assertable, but "one card left the
    # hand and reached the discard pile" is, and that is the whole effect.
    #
    # The main scheme is asserted untouched because the two clauses share a
    # card: an engine that ran the hero clause instead would place threat here.
    Given I am in alter-ego form
    And my hand is "Crisis Interdiction", "Alpha Flight Station", "Photonic Blast"
    And "Klaw's Vengeance" is revealed

    Then I have 2 cards in hand
    And I have 1 card in my discard pile
    And I have 0 damage
    And the main scheme has 0 threat
    And I am not prompted again

  @card:01122
  Scenario: revealed to a hero Klaw attacks and the damage buys a threat
    # The hero clause, both halves. Two boost cards of 1 icon each make the
    # attack land 2 on a hero who declines to defend, and the threat follows
    # from that damage rather than from the card being revealed.
    #
    # The hand is stocked and asserted intact for the same reason the scenario
    # above asserts the main scheme is: the two clauses are alternatives, and
    # an engine that ran the alter-ego one as well would take a card out of it.
    Given I am in hero form
    And my hand is "Crisis Interdiction", "Alpha Flight Station", "Photonic Blast"
    And the encounter deck is "Armored Guard", "Armored Guard", "Armored Guard", "Armored Guard"
    And "Klaw's Vengeance" is revealed

    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 2 damage
    And the main scheme has 1 threat
    And I have 3 cards in hand
    And I have 0 cards in my discard pile
    And I am not prompted again

  @card:01122
  Scenario: an attack that deals no damage places no threat
    # "If this attack deals damage" is a condition, and this is the board that
    # separates it from "if this card is revealed". The attack happens -- the
    # hero is asked to defend, so it is a real activation -- and lands nothing,
    # because both boost cards are [star] boosts with no icons against Klaw
    # stage I's printed ATK 0.
    #
    # An engine that placed the threat unconditionally is indistinguishable
    # from a correct one on the scenario above and fails here.
    Given I am in hero form
    And the encounter deck is "Sonic Boom", "Sonic Boom", "Armored Guard", "Armored Guard"
    And "Klaw's Vengeance" is revealed

    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 0 damage
    And the main scheme has 0 threat
    And I am not exhausted
    And I am not prompted again
