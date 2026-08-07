from . import *

# * Wade Cole

def GetAbilities() -> Sequence['Ability']:

    def wade_cole_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Search.EncounterCard(
            effect,
            include_discard_pile=True,
            name="Cybernetic Enhancements",
            card_type=Attachment
        )
        if face:
            face.AttachTo2(this, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wade_cole_revealed
        ),
    ]

