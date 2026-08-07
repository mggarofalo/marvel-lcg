from . import *

# * Topaz

def GetAbilities() -> Sequence['Ability']:

    def topaz_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            include_set_aside=True,
            name="Superpower Feedback",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(identity, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            topaz_revealed
        ),
    ]

