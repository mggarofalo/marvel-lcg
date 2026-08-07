from . import *

# By Any Means

def GetAbilities() -> Sequence['Ability']:

    def by_any_means_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = 1 * Worlds.GetPlayerNumIcon(effect) * len(GetVillainsUnderRouted(effect))
        this.PlaceThreatOnSchemes([this], value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            by_any_means_revealed
        ),
    ]

