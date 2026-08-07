from . import *

# Press Conference

def GetAbilities() -> Sequence['Ability']:

    def press_conference(effect: 'Effect', message: 'Message.AfterPhaseEnd') -> None:
        this = effect.this.CastTo(Obligation)
        player = this.GetGaveToPlayer()

        faces = player.GetControlCardsByType(support=True)
        Faces.RemoveCountersOn(faces, 1, 'all-purpose', effect)


    return [
        AbilityFactory.AfterPhaseEnd(
            AbilityType.ForcedResponse,
            "Player",
            press_conference
        ),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Exhaust("YourIdentity")),
    ]

