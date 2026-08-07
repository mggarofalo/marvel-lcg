from . import *

# * Wanda Maximoff

def GetAbilities() -> Sequence['Ability']:

    def wanda_maximoff(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        num = 2
        pietro_maximoff = Worlds.FindCardOnField(
            effect,
            name="Pietro Maximoff"
        )
        if pietro_maximoff:
            num = 3
        initiator.DrawUp(num, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            wanda_maximoff
        ).SetCostFunc(CostFunc.Discard("YourHandCards", range=(2, 2)))
        .SetName("Superpowered Siblings")
        .LimitOncePerRound(),
    ]

