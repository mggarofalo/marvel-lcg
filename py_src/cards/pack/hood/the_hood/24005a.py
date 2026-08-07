from . import *

# Promised Prosperity

def GetAbilities() -> Sequence['Ability']:

    def promised_prosperity_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        AsideModularSet.ChooseRandom(effect, do_shuffle=True)
        scheme = Worlds.FindMainScheme(this)
        if scheme:
            scheme.PlaceAccelerationToken(1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            promised_prosperity_revealed
        ),
    ]

