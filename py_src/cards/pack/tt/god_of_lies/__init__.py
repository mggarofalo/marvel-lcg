from cards.pack import *

def PlaceShatterCountersOnTheAvatarOfLokivillain(size: int):
    ...

def WhenDefeatedPlaceShatterCountersOnTheAvatarOfLokivillain(size: int, action: Callable[['Effect', 'Message.WhenUnitBeDefeated'], None]|None=None) -> 'Ability':
    def grendell_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        PlaceShatterCountersOnTheAvatarOfLokivillain(size)

        if action:
            action(effect, message)

    return AbilityFactory.WhenUnitBeDefeated(
        AbilityType.WhenDefeated,
        "This",
        grendell_defeated
    )

