from . import *

# Earth's Mightiest Heroes

def GetAbilities() -> Sequence['Ability']:

    def earths_mightiest_heroes(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            earths_mightiest_heroes,
        ).SetPlay()
        .SetTarget("YouControlUnit", trait="AVENGER", canbe_ready=True)
        .SetCostFunc(CostFunc.Exhaust("YouControlUnit", trait="AVENGER"))
    ]

