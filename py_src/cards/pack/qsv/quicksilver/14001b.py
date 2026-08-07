from . import *

# * Pietro Maximoff

def GetAbilities() -> Sequence['Ability']:

    def pietro_maximoff(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        value = 2
        ally = Worlds.FindCardOnField(
            effect,
            name="Wanda Maximoff",
        )
        if ally:
            value = 3
        initiator.DrawUp(value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            pietro_maximoff
        ).SetCostFunc(CostFunc.Discard("YourHandCards", range=(2, 2)))
        .LimitOncePerRound()
        .SetName("Superpowered Siblings")
    ]

