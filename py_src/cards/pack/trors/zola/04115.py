from . import *

# Zola's Mutate

def GetAbilities() -> Sequence['Ability']:

    def zolas_mutate(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        face = Worlds.DiscardEncounterCardsUntil(
            effect,
            card_type=Attachment,
            trait="TECH",
        )
        if face:
            face.AttachTo2(this, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            zolas_mutate
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            ShuffleThisCardIntoEncounter
        )
    ]

