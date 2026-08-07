from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            traits=["ATTACK", "THWART"],
            card_type=Event
        )
        if face:
            Faces.AddToHand([face], initiator, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            nebula
        ),
    ]

