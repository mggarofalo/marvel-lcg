from . import *

# * Kaluu

def GetAbilities() -> Sequence['Ability']:

    def kaluu(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerDeckTop(
            effect,
            initiator,
            5,
            card_type=Event,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            kaluu
        ),
    ]

