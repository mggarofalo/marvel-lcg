from . import *

# Contaminant Immunity

def GetAbilities() -> Sequence['Ability']:

    def contaminant_immunity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.HealthUnits(effect.targets, 3, effect)
        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            contaminant_immunity
        ).SetPlay().SetLabel()
        .SetTarget(
            CardFinder(name="Spider-Woman", canbe_heal=True)|
            CardFinder(name="Spider-Woman", canbe_tough=True)
        ),
    ]

