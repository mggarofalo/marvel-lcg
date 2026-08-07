from . import *

# Focused Rage

def GetAbilities() -> Sequence['Ability']:

    def focused_rage(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            focused_rage
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeDamage(1, "Initiator")),
    ]

