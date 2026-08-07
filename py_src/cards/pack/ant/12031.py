from . import *

# Lay Down the Law

def GetAbilities() -> Sequence['Ability']:

    def lay_down_the_law(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        num = 3
        if effect.GetPaidResources().GetColor("B"):
            num = 4
        this.RemoveThreatFromSchemes(effect.targets, num, effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            lay_down_the_law
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2),
    ]

