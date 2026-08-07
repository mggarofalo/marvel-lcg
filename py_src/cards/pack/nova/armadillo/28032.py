from . import *

# Tough It Out

def GetAbilities() -> Sequence['Ability']:

    def tough_it_out_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        units: List[CardFace] = []
        villain = Worlds.FindVillain(effect)
        if villain:
            units.append(villain)
        units += Worlds.FindCardsOnField(
            effect,
            name="Armadillo",
        )
        if Faces.GiveStatus(units, "Tough", effect) <= 1:
            ThisCardGainSurge(effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            tough_it_out_revealed
        ),
    ]

