from . import *

# Emergency Teleporter

def GetAbilities() -> Sequence['Ability']:

    def emergency_teleporter(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Ally,
        )
        if face:
            face.PutIntoPlay(initiator, effect)
            Faces.GiveStatus([face], "Tough", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            emergency_teleporter
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetCostFunc(CostFunc.RemoveFromCampaignLog("This")),
    ]

