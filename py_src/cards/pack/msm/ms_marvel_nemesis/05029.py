from . import *

# Harvest

def GetAbilities() -> Sequence['Ability']:

    def harvest_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            card_type=Support,
            trait="PERSONA"
        )
        faces = Faces.ExhaustAll(faces, effect)
        value = len(faces)

        if value > 0:
            villain = Worlds.FindVillain(effect)
            if villain:
                villain.HealHealth(value, effect)
        else:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            harvest_revealed
        ),
    ]

