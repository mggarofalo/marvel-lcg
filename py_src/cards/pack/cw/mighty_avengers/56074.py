from . import *

# * Wasp

def GetAbilities() -> Sequence['Ability']:

    def wasp(effect: 'Effect', message: 'Message.WhenCardRevealed|Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wasp
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            wasp
        ),
    ]

