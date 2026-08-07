from . import *

# * Fantomex: Charlie Cluster-7

def GetAbilities() -> Sequence['Ability']:

    def fantomex(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            include_discard_pile=True,
            name="E.V.A.",
            card_type=Support
        )
        if face:
            face.PutIntoPlay(initiator, effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            fantomex
        ),
    ]

