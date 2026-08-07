from . import *

# Medical Emergency

def GetAbilities() -> Sequence['Ability']:

    def medical_emergency(effect: 'Effect', message: 'Message.WhenPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        if player.IsHero():
            player.GetIdentity().TakeDamage(this, 1, effect)


    return [
        AbilityFactory.WhenPlayerTurnEnd(
            AbilityType.ForcedResponse,
            "You",
            medical_emergency
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCost(Cost("R"))
        .SetCostFunc(CostFunc.DiscardDeckTopCards("YourDeck", 5)),
    ]

