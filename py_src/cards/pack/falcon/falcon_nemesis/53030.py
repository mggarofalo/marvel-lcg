from . import *

# * Viper

def GetAbilities() -> Sequence['Ability']:

    def viper(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Worlds.DiscardEncounterCards(5, effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            viper
        ),
    ]

