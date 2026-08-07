from . import *

# Fluid Motion

def GetAbilities() -> Sequence['Ability']:

    def fluid_motion(effect: 'Effect', message: 'Message.AfterPlayerPlayedCard') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()
        hero.GainUntilPhaseEnd(
            effect,
            attack=1
        )


    return [
        AbilityFactory.AfterPlayerPlayedCard(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("ATTACK", Event),
            fluid_motion,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHero")
        .LimitOncePerEvent(),
    ]

