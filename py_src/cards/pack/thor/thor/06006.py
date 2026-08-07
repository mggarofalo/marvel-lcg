from . import *

# Lightning Strike

def GetAbilities() -> Sequence['Ability']:

    def lightning_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()

        res = effect.cost_func.Get(CostFunc.Spend).return_res
        value = res.GetColor("Y")

        identity = initiator.GetIdentity()

        ignore_tough = False
        if identity.HasTrait("AERIAL"):
            ignore_tough = True

        this.DealDamage(effect.targets, DamageProperty(value, ignore_tough=ignore_tough), effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            lightning_strike
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.Spend(Cost("Y")))
        .SetTarget("VillainAndYouEngagedMinion"),
    ]

