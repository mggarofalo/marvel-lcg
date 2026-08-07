from . import *

# * Surveillance Specialist

def GetAbilities() -> Sequence['Ability']:

    def surveillance_specialist(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        ),
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            surveillance_specialist,
            powers=["THW"],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

