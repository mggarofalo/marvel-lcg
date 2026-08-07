from . import *

# * Klaw

def GetAbilities() -> Sequence['Ability']:

    def klaw(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        message.GiveAdditionalBoostCardForThisActivation(1, effect)

    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.ForcedInterrupt,
            "This",
            klaw
        ),
    ]

