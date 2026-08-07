from . import *

# Upgraded Chassis

def GetAbilities() -> Sequence['Ability']:

    def upgraded_chassis(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="War Machine"),
            trait="AERIAL",
        ),
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.HeroResponse,
            "You",
            upgraded_chassis,
            to_form=Hero,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(name="War Machine"),
    ]

