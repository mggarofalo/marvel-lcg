from . import *

# * 616 Hickory Branch Lane

def GetAbilities() -> Sequence['Ability']:

    def hickory_branch_lane(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally,
            trait="ANDROID",
        )
        if face:
            initiator.GainCard(face, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            hickory_branch_lane
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

