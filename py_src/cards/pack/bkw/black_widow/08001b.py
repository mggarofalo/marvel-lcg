from . import *

# * Natasha Romanoff

def GetAbilities() -> Sequence['Ability']:

    def natasha_romanoff(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("PREPARATION"),
            natasha_romanoff
        ).SetName("Mission Prep")
        .LimitOncePerPhase(),
    ]

