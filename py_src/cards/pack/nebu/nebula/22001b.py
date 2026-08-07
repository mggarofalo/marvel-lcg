from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(AlterEgo)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.Response,
            "You",
            CardFinder2("TECHNIQUE", Upgrade),
            nebula
        ).SetName("Cybernetic Upgrades")
        .LimitOncePerRound(),
    ]

