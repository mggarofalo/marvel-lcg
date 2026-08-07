from . import *

# Grow Invulnerable

def GetAbilities() -> Sequence['Ability']:

    def grow_invulnerable_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        face = Worlds.FindCardOnField(effect, name="Atlas", card_type=Enemy)
        if face:
            value = face.GetCounters('growth')
            this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            grow_invulnerable_revealed
        ),
        AbilityFactory.IfCardHasOrMoreCounters(
            CardFinder(name="Atlas"),
            10,
            'growth',
            lambda effect, message:
                effect.world.SetGameOver(False, effect)
        ),
    ]

