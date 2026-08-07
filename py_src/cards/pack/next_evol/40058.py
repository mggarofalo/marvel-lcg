from . import *

# The Posse

def GetAbilities() -> Sequence['Ability']:

    def the_posse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 1, effect)
        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            the_posse
        ).SetPlay(only_if_you_control_3_character_with_trait="POSSE").SetLabel()
        .SetTarget(Unit2, trait="POSSE", range="All"),
    ]

