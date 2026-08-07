from . import *

# Power Within

def GetAbilities() -> Sequence['Ability']:

    def power_within(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GetIdentity().ResolveSpecialAbility(initiator, effect, name="Venom Blast")


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            "YourHero",
            power_within
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

