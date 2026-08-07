from . import *

# Iron Will

def GetAbilities() -> Sequence['Ability']:

    def iron_will(effect: 'Effect', message: 'Message.AfterStatusDiscardFrom') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:
            player = target.GetControlByPlayer()
            player.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Colossus"),
            thwart=1,
        ),
        AbilityFactory.AfterStatusDiscardFrom(
            AbilityType.HeroResponse,
            CardFinder(name="Colossus"),
            "Tough",
            iron_will
        ).SetTarget("Initiator"),
    ]

