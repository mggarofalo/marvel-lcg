from . import *

# Reactor Core

def GetAbilities() -> Sequence['Ability']:

    def reactor_core(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        Worlds.UpdateNextCardPlayCost(
            initiator,
            -1,
            effect,
            finder=CardFinder(card_type=Event),
            in_this="Turn"
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            reactor_core
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck",
            lambda effect:
                1 if effect.GetInitiator().IsControl(CardFinder(name="Milano")) else 2
        )),
    ]

