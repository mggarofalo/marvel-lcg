from . import *

# * White Queen: Emma Frost

def GetAbilities() -> Sequence['Ability']:

    def white_queen(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.DiscardAll(effect.targets, effect)

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_your_identity_has_one_of_traits=["X-FORCE", "X-MEN"]),
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.Response,
            "This",
            white_queen
        ).SetTarget(StatusCard),
    ]

