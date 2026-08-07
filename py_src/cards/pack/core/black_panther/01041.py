from . import *

# * Shuri

def GetAbilities() -> Sequence['Ability']:

    def shuri(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            shuri
        )
        # .SetDesc("search your deck for an upgrade and add it to your hand")
    ]

