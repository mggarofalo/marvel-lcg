from . import *

# * Orion

def GetAbilities() -> Sequence['Ability']:

    def orion(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)

    return [
        AbilityFactory.ThisCanHaveAdditionalTough("Any"),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.ForcedResponse,
            "This",
            orion
        ),
    ]

