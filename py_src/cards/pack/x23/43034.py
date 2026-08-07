from . import *

# * Combat Specialist

def GetAbilities() -> Sequence['Ability']:

    def combat_specialist(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            combat_specialist,
            powers=["ATK"],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

