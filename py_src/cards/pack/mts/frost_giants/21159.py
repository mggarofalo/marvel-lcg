from . import *

# Unnatural Storm

def GetAbilities() -> Sequence['Ability']:

    def unnatural_storm_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.ExhaustAll(Worlds.GetOnFieldAllies(effect), effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            unnatural_storm_revealed
        ),
        *AbilityFactory.CardCannotReady(
            CardFinder(card_type=Hero|Ally),
            by_effect_face=CardFinder(card_type=PlayerCard)
        ),
    ]

