from . import *

# Crime State

def GetAbilities() -> Sequence['Ability']:

    def crime_state_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        AsideModularSet.ChooseRandom(effect, do_shuffle=True)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            scheme.PlaceAccelerationToken(1, effect)

        EachPlayerMustResolveFoulPlayAbility(effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            crime_state_revealed
        ),
    ]

