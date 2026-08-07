from . import *

# * T'Challa

def GetAbilities() -> Sequence['Ability']:

    def t_challa(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()

        face = Search.PlayerCard(
            effect,
            initiator,
            include_player_deck=True,
            trait="BLACK PANTHER",
            card_type=Upgrade,
        )
        if face:
            initiator.GainCard(face, effect)

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            t_challa
        ).SetName("Foresight")
    ]

