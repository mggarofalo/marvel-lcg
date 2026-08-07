from . import *

# Titanium Muscles

def GetAbilities() -> Sequence['Ability']:

    def titanium_muscles(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost'):
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        return Resources("R") * initiator.GetIdentity().tough


    return [
        *AbilityFactory.GiveKeywordToAttached(
            CardFinder(name="Colossus"),
            attack=1,
        ),
        AbilityFactory.CanGenerateResources(
            AbilityType.HeroResource,
            resources_fn=titanium_muscles,
            # condition=lambda effect, message:
            #     # effect.GetInitiator().GetIdentity().IsTough(),
            #     True,
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

