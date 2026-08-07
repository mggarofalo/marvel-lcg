from . import *

# Drawing Near

def GetAbilities() -> Sequence['Ability']:

    def drawing_near(effect: 'Effect', message: 'Message.AfterPlayerTurnBegin') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()
        face = player.DiscardDeckTopCard(effect)
        value = FacesCounter.GetPrintedResourcesIcon([face], player.player_deck)

        env = GetPursuedByThePast(effect)
        if env:
            Faces.PlaceCountersOn([env], value, 'pursuit', effect)

    return [
        AbilityFactory.AfterPlayerTurnBegin(
            AbilityType.ForcedResponse,
            "AttachedPlayer",
            drawing_near
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            card_class="IdentitySpecific",
        ))
    ]

