from . import *

# Lethal Intent

def GetAbilities() -> Sequence['Ability']:

    def lethal_intent_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
            trait="TECHNIQUE"
        )
        if face:
            face.Reveal(player, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            lethal_intent_revealed
        ),
    ]

