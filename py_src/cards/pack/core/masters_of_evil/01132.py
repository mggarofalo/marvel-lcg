from . import *

# * Melter

def GetAbilities() -> Sequence['Ability']:

    def melter_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        player = message.GetToPlayer()

        Faces.ExhaustAll(player.GetControlAllies(), effect)

    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            melter_boost
        ),
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.NonKeywordStar,
            "This",
            lambda effect, message:
                message.MustDefendWithAlly(effect),
        )
    ]
