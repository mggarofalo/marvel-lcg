from . import *

# Spider Bites

def GetAbilities() -> Sequence['Ability']:

    def spider_bites(effect: 'Effect', message: 'Message.WhenResolveSpecialAbility') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        this.DealDamage(effect.targets, 1, effect)

        initiator.MayChooseOneAbility(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Discard this card to stun each of those enemies",
                lambda targets:
                    Faces.GiveStatus(targets, "Stunned", effect)
            ).SetCostFunc(CostFunc.Discard("This"))
            .SetTarget(effect.targets, range="All", canbe_stunned=True)
        )


    return [
        AbilityFactory.WhenResolveSpecialAbility(
            "This",
            spider_bites
        ).SetTarget("VillainAndEngagedSamePlayerMinion"),
    ]

