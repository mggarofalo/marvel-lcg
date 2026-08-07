from . import *

# * Goldballs: Fabio Medina

def GetAbilities() -> Sequence['Ability']:

    def goldballs(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        value = len(effect.cost_func.Get(CostFunc.DiscardDeckTopCards).return_discarded_cards)
        message.GainATKForThisAttack(value, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "This",
            goldballs
        ).SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", (1, 3))),
    ]

