from . import *

# Mutant Insurrection

def GetAbilities() -> Sequence['Ability']:

    def mutant_insurrection_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            card_type=Unit2,
            trait="MUTANT LIBERATION FRONT",
        )

        placed = 0

        if faces:
            value = len(faces) * 2
            placed = this.PlaceThreatOnSchemes([this], value, effect)

        if not placed:
            ThisCardGainSurge(effect)

    return [
        *AbilityFactory.GiveKeywordToInPlayWhenApplyThis(
            Minion,
            toughness=1,
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            mutant_insurrection_revealed
        ),
    ]

