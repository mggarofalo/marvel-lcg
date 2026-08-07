from . import *

# * Black Widow

def GetAbilities() -> Sequence['Ability']:

    def black_widow(effect: 'Effect', message: 'Message.AfterUnitSchemeEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        face = Search.EncounterCard(
            effect,
            player,
            include_discard_pile=True,
            name="Handspring",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(this, effect)
        elif player:
            identity = player.GetIdentity()
            Faces.GiveStatus([identity], "Confused", effect)


    return [
        AbilityFactory.AfterUnitSchemeAgainst(
            AbilityType.ForcedResponse,
            "This",
            "You",
            black_widow
        ),
    ]

