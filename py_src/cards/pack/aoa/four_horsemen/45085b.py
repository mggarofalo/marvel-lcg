from . import *

# The Horsemen of Apocalypse

def GetAbilities() -> Sequence['Ability']:

    def the_horsemen_of_apocalypse(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = GetNextVillain(effect, message.trigger.CastTo(Villain))

        villain.SetActive(effect)

    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            Villain,
            the_horsemen_of_apocalypse
        )
    ]

