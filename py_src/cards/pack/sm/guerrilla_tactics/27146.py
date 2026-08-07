from . import *

# From Every Direction

def GetAbilities() -> Sequence['Ability']:

    def from_every_direction_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        value = len(Worlds.GetOnFieldEnemies(effect))
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], value, effect)


    return [
        AbilityFactory.IfExpertModeThisGainKeyword(
            surge=1
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            from_every_direction_revealed
        ),
    ]

