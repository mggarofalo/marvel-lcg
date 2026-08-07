from . import *

# * Rocket Raccoon

def GetAbilities() -> Sequence['Ability']:

    def rocket_raccoon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            rocket_raccoon
        ).SetCostFunc(CostFunc.Discard(
            card_type=Upgrade,
            trait="TECH",
            from_where=["YouControlCards"]
        ))
        .SetName("Tinkering")
        .LimitOncePerRound(),
    ]

