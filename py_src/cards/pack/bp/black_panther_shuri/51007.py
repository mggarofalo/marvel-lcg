from . import *

# * The Elephant's Trunk

def GetAbilities() -> Sequence['Ability']:

    def the_elephants_trunk(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        size = 1 + len(effect.cost_func.Get(CostFunc.Exhaust, index=1).return_exhausted_cards)
        initiator.DrawUp(size, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            the_elephants_trunk
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Support|Ally,
            trait="WAKANDA",
            from_where=["YouControlCards"],
            range=(0, 2)
        )),
    ]

