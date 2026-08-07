from . import *

# * Cindy Moon

def GetAbilities() -> Sequence['Ability']:

    def cindy_moon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)

    return [
        IfThereAreMoreThan4TuckedCardsHere(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            cindy_moon
        ).SetCostFunc(CostFunc.Discard("TuckHereCard"))
        .LimitOncePerRound(),
    ]

