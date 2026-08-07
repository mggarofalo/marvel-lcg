from . import *

# * Captain Marvel

def GetAbilities() -> Sequence['Ability']:

    def captain_marvel(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        Players.DrawUp(effect.targets, 1, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            captain_marvel,
        ).SetCost(Cost("Y"))
        .SetCostFunc(CostFunc.Heal(1, "This"))
        .SetTarget("Initiator")
        .SetName("Rechannel")
        .LimitOncePerRound()
    ]

