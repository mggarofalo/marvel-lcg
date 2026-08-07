from . import *

# Meditation

def GetAbilities() -> Sequence['Ability']:

    def meditation(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.PlayCardsLikeInTurn(effect.targets, effect, update_resources_cost=-3)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            meditation
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Exhaust("YourAlterEgo"))
        .SetTarget(canbe_play=True, from_where=["YourHandCards"]),
    ]

