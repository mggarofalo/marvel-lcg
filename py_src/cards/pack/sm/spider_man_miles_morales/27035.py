from . import *

# * Ganke Lee

def GetAbilities() -> Sequence['Ability']:

    def ganke_lee(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)
        if initiator.IsInHeroForm():
            initiator.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            ganke_lee
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

