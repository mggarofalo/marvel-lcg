from . import *

# Aggressive Stance

def GetAbilities() -> Sequence['Ability']:

    def aggressive_stance(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="ATTACK",
            card_type=Event,
        )
        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.AfterMinionEngagePlayer(
            AbilityType.HeroResponse,
            None,
            "You",
            aggressive_stance
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

