from . import *

# Wheel of Genres

def GetAbilities() -> Sequence['Ability']:

    def wheel_of_genres(effect: 'Effect', message: 'Message.AfterDeckReset') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        if AsideModularSet.GetSize(effect) == 0:
            Worlds.SetGameOver(False, effect)
            return

        this.card.Flip(effect)


    return [
        AbilityFactory.AfterDeckReset(
            AbilityType.ForcedResponse,
            lambda effect: Worlds.GetEncounterDeck(effect),
            wheel_of_genres
        ),
    ]

