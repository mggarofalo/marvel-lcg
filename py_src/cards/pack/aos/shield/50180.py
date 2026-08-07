from . import *

# Disavowed

def GetAbilities() -> Sequence['Ability']:

    def disavowed_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Worlds.FindCardSizeOnField(effect, trait="S.H.I.E.L.D")
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.IncreaseResourceCostToPlay(
            CardFinder2("S.H.I.E.L.D"),
            +1,
            "AnyPlayer",
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            disavowed_revealed
        ),
    ]

