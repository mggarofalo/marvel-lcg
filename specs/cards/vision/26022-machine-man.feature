# Printed: "Interrupt: When Machine Man attacks or thwarts, spend up to 3
# resources of any type -> Machine Man gets +1 THW and +1 ATK for each resource
# spent this way."
#
# Printed statistics: cost 2, 3 hit points, ATK 1, THW 1. Resource icon:
# [[physical]]. Trait: ANDROID.
#
# ---------------------------------------------------------------------------
# The card that says the original investigation was never only about the three X-cost cards.
#
# "Spend up to 3" is a ceiling on an effect, not a price, and spending nothing
# satisfies it -- so the bot spent nothing, every time, and this ally's only
# printed ability did nothing in every self-play game ever generated. Unlike a
# printed X, the planner could always *see* this one: `UpTo` has been in the
# option's rule list since the client needed it. It just had no reason to look,
# because zero matched.
#
# It is also the one that moves the digest. Of seven wide-matrix cases none
# reach a variable cost at all; of six constructed ones, the two with Vision in
# them move, and this ally is why. That is the measurement the fix was bounded
# by -- an unmoved matrix would have proved nothing.
#
# ## What the payment is, without a step that states it
#
# The runner spends what the option offers, in engine order, up to the ceiling.
# So the hand is the payment, and these scenarios differ only in what is in it.
# Enhanced Spider-Sense (01004) is the one-icon filler: it prints a single
# [[mental]] and it is an Interrupt on being attacked, so it is never playable
# on this board and never becomes an option of its own. Energy (01088) prints
# **two** [[energy]], which is what makes the third scenario possible.
#
# The interrupt is its own decision -- `When I choose "Interrupt"` after the
# attack -- because the engine opens the window before the attack resolves. It
# takes no target, so there is nothing to name.

Feature: Machine Man

  Background:
    Given the scenario is "rhino"
    And the hero is "spider_man"
    And I am in hero form
    And "Machine Man" is in play

  @card:26022
  Scenario: three resources spent is three more attack
    # 1 printed + 3 spent. The whole hand goes.
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Attack" on "Machine Man" targeting "Rhino"
    When I choose "Interrupt" on "Machine Man"
    Then "Rhino" has 4 damage
    And I have 0 cards in hand

  @card:26022
  Scenario: the printed 3 is a ceiling, not a demand
    # A fourth resource on offer and it stays in hand. Without this, "spend as
    # much as you can" would read as "spend everything" -- and an overpaid
    # `UpTo` cost is not payable at all, so the ability would be withheld.
    Given my hand is "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Enhanced Spider-Sense", "Enhanced Spider-Sense"

    When I choose "Attack" on "Machine Man" targeting "Rhino"
    When I choose "Interrupt" on "Machine Man"
    Then "Rhino" has 4 damage
    And I have 1 cards in hand

  @card:26022
  Scenario: a resource that does not fit under the ceiling is skipped, not final
    # Energy prints two [[energy]] at once. Offered first it fits; offered
    # second it would take the total to 4 and is passed over; the one-icon card
    # behind it then brings the total to exactly 3.
    #
    # A planner that stopped at the first resource it could not take would spend
    # 2 here and deal 3. The card left in hand is which one it was.
    Given my hand is "01088", "01088", "Enhanced Spider-Sense"

    When I choose "Attack" on "Machine Man" targeting "Rhino"
    When I choose "Interrupt" on "Machine Man"
    Then "Rhino" has 4 damage
    And I have 1 cards in hand
    And "01088 #1" is in the "DiscardPile"
    And "01088 #2" is in the "HandsArea"

  @card:26022
  Scenario: with nothing to spend the ally attacks for its printed 1
    # A puzzle scene deals no opening hand, so this is the empty-hand board.
    # The interrupt is still offered -- an `UpTo` cost is affordable with
    # nothing -- and taking it changes nothing at all. This is what every
    # generated game did with this card before the original investigation.

    When I choose "Attack" on "Machine Man" targeting "Rhino"
    When I choose "Interrupt" on "Machine Man"
    Then "Rhino" has 1 damage
    And I have 0 cards in hand
