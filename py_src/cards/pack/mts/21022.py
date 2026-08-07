from . import *

# Ready to Rumble

def GetAbilities() -> Sequence['Ability']:

    def ready_to_rumble(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
            "Players"
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            ready_to_rumble
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourHero", canbe_ready=True),
    ]

