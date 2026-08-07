from . import *

# Air Cover

def GetAbilities() -> Sequence['Ability']:

    def air_cover(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            trait="TACTIC",
            card_type=Upgrade,
            canbe_attach_to=message.trigger
        )

        if face:
            Faces.AddToHand([face], initiator, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            Minion,
            air_cover
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'fuel')),
    ]

