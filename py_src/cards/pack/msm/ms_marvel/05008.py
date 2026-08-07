from . import *

# * Nakia Bahadir

def GetAbilities() -> Sequence['Ability']:

    def nakia_bahadir(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Worlds.UpdateNextCardPlayCost(
            initiator,
            -1,
            effect,
            in_this="Phase"
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            nakia_bahadir
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

