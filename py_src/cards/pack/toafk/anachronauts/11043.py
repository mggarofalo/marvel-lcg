from . import *

# * Terminatrix

def GetAbilities() -> Sequence['Ability']:

    def terminatrix_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveBoostCardForThisActivation(Enemy, 2, effect)


    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            terminatrix_boost
        ),
    ]

