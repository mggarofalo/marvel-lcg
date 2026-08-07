from . import *

# Culling the Weak

def GetAbilities() -> Sequence['Ability']:

    def culling_the_weak_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 4, effect)

    def culling_the_weak_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = GetGenePool(effect)
        if scheme:
            this.PlaceThreatOnSchemes([scheme], 2, effect)



    return [
        AbilityFactory.WhenThisRevealed(
            None,
            culling_the_weak_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            culling_the_weak_boost
        ),
    ]

