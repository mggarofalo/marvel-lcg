from . import *

# Smoke Bombs

def GetAbilities() -> Sequence['Ability']:

    def smoke_bombs(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Attachment)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardHandCards((1, 1), effect, card_type=Event, lowest_cost=True)


    return [
        AbilityFactory.AttachToFaceWhenPutIntoPlay(
            Villain
        ),
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.ForcedResponse,
            Villain,
            "You",
            smoke_bombs
        ).SetCostFunc(CostFunc.Counter("This", 1, 'bomb'))
    ]

