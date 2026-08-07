from . import *

# Deft Focus

def GetAbilities() -> Sequence['Ability']:

    def deft_focus(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Worlds.UpdateNextCardPlayCost(
            initiator,
            -1,
            effect,
            finder=CardFinder2("SUPERPOWER"),
            in_this="Turn"
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            deft_focus
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

