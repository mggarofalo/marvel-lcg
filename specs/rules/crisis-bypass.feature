# The crisis-icon bypass: the branch `specs/rules/damage-and-threat.feature`
# names and cannot reach. MARVEL-90.
#
# The crisis rule itself is specified there: while a card bearing the icon is in
# play, threat cannot be removed from the main scheme. `scheme_main.py` guards
# that in `RemoveThreatInternal`, and the guard has an `else`:
#
#     if crisis_faces:
#         if not by_effect.IsIgnoreKeyword('Crisis', by_effect):
#             ...
#             return 0
#         else:
#             ignored_crisis = True
#
# 21 printed cards carry an effect that takes the `else`. None of them is in the
# core set -- the earliest is Cable Arrow in pack 04 -- so the core-set shard
# could state the rule but never measure its exception. That is what this file
# is for, and it is why every scenario here is paired with a control on the same
# card with no crisis in play: without the control a scenario measures the card
# rather than the bypass.
#
# Four bypass cards are measured, deliberately covering all three shapes the
# engine implements, because they are three different code paths and a scenario
# on one of them says nothing about the other two:
#
#   an ability that ignores      Cable Arrow (04008), 'Pool Inspection (44023)
#     -- `.SetIgnoreKeyword('Crisis')` on the ability itself
#   a character that ignores     Shadowcat (32002)
#     -- `UnitIgnoreKeywordIcons("This", crisis=True)`, so it travels with the
#        character and not with the board; the last Shadowcat scenario is the
#        control for exactly that
#   a character that ignores     Fandral (21191)
#     while using one power         -- the same factory with `while_use_basic_thw`
#
# The board is the Rhino scenario throughout, as in damage-and-threat.feature,
# so the crisis card is the same Under Attack (01151) measured there: printed
# with the crisis icon and 3 starting threat. The hero changes per scenario,
# because these are hero-specific cards and the honest board for Cable Arrow is
# Hawkeye's. The Break-In! stage 1B completes at 7 threat, so no scenario here
# starts the main scheme above 6.
#
# ALSO PINNED HERE: taking the `else` sends `Message.AfterIgnoreKeywordOnCard`
# after the removal, which is a second observable and not merely a variable the
# engine sets. Intangible Interference (32035) is the card that reads it, and
# the last three scenarios use it to show the message fires, that it does not
# fire when there was no icon to ignore, and who it names as having ignored.

