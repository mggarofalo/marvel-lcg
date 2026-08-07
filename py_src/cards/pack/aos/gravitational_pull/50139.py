from . import *

# * Moonstone

def GetAbilities() -> Sequence['Ability']:

    def moonstone(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            moonstone
        ),
    ]

