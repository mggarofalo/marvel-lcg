from . import *

# Conditioning Room

def GetAbilities() -> Sequence['Ability']:

    def conditioning_room(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)
        this.HealthUnits(effect.targets2, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            conditioning_room
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Event, traits=["ATTACK", "THWART"], from_where=["YourDiscardPileBottommost"])
        .SetTarget2(name="Gamora", canbe_heal=True),
    ]