Feature: The crisis-icon bypass

  Background:
    Given the scenario is "rhino"

  # --------------------------------------------------------------------------
  # An ability that ignores the icon: Cable Arrow (04008)
  #
  # "Hero Action (thwart): Exhaust Hawkeye's Bow -> remove 3 threat from a
  #  scheme, ignoring any crisis icons in play."
  #
  # An event, so the comparison is with "a card effect is stopped by the icon
  # too, and is still spent" in damage-and-threat.feature: For Justice! is the
  # same printed shape without the last clause, and it removes nothing.

  @card:04008
  @rr:ignore
  @rr:crisis-icon.1
  Scenario: an effect that ignores the icon removes threat the icon would have blocked
    # Hawkeye's Bow is in play because the printed cost exhausts it, and the
    # Energy pays the printed cost of 1.
    Given the hero is "hawkeye"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "Hawkeye's Bow" is in play
    And my hand is "Cable Arrow", "01088"

    # The icon does not filter the main scheme out of the target list. It never
    # removes a target -- it removes the threat removal -- which is the
    # distinction damage-and-threat.feature draws between crisis and patrol, and
    # it holds for the card that ignores it too.
    Then the legal targets for "Play" on "Cable Arrow" are
      | The Break-In! |
      | Under Attack  |

    When I play "Cable Arrow" targeting "The Break-In!"
    Then the main scheme has 2 threat
    And I am not prompted again

  @card:04008
  @rr:threat
  Scenario: the same effect removes the same 3 with no crisis in play
    # The control. Same hand, same bow, no icon: still 3, so the scenario above
    # measured the bypass and not Cable Arrow.
    Given the hero is "hawkeye"
    And I am in hero form
    And the main scheme has 5 threat
    And "Hawkeye's Bow" is in play
    And my hand is "Cable Arrow", "01088"

    When I play "Cable Arrow" targeting "The Break-In!"
    Then the main scheme has 2 threat

  # --------------------------------------------------------------------------
  # A character that ignores the icon: Shadowcat (32002)
  #
  # "Shadowcat ignores the guard and patrol keywords, and any crisis icons in
  #  play."
  #
  # Printed THW 2, and the ally is named by id because 32002 shares its printed
  # name with the Shadowcat hero identity (32030a).

  @card:32002
  @rr:ignore
  @rr:crisis-icon.1
  Scenario: an ally that ignores the icon thwarts the main scheme through it
    # The direct comparison is "an ally is stopped by the icon as well" in
    # damage-and-threat.feature: Black Cat thwarts the same board for nothing
    # and is exhausted for it.
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "32002" is in play

    When I choose "thwart" on "32002" targeting "The Break-In!"
    Then the main scheme has 3 threat
    And "32002" is exhausted

  @card:32002
  @rr:thwart.1
  Scenario: the same ally removes the same 2 with no crisis in play
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "32002" is in play

    When I choose "thwart" on "32002" targeting "The Break-In!"
    Then the main scheme has 3 threat

  @card:32002
  @rr:ignore
  Scenario: the ally's bypass does not extend to the hero thwarting beside it
    # The control that says what kind of thing the bypass is. Shadowcat's
    # ability is `UnitIgnoreKeywordIcons("This", ...)`, so it is a property of
    # the effect removing the threat and not of the board: with Shadowcat
    # standing in play, Spider-Man's own basic thwart is refused exactly as it
    # is in damage-and-threat.feature, and still costs him the exhaust.
    #
    # Without this scenario the two above are equally consistent with "a card
    # that ignores the icon switches the icon off for everyone", which is false.
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "32002" is in play

    When I thwart "The Break-In!"
    Then the main scheme has 5 threat
    And I am exhausted

  # --------------------------------------------------------------------------
  # A character that ignores the icon while using one power: Fandral (21191)
  #
  # "When Fandral uses his basic THW, ignore any crisis icons in play."
  #
  # Printed THW 3 with two stars, which the engine reads as 2 consequential
  # damage rather than the usual 1, and 3 hit points. He is a campaign ally, and
  # named by id for the same reason as Shadowcat.

  @card:21191
  @rr:ignore
  @rr:crisis-icon.1
  Scenario: an ally whose basic THW ignores the icon removes threat through it
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "21191" is in play

    When I choose "thwart" on "21191" targeting "The Break-In!"
    Then the main scheme has 2 threat
    And "21191" has 2 damage

  @card:21191
  @rr:thwart.1
  Scenario: the same ally removes the same 3 with no crisis in play
    # The control, and it also pins that the consequential damage is not part of
    # what the bypass changes: 2 either way.
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "21191" is in play

    When I choose "thwart" on "21191" targeting "The Break-In!"
    Then the main scheme has 2 threat
    And "21191" has 2 damage

  # --------------------------------------------------------------------------
  # An ability that ignores the icon and then counts it: 'Pool Inspection (44023)
  #
  # "Hero Action (thwart): Remove 5 threat from the main scheme, ignoring the
  #  crisis icon. Remove 1 threat from each scheme for each [crisis],
  #  [acceleration], [amplify], and [hazard] in play."
  #
  # The card that reads the icon twice: it ignores the crisis icon and then
  # counts it. Printed cost 6, paid by three Energy at 2 resources each.
  #
  # The control swaps Under Attack for Bomb Scare (01109), which is printed with
  # an acceleration icon and 2 starting threat and no crisis icon. That keeps
  # the icon count at 1 across both scenarios, so the second clause removes the
  # same 1 in each and the only difference between the boards is whether the
  # first clause had an icon to ignore.

  @card:44023
  @rr:ignore
  @rr:crisis-icon
  Scenario: an effect that ignores the icon and counts it does both
    # 6 threat off the main scheme: 5 from the first clause plus 1 from the
    # second, for the one icon in play. Blocked, it would still read 6.
    Given the hero is "deadpool"
    And I am in hero form
    And the main scheme has 6 threat
    And "Under Attack" is in play
    And my hand is "44023", "01088", "01088", "01088"

    When I play "44023"
    Then the main scheme has 0 threat
    And "Under Attack" has 2 threat

  @card:44023
  @rr:acceleration-icon
  Scenario: the same effect removes the same 6 with an acceleration icon instead
    # The control. One icon either way, so the second clause is unchanged; the
    # main scheme empties the same way with nothing to ignore.
    Given the hero is "deadpool"
    And I am in hero form
    And the main scheme has 6 threat
    And "Bomb Scare" is in play
    And my hand is "44023", "01088", "01088", "01088"

    When I play "44023"
    Then the main scheme has 0 threat
    And "Bomb Scare" has 1 threat

  # --------------------------------------------------------------------------
  # The bypass is announced, not merely performed
  #
  # `ignored_crisis` sends `Message.AfterIgnoreKeywordOnCard` after the removal.
  # Intangible Interference (32035) is the only printed card that reads it:
  #
  #   "Hero Response: After you ignore the crisis icon on a scheme, exhaust
  #    Intangible Interference -> remove 2 threat from that scheme."
  #
  # "that scheme" is the scheme carrying the icon, not the scheme the threat
  # came off: the message carries the crisis faces and the ability targets them.
  # So the 2 threat comes off Under Attack, which is not blocked by the icon --
  # the icon locks the main scheme only, as damage-and-threat.feature pins.
  #
  # These three are worth having over an assertion about threat alone because
  # threat coming off is also what a bypass that quietly skipped the message
  # would look like.

  @card:32035
  @rr:response
  @rr:ignore
  Scenario: ignoring the icon fires a response the board can see
    Given the hero is "hawkeye"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "Hawkeye's Bow" is in play
    And "32035" is in play
    And my hand is "Cable Arrow", "01088"

    When I play "Cable Arrow" targeting "The Break-In!"
    Then I am prompted to choose one
      | Hero Response |

    When I choose "Hero Response"
    Then the main scheme has 2 threat
    And "Under Attack" has 1 threat
    And "32035" is exhausted

  @card:32035
  @rr:response
  Scenario: no crisis icon in play is no response, not a silent one
    # The control. Cable Arrow says it ignores crisis icons whether or not one
    # is in play, so the message has to be tied to an icon actually having been
    # ignored -- if it fired on the printed text alone this would open the same
    # window with nothing to have ignored.
    Given the hero is "hawkeye"
    And I am in hero form
    And the main scheme has 5 threat
    And "Hawkeye's Bow" is in play
    And "32035" is in play
    And my hand is "Cable Arrow", "01088"

    When I play "Cable Arrow" targeting "The Break-In!"
    Then I am not prompted again
    And the main scheme has 2 threat

  @card:32035
  @rr:ignore
  @rr:you-your
  Scenario: an ally ignoring the icon is not you ignoring it
    # The second control, and it is about the word "you". The message names the
    # character that removed the threat, and Intangible Interference asks
    # whether that character is your identity. Shadowcat ignores the icon here
    # -- the main scheme drops the same 2 as in her own scenario above, so the
    # bypass certainly happened -- and no response is offered, because the ally
    # ignored it and not you.
    Given the hero is "spider_man"
    And I am in hero form
    And the main scheme has 5 threat
    And "Under Attack" is in play
    And "32002" is in play
    And "32035" is in play

    When I choose "thwart" on "32002" targeting "The Break-In!"
    Then I am not prompted again
    And the main scheme has 3 threat
    And "Under Attack" has 3 threat
    And "32035" is not exhausted
