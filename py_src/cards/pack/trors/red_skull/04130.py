from . import *

# * The Sleeper

def GetAbilities() -> Sequence['Ability']:

    def the_sleeper_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)
        this.PutIntoPlay(first_player, effect)


    def the_sleeper_defeated(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        Faces.RemoveAllFromGame([this], effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_sleeper_revealed
        ),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            the_sleeper_defeated
        ),
    ]

