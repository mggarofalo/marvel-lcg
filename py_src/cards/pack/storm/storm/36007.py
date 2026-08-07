from . import *

# * Storm's Cape

def GetAbilities() -> Sequence['Ability']:

    def storms_cape(effect: 'Effect', message: 'Message.AfterEffectResolved') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Storm"),
            defense=1,
            trait="AERIAL",
        ),
        AbilityFactory.AfterPlayerResolveAbility(
            AbilityType.HeroResponse,
            "You",
            CardFinder2("WEATHER", Support),
            storms_cape,
            which_abilities=["Special"],
            control_by_you=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourHero", canbe_ready=True),
    ]

