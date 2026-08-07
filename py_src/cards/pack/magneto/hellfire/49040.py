from . import *

# Hellfire Pawn

def GetAbilities() -> Sequence['Ability']:

    def hellfire_pawn_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        this.PutIntoPlay(player, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            hellfire_pawn_boost
        ),
    ]

