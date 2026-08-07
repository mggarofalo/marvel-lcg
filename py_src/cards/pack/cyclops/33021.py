from . import *

# * Danger Room

def GetAbilities() -> Sequence['Ability']:

    def danger_room(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            card_type=Upgrade,
            trait="TRAINING",
        )
        if face:
            face.AttachTo2(message.trigger, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.AlterEgoResponse,
            CardFinder2("X-MEN", Ally),
            danger_room,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .AnyPlayerCanDoThis(),
    ]

