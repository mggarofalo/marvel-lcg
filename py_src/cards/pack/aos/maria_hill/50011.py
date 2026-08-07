from . import *

# * S.H.I.E.L.D. Director

def GetAbilities() -> Sequence['Ability']:

    def shield_director(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 1, 'all-purpose', effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            shield_director
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Support, trait="S.H.I.E.L.D"),
    ]

