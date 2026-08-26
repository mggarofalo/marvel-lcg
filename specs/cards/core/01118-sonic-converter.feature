# Printed: "Attach to Klaw."
# "[star] Forced Response: After Klaw attacks and damages a character, stun that
#  character."
# "Hero Action: Spend [energy] [mental] [physical] resources -> discard this
#  card."
# Printed statistics: Boost 3, ATK +1.
#
# Three printed abilities and a conditional inside one of them, so four paths
# plus the control that separates "damages a character" from "attacks a
# character". They are written apart rather than stacked into one transcript
# because they are four different things the engine could get wrong
# independently.
#
# ---------------------------------------------------------------------------
# What the boost cards are doing in these scenarios.
#
# Klaw stage I prints ATK 0 and gives himself 1 additional boost card whenever
# he attacks, so his activation takes two cards off the top of the encounter
# deck and the attack is 0 + this attachment's +1 + whatever those two carry:
#
#   "Armored Guard", "Armored Guard"   ->  0 + 1 + 1 + 1  =  3 damage
#   "Sonic Boom", "Sonic Boom"         ->  0 + 1 + 0 + 0  =  1 damage
#
# The second pair is what makes a damageless activation reachable at all: 1
# damage against Captain Marvel's printed DEF 1 is 0 once she defends, and the
# forced response has nothing to fire on. Without the attachment's own +1 the
# attack would be 0 before the defence and the scenario would be measuring an
# attack that never threatened anything.
#
# Illegal Arms Factory is the third card in every deck here, so the encounter
# card dealt after the activation is a side scheme rather than a minion -- a
# minion would arrive engaged and change what the *next* villain phase does,
# which none of these scenarios want to think about.

Feature: Sonic Converter

  Background:
    Given the scenario is "klaw"
    And the hero is "captain_marvel"
    And my deck is "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts", "Pepper Potts"

  @card:01118
  Scenario: it attaches itself to Klaw and adds 1 to his attack
    # "Attach to Klaw" is resolved by the card as it enters play, not by
    # whoever put it there, so a bare put-into-play is the whole test. The
    # attack value is the printed ATK +1 landing on the host: Klaw stage I
    # prints ATK 0, so 1 is entirely this card's doing.
    Given I am in hero form
    And "Sonic Converter" is in play

    Then "Sonic Converter" is in the "UpgradesArea"
    And "Klaw" has 1 "attack"

  @card:01118
  Scenario: an attack that damages the hero stuns the hero
    # The forced response, on the character the activation damaged. Declining
    # to defend does not stun anybody by itself, so the status here is this
    # card's and nothing else's.
    Given I am in hero form
    And "Sonic Converter" is in play
    And the encounter deck is "Armored Guard", "Armored Guard", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I pass
    Then I have 3 damage
    And "me" is stunned
    And it is round 2

  @card:01118
  Scenario: the character stunned is the one that took the damage
    # "stun that character", not "stun the hero". Black Cat defends, takes the
    # 1 the activation deals, and is the one holding the status afterwards --
    # the hero is untouched and unstunned in the same breath.
    #
    # Black Cat exhausts because she defended, which is the ordinary rule and
    # not this card; the assertion that carries the claim is "Black Cat is
    # stunned" next to "me is not stunned".
    Given I am in hero form
    And "Sonic Converter" is in play
    And "Black Cat" is in play
    And the encounter deck is "Sonic Boom", "Sonic Boom", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then I am prompted to choose one
      | Defense |
      | Defense |

    When I choose "Defense" on "Black Cat"
    Then "Black Cat" has 1 damage
    And "Black Cat" is stunned
    And I have 0 damage
    And "me" is not stunned
    And it is round 2

  @card:01118
  Scenario: an attack that damages nobody stuns nobody
    # "attacks and damages" is two things and this board has only the first.
    # Klaw's activation is a real attack -- the hero is asked to defend -- and
    # the defence takes all of it, so no damage is dealt and no status is
    # applied.
    #
    # An engine that read the trigger as "after Klaw attacks" passes the two
    # scenarios above and fails here.
    Given I am in hero form
    And "Sonic Converter" is in play
    And the encounter deck is "Sonic Boom", "Sonic Boom", "Illegal Arms Factory", "Armored Guard"

    When I pass
    Then I am prompted to choose one
      | Defense |

    When I choose "Defense"
    Then I have 0 damage
    And "me" is not stunned
    And it is round 2

  @card:01118
  Scenario: the hero action pays three resources and takes the attachment off
    # "Spend [energy] [mental] [physical] resources -> discard this card." The
    # hand is one card of each printed resource -- Crisis Interdiction is
    # [energy], Alpha Flight Station is [mental], Photonic Blast is [physical]
    # -- so the cost is payable exactly once and paying it empties the hand.
    #
    # Klaw dropping back to his printed ATK 0 is what says the attachment left
    # play rather than merely changing zone.
    Given I am in hero form
    And "Sonic Converter" is in play
    And my hand is "Crisis Interdiction", "Alpha Flight Station", "Photonic Blast"

    When I choose "Hero Action" on "Sonic Converter"
    Then "Sonic Converter" is in the "EncounterDiscardPile"
    And "Klaw" has 0 "attack"
    And I have 0 cards in hand
    And I have 3 cards in my discard pile
