from . import *

# Alien Physiology

def GetAbilities() -> Sequence['Ability']:

    def alien_physiology(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        if message.paying_for_card and \
            message.paying_for_card.IsClass("IdentitySpecific"):
            return Resources("GG")
        else:
            return Resources("G")


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            resources_fn=alien_physiology
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

