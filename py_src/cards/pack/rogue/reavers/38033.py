from . import *

# * Murray Reese

def GetAbilities() -> Sequence['Ability']:

    def murray_reese_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
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
            murray_reese_revealed
        ),
    ]

