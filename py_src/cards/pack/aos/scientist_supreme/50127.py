from . import *

# Diplomatic Immunity

def GetAbilities() -> Sequence['Ability']:

    def diplomatic_immunity_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Worlds.VictoryDisplay(effect).FindCardSize(CardFinder2("A.I.M", Minion))
        this.PlaceAccelerationToken(value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            diplomatic_immunity_revealed
        ),
    ]

