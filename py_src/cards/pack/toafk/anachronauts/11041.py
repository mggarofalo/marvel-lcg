from . import *

# * Deathunt 9000

def GetAbilities() -> Sequence['Ability']:

    def deathunt_9000_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([message.activating_enemy], "Tough", effect)
        message.GiveBoostCardForThisActivation(Enemy, 1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            deathunt_9000_boost
        ),
    ]

