from . import *

# Blob 3.14

def GetAbilities() -> Sequence['Ability']:

    def blob_314_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        Faces.GiveStatus([identity], "Stunned", effect)


    return [
        AbilityFactory.UnitCannotTakeDamageWhile(
            AbilityType.NonKeyword,
            "This",
            is_from_attack=True,
            cannot_take_more_than_damage=2
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            blob_314_boost
        ),
    ]

