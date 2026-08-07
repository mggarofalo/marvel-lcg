from . import *

# The Widow's Web

def GetAbilities() -> Sequence['Ability']:

    def the_widows_web(effect: 'Effect') -> int:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        villain = Worlds.FindVillain(effect, name="Black Widow")
        if villain:
            return villain.printed_stage * Worlds.GetPlayerNumIcon(effect)
        else:
            return 0


    return [
        AbilityFactory.WhenCalcThisSchemeEscalation(
            the_widows_web
        ),
    ]

