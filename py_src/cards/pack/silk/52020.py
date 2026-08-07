from . import *

# Stun Gun

def GetAbilities() -> Sequence['Ability']:

    def stun_gun(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            stun_gun
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget(Minion, canbe_stunned=True),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            stun_gun
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 2, 'charge'))
        .SetTarget(Villain, canbe_stunned=True),
    ]

