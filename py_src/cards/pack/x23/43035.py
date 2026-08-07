from . import *

# * Defense Specialist

def GetAbilities() -> Sequence['Ability']:

    def defense_specialist(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=1,
        ),
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            defense_specialist,
            powers=["DEF"],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

