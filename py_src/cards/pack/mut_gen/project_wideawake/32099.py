from . import *

# Warn the Others

def GetAbilities() -> Sequence['Ability']:

    def warn_the_others(effect: 'Effect', message: 'Message.AfterPlayerTurnEnd') -> None:
        this = effect.this.CastTo(Obligation)

        PlaceCardFacedownUnderOperationZeroTolerance(this, effect)


    return [
        AbilityFactory.AfterPlayerTurnEnd(
            AbilityType.ForcedResponse,
            "AttachedPlayer",
            warn_the_others
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity"))
    ]

