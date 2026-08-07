from . import *

# * Groot

def GetAbilities() -> Sequence['Ability']:

    def groot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        Faces.PlaceCountersOn(effect.targets, 2, 'growth', effect, maximum=10)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            groot
        ).SetName("Growth Spurt")
        .SetTarget(name="Groot")
        .LimitOncePerRound(),
    ]

