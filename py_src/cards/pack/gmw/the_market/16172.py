from . import *

# Navigation Column

def GetAbilities() -> Sequence['Ability']:

    def navigation_column(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

    def navigation_column_cost(targets: Sequence['CardFace'], effect: 'Effect') -> bool:
        initiator = effect.GetInitiator()

        if initiator.IsControl(CardFinder(name="Milano")):
            initiator.DiscardDeckTopCards(1, effect)
            return True
        else:
            return initiator.DiscardHandCards((1, 1), effect) != []

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            navigation_column
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Custom(None, navigation_column_cost)),
    ]

