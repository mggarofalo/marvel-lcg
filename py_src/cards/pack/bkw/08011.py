from . import *

# * Agent Coulson

def GetAbilities() -> Sequence['Ability']:

    def agent_coulson(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            trait="PREPARATION",
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            agent_coulson
        ),
    ]

