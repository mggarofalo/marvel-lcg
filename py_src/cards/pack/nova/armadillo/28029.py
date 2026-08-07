from . import *

# * Armadillo

def GetAbilities() -> Sequence['Ability']:

    def armadillo(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.ThisCanHaveAdditionalTough(
            "Any",
        ),
        AbilityFactory.AfterEnemyActivatesAgainstYou(
            AbilityType.ForcedResponse,
            "This",
            armadillo,
        ),
    ]

