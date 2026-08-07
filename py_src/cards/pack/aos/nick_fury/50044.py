from . import *

# * Fury's Watch

def GetAbilities() -> Sequence['Ability']:

    def furys_watch_res(effect: 'Effect', message: 'Message.WhenPlayerPayingResources') -> 'Resources':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = effect.cost_func.Get(CostFunc.RemoveTokens).return_removed_tokens
        return Resources("B") * value

    def furys_watch(effect: 'Effect', message: 'Message.CheckPlayerCanPayCost') -> 'Resources|None':
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        upgrade = initiator.form.FindSuitForm()
        if upgrade:
            return Resources("B") * min(2, upgrade.GetTokens('threat'))


    return [
        AbilityFactory.DoGenerateResources(
            AbilityType.Resource,
            "This",
            res_fn=furys_watch_res,
        ).SetCostFunc(CostFunc.RemoveTokens("YourSuitFormUpgrade", (1, 2), 'threat')),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            resources_fn=furys_watch
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

