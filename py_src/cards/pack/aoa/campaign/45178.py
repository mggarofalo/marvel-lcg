from . import *

# Panicked Refugees

def GetAbilities() -> Sequence['Ability']:

    def panicked_refugees(effect: 'Effect', message: 'Message.AfterCardEnterHand') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        this.Reveal(player, effect)
        player.DrawUp(1, effect)


    return [
        AbilityFactory.AfterCardEnterHand(
            AbilityType.ForcedResponse,
            "This",
            panicked_refugees
        ),
        AbilityFactory.PlayerActionToRemoveThisFromGame(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

