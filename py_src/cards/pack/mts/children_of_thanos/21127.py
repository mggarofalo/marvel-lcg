from . import *

# * Ebony Maw

def GetAbilities() -> Sequence['Ability']:

    def ebony_maw_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveBoostCardForThisActivation(Enemy, 1, effect)


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ebony_maw_boost
        ),
    ]

