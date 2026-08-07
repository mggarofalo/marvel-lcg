from . import *

# * Cat's Head Amulet

def GetAbilities() -> Sequence['Ability']:

    def cats_head_amulet(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        size = len(initiator.GetEngagedMinions())
        size = min(size, 3)
        return Resources.FromText("R"*size)


    return [
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            resources_fn=cats_head_amulet
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

