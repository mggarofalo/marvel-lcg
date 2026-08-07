from . import *

# * Patriot: Elijah Bradley

def GetAbilities() -> Sequence['Ability']:

    def patriot(effect: 'Effect', message: 'Message.AfterMinionEngagePlayer') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.GiveStatus([this], "Tough", effect)


    return [
        AbilityFactory.AfterPlayerEngageMinion(
            AbilityType.Response,
            "You",
            patriot
        ).SetTarget("This", canbe_tough=True),
    ]

