from . import *

# * Angel

def GetAbilities() -> Sequence['Ability']:

    def angel(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Hero)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("AERIAL", Event),
            angel
        ).SetName("Angel of Life")
        .LimitOncePerPhase(),
    ]

