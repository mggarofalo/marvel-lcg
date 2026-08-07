from . import *

# Surveillance Team

def GetAbilities() -> Sequence['Ability']:

    def surveillance_team(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            surveillance_team
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'snoop'))
        .SetTarget(Scheme2)
        # .SetDesc("Exhaust Surveillance Team and remove 1 snoop counter from it → remove 1 threat from a scheme")
    ]

