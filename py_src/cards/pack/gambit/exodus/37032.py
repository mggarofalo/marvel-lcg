from . import *

# * Exodus

def GetAbilities() -> Sequence['Ability']:

    def exodus_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            card_type=Attachment,
            name="Psionic Shield"
        )
        if face:
            face.AttachTo2(this, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            exodus_revealed
        ),
    ]

