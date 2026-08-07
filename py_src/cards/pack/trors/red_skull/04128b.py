from . import *

# The Rise of the Red Skull

def GetAbilities() -> Sequence['Ability']:

    def the_rise_of_the_red_skull(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        RevealSideSchemeDeckTopCard(effect)


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            the_rise_of_the_red_skull
        )
    ]

